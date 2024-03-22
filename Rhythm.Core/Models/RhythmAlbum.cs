#nullable enable

namespace Rhythm.Core.Models;

public class RhythmAlbum
{
    public required string AlbumId
    {
        get; set;
    }

    public required string AlbumName
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

    public required string AlbumType
    {
        get; set;
    }

    public string? AlbumImageURL
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
            $"Album Type: {AlbumType}\n" +
            $"Album Image URL: {AlbumImageURL}\n";
        return AlbumString;
    }
}
