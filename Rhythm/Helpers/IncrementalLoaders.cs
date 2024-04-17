using CommunityToolkit.Common.Collections;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.Helpers;

public class IncrementalTrackLoader : IIncrementalSource<RhythmTrackItem>
{
    public static string queryString = "";

    public async Task<IEnumerable<RhythmTrackItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var db = App.GetService<IDatabaseService>();
        var query = $"SELECT track_id FROM tracks WHERE LOWER(track_name) LIKE LOWER('%{queryString}%') OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";
        var offset = pageSize * pageIndex;
        var limit = pageSize;
        var trackIds = await Task.Run(() =>
               {
                   var cmd = db.GetOracleConnection().CreateCommand();
                   cmd.CommandText = query;
                   cmd.Parameters.Add(new OracleParameter("offset", offset));
                   cmd.Parameters.Add(new OracleParameter("limit", limit));
                   var reader = cmd.ExecuteReader();
                   var ids = new List<string>();
                   while (reader.Read())
                   {
                       ids.Add(reader.GetString(0));
                   }
                   return ids;
               });
        var page = (ShellPage)App.MainWindow.Content;
        var tracksData = await Task.Run(() => db.GetTracks(trackIds.ToArray()));
        var tracks = new List<RhythmTrackItem>();
        foreach (var track in tracksData)
        {
            tracks.Add(new RhythmTrackItem
            {
                RhythmTrack = track,
                RhythmMediaPlayer = page.RhythmPlayer,
            });
        }
        return tracks;
    }
}

public class IncrementalArtistLoader : IIncrementalSource<RhythmArtist>
{
    public static string queryString = "";

    public async Task<IEnumerable<RhythmArtist>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var db = App.GetService<IDatabaseService>();
        var query = $"SELECT artist_id FROM artists WHERE LOWER(artist_name) LIKE LOWER('%{queryString}%') OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";
        var offset = pageSize * pageIndex;
        var limit = pageSize;
        var artistIds = await Task.Run(() =>
               {
                   var cmd = db.GetOracleConnection().CreateCommand();
                   cmd.CommandText = query;
                   cmd.Parameters.Add(new OracleParameter("offset", offset));
                   cmd.Parameters.Add(new OracleParameter("limit", limit));
                   var reader = cmd.ExecuteReader();
                   var ids = new List<string>();
                   while (reader.Read())
                   {
                       ids.Add(reader.GetString(0));
                   }
                   return ids;
               });
        return await Task.Run(() => db.GetArtists(artistIds.ToArray()));
    }
}


public class IncrementalAlbumLoader : IIncrementalSource<RhythmAlbum>
{
    public static string queryString = "";

    public async Task<IEnumerable<RhythmAlbum>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var db = App.GetService<IDatabaseService>();
        var query = $"SELECT album_id FROM albums WHERE LOWER(album_name) LIKE LOWER('%{queryString}%') OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";
        var offset = pageSize * pageIndex;
        var limit = pageSize;
        var albumIds = await Task.Run(() =>
                      {
                          var cmd = db.GetOracleConnection().CreateCommand();
                          cmd.CommandText = query;
                          cmd.Parameters.Add(new OracleParameter("offset", offset));
                          cmd.Parameters.Add(new OracleParameter("limit", limit));
                          var reader = cmd.ExecuteReader();
                          var ids = new List<string>();
                          while (reader.Read())
                          {
                              ids.Add(reader.GetString(0));
                          }
                          return ids;
                      });
        return await Task.Run(() => db.GetAlbums(albumIds.ToArray()));
    }
}

public class IncrementalPlaylistLoader : IIncrementalSource<RhythmPlaylist>
{
    public static string queryString = "";

    public async Task<IEnumerable<RhythmPlaylist>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var db = App.GetService<IDatabaseService>();
        var query = $"SELECT playlist_id FROM playlists WHERE LOWER(playlist_name) LIKE LOWER('%{queryString}%') OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";
        var offset = pageSize * pageIndex;
        var limit = pageSize;
        var playlistIds = await Task.Run(() =>
                             {
                                 var cmd = db.GetOracleConnection().CreateCommand();
                                 cmd.CommandText = query;
                                 cmd.Parameters.Add(new OracleParameter("offset", offset));
                                 cmd.Parameters.Add(new OracleParameter("limit", limit));
                                 var reader = cmd.ExecuteReader();
                                 var ids = new List<string>();
                                 while (reader.Read())
                                 {
                                     ids.Add(reader.GetString(0));
                                 }
                                 return ids;
                             });
        return await Task.Run(() => db.GetPlaylists(playlistIds.ToArray()));
    }
}

public class IncrementalUserLoader : IIncrementalSource<RhythmUser>
{
    public static string queryString = "";

    public async Task<IEnumerable<RhythmUser>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var db = App.GetService<IDatabaseService>();
        var query = $"SELECT user_id FROM users WHERE LOWER(username) LIKE LOWER('%{queryString}%') OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY";
        var offset = pageSize * pageIndex;
        var limit = pageSize;
        var userIds = await Task.Run(() =>
                      {
                          var cmd = db.GetOracleConnection().CreateCommand();
                          cmd.CommandText = query;
                          cmd.Parameters.Add(new OracleParameter("offset", offset));
                          cmd.Parameters.Add(new OracleParameter("limit", limit));
                          var reader = cmd.ExecuteReader();
                          var ids = new List<string>();
                          while (reader.Read())
                          {
                              ids.Add(reader.GetString(0));
                          }
                          return ids;
                      });
        return await Task.Run(() => db.GetUsers(userIds.ToArray()));
    }
}
