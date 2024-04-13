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

public partial class TrackDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private RhythmTrack? item;

    public RhythmAlbum? album;

    [ObservableProperty]
    private bool isDataLoading;

    [ObservableProperty]
    private string? infoString;

    [ObservableProperty]
    private bool artistsAvailable = true;

    [ObservableProperty]
    private bool _artistsLoaded = false;

    [ObservableProperty]
    private bool _albumsLoaded = false;

    private DispatcherQueue? dispatcherQueue;
    public RhythmMediaPlayer player
    {
        get; set;
    }


    public ObservableCollection<RhythmArtist> Artists { get; } = new ObservableCollection<RhythmArtist>();

    public TrackDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        var page = (ShellPage)App.MainWindow.Content;
        player = page.RhythmPlayer;
    }

    [RelayCommand]
    private void OnAlbumClick(RhythmAlbum album)
    {
        if (album != null)
        {/*
            _navigationService.SetListDataItemForNextConnectedAnimation(album);*/
            _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, album.AlbumId);
        }
    }

    [RelayCommand]
    private void OnArtistClick(RhythmArtist artist)
    {
        if (artist != null)
        {
            //_navigationService.SetListDataItemForNextConnectedAnimation(artist);
            _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artist.ArtistId);
        }

    }

    public void OnNavigatedFrom()
    {
    }

    public async Task<RhythmArtist[]?> GetTrackArtists()
    {
        if (Item is null) return null;
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT artist_id FROM track_artists WHERE track_id = :track_id", conn);
        cmd.Parameters.Add("track_id", OracleDbType.Varchar2).Value = Item.TrackId;
        var reader = await cmd.ExecuteReaderAsync();
        var artists = new List<string>();

        while (reader.Read())
        {
            artists.Add(reader.GetString(0));
        }
        var artistData = await App.GetService<IDatabaseService>().GetArtists(artists.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
        {
            Artists.Clear();
            foreach (var _item in artistData)
            {
                if (!Artists.Contains(_item)) Artists.Add(_item);
            }
            ArtistsLoaded = true;
            ArtistsAvailable = Artists.Count > 0;
        });
        return artistData is null ? Array.Empty<RhythmArtist>() : artistData;
    }

    public async Task<RhythmAlbum> GetTrackAlbum()
    {
        if (Item is null) return null;

        // Get the database connection
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        // Create and execute the command to retrieve the album ID
        var cmd = new OracleCommand("SELECT track_album_id FROM tracks WHERE track_id = :track_id", conn);
        cmd.Parameters.Add("track_id", OracleDbType.Varchar2).Value = Item.TrackId;
        var reader = await cmd.ExecuteReaderAsync();
        var albumID = "";
        while (reader.Read())
        {
            albumID = reader.GetString(0);

        }
        var albumData = await App.GetService<IDatabaseService>().GetAlbum(albumID);
        AlbumsLoaded = true;
        return albumData;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is string trackID)
        {
            IsDataLoading = true;
            InfoString = InfoText;
            var data = await Task.Run(() => App.GetService<IDatabaseService>().GetTrack(trackID));
            Item = data;

            if (Item is null) return;

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (!ArtistsLoaded)
            {
                Artists.Clear();
                _ = await Task.Run(() => GetTrackArtists());
            }

            var albumData = await Task.Run(() => GetTrackAlbum());
            album = albumData;
            InfoString = InfoText;
            IsDataLoading = false;
        }
    }

    public async Task ToggleLike(RhythmTrack track)
    {
        var check = await App.GetService<IDatabaseService>().ToggleLike(track.TrackId, App.currentUser?.UserId!);
        if (Item is null) return;
        Item.Liked = check;
    }

    public void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, albumId);
    }

    public void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artistId);
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

    public string CreatedAt => Item != null ? Relativize(Item.ReleaseDate) : "Unknown";

    public string InfoText => $"{Item?.ArtistNames}";
}
