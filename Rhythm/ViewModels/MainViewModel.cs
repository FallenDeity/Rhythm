using System.Collections.ObjectModel;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    public ObservableCollection<Shimmer> shimmers { get; } = new ObservableCollection<Shimmer>();

    [ObservableProperty]
    private bool _albumsLoaded = false;

    public ObservableCollection<RhythmAlbum> rhythmAlbums { get; } = new ObservableCollection<RhythmAlbum>();

    [ObservableProperty]
    private bool _artistsLoaded = false;

    public ObservableCollection<RhythmArtist> rhythmArtists { get; } = new ObservableCollection<RhythmArtist>();

    [ObservableProperty]
    private bool _tracksLoaded = false;

    public ObservableCollection<RhythmTrack> rhythmTracks { get; } = new ObservableCollection<RhythmTrack>();

    [ObservableProperty]
    private bool _playlistsLoaded = false;

    public ObservableCollection<RhythmPlaylist> rhythmPlaylists { get; } = new ObservableCollection<RhythmPlaylist>();

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        for (var i = 0; i < 8; i++)
        {
            var shimmer = new Shimmer();
            shimmer.Width = 300;
            shimmer.Height = 120;
            shimmer.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6);
            shimmer.Margin = new Microsoft.UI.Xaml.Thickness(8);
            shimmer.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
            shimmers.Add(shimmer);
        }
    }

    public async Task<RhythmAlbum[]> GetRecommendedAlbums()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT album_id FROM albums ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY", conn);
        var reader = await cmd.ExecuteReaderAsync();
        var albums = new List<string>();
        while (reader.Read())
        {
            albums.Add(reader.GetString(0));
        }
        var albumData = await App.GetService<IDatabaseService>().GetAlbums(albums.ToArray());
        return albumData is null ? Array.Empty<RhythmAlbum>() : albumData;
    }

    public async Task<RhythmArtist[]> GetRecommendedArtists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT artist_id FROM artists ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY", conn);
        var reader = await cmd.ExecuteReaderAsync();
        var artists = new List<string>();
        while (reader.Read())
        {
            artists.Add(reader.GetString(0));
        }
        var artistData = await App.GetService<IDatabaseService>().GetArtists(artists.ToArray());
        return artistData is null ? Array.Empty<RhythmArtist>() : artistData;
    }

    public async Task<RhythmTrack[]> GetRecommendedTracks()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT track_id FROM tracks ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY", conn);
        var reader = await cmd.ExecuteReaderAsync();
        var tracks = new List<string>();
        while (reader.Read())
        {
            tracks.Add(reader.GetString(0));
        }
        var trackData = await App.GetService<IDatabaseService>().GetTracks(tracks.ToArray());
        return trackData is null ? Array.Empty<RhythmTrack>() : trackData;
    }

    public async Task<RhythmPlaylist[]> GetRecommendedPlaylists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT playlist_id FROM playlists WHERE playlist_owner = :user_id ORDER BY DBMS_RANDOM.VALUE FETCH FIRST 8 ROWS ONLY", conn);
        cmd.Parameters.Add(new OracleParameter("user_id", App.currentUser?.UserId!));
        var reader = await cmd.ExecuteReaderAsync();
        var playlists = new List<string>();
        while (reader.Read())
        {
            playlists.Add(reader.GetString(0));
        }
        var playlistData = await App.GetService<IDatabaseService>().GetPlaylists(playlists.ToArray());
        return playlistData is null ? Array.Empty<RhythmPlaylist>() : playlistData;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!TracksLoaded)
        {
            var data = await Task.Run(() => GetRecommendedTracks());
            rhythmTracks.Clear();
            foreach (var item in data)
            {
                if (!rhythmTracks.Contains(item)) rhythmTracks.Add(item);
            }
            TracksLoaded = true;
        }
        if (!AlbumsLoaded)
        {
            var data = await Task.Run(() => GetRecommendedAlbums());
            rhythmAlbums.Clear();
            foreach (var item in data)
            {
                if (!rhythmAlbums.Contains(item)) rhythmAlbums.Add(item);
            }
            AlbumsLoaded = true;
        }
        if (!ArtistsLoaded)
        {
            var data = await Task.Run(() => GetRecommendedArtists());
            rhythmArtists.Clear();
            foreach (var item in data)
            {
                if (!rhythmArtists.Contains(item)) rhythmArtists.Add(item);
            }
            ArtistsLoaded = true;
        }
        if (!PlaylistsLoaded)
        {

            var data = await Task.Run(() => GetRecommendedPlaylists());
            rhythmPlaylists.Clear();
            foreach (var item in data)
            {

                if (!rhythmPlaylists.Contains(item)) rhythmPlaylists.Add(item);
            }
            PlaylistsLoaded = true;
        }
    }
    public void OnNavigatedFrom()
    {
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

    [RelayCommand]
    private void OnArtistClick(RhythmArtist artist)
    {

    }

    [RelayCommand]
    private void OnTrackClick(RhythmTrack track)
    {
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.TrackId)
        {
            page.RhythmPlayer.PlayTrack(track.TrackId);
        }
    }

    [RelayCommand]
    private void OnPlaylistClick(RhythmPlaylist playlist)
    {

    }
}
