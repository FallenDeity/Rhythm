using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Rhythm.Contracts.Services;
using Rhythm.Core.Models;
using Rhythm.Views;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Controls;

public class TrackQueue : IOracleCustomType
{
    private string[]? _array;

    [OracleArrayMapping()]
    public string[] Array
    {
        get => _array ?? new string[0];
        set => _array = value;
    }

    public void FromCustomObject(OracleConnection con, object udt)
    {
        OracleUdt.SetValue(con, udt, "TRACKS_QUEUE_TABLE", _array);
    }
    public void ToCustomObject(OracleConnection con, object udt)
    {
        _array = (string[])OracleUdt.GetValue(con, udt, "TRACKS_QUEUE_TABLE");
    }
}

[OracleCustomTypeMapping("MUSIC.QUEUE_TYPE")]
public class TrackQueueFactory : IOracleCustomTypeFactory, IOracleArrayTypeFactory
{
    public IOracleCustomType CreateObject()
    {
        return new TrackQueue();
    }

    public Array CreateArray(int numElems)
    {
        return new string[numElems];
    }

    public Array CreateStatusArray(int numElems)
    {
        return new OracleUdtStatus[numElems];
    }
}


public sealed partial class RhythmMediaPlayer : UserControl, INotifyPropertyChanged
{
    public static readonly MediaPlayer mediaPlayer = new();

    private DispatcherQueue? dispatcherQueue;
    private readonly DispatcherTimer? progressTimer;

    private RhythmPlayState? _playState;
    private RhythmTrack? _track;
    private RhythmAlbum? _album;
    private RhythmArtist[] _artists = [];
    private InMemoryRandomAccessStream? _trackStream;

    private string _albumId = "";
    private string _playlistId = "";
    private string _trackId = "";

    private readonly LinkedList<string> _original = new();
    private LinkedList<string> _trackQueue = new();
    private readonly Stack<string> _history = new();

