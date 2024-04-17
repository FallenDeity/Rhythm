using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.ViewModels;

public partial class ArtistDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private RhythmArtist? item;

    [ObservableProperty]
    private bool isDataLoading;

    [ObservableProperty]
    private string? infoString;

    [ObservableProperty]
    private bool albumsAvailable = true;

    private DispatcherQueue? dispatcherQueue;
    public RhythmMediaPlayer player
    {
        get; set;
    }

    public ObservableCollection<RhythmTrackItem> Tracks { get; } = new ObservableCollection<RhythmTrackItem>();

    [ObservableProperty]
    private bool _albumsLoaded = false;
    public ObservableCollection<RhythmAlbum> rhythmAlbums { get; } = new ObservableCollection<RhythmAlbum>();
    public ObservableCollection<string> shimmers { get; } = new ObservableCollection<string>();

    public ArtistDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        var page = (ShellPage)App.MainWindow.Content;
        player = page.RhythmPlayer;
    }

    [RelayCommand]
    private void OnAlbumClick(RhythmAlbum album)
    {
        if (album != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(album);
            _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, album.AlbumId);
        }
    }

    public void OnNavigatedFrom()
    {
    }

    public async Task<RhythmTrack[]?> GetArtistTracks()
    {
        if (Item is null) return null;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM artist_tracks WHERE artist_id = :artist_id ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 5 ROWS ONLY", conn);
        cmd.Parameters.Add("artist_id", OracleDbType.Varchar2).Value = Item.ArtistId;
        var reader = await cmd.ExecuteReaderAsync();
        var trackIds = new List<string>();
        while (await reader.ReadAsync())
        {
            trackIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetTracks(trackIds.ToArray());
        return data;
    }

    public async Task<RhythmAlbum[]> GetArtistAlbums()
    {
        if (Item is null) return Array.Empty<RhythmAlbum>();
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT album_id FROM artist_albums WHERE artist_id = :artist_id", conn);
        cmd.Parameters.Add("artist_id", OracleDbType.Varchar2).Value = Item.ArtistId;
        var reader = await cmd.ExecuteReaderAsync();
        var albums = new List<string>();
        while (reader.Read())
        {
            albums.Add(reader.GetString(0));
        }
        var albumData = await App.GetService<IDatabaseService>().GetAlbums(albums.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
        {
            rhythmAlbums.Clear();
            foreach (var _item in albumData)
            {
                if (!rhythmAlbums.Contains(_item)) rhythmAlbums.Add(_item);
            }
            AlbumsLoaded = true;
            AlbumsAvailable = rhythmAlbums.Count > 0;
        });
        return albumData is null ? Array.Empty<RhythmAlbum>() : albumData;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is string artistID)
        {
            IsDataLoading = true;
            InfoString = InfoText;
            var data = await Task.Run(() => App.GetService<IDatabaseService>().GetArtist(artistID));
            Item = data;
            InfoString = InfoText;
            if (Item is null) return;
            for (var i = 0; i < Item.TrackCount; i++)
            {
                shimmers.Add("" + (i + 1));
            }
            var result = await Task.Run(() => GetArtistTracks());
            if (result is not null)
            {
                Tracks.Clear();
                var count = 1;
                foreach (var track in result)
                {
                    track.Count = count++;
                    track.Liked = App.LikedSongIds.Contains(track.TrackId);
                    Tracks.Add(new RhythmTrackItem { RhythmTrack = track, RhythmMediaPlayer = player });
                }
            }
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (!AlbumsLoaded)
            {
                rhythmAlbums.Clear();
                _ = Task.Run(() => GetArtistAlbums());
            }
            InfoString = InfoText;
            IsDataLoading = false;
        }
    }

    public void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, albumId);
    }

    public string DurationText()
    {
        if (Item is null) return "0 minutes";
        var regex = new Regex(@"\+00 (\d{2}:\d{2}:\d{2}\.\d{1,})");
        double totalSeconds = 0, total = 0;
        foreach (var track in Tracks)
        {
            var match = regex.Match(track.RhythmTrack.TrackDuration);
            if (match.Success)
            {
                var time = TimeSpan.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                totalSeconds += time.TotalSeconds;
            }
        }
        var text = "";
        var span = TimeSpan.FromSeconds(totalSeconds);
        if (span.Hours > 0)
        {
            text += $"{span.Hours} hours ";
            total++;
        }
        if (span.Minutes > 0)
        {
            text += $"{span.Minutes} minutes ";
            total++;
        }
        if (span.Seconds > 0 && total < 2) text += $"{span.Seconds} seconds";
        return text;

    }

    public async Task ToggleLike(RhythmTrack track)
    {
        var check = await Task.Run(() => App.GetService<IDatabaseService>().ToggleLike(track.TrackId, App.currentUser?.UserId!));
        var idx = Tracks.IndexOf(Tracks.First(t => t.RhythmTrack.TrackId == track.TrackId));
        if (idx != -1)
        {
            Tracks[idx].RhythmTrack.Liked = check;
        }
    }

    public async Task ToggleFollow(RhythmArtist artist)
    {
        await Task.Run(() => App.GetService<IDatabaseService>().ToggleFollow(artist.ArtistId, App.currentUser?.UserId!));
    }

    public static string Relativize(DateTime date)
    {
        var span = DateTime.Now - date;
        if (span.Days > 365) return $"about {span.Days / 365} year{(span.Days / 365 == 1 ? "" : "s")} ago";
        if (span.Days > 30) return $"about {span.Days / 30} month{(span.Days / 30 == 1 ? "" : "s")} ago";
        if (span.Days > 0) return $"about {span.Days} day{(span.Days == 1 ? "" : "s")} ago";
        if (span.Hours > 0) return $"about {span.Hours} hour{(span.Hours == 1 ? "" : "s")} ago";
        if (span.Minutes > 0) return $"about {span.Minutes} minute{(span.Minutes == 1 ? "" : "s")} ago";
        return span.Seconds > 5 ? $"about {span.Seconds} seconds ago" : "just now";
    }

    public string JoinedOn => Item != null ? Relativize(Item.CreatedAt) : "Unknown";

    public string InfoText => $"{Item?.ArtistBio} \n\n{Item?.AlbumCount} Albums � {Item?.TrackCount} Tracks � {Item?.FollowerCount} Followers\nJoined {JoinedOn}";
}
