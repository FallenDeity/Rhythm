#nullable enable

namespace Rhythm.Core.Models;

public class RhythmTrack
{
    public required string TrackId
    {
        get; set;
    }

    public required string TrackName
    {
        get; set;
    }

    public required string TrackDuration
    {
        get; set;
    }

    public required string TrackAlbumId
    {
        get; set;
    }

    public DateTime ReleaseDate
    {
        get; set;
    }

    public int Streams
    {
        get; set;
    }

    public int Likes
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

        var trackString = $"Track ID: {TrackId}\n" +
            $"Track Name: {TrackName}\n" +
            $"Track Duration: {TrackDuration}\n" +
            $"Track Album ID: {TrackAlbumId}\n" +
            $"Release Date: {ReleaseDate}\n" +
            $"Streams: {Streams}\n" +
            $"Likes: {Likes}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n";
        return trackString;
    }
}
