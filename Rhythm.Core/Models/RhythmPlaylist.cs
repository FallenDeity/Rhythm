namespace Rhythm.Core.Models;

/*
 *  Name                                      Null?    Type
 ----------------------------------------- -------- ----------------------------
 PLAYLIST_ID                               NOT NULL VARCHAR2(36)
 PLAYLIST_NAME                             NOT NULL VARCHAR2(100)
 PLAYLIST_IMAGE                            NOT NULL BLOB
 PLAYLIST_DESCRIPTION                      NOT NULL VARCHAR2(1000)
 PLAYLIST_OWNER                            NOT NULL VARCHAR2(36)
 TRACK_COUNT                               NOT NULL NUMBER(10)
 FOLLOWER_COUNT                            NOT NULL NUMBER(10)
 LIKES_COUNT                               NOT NULL NUMBER(10)
 CREATED_AT                                NOT NULL TIMESTAMP(6) WITH TIME ZONE
 UPDATED_AT                                NOT NULL TIMESTAMP(6) WITH TIME ZONE
*/

public class RhythmPlaylist
{
    public string PlaylistId
    {
        get; set;
    }

    public string PlaylistName
    {
        get; set;
    }

    public byte[] PlaylistImage
    {
        get; set;
    }

    public string PlaylistDescription
    {
        get; set;
    }

    public string PlaylistOwner
    {
        get; set;
    }

    public int TrackCount
    {
        get; set;
    }

    public int FollowerCount
    {
        get; set;
    }

    public int LikesCount
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    public DateTime UpdatedAt
    {
        get; set;
    }

    public override string ToString()
    {
        var PlaylistString = $"Playlist ID: {PlaylistId}\n" +
            $"Playlist Name: {PlaylistName}\n" +
            $"Playlist Description: {PlaylistDescription}\n" +
            $"Playlist Owner: {PlaylistOwner}\n" +
            $"Track Count: {TrackCount}\n" +
            $"Follower Count: {FollowerCount}\n" +
            $"Likes Count: {LikesCount}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n";
        return PlaylistString;
    }
}
