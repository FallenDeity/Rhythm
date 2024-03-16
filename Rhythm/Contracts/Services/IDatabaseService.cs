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

    Task<byte[]> GetAlbumCover(string albumId);

    Task<List<byte[]>> GetAlbumCovers(string[] albumIds);

    Task<RhythmAlbum?> GetAlbum(string albumId);

    Task<RhythmTrack?> GetTrack(string trackId);

    Task<RhythmArtist?> GetArtist(string artistId);

    Task<RhythmTrack[]> GetTracks(string[] trackIds);

    Task<RhythmAlbum[]> GetAlbums(string[] albumIds);

    Task<RhythmArtist[]> GetArtists(string[] artistIds);

    Task<RhythmPlaylist[]> GetPlaylists(string[] playlistIds);

    Task<RhythmPlaylist?> GetPlaylist(string playlistId);

    Task<RhythmArtist[]> GetTrackArtists(string trackId);

    bool IsConnected();
}
