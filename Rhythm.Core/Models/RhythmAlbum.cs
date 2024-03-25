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

    public RhythmArtist[]? Artists
    {
        get; set;
    }

    public string ArtistNames
    {
        get
        {
            if (Artists == null)
            {
                return string.Empty;
            }
            return string.Join(", ", Artists.Select(a => a.ArtistName));
        }
    }

    public string ArtistName
    {
        get
        {
            if (Artists == null)
            {
                return "Unknown Artist";
            }
            return Artists[0].ArtistName;
        }
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
