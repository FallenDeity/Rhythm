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

    Task<Dictionary<string, RhythmArtist[]>> GetArtistsForTracks(string[] trackIds);

    Task<bool> ToggleLike(string trackId, string userId);

    bool IsConnected();
}
