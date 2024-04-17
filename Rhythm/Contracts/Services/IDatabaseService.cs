using Oracle.ManagedDataAccess.Client;
using Rhythm.Core.Models;

namespace Rhythm.Contracts.Services;

internal interface IDatabaseService
{
    OracleConnection? Connection
    {
        get;
    }

    bool ConnectToOracle();

    void DisconnectFromOracle();

    OracleConnection GetOracleConnection();

    Task<RhythmAlbum?> GetAlbum(string albumId);

    Task<RhythmTrack?> GetTrack(string trackId);

    Task<RhythmArtist?> GetArtist(string artistId);

    Task<RhythmTrack[]> GetTracks(string[] trackIds);

    Task<RhythmAlbum[]> GetAlbums(string[] albumIds);

    Task<RhythmArtist[]> GetArtists(string[] artistIds);

    Task<RhythmPlaylist[]> GetPlaylists(string[] playlistIds);

    Task<RhythmPlaylist?> GetPlaylist(string playlistId);

    Task<RhythmUser[]> GetUsers(string[] userIds);

    Task<Dictionary<string, RhythmArtist[]>> GetArtistsForTracks(string[] trackIds);

    Task<bool> ToggleLike(string trackId, string userId);

    Task<bool> ToggleFollow(string artistId, string userId);

    Task<bool> ToggleFollowPlaylist(string playlistId, string userId);

    Task<bool> ToggleLikePlaylist(string playlistId, string userId);

    Task<bool> ToggleAlbumSave(string albumId, string userId);

    Dictionary<string, RhythmTrack> GetAllTracks();

    bool IsConnected();
}
