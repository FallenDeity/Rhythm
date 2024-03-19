using System.Text;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
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

    public async Task<RhythmTrack?> GetTrack(string trackId)
    {
        try
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
                    AudioAvailable = reader.GetBoolean(reader.GetOrdinal("AUDIO_AVAILABLE")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL"))
                };
                if (!tracks.ContainsKey(trackId)) tracks.Add(trackId, track);
                return track;
            }
            return null;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting track" + e.Message);
            return null;
        }
    }

    public async Task<RhythmTrack[]> GetTracks(string[] trackIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM tracks WHERE track_id IN (");
            var added = false;
            foreach (var trackId in trackIds)
            {
                if (tracks.ContainsKey(trackId)) continue;
                sql.Append($"'{trackId}',");
                added = true;
            }
            if (!added) return trackIds.Where(tracks.ContainsKey).Select(track => tracks[track]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var t = new List<RhythmTrack>();
            while (reader.Read())
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
                    AudioAvailable = reader.GetBoolean(reader.GetOrdinal("AUDIO_AVAILABLE")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL"))
                };
                if (!tracks.ContainsKey(track.TrackId)) tracks.Add(track.TrackId, track);
                t.Add(track);
            }
            return t.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting tracks" + e.Message);
            return Array.Empty<RhythmTrack>();
        }
    }

    public async Task<RhythmArtist?> GetArtist(string artistId)
    {
        try
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
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting artist" + e.Message);
            return null;
        }
    }

    public async Task<RhythmArtist[]> GetArtists(string[] artistIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM artists WHERE artist_id IN (");
            var added = false;
            foreach (var artistId in artistIds)
            {
                if (artists.ContainsKey(artistId)) continue;
                sql.Append($"'{artistId}',");
                added = true;
            }
            if (!added) return artistIds.Where(artists.ContainsKey).Select(artist => artists[artist]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var a = new List<RhythmArtist>();
            while (reader.Read())
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
                if (!artists.ContainsKey(artist.ArtistId)) artists.Add(artist.ArtistId, artist);
                a.Add(artist);
            }
            return a.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    public async Task<RhythmAlbum?> GetAlbum(string albumId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Getting album {albumId}");
            if (albums.ContainsKey(albumId)) return albums[albumId];
            System.Diagnostics.Debug.WriteLine($"Album {albumId} not found in cache");
            var cmd = new OracleCommand($"SELECT * FROM albums WHERE album_id = '{albumId}'", GetOracleConnection());
            cmd.FetchSize *= 2;
            cmd.AddToStatementCache = true;
            var reader = await cmd.ExecuteReaderAsync();
            if (reader.Read())
            {
                var album = new RhythmAlbum
                {
                    AlbumId = reader.GetString(reader.GetOrdinal("ALBUM_ID")),
                    AlbumName = reader.GetString(reader.GetOrdinal("ALBUM_NAME")),
                    ReleaseDate = reader.GetDateTime(reader.GetOrdinal("RELEASE_DATE")),
                    TrackCount = reader.GetInt32(reader.GetOrdinal("TRACK_COUNT")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
                    AlbumType = reader.GetString(reader.GetOrdinal("ALBUM_TYPE")),
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? null : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL"))
                };
                if (!albums.ContainsKey(albumId)) albums.Add(albumId, album);
                return album;
            }
            return null;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting album" + e.Message);
            return null;
        }
    }

    public async Task<RhythmAlbum[]> GetAlbums(string[] albumIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM albums WHERE album_id IN (");
            var added = false;
            foreach (var albumId in albumIds)
            {
                if (albums.ContainsKey(albumId)) continue;
                sql.Append($"'{albumId}',");
                added = true;
            }
            if (!added) return albumIds.Where(albums.ContainsKey).Select(album => albums[album]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var a = new List<RhythmAlbum>();
            while (reader.Read())
            {
                var album = new RhythmAlbum
                {
                    AlbumId = reader.GetString(reader.GetOrdinal("ALBUM_ID")),
                    AlbumName = reader.GetString(reader.GetOrdinal("ALBUM_NAME")),
                    ReleaseDate = reader.GetDateTime(reader.GetOrdinal("RELEASE_DATE")),
                    TrackCount = reader.GetInt32(reader.GetOrdinal("TRACK_COUNT")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
                    AlbumType = reader.GetString(reader.GetOrdinal("ALBUM_TYPE")),
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? null : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL"))
                };
                if (!albums.ContainsKey(album.AlbumId)) albums.Add(album.AlbumId, album);
                a.Add(album);
            }
            return a.ToArray();
        }

        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting albums" + e.Message);
            return Array.Empty<RhythmAlbum>();
        }
    }

    public async Task<RhythmPlaylist?> GetPlaylist(string playlistId)
    {
        try
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
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting playlist" + e.Message);
            return null;
        }
    }

    public async Task<RhythmPlaylist[]> GetPlaylists(string[] playlistIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM playlists WHERE playlist_id IN (");
            var added = false;
            foreach (var playlistId in playlistIds)
            {
                if (playlists.ContainsKey(playlistId)) continue;
                sql.Append($"'{playlistId}',");
                added = true;
            }
            if (!added) return playlistIds.Where(playlists.ContainsKey).Select(playlist => playlists[playlist]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var p = new List<RhythmPlaylist>();
            while (reader.Read())
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
                if (!playlists.ContainsKey(playlist.PlaylistId)) playlists.Add(playlist.PlaylistId, playlist);
                p.Add(playlist);
            }
            return p.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting playlists" + e.Message);
            return Array.Empty<RhythmPlaylist>();
        }
    }

    public async Task<RhythmArtist[]> GetTrackArtists(string trackId)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM artists WHERE artist_id IN (SELECT artist_id FROM track_artists WHERE track_id = '");
            sql.Append(trackId);
            sql.Append("')");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.AddToStatementCache = true;
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var a = new List<RhythmArtist>();
            while (reader.Read())
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
                if (!artists.ContainsKey(artist.ArtistId)) artists.Add(artist.ArtistId, artist);
                a.Add(artist);
            }
            return a.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting track artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    public bool IsConnected() => _connected;
}
