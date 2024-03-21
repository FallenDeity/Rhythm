namespace Rhythm.Core.Models;

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

    public string PlaylistImageURL
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
            $"Playlist Image URL: {PlaylistImageURL}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n";
        return PlaylistString;
    }
}
