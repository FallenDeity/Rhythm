using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.ViewModels;

public partial class PlaylistDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private RhythmPlaylist? item;

    [ObservableProperty]
    private bool isDataLoading;

    [ObservableProperty]
    private string? infoString;

    [ObservableProperty]
    private string? owner;

    public RhythmMediaPlayer player
    {
        get; set;
    }

    public ObservableCollection<RhythmTrackItem> Tracks { get; } = new ObservableCollection<RhythmTrackItem>();
    public ObservableCollection<RhythmTrackItem> SearchedTracks { get; } = new ObservableCollection<RhythmTrackItem>();

    public ObservableCollection<string> shimmers { get; } = new ObservableCollection<string>();

    public PlaylistDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        var page = (ShellPage)App.MainWindow.Content;
        player = page.RhythmPlayer;
    }

    public async Task<RhythmTrack[]?> GetPlaylistTracks()
    {
        if (Item is null) return null;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM playlist_tracks WHERE playlist_id = :playlist_id", conn);
        cmd.Parameters.Add("playlist_id", OracleDbType.Varchar2).Value = Item.PlaylistId;
        var reader = await cmd.ExecuteReaderAsync();
        var trackIds = new List<string>();
        while (await reader.ReadAsync())
        {
            trackIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetTracks(trackIds.ToArray());
        return data;
    }

    public async Task<string> GetPlaylistOwner()
    {
        if (Item is null) return "Uknown User";
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT username FROM users WHERE user_id = :user_id", conn);
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = Item.PlaylistOwner;
        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.GetString(0);
        }
        return "Uknown User";
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is string playlistID)
        {
            IsDataLoading = true;
            InfoString = InfoText;
            var data = await Task.Run(() => App.GetService<IDatabaseService>().GetPlaylist(playlistID));
            Item = data;
            InfoString = InfoText;
            Owner = await Task.Run(() => GetPlaylistOwner());
            InfoString = InfoText;
            if (Item is null) return;
            for (var i = 0; i < Item.TrackCount; i++)
            {
                shimmers.Add("" + (i + 1));
            }
            var result = await Task.Run(() => GetPlaylistTracks());
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
            InfoString = InfoText;
            IsDataLoading = false;
        }
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

    public void OnNavigatedFrom()
    {
    }

    public void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, albumId);
    }

    public void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artistId);
    }

    public async Task ToggleLike(RhythmTrack track)
    {
        var check = await App.GetService<IDatabaseService>().ToggleLike(track.TrackId, App.currentUser?.UserId!);
        var idx = Tracks.IndexOf(Tracks.First(t => t.RhythmTrack.TrackId == track.TrackId));
        if (idx != -1)
        {
            Tracks[idx].RhythmTrack.Liked = check;
        }
    }

    public ObservableCollection<RhythmTrackItem> GetSearchPlaylist(string queryText)
    {

        var filteredTracks = Tracks.Where(track =>
            track.RhythmTrack.TrackName.Contains(queryText, StringComparison.OrdinalIgnoreCase));
        SearchedTracks.Clear();
        foreach (var track in filteredTracks)
        {
            SearchedTracks.Add(track);
        }
        return SearchedTracks;
    }

    public string InfoText => $"{Owner} • {Item?.TrackCount} Tracks • {DurationText()}";
}
