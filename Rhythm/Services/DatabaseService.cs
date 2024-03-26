using System.Text;
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

    private readonly string defaultCover = "ms-appx:///Assets/Track.jpeg";

    private readonly Dictionary<string, RhythmTrack> tracks = new();

    private readonly Dictionary<string, RhythmArtist> artists = new();

    private readonly Dictionary<string, RhythmAlbum> albums = new();

    private readonly Dictionary<string, RhythmPlaylist> playlists = new();

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
                    Lyrics = reader.IsDBNull(reader.GetOrdinal("LYRICS")) ? null : reader.GetString(reader.GetOrdinal("LYRICS")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL")),
                    TrackImageURL = reader.IsDBNull(reader.GetOrdinal("TRACK_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("TRACK_IMAGE_URL")),
                    Artists = await GetTrackArtists(trackId)
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
            await GetTracksArtists(trackIds);
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
                    Lyrics = reader.IsDBNull(reader.GetOrdinal("LYRICS")) ? null : reader.GetString(reader.GetOrdinal("LYRICS")),
                    TrackAudioURL = reader.IsDBNull(reader.GetOrdinal("TRACK_AUDIO_URL")) ? null : reader.GetString(reader.GetOrdinal("TRACK_AUDIO_URL")),
                    TrackImageURL = reader.IsDBNull(reader.GetOrdinal("TRACK_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("TRACK_IMAGE_URL")),
                    Artists = await GetTrackArtists(reader.GetString(reader.GetOrdinal("TRACK_ID")))
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
            if (artists.ContainsKey(artistId)) return artists[artistId];
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
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL")),
                    Artists = await GetAlbumArtists(reader.GetString(reader.GetOrdinal("ALBUM_ID")))
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
            await GetAlbumsArtists(albumIds);
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
                    AlbumImageURL = reader.IsDBNull(reader.GetOrdinal("ALBUM_IMAGE_URL")) ? defaultCover : reader.GetString(reader.GetOrdinal("ALBUM_IMAGE_URL")),
                    Artists = await GetAlbumArtists(reader.GetString(reader.GetOrdinal("ALBUM_ID")))
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
            if (playlists.ContainsKey(playlistId)) return playlists[playlistId];
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
            var sql = new OracleCommand($"SELECT artist_id FROM track_artists WHERE track_id = '{trackId}'", GetOracleConnection());
            var reader = await sql.ExecuteReaderAsync();
            var artistIds = new List<string>();
            while (reader.Read())
            {
                artistIds.Add(reader.GetString(0));
            }
            return await GetArtists(artistIds.ToArray());
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting track artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    private async Task<RhythmArtist[]> GetTracksArtists(string[] trackIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT artist_id FROM track_artists WHERE track_id IN (");
            var added = false;
            foreach (var trackId in trackIds)
            {
                if (artists.ContainsKey(trackId)) continue;
                sql.Append($"'{trackId}',");
                added = true;
            }
            if (!added) return trackIds.Where(artists.ContainsKey).Select(track => artists[track]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var artistIds = new List<string>();
            while (reader.Read())
            {
                artistIds.Add(reader.GetString(0));
            }
            return await GetArtists(artistIds.ToArray());
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting tracks artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    private async Task<RhythmArtist[]> GetAlbumArtists(string albumId)
    {
        try
        {
            var sql = new OracleCommand($"SELECT artist_id FROM album_artists WHERE album_id = '{albumId}'", GetOracleConnection());
            var reader = await sql.ExecuteReaderAsync();
            var artistIds = new List<string>();
            while (reader.Read())
            {
                artistIds.Add(reader.GetString(0));
            }
            return await GetArtists(artistIds.ToArray());
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting album artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    private async Task<RhythmArtist[]> GetAlbumsArtists(string[] albumIds)
    {
        try
        {
            var sql = new StringBuilder();
            sql.Append("SELECT artist_id FROM album_artists WHERE album_id IN (");
            var added = false;
            foreach (var albumId in albumIds)
            {
                if (artists.ContainsKey(albumId)) continue;
                sql.Append($"'{albumId}',");
                added = true;
            }
            if (!added) return albumIds.Where(artists.ContainsKey).Select(album => artists[album]).ToArray();
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            var cmd = new OracleCommand(sql.ToString(), GetOracleConnection());
            cmd.FetchSize *= 2;
            var reader = await cmd.ExecuteReaderAsync();
            var artistIds = new List<string>();
            while (reader.Read())
            {
                artistIds.Add(reader.GetString(0));
            }
            return await GetArtists(artistIds.ToArray());
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine("Error getting albums artists" + e.Message);
            return Array.Empty<RhythmArtist>();
        }
    }

    public bool IsConnected() => _connected;
}
