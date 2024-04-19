using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Core.Models;

namespace Rhythm.ViewModels;
public partial class LibraryViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private DispatcherQueue? dispatcherQueue;

    private readonly string _userId;

    [ObservableProperty]
    private bool _albumsLoaded = false;

    public ObservableCollection<RhythmAlbum> _albums { get; } = new();

    [ObservableProperty]
    private bool _artistsLoaded = false;

    public ObservableCollection<RhythmArtist> _artists { get; } = new();

    [ObservableProperty]
    private bool _playlistsLoaded = false;

    public ObservableCollection<RhythmPlaylist> _playlists { get; } = new();

    public ObservableCollection<string> _shimmers { get; } = new();

    public LibraryViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _userId = App.currentUser!.UserId;
        for (var i = 0; i < 10; i++)
        {
            _shimmers.Add("" + i);
        }
    }

    public void OnNavigatedFrom()
    {
    }
    public void OnNavigatedTo(object parameter)
    {
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (!AlbumsLoaded)
        {
            _ = Task.Run(GetUserSavedAlbums);
        }
        if (!ArtistsLoaded)
        {
            _ = Task.Run(GetUserFollowedArtists);
        }
        if (!PlaylistsLoaded)
        {
            _ = Task.Run(GetUserPlaylists);
        }
    }

    private async Task GetUserSavedAlbums()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT album_id FROM user_saved_albums WHERE user_id = :user_id", conn);
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = _userId;
        var reader = await cmd.ExecuteReaderAsync();
        var albumIds = new List<string>();
        while (await reader.ReadAsync())
        {
            albumIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetAlbums(albumIds.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
               {
                   _albums.Clear();
                   foreach (var album in data)
                   {
                       _albums.Add(album);
                   }
                   AlbumsLoaded = true;
               });
    }

    private async Task GetUserFollowedArtists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT artist_id FROM artist_followers WHERE user_id = :user_id", conn);
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = _userId;
        var reader = await cmd.ExecuteReaderAsync();
        var artistIds = new List<string>();
        while (await reader.ReadAsync())
        {
            artistIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetArtists(artistIds.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
                      {
                          _artists.Clear();
                          foreach (var artist in data)
                          {
                              _artists.Add(artist);
                          }
                          ArtistsLoaded = true;
                      });
    }

    private async Task GetUserPlaylists()
    {
        var conn = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT playlist_id FROM playlists WHERE playlist_owner = :user_id UNION SELECT playlist_id FROM playlist_followers WHERE user_id = :user_id", conn);
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = _userId;
        var reader = await cmd.ExecuteReaderAsync();
        var playlistIds = new List<string>();
        while (await reader.ReadAsync())
        {
            playlistIds.Add(reader.GetString(0));
        }
        var data = await App.GetService<IDatabaseService>().GetPlaylists(playlistIds.ToArray());
        dispatcherQueue?.TryEnqueue(() =>
               {
                   _playlists.Clear();
                   foreach (var playlist in data)
                   {
                       _playlists.Add(playlist);
                   }
                   PlaylistsLoaded = true;
               });
    }

    public void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, albumId);
    }

    public void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artistId);
    }

    public void NavigateToPlaylist(string playlistId)
    {
        _navigationService.NavigateTo(typeof(PlaylistDetailViewModel).FullName!, playlistId);
    }
}