    private bool _loop;
    private bool _shuffle;
    private bool _loaded;
    private bool _isPlaying;
    private bool _cacheDirty;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void SetProperty<T>(ref T field, T value, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(name);
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value, nameof(IsPlaying));
    }

    public string TrackId
    {
        get => _trackId;
        set
        {
            _trackId = value;
            _ = PlayCurrentTrack();
        }
    }

    public RhythmMediaPlayer()
    {
        this.InitializeComponent();
        mediaPlayer.Volume = 0.5;
        mediaPlayer.MediaEnded += PlaybackSession_MediaEnded;
        progressTimer = new DispatcherTimer();
        progressTimer.Interval = TimeSpan.FromMilliseconds(250);
        progressTimer.Tick += (s, e) =>
        {
            if (_track is null) return;
            var duration = Regex.Match(_track.TrackDuration, @"(\d+):(\d+):(\d+).(\d+)").Groups;
            var totalDuration = int.Parse(duration[2].Value) * 60 + int.Parse(duration[3].Value);
            if (totalDuration > 0)
            {
                var progress = mediaPlayer.PlaybackSession.Position.TotalSeconds / totalDuration;
                dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                               {
                                   TrackSeek.Value = progress * 100;
                                   TrackTime.Text = $"{mediaPlayer.PlaybackSession.Position.Minutes:00}:{mediaPlayer.PlaybackSession.Position.Seconds:00}";
                               });
            }
        };
    }

    private async Task UpdatePlayState()
    {
        var user = App.currentUser?.UserId;
        if (user is null) return;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("UPDATE play_state SET tracks_queue = :current_queue, current_track = :current_track, loop = :is_loop, shuffle = :is_shuffle, updated_at = CURRENT_TIMESTAMP WHERE user_id = :userId", conn);
        cmd.Parameters.Add(new OracleParameter("current_queue", OracleDbType.Array, ParameterDirection.Input)
        {
            UdtTypeName = "MUSIC.QUEUE_TYPE",
            Value = new TrackQueue { Array = _trackQueue.ToArray() }
        });
        cmd.Parameters.Add(new OracleParameter("current_track", _trackId));
        cmd.Parameters.Add(new OracleParameter("is_loop", _loop));
        cmd.Parameters.Add(new OracleParameter("is_shuffle", _shuffle));
        cmd.Parameters.Add(new OracleParameter("userId", user));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateStreams(string trackId)
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("UPDATE tracks SET streams = streams + 1 WHERE track_id = :trackId", conn);
        cmd.Parameters.Add(new OracleParameter("trackId", trackId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CacheDataBatch()
    {
        var queueCopy = new LinkedList<string>(_trackQueue);
        var t = await App.GetService<IDatabaseService>().GetTracks(queueCopy.ToArray());
        var albumIds = t.Select(x => x.TrackAlbumId).Distinct().ToArray();
        await App.GetService<IDatabaseService>().GetAlbums(albumIds);
        // await App.GetService<IDatabaseService>().GetAlbumCovers(albumIds);
    }

    private async Task GetPlayState()
    {
        var user = App.currentUser?.UserId;
        if (user is null) return;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT * FROM play_state WHERE user_id = :userId", conn);
        cmd.Parameters.Add(new OracleParameter("userId", user));
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            var queue = (TrackQueue)reader.GetValue(reader.GetOrdinal("TRACKS_QUEUE"));
            var current = reader.IsDBNull(reader.GetOrdinal("CURRENT_TRACK")) ? null : reader.GetString(reader.GetOrdinal("CURRENT_TRACK"));
            var loop = reader.GetBoolean(reader.GetOrdinal("LOOP"));
            var shuffle = reader.GetBoolean(reader.GetOrdinal("SHUFFLE"));
            _playState = new RhythmPlayState
            {
                UserId = user,
                TracksQueue = queue.Array.ToList(),
                CurrentTrack = current,
                Loop = loop,
                Shuffle = shuffle
            };
            _loop = loop;
            _shuffle = shuffle;
            dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                       {
                           RepeatIcon.Glyph = _loop ? "\uE8ED" : "\uE8EE";
                           VisualStateManager.GoToState(this, _loop ? "RepeatStateOn" : "RepeatStateOff", true);
                           VisualStateManager.GoToState(this, _shuffle ? "ShuffleStateOn" : "ShuffleStateOff", true);
                       });
            _trackQueue = new LinkedList<string>(_playState.TracksQueue);
            _original.Clear();
            foreach (var track in _trackQueue) _original.AddLast(track);
            if (_playState.CurrentTrack is not null) TrackId = _playState.CurrentTrack;
            _cacheDirty = true;
        }

    }

    private async Task LoadTrack()
    {
        var prevAlbum = _track?.TrackAlbumId;
        _track = await App.GetService<IDatabaseService>().GetTrack(TrackId);
        if (_track is null) return;
        dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            var page = (ShellPage)App.MainWindow.Content;
            page.RhythmPlayer.TrackTitle.Text = _track.TrackName.Length > 20 ? _track.TrackName[..20] + "..." : _track.TrackName;
            var duration = Regex.Match(_track.TrackDuration, @"(\d+):(\d+):(\d+).(\d+)").Groups;
            page.RhythmPlayer.TrackDuration.Text = $"{duration[2]}:{duration[3]}";
        });
        Thread streamThread = new(async () => await StreamTrack());
        streamThread.Priority = ThreadPriority.Highest;
        streamThread.Start();
        _ = Task.Run(() => LoadArtists());
        _ = Task.Run(() => LoadAlbum(prevAlbum));
        if (_cacheDirty)
        {
            _cacheDirty = false;
            _ = Task.Run(() => CacheDataBatch());
        }
    }

    private async Task LoadArtists()
    {
        if (_track is null) return;
        _artists = await App.GetService<IDatabaseService>().GetTrackArtists(_track.TrackId);
        dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            var page = (ShellPage)App.MainWindow.Content;
            page.RhythmPlayer.TrackArtist.Text = _artists[0].ArtistName;
        });
    }

    private async Task LoadAlbum(string? PrevAlbum)
    {
        if (_track is null) return;
        if (PrevAlbum == _track.TrackAlbumId && _album is not null) return;
        _album = await App.GetService<IDatabaseService>().GetAlbum(_track.TrackAlbumId);
        dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                       {
                           if (_album is null) return;
                           var img = _album.AlbumImageURL is not null ? new BitmapImage(new Uri(_album.AlbumImageURL)) : null;
                           var page = (ShellPage)App.MainWindow.Content;
                           page.RhythmPlayer.TrackImage.Source = img;
                       });
    }

    private async Task StreamTrack()
    {
        if (_track is null) return;
        if (_trackStream is not null)
        {
            _trackStream.Dispose();
            _trackStream = null;
        }
        if (!_track.AudioAvailable)
        {
            System.Diagnostics.Debug.WriteLine("Using preview URL");
            var url = _track.TrackAudioURL;
            dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                       {
                           var source = url is null ? null : MediaSource.CreateFromUri(new Uri(url));
                           mediaPlayer.Source = source;
                           mediaPlayer.Play();
                           _loaded = true;
                       });
            return;
        }
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var trackId = _track.TrackId;
        var cmd = new OracleCommand("SELECT track_audio FROM track_audio WHERE track_id = :trackId", conn);
        cmd.Parameters.Add(new OracleParameter("trackId", trackId));
        _trackStream = new InMemoryRandomAccessStream();
        var dataWriter = new DataWriter(_trackStream.GetOutputStreamAt(0));
        var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        if (reader.Read())
        {
            OracleBlob oracleBlob = reader.GetOracleBlob(reader.GetOrdinal("TRACK_AUDIO"));
            var bufferSize = 65536 * 2;
            var buffer = new byte[bufferSize];
            var count = 0;
            while (await oracleBlob.ReadAsync(buffer, 0, bufferSize) > 0)
            {
                if (trackId != _track.TrackId) return;
                dataWriter.WriteBytes(buffer);
                await dataWriter.StoreAsync();
                dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                                          {
                                              if (count % 8 == 0 || (count <= 10 && count % 3 == 0))
                                              {
                                                  // mediaPlayer.Pause();
                                                  var current = mediaPlayer.PlaybackSession.Position.Ticks + 1;
                                                  mediaPlayer.SetStreamSource(_trackStream.CloneStream());
                                                  mediaPlayer.PlaybackSession.Position = new TimeSpan(current);
                                                  if (_isPlaying) mediaPlayer.Play();
                                                  _loaded = true;
                                              }
                                          });
                count += 1;
                System.Diagnostics.Debug.WriteLine($"Buffer {count} complete - Read {buffer.Length}");
            }
            dataWriter.DetachStream();
            dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                // mediaPlayer.Pause();
                var current = mediaPlayer.PlaybackSession.Position.Ticks + 1;
                mediaPlayer.SetStreamSource(_trackStream.CloneStream());
                mediaPlayer.PlaybackSession.Position = new TimeSpan(current);
                if (_isPlaying) mediaPlayer.Play();
            });
        }
    }

    private async Task LoadAlbumTracks()
    {
        if (string.IsNullOrEmpty(_albumId)) return;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM tracks WHERE track_album_id = :albumId", conn);
        cmd.Parameters.Add(new OracleParameter("albumId", _albumId));
        var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            var trackId = reader.GetString(reader.GetOrdinal("TRACK_ID"));
            _trackQueue.AddLast(trackId);
            _original.AddLast(trackId);
        }
        if (_shuffle)
        {
            var r = new Random();
            _trackQueue = new LinkedList<string>(_trackQueue.OrderBy(x => r.Next()));
        }
        _cacheDirty = true;
    }

    public async Task LoadPlaylistTracks()
    {
        if (string.IsNullOrEmpty(_playlistId)) return;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM playlist_tracks WHERE playlist_id = :playlistId", conn);
        cmd.Parameters.Add(new OracleParameter("playlistId", _playlistId));
        var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            var trackId = reader.GetString(reader.GetOrdinal("TRACK_ID"));
            _trackQueue.AddLast(trackId);
            _original.AddLast(trackId);
        }
        if (_shuffle)
        {
            var r = new Random();
            _trackQueue = new LinkedList<string>(_trackQueue.OrderBy(x => r.Next()));
        }
        _cacheDirty = true;
    }

    public async Task PlayCurrentTrack()
    {
        if (_track is null || !_track.TrackId.Equals(TrackId))
        {
            _loaded = false;
            await Task.Run(() => LoadTrack());
        }
        if (_track != null)
        {
            while (!_loaded) await Task.Delay(100);
            dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                       {
                           var page = (ShellPage)App.MainWindow.Content;
                           mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(0);
                           if (IsPlaying)
                           {
                               page.RhythmPlayer.PlayPauseIcon.Glyph = "\uE769";
                               mediaPlayer.Play();
                               progressTimer?.Start();
                           }
                           else
                           {
                               page.RhythmPlayer.PlayPauseIcon.Glyph = "\uE768";
                               mediaPlayer.Pause();
                               progressTimer?.Stop();
                           }
                       });
            // IsPlaying = true;
            var current = _trackQueue.First?.Value;
            if (current is not null)
            {
                _history.Push(current);
                _trackQueue.RemoveFirst();
                _ = Task.Run(() => UpdatePlayState());
            }
        }
    }

    public async Task PlayAlbum(string albumId)
    {
        _trackQueue.Clear();
        _original.Clear();
        _albumId = albumId;
        await Task.Run(() => LoadAlbumTracks());
        if (_trackQueue.Count > 0 && _trackQueue.First is not null)
        {
            IsPlaying = true;
            TrackId = _trackQueue.First.Value;
        }
    }

    public async Task PlayPlaylist(string playlistId)
    {
        _trackQueue.Clear();
        _original.Clear();
        _playlistId = playlistId;
        await Task.Run(() => LoadPlaylistTracks());
        if (_trackQueue.Count > 0 && _trackQueue.First is not null)
        {
            IsPlaying = true;
            TrackId = _trackQueue.First.Value;
        }
    }

    public void PlayTrack(string trackId)
    {
        _trackQueue.Clear();
        _original.Clear();
        _original.AddLast(trackId);
        _trackQueue.AddLast(trackId);
        IsPlaying = true;
        TrackId = trackId;
    }

    private void PlaybackSession_MediaEnded(MediaPlayer sender, object args)
    {
        dispatcherQueue?.TryEnqueue(DispatcherQueuePriority.High, () =>
                  {
                      var page = (ShellPage)App.MainWindow.Content;
                      mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(0);
                      mediaPlayer.Pause();
                      page.RhythmPlayer.TrackTime.Text = "00:00";
                      page.RhythmPlayer.TrackSeek.Value = 0;
                      page.RhythmPlayer.PlayPauseIcon.Glyph = "\uE768";
                      progressTimer?.Stop();
                      _ = Task.Run(() => UpdateStreams(TrackId));
                      if (_trackQueue.Count > 0 && _trackQueue.First is not null)
                      {
                          TrackId = _trackQueue.First.Value;
                      }
                  });
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(TrackId) || !_loaded) return;
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            if (progressTimer?.IsEnabled == false) progressTimer.Start();
            mediaPlayer.Play();
            PlayPauseIcon.Glyph = "\uE769";
        }
        else
        {
            mediaPlayer.Pause();
            PlayPauseIcon.Glyph = "\uE768";
        }

    }

    private void UpdateComponent()
    {
        var width = App.MainWindow.Bounds.Width;
        VolumeSlider.Visibility = width < 750 ? Visibility.Collapsed : Visibility.Visible;
        VolumeButton.Visibility = width < 750 ? Visibility.Visible : Visibility.Collapsed;
        TrackSeek.Width = width < 1125 ? 170 : 300;
        MediaBar.Margin = width < 800 ? new Thickness(0) : new Thickness(12, 0, 12, 12);
        TrackTitle.Visibility = width < 800 ? Visibility.Collapsed : Visibility.Visible;
        TrackArtist.Visibility = width < 800 ? Visibility.Collapsed : Visibility.Visible;
        TrackInfo.Width = width < 800 ? 0 : 120;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UpdateComponent();
        _ = Task.Run(() => GetPlayState());
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateComponent();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_trackQueue.Count == 0 || _trackQueue.First is null) return;
        IsPlaying = true;
        TrackId = _trackQueue.First.Value;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (_history.Count == 0) return;
        if (_history.Count == 1)
        {
            var current = _history.Pop();
            _trackQueue.AddFirst(current);
            TrackId = current;
            return;
        }
        var c = _history.Pop();
        var p = _history.Pop();
        _trackQueue.AddFirst(c);
        _trackQueue.AddFirst(p);
        IsPlaying = true;
        TrackId = p;
    }

    private void TrackSeek_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_track is null) return;
        var duration = Regex.Match(_track.TrackDuration, @"(\d+):(\d+):(\d+).(\d+)").Groups;
        var totalDuration = int.Parse(duration[2].Value) * 60 + int.Parse(duration[3].Value);
        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(totalDuration * (e.NewValue / 100));
    }

    private void RepeatButton_Click(object sender, RoutedEventArgs e)
    {
        _loop = !_loop;
        RepeatIcon.Glyph = _loop ? "\uE8ED" : "\uE8EE";
        VisualStateManager.GoToState(this, _loop ? "RepeatStateOn" : "RepeatStateOff", true);
        mediaPlayer.IsLoopingEnabled = _loop;
        _ = Task.Run(() => UpdatePlayState());
    }

    private void ShuffleButton_Click(object sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        VisualStateManager.GoToState(this, _shuffle ? "ShuffleStateOn" : "ShuffleStateOff", true);
        if (_shuffle)
        {
            var r = new Random();
            var newQueue = new LinkedList<string>(_trackQueue.OrderBy(x => r.Next()));
            _trackQueue = newQueue;
        }
        else
        {
            var idx = _original.Find(TrackId);
            if (idx is null) return;
            var rest = _original.SkipWhile(x => x != idx.Value).Skip(1);
            var newQueue = new LinkedList<string>(rest);
            _trackQueue = newQueue;
        }
        _ = Task.Run(() => UpdatePlayState());
    }

    public string? GetTrackName() => _track?.TrackName;

    public string? GetTrackArtist() => _artists.Length > 0 ? _artists[0].ArtistName : null;

    public string? GetTrackLyrics() => null;

    public string[] GetQueue() => _trackQueue.ToArray();
}
