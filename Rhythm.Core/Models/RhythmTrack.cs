#nullable enable

using System.Text.RegularExpressions;

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

    public string? TrackAudioURL
    {
        get; set;
    }

    public bool AudioAvailable
    {
        get; set;
    }

    public string? TrackImageURL
    {
        get; set;
    }

    public required RhythmArtist[]? Artists
    {
        get; set;
    }

    public string? Lyrics
    {
        get; set;
    }

    public int? Count
    {
        get; set;
    }

    public string TrackDurationString
    {

        get
        {
            var pattern = @"\+00 00:(\d{2}:\d{2})";
            var match = Regex.Match(TrackDuration, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "00:00";
        }
    }

    public string ArtistName => Artists?[0].ArtistName ?? "Unknown Artist";

    public string ArtistNames
    {
        get
        {
            if (Artists is null) return "Unknown Artist";
            return string.Join(", ", Artists.Select(artist => artist.ArtistName));
        }
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
            $"Updated At: {UpdatedAt}\n" +
            $"Track Audio URL: {TrackAudioURL}\n" +
            $"Audio Available: {AudioAvailable}\n";
        return trackString;
    }
}
