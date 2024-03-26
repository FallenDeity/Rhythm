using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Core.Models;

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

    public ObservableCollection<RhythmTrack> tracks { get; } = new ObservableCollection<RhythmTrack>();

    public ObservableCollection<string> shimmers { get; } = new ObservableCollection<string>();

    public PlaylistDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
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
                tracks.Clear();
                var count = 1;
                foreach (var track in result)
                {
                    track.Count = count++;
                    tracks.Add(track);
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
        foreach (var track in tracks)
        {
            var match = regex.Match(track.TrackDuration);
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

    public string InfoText => $"{Owner} • {Item?.TrackCount} Tracks • {DurationText()}";
}
