using System.Collections.ObjectModel;
using System.Data;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private DispatcherQueue? dispatcherQueue;

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
        var cmd = new OracleCommand("get_user_albums", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("v_user_id", OracleDbType.Varchar2).Value = App.currentUser!.UserId;
        cmd.Parameters.Add("v_user_albums", OracleDbType.RefCursor, ParameterDirection.Output);
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
            foreach (var item in albumData)
            {
                if (!rhythmAlbums.Contains(item)) rhythmAlbums.Add(item);
            }
            AlbumsLoaded = true;
        });
        return albumData is null ? Array.Empty<RhythmAlbum>() : albumData;
    }

    public async Task<RhythmArtist[]> GetRecommendedArtists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("get_user_artists", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("v_user_id", OracleDbType.Varchar2).Value = App.currentUser!.UserId;
        cmd.Parameters.Add("v_user_artists", OracleDbType.RefCursor, ParameterDirection.Output);
        var reader = await cmd.ExecuteReaderAsync();
        var artists = new List<string>();
        while (reader.Read())
        {
            artists.Add(reader.GetString(0));
        }
        var artistData = await App.GetService<IDatabaseService>().GetArtists(artists.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
        {
            rhythmArtists.Clear();
            foreach (var item in artistData)
            {
                if (!rhythmArtists.Contains(item)) rhythmArtists.Add(item);
            }
            ArtistsLoaded = true;
        });
        return artistData is null ? Array.Empty<RhythmArtist>() : artistData;
    }

    public async Task<RhythmTrack[]> GetRecommendedTracks()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("get_user_tracks", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("v_user_id", OracleDbType.Varchar2).Value = App.currentUser!.UserId;
        cmd.Parameters.Add("v_user_tracks", OracleDbType.RefCursor, ParameterDirection.Output);
        var reader = await cmd.ExecuteReaderAsync();
        var tracks = new List<string>();
        while (reader.Read())
        {
            tracks.Add(reader.GetString(0));
        }
        var trackData = await App.GetService<IDatabaseService>().GetTracks(tracks.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
        {
            rhythmTracks.Clear();
            foreach (var item in trackData)
            {
                if (!rhythmTracks.Contains(item)) rhythmTracks.Add(item);
            }
            TracksLoaded = true;
        });
        return trackData is null ? Array.Empty<RhythmTrack>() : trackData;
    }

    public async Task<RhythmPlaylist[]> GetRecommendedPlaylists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("get_user_playlists", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add("v_user_id", OracleDbType.Varchar2).Value = App.currentUser!.UserId;
        cmd.Parameters.Add("v_user_playlists", OracleDbType.RefCursor, ParameterDirection.Output);
        var reader = await cmd.ExecuteReaderAsync();
        var playlists = new List<string>();
        while (reader.Read())
        {
            playlists.Add(reader.GetString(0));
        }
        var playlistData = await App.GetService<IDatabaseService>().GetPlaylists(playlists.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
        {
            rhythmPlaylists.Clear();
            foreach (var item in playlistData)
            {
                if (!rhythmPlaylists.Contains(item)) rhythmPlaylists.Add(item);
            }
            PlaylistsLoaded = true;
        });
        return playlistData is null ? Array.Empty<RhythmPlaylist>() : playlistData;
    }

    public void OnNavigatedTo(object parameter)
    {
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (!TracksLoaded)
        {
            _ = Task.Run(() => GetRecommendedTracks());
        }
        if (!AlbumsLoaded)
        {
            _ = Task.Run(() => GetRecommendedAlbums());
        }
        if (!ArtistsLoaded)
        {
            _ = Task.Run(() => GetRecommendedArtists());
        }
        if (!PlaylistsLoaded)
        {
            _ = Task.Run(() => GetRecommendedPlaylists());
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
        if (artist != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(artist);
            _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artist.ArtistId);
        }

    }

    [RelayCommand]
    private void OnTrackClick(RhythmTrack track)
    {
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.TrackId)
        {
            page.RhythmPlayer.PlayTrack(track.TrackId);
        }
        if (track != null)
        {
            _navigationService.NavigateTo(typeof(TrackDetailViewModel).FullName!, track.TrackId);
        }
    }

    [RelayCommand]
    private void OnPlaylistClick(RhythmPlaylist playlist)
    {
        if (playlist != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(playlist);
            _navigationService.NavigateTo(typeof(PlaylistDetailViewModel).FullName!, playlist.PlaylistId);
        }
    }
}
