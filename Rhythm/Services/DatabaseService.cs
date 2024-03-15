using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Core.Models;

namespace Rhythm.Services;

public class DatabaseNullException : Exception
{
    public DatabaseNullException(string message) : base(message)
    {

    }
}

public class DatabaseService : IDatabaseService
{

    private readonly string connectionString = "ORACLE_CONNECTION_STRING";

    private bool _connected = false;

    private readonly Dictionary<string, byte[]> albumCovers = new();

    private readonly Dictionary<string, RhythmTrack> tracks = new();

    private readonly Dictionary<string, RhythmArtist> artists = new();

    private readonly Dictionary<string, RhythmAlbum> albums = new();

    private readonly Dictionary<string, RhythmPlaylist> playlists = new();

    public OracleConnection? Connection
    {
        get;
        private set;
    }

    public DatabaseService()
    {
        ConnectToOracle();
    }

    public bool ConnectToOracle()
    {
        if (!_connected)
        {
            try
            {
                Connection = new OracleConnection(connectionString);
                Connection.Open();
                _connected = true;
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }
        return true;
    }

    public void DisconnectFromOracle()
    {
        if (Connection != null)
        {
            Connection.Dispose();
        }
    }

    public OracleConnection GetOracleConnection()
    {
        if (!_connected || Connection is null)
        {
            throw new DatabaseNullException("Oracle Connection is null");
        }
        return Connection;
    }

    public async Task<byte[]> GetAlbumCover(string albumId)
    {
        if (albumCovers.ContainsKey(albumId)) return albumCovers[albumId];
        var command = new OracleCommand($"SELECT album_image FROM albums WHERE album_id = '{albumId}'", GetOracleConnection());
        command.AddToStatementCache = true;
        var reader = await command.ExecuteReaderAsync();
        if (reader.Read())
        {
            var blob = reader.GetOracleBlob(0);
            var buffer = new byte[blob.Length];
            blob.Read(buffer, 0, buffer.Length);
            if (!albumCovers.ContainsKey(albumId)) albumCovers.Add(albumId, buffer);
            return buffer;
        }
        return Array.Empty<byte>();
    }

    public async Task<RhythmTrack?> GetTrack(string trackId)
    {
        System.Diagnostics.Debug.WriteLine($"Getting track {trackId}");
        if (tracks.ContainsKey(trackId)) return tracks[trackId];
        System.Diagnostics.Debug.WriteLine($"Track {trackId} not found in cache");
        var cmd = new OracleCommand($"SELECT * FROM tracks WHERE track_id = '{trackId}'", GetOracleConnection());
        cmd.FetchSize *= 2;
        cmd.AddToStatementCache = true;
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            var track = new RhythmTrack
            {
                TrackId = reader.GetString(reader.GetOrdinal("TRACK_ID")),
                TrackName = reader.GetString(reader.GetOrdinal("TRACK_NAME")),
                TrackDuration = reader.GetString(reader.GetOrdinal("TRACK_DURATION")),
                TrackAlbumId = reader.GetString(reader.GetOrdinal("TRACK_ALBUM_ID")),
                ReleaseDate = reader.GetDateTime(reader.GetOrdinal("RELEASE_DATE")),
                Streams = reader.GetInt32(reader.GetOrdinal("STREAMS")),
                Likes = reader.GetInt32(reader.GetOrdinal("LIKE_COUNT")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
            };
            if (!tracks.ContainsKey(trackId)) tracks.Add(trackId, track);
            return track;
        }
        return null;
    }

    public async Task<RhythmTrack[]> GetTracks(string[] trackIds)
    {
        var tasks = new List<Task<RhythmTrack?>>();
        foreach (var trackId in trackIds)
        {
            tasks.Add(GetTrack(trackId));
        }
        var results = await Task.WhenAll(tasks);
        if (results is null) return Array.Empty<RhythmTrack>();
        return results.Where(x => x is not null).Select(x => x!).ToArray();
    }

    public async Task<RhythmArtist?> GetArtist(string artistId)
    {
        System.Diagnostics.Debug.WriteLine($"Getting artist {artistId}");
        if (artists.ContainsKey(artistId)) return artists[artistId];
        System.Diagnostics.Debug.WriteLine($"Artist {artistId} not found in cache");
        var cmd = new OracleCommand($"SELECT * FROM artists WHERE artist_id = '{artistId}'", GetOracleConnection());
        cmd.AddToStatementCache = true;
        cmd.FetchSize *= 2;
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            var artist = new RhythmArtist
            {
                ArtistId = reader.GetString(reader.GetOrdinal("ARTIST_ID")),
                UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                ArtistName = reader.GetString(reader.GetOrdinal("ARTIST_NAME")),
                ArtistBio = reader.GetString(reader.GetOrdinal("ARTIST_BIO")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TRACK_COUNT")),
                AlbumCount = reader.GetInt32(reader.GetOrdinal("ALBUM_COUNT")),
                FollowerCount = reader.GetInt32(reader.GetOrdinal("FOLLOWER_COUNT")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
            };
            if (!artists.ContainsKey(artistId)) artists.Add(artistId, artist);
            return artist;
        }
        return null;
    }

    public async Task<RhythmArtist[]> GetArtists(string[] artistIds)
    {
        var tasks = new List<Task<RhythmArtist?>>();
        foreach (var artistId in artistIds)
        {
            tasks.Add(GetArtist(artistId));
        }
        var results = await Task.WhenAll(tasks);
        if (results is null) return Array.Empty<RhythmArtist>();
        return results.Where(x => x is not null).Select(x => x!).ToArray();
    }

    public async Task<RhythmAlbum?> GetAlbum(string albumId)
    {
        System.Diagnostics.Debug.WriteLine($"Getting album {albumId}");
        if (albums.ContainsKey(albumId)) return albums[albumId];
        System.Diagnostics.Debug.WriteLine($"Album {albumId} not found in cache");
        var cmd = new OracleCommand($"SELECT album_id, album_name, release_date, track_count, created_at, updated_at, album_type FROM albums WHERE album_id = '{albumId}'", GetOracleConnection());
        cmd.FetchSize *= 2;
        cmd.AddToStatementCache = true;
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            var album = new RhythmAlbum
            {
                AlbumId = reader.GetString(reader.GetOrdinal("ALBUM_ID")),
                AlbumName = reader.GetString(reader.GetOrdinal("ALBUM_NAME")),
                AlbumImage = Array.Empty<byte>(),
                ReleaseDate = reader.GetDateTime(reader.GetOrdinal("RELEASE_DATE")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TRACK_COUNT")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
                AlbumType = reader.GetString(reader.GetOrdinal("ALBUM_TYPE"))
            };
            if (!albums.ContainsKey(albumId)) albums.Add(albumId, album);
            return album;
        }
        return null;
    }

    public async Task<RhythmAlbum[]> GetAlbums(string[] albumIds)
    {
        var tasks = new List<Task<RhythmAlbum?>>();
        foreach (var albumId in albumIds)
        {
            tasks.Add(GetAlbum(albumId));
        }
        var results = await Task.WhenAll(tasks);
        if (results is null) return Array.Empty<RhythmAlbum>();
        return results.Where(x => x is not null).Select(x => x!).ToArray();
    }

    public async Task<RhythmPlaylist?> GetPlaylist(string playlistId)
    {
        System.Diagnostics.Debug.WriteLine($"Getting playlist {playlistId}");
        if (playlists.ContainsKey(playlistId)) return playlists[playlistId];
        System.Diagnostics.Debug.WriteLine($"Playlist {playlistId} not found in cache");
        var cmd = new OracleCommand($"SELECT * FROM playlists WHERE playlist_id = '{playlistId}'", GetOracleConnection());
        cmd.FetchSize *= 2;
        cmd.AddToStatementCache = true;
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            var playlist = new RhythmPlaylist
            {
                PlaylistId = reader.GetString(reader.GetOrdinal("PLAYLIST_ID")),
                PlaylistName = reader.GetString(reader.GetOrdinal("PLAYLIST_NAME")),
                PlaylistImage = Array.Empty<byte>(),
                PlaylistDescription = reader.GetString(reader.GetOrdinal("PLAYLIST_DESCRIPTION")),
                PlaylistOwner = reader.GetString(reader.GetOrdinal("PLAYLIST_OWNER")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TRACK_COUNT")),
                FollowerCount = reader.GetInt32(reader.GetOrdinal("FOLLOWER_COUNT")),
                LikesCount = reader.GetInt32(reader.GetOrdinal("LIKES_COUNT")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
            };
            if (!playlists.ContainsKey(playlistId)) playlists.Add(playlistId, playlist);
            return playlist;
        }
        return null;
    }

    public async Task<RhythmPlaylist[]> GetPlaylists(string[] playlistIds)
    {
        var tasks = new List<Task<RhythmPlaylist?>>();
        foreach (var playlistId in playlistIds)
        {
            tasks.Add(GetPlaylist(playlistId));
        }
        var results = await Task.WhenAll(tasks);
        if (results is null) return Array.Empty<RhythmPlaylist>();
        return results.Where(x => x is not null).Select(x => x!).ToArray();
    }

    public bool IsConnected() => _connected;
}
