using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.Services;
using Rhythm.Views;

namespace Rhythm.ViewModels;

public partial class AlbumDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private RhythmAlbum? item;

    [ObservableProperty]
    private bool isDataLoading;

    [ObservableProperty]
    private string? infoString;

    public RhythmMediaPlayer player
    {
        get; set;
    }

    public ObservableCollection<RhythmTrackItem> Tracks { get; } = new ObservableCollection<RhythmTrackItem>();
    public ObservableCollection<RhythmTrackItem> SearchedTracks { get; } = new ObservableCollection<RhythmTrackItem>();
    public ObservableCollection<string> shimmers { get; } = new ObservableCollection<string>();

    public AlbumDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        var page = (ShellPage)App.MainWindow.Content;
        player = page.RhythmPlayer;
    }

    public void OnNavigatedFrom()
    {
    }

    public async Task<RhythmTrack[]?> GetAlbumTracks()
    {
        if (Item is null) return null;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM album_tracks WHERE album_id = :album_id", conn);
        cmd.Parameters.Add("album_id", OracleDbType.Varchar2).Value = Item.AlbumId;
        var reader = await cmd.ExecuteReaderAsync();
        var trackIds = new List<string>();
        while (await reader.ReadAsync())
        {
            trackIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetTracks(trackIds.ToArray());
        return data;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is string albumID)
        {
            IsDataLoading = true;
            InfoString = InfoText;
            var data = await Task.Run(() => App.GetService<IDatabaseService>().GetAlbum(albumID));
            Item = data;
            InfoString = InfoText;
            if (Item is null) return;
            for (var i = 0; i < Item.TrackCount; i++)
            {
                shimmers.Add("" + (i + 1));
            }
            var result = await Task.Run(() => GetAlbumTracks());
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

    public async Task ToggleLike(RhythmTrack track)
    {
        var check = await Task.Run(() => App.GetService<IDatabaseService>().ToggleLike(track.TrackId, App.currentUser?.UserId!));
        var idx = Tracks.IndexOf(Tracks.First(t => t.RhythmTrack.TrackId == track.TrackId));
        if (idx != -1)
        {
            Tracks[idx].RhythmTrack.Liked = check;
        }
    }

    public async Task ToggleSave(RhythmAlbum album)
    {
        await Task.Run(() => App.GetService<IDatabaseService>().ToggleAlbumSave(album.AlbumId, App.currentUser?.UserId!));
    }

    public void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artistId);
    }

    public ObservableCollection<RhythmTrackItem> GetSearchAlbums(string queryText)
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
    public string InfoText => $"{Item?.ArtistName} • {Item?.TrackCount} Tracks • {DurationText()}";
}
