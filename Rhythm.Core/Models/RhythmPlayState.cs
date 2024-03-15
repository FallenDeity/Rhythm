namespace Rhythm.Core.Models;

public class RhythmPlayState
{
    public string UserId
    {
        get; set;
    }

    public List<string> TracksQueue
    {
        get; set;
    }

    public string CurrentTrack
    {
        get; set;
    }

    public bool Loop
    {
        get; set;
    }

    public bool Shuffle
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
        var PlayStateString = $"User ID: {UserId}\n" +
                    $"Tracks Queue: {string.Join(", ", TracksQueue)}\n" +
                    $"Current Track: {CurrentTrack}\n" +
                    $"Loop: {Loop}\n" +
                    $"Shuffle: {Shuffle}\n" +
                    $"Created At: {CreatedAt}\n" +
                    $"Updated At: {UpdatedAt}\n";
        return PlayStateString;
    }
}
