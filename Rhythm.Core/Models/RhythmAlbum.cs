namespace Rhythm.Core.Models;

public class RhythmAlbum
{
    public string AlbumId
    {
        get; set;
    }

    public string AlbumName
    {
        get; set;
    }

    public byte[] AlbumImage
    {
        get; set;
    }

    public DateTime ReleaseDate
    {
        get; set;
    }

    public int TrackCount
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

    public string AlbumType
    {
        get; set;
    }

    public override string ToString()
    {
        var AlbumString = $"Album ID: {AlbumId}\n" +
            $"Album Name: {AlbumName}\n" +
            $"Release Date: {ReleaseDate}\n" +
            $"Track Count: {TrackCount}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n" +
            $"Album Type: {AlbumType}\n";
        return AlbumString;
    }
}
