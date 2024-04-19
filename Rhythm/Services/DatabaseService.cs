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

    private readonly string defaultCover = "ms-appx:///Assets/Track.jpeg";

    private readonly Dictionary<string, RhythmTrack> tracks = new();

    private readonly Dictionary<string, RhythmArtist> artists = new();

    private readonly Dictionary<string, RhythmAlbum> albums = new();

    private readonly Dictionary<string, RhythmPlaylist> playlists = new();

    private readonly Dictionary<string, RhythmUser> users = new();

    public OracleConnection? Connection
    {
        get;
        private set;
    }

    public bool ConnectToOracle()
    {
        if (!_connected)
        {
            try
            {
                Connection = new OracleConnection(connectionString);
                Connection.KeepAlive = true;
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
            Connection.Close();
            Connection.Dispose();
            Connection = null;
            _connected = false;
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
            if (tracks.ContainsKey(trackId)) return tracks[trackId];
            var cmd = new OracleCommand($"SELECT * FROM tracks WHERE track_id = :track_id", GetOracleConnection());
            cmd.Parameters.Add("track_id", OracleDbType.Varchar2).Value = trackId;
            cmd.FetchSize *= 2;
            cmd.AddToStatementCache = true;
            var reader = await cmd.ExecuteReaderAsync();
            var a = await GetArtistsForTracks(new[] { trackId });
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
                    Lyrics = reader.IsDBNull(reader.GetOrdinal("LYRICS")) ? null : reader.GetString(reader.GetOrdinal("LYRICS")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL")),
                    TrackImageURL = reader.IsDBNull(reader.GetOrdinal("TRACK_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("TRACK_IMAGE_URL")),
                    Artists = a.ContainsKey(trackId) ? a[trackId] : Array.Empty<RhythmArtist>()
                };
                if (!tracks.ContainsKey(trackId)) tracks.Add(trackId, track);
                await GetAlbum(track.TrackAlbumId);
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
            var presentTracks = trackIds.Where(tracks.ContainsKey).Select(track => tracks[track]).ToArray();
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
            cmd.FetchSize *= 3;
            var reader = await cmd.ExecuteReaderAsync();
            var t = new List<RhythmTrack>();
            var tArtists = await GetArtistsForTracks(trackIds);
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
                    Lyrics = reader.IsDBNull(reader.GetOrdinal("LYRICS")) ? null : reader.GetString(reader.GetOrdinal("LYRICS")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL")),
                    TrackImageURL = reader.IsDBNull(reader.GetOrdinal("TRACK_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("TRACK_IMAGE_URL")),
                    Artists = tArtists.ContainsKey(reader.GetString(reader.GetOrdinal("TRACK_ID"))) ? tArtists[reader.GetString(reader.GetOrdinal("TRACK_ID"))] : Array.Empty<RhythmArtist>()
                };
                if (!tracks.ContainsKey(track.TrackId)) tracks.Add(track.TrackId, track);
                t.Add(track);
            }
            t.AddRange(presentTracks);
            t.Sort((a, b) => Array.IndexOf(trackIds, a.TrackId).CompareTo(Array.IndexOf(trackIds, b.TrackId)));
            await GetAlbums(t.Select(track => track.TrackAlbumId).ToArray());
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
            if (artists.ContainsKey(artistId)) return artists[artistId];
            var cmd = new OracleCommand($"SELECT * FROM artists WHERE artist_id = :artist_id", GetOracleConnection());
            cmd.Parameters.Add("artist_id", OracleDbType.Varchar2).Value = artistId;
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
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
                    ArtistImageURL = reader.IsDBNull(reader.GetOrdinal("ARTIST_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ARTIST_IMAGE_URL"))
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
            var presentArtists = artistIds.Where(artists.ContainsKey).Select(artist => artists[artist]).ToArray();
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
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT")),
                    ArtistImageURL = reader.IsDBNull(reader.GetOrdinal("ARTIST_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ARTIST_IMAGE_URL"))
                };
                if (!artists.ContainsKey(artist.ArtistId)) artists.Add(artist.ArtistId, artist);
                a.Add(artist);
            }
            a.AddRange(presentArtists);
            a.Sort((a, b) => Array.IndexOf(artistIds, a.ArtistId).CompareTo(Array.IndexOf(artistIds, b.ArtistId)));
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
            if (albums.ContainsKey(albumId)) return albums[albumId];
            var cmd = new OracleCommand($"SELECT * FROM albums WHERE album_id = :album_id", GetOracleConnection());
            cmd.Parameters.Add("album_id", OracleDbType.Varchar2).Value = albumId;
            cmd.FetchSize *= 2;
            cmd.AddToStatementCache = true;
            var reader = await cmd.ExecuteReaderAsync();
            var a = await GetArtistsForAlbums(new[] { albumId });
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
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL")),
                    Artists = a.ContainsKey(albumId) ? a[albumId] : Array.Empty<RhythmArtist>()
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
            var presentAlbums = albumIds.Where(albums.ContainsKey).Select(album => albums[album]).ToArray();
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
            var albumArtists = await GetArtistsForAlbums(albumIds);
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
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL")),
                    Artists = albumArtists.ContainsKey(reader.GetString(reader.GetOrdinal("ALBUM_ID"))) ? albumArtists[reader.GetString(reader.GetOrdinal("ALBUM_ID"))] : Array.Empty<RhythmArtist>()
                };
                if (!albums.ContainsKey(album.AlbumId)) albums.Add(album.AlbumId, album);
                a.Add(album);
            }
            a.AddRange(presentAlbums);
            a.Sort((a, b) => Array.IndexOf(albumIds, a.AlbumId).CompareTo(Array.IndexOf(albumIds, b.AlbumId)));
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
            if (playlists.ContainsKey(playlistId)) return playlists[playlistId];
            var cmd = new OracleCommand($"SELECT * FROM playlists WHERE playlist_id = :playlist_id", GetOracleConnection());
            cmd.Parameters.Add("playlist_id", OracleDbType.Varchar2).Value = playlistId;
            cmd.FetchSize *= 2;
            cmd.AddToStatementCache = true;
            var reader = await cmd.ExecuteReaderAsync();
            if (reader.Read())
            {
                var playlist = new RhythmPlaylist
                {
                    PlaylistId = reader.GetString(reader.GetOrdinal("PLAYLIST_ID")),
                    PlaylistName = reader.GetString(reader.GetOrdinal("PLAYLIST_NAME")),
                    PlaylistImageURL = reader.IsDBNull(reader.GetOrdinal("PLAYLIST_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("PLAYLIST_IMAGE_URL")),
                    PlaylistDescription = reader.GetString(reader.GetOrdinal("PLAYLIST_DESCRIPTION")).Trim(),
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
            var presentPlaylists = playlistIds.Where(playlists.ContainsKey).Select(playlist => playlists[playlist]).ToArray();
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
                    PlaylistImageURL = reader.IsDBNull(reader.GetOrdinal("PLAYLIST_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("PLAYLIST_IMAGE_URL")),
                    PlaylistDescription = reader.GetString(reader.GetOrdinal("PLAYLIST_DESCRIPTION")).Trim(),
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
            p.AddRange(presentPlaylists);
            p.Sort((a, b) => Array.IndexOf(playlistIds, a.PlaylistId).CompareTo(Array.IndexOf(playlistIds, b.PlaylistId)));
            return p.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting playlists" + e.Message);
            return Array.Empty<RhythmPlaylist>();
        }
    }

    public async Task<RhythmUser[]> GetUsers(string[] userIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM users WHERE user_id IN (");
            var added = false;
            foreach (var userId in userIds)
            {
                if (users.ContainsKey(userId)) continue;
                sql.Append($"'{userId}',");
                added = true;
            }
            if (!added) return Array.Empty<RhythmUser>();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var u = new List<RhythmUser>();
            while (reader.Read())
            {
                var user = new RhythmUser
                {
                    UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                    UserName = reader.GetString(reader.GetOrdinal("USERNAME")),
                    Password = "",
                    UserImageURL = reader.IsDBNull(reader.GetOrdinal("USER_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("USER_IMAGE_URL")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
                };
                if (!users.ContainsKey(user.UserId)) users.Add(user.UserId, user);
                u.Add(user);
            }
            return u.ToArray();
        }
        catch (Exception e)
        {

            System.Diagnostics.Debug.WriteLine("Error getting users" + e.Message);
            return Array.Empty<RhythmUser>();
        }
    }

    public async Task<Dictionary<string, RhythmArtist[]>> GetArtistsForTracks(string[] trackIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT ta.track_id, a.artist_id, a.user_id, a.artist_name, a.artist_bio, a.track_count, a.album_count, a.follower_count, a.created_at, a.updated_at, a.artist_image_url FROM track_artists ta JOIN artists a ON ta.artist_id = a.artist_id WHERE ta.track_id IN (");
            var added = false;
            foreach (var trackId in trackIds)
            {
                sql.Append($"'{trackId}',");
                added = true;
            }
            if (!added) return new Dictionary<string, RhythmArtist[]>();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var a = new Dictionary<string, List<RhythmArtist>>();
            while (reader.Read())
            {
                var trackId = reader.GetString(0);
                if (!a.ContainsKey(trackId)) a.Add(trackId, new List<RhythmArtist>());
                a[trackId].Add(new RhythmArtist
                {
                    ArtistId = reader.GetString(1),
                    UserId = reader.GetString(2),
                    ArtistName = reader.GetString(3),
                    ArtistBio = reader.GetString(4),
                    TrackCount = reader.GetInt32(5),
                    AlbumCount = reader.GetInt32(6),
                    FollowerCount = reader.GetInt32(7),
                    CreatedAt = reader.GetDateTime(8),
                    UpdatedAt = reader.GetDateTime(9),
                    ArtistImageURL = reader.IsDBNull(10) ? defaultCover : reader.GetString(10)
                });
                foreach (var artist in a[trackId])
                {
                    if (!artists.ContainsKey(artist.ArtistId)) artists.Add(artist.ArtistId, artist);
                }
            }
            return a.ToDictionary(k => k.Key, v => v.Value.ToArray());

        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting artists for tracks" + e.Message);
            return new Dictionary<string, RhythmArtist[]>();
        }
    }

    private async Task<Dictionary<string, RhythmArtist[]>> GetArtistsForAlbums(string[] albumIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT aa.album_id, a.artist_id, a.user_id, a.artist_name, a.artist_bio, a.track_count, a.album_count, a.follower_count, a.created_at, a.updated_at, a.artist_image_url FROM album_artists aa JOIN artists a ON aa.artist_id = a.artist_id WHERE aa.album_id IN (");
            var added = false;
            foreach (var albumId in albumIds)
            {
                sql.Append($"'{albumId}',");
                added = true;
            }
            if (!added) return new Dictionary<string, RhythmArtist[]>();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var a = new Dictionary<string, List<RhythmArtist>>();
            while (reader.Read())
            {
                var albumId = reader.GetString(0);
                if (!a.ContainsKey(albumId)) a.Add(albumId, new List<RhythmArtist>());
                a[albumId].Add(new RhythmArtist
                {
                    ArtistId = reader.GetString(1),
                    UserId = reader.GetString(2),
                    ArtistName = reader.GetString(3),
                    ArtistBio = reader.GetString(4),
                    TrackCount = reader.GetInt32(5),
                    AlbumCount = reader.GetInt32(6),
                    FollowerCount = reader.GetInt32(7),
                    CreatedAt = reader.GetDateTime(8),
                    UpdatedAt = reader.GetDateTime(9),
                    ArtistImageURL = reader.IsDBNull(10) ? defaultCover : reader.GetString(10)
                });
                foreach (var artist in a[albumId])
                {
                    if (!artists.ContainsKey(artist.ArtistId)) artists.Add(artist.ArtistId, artist);
                }
            }
            return a.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting artists for albums" + e.Message);
            return new Dictionary<string, RhythmArtist[]>();
        }
    }

    public bool IsConnected() => _connected;

    public async Task<bool> ToggleLike(string trackId, string userId)
    {
        var cmd = new OracleCommand("toggle_track_like", GetOracleConnection());
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId;
        cmd.Parameters.Add("track_id", OracleDbType.Varchar2).Value = trackId;
        cmd.Parameters.Add("return", OracleDbType.Boolean).Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.BindByName = true;
        await cmd.ExecuteNonQueryAsync();
        var result = ((OracleBoolean)cmd.Parameters["return"].Value).Value;
        App.LikedSongIds = result ? App.LikedSongIds.Append(trackId).ToArray() : App.LikedSongIds.Where(id => id != trackId).ToArray();
        if (tracks.ContainsKey(trackId))
        {
            tracks[trackId].Likes += result ? 1 : -1;
        }
        return result;
    }

    public async Task<bool> ToggleFollow(string artistId, string userId)
    {
        var cmd = new OracleCommand("toggle_artist_follow", GetOracleConnection());
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId;
        cmd.Parameters.Add("artist_id", OracleDbType.Varchar2).Value = artistId;
        cmd.Parameters.Add("return", OracleDbType.Boolean).Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.BindByName = true;
        await cmd.ExecuteNonQueryAsync();
        var result = ((OracleBoolean)cmd.Parameters["return"].Value).Value;
        App.FollowedArtistIds = result ? App.FollowedArtistIds.Append(artistId).ToArray() : App.FollowedArtistIds.Where(id => id != artistId).ToArray();
        if (artists.ContainsKey(artistId))
        {
            artists[artistId].FollowerCount += result ? 1 : -1;
        }
        return result;
    }

    public async Task<bool> ToggleFollowPlaylist(string playlistId, string userId)
    {
        var cmd = new OracleCommand("toggle_playlist_follow", GetOracleConnection());
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId;
        cmd.Parameters.Add("playlist_id", OracleDbType.Varchar2).Value = playlistId;
        cmd.Parameters.Add("return", OracleDbType.Boolean).Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.BindByName = true;
        await cmd.ExecuteNonQueryAsync();
        var result = ((OracleBoolean)cmd.Parameters["return"].Value).Value;
        App.FollowedPlaylistIds = result ? App.FollowedPlaylistIds.Append(playlistId).ToArray() : App.FollowedPlaylistIds.Where(id => id != playlistId).ToArray();
        if (playlists.ContainsKey(playlistId))
        {
            playlists[playlistId].FollowerCount += result ? 1 : -1;
        }
        return result;
    }

    public async Task<bool> ToggleLikePlaylist(string playlistId, string userId)
    {
        var cmd = new OracleCommand("toggle_playlist_like", GetOracleConnection());
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId;
        cmd.Parameters.Add("playlist_id", OracleDbType.Varchar2).Value = playlistId;
        cmd.Parameters.Add("return", OracleDbType.Boolean).Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.BindByName = true;
        await cmd.ExecuteNonQueryAsync();
        var result = ((OracleBoolean)cmd.Parameters["return"].Value).Value;
        App.LikedPlaylistIds = result ? App.LikedPlaylistIds.Append(playlistId).ToArray() : App.LikedPlaylistIds.Where(id => id != playlistId).ToArray();
        if (playlists.ContainsKey(playlistId))
        {
            playlists[playlistId].LikesCount += result ? 1 : -1;
        }
        return result;
    }

    public async Task<bool> ToggleAlbumSave(string albumId, string userId)
    {
        var cmd = new OracleCommand("toggle_album_save", GetOracleConnection());
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Parameters.Add("user_id", OracleDbType.Varchar2).Value = userId;
        cmd.Parameters.Add("album_id", OracleDbType.Varchar2).Value = albumId;
        cmd.Parameters.Add("return", OracleDbType.Boolean).Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.BindByName = true;
        await cmd.ExecuteNonQueryAsync();
        var result = ((OracleBoolean)cmd.Parameters["return"].Value).Value;
        App.SavedAlbumIds = result ? App.SavedAlbumIds.Append(albumId).ToArray() : App.SavedAlbumIds.Where(id => id != albumId).ToArray();
        return result;
    }

    public Dictionary<string, RhythmTrack> GetAllTracks() => tracks;
}
