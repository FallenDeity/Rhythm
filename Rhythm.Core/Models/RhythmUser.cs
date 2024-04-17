#nullable enable

namespace Rhythm.Core.Models;

public class RhythmUser
{
    public required string UserId
    {
        get; set;
    }

    public required string UserName
    {
        get; set;
    }

    public required string Password
    {
        get; set;
    }

    public string? UserImageURL
    {
        get; set;
    }

    public string? Country
    {
        get; set;
    }

    public string? Gender
    {
        get; set;
    }

    public int PlaylistCount
    {
        get; set;
    }

    public int FavoriteSongCount
    {
        get; set;
    }

    public int SavedAlbumCount
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

    public string UserImageFileName
    {
        get
        {
            if (UserImageURL is null) return "";
            var parts = UserImageURL.Split('/');
            return parts[^1].Split("?")[0];
        }
    }

    public static string Relativize(DateTime date)
    {
        var span = DateTime.Now - date;
        if (span.Days > 365) return $"about {span.Days / 365} year{(span.Days / 365 == 1 ? "" : "s")} ago";
        if (span.Days > 30) return $"about {span.Days / 30} month{(span.Days / 30 == 1 ? "" : "s")} ago";
        if (span.Days > 0) return $"about {span.Days} day{(span.Days == 1 ? "" : "s")} ago";
        if (span.Hours > 0) return $"about {span.Hours} hour{(span.Hours == 1 ? "" : "s")} ago";
        if (span.Minutes > 0) return $"about {span.Minutes} minute{(span.Minutes == 1 ? "" : "s")} ago";
        return span.Seconds > 5 ? $"about {span.Seconds} seconds ago" : "just now";
    }

    public string GetAge => "joined " + Relativize(CreatedAt);

    public override string ToString()
    {
        var userString = $"User ID: {UserId}\n" +
            $"Username: {UserName}\n" +
            $"Country: {Country}\n" +
            $"Gender: {Gender}\n" +
            $"Playlist Count: {PlaylistCount}\n" +
            $"Favorite Song Count: {FavoriteSongCount}\n" +
            $"Saved Album Count: {SavedAlbumCount}\n" +
            $"User Image URL: {UserImageURL}\n" +
            $"Created At: {CreatedAt}\n" +
            $"Updated At: {UpdatedAt}\n";
        return userString;
    }
}
