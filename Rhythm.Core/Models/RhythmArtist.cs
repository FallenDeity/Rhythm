namespace Rhythm.Core.Models;

public class RhythmArtist
{
    public string ArtistId
    {

        get; set;
    }

    public string UserId
    {

        get; set;
    }

    public string ArtistName
    {

        get; set;
    }

    public string ArtistBio
    {

        get; set;
    }

    public int TrackCount
    {

        get; set;
    }

    public int AlbumCount
    {

        get; set;
    }

    public int FollowerCount
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

        var ArtistString = $"Artist ID: {ArtistId}\n" +
            $"User ID: {UserId}\n" +
            $"Artist Name: {ArtistName}\n" +
            $"Artist Bio: {ArtistBio}\n" +
            $"Track Count: {TrackCount}\n" +
            $"Album Count: {AlbumCount}\n" +
            $"Follower Count: {FollowerCount}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n";
        return ArtistString;
    }
}
