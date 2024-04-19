using Microsoft.UI.Xaml.Controls;
using Rhythm.Core.Models;
using Rhythm.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel
    {
        get;
    }

    public static readonly string PageName = "Library";

    public static readonly bool IsPageHidden = false;

    public LibraryPage()
    {
        ViewModel = App.GetService<LibraryViewModel>();
        InitializeComponent();
    }

    private void Playlist_ItemClick(object sender, ItemClickEventArgs e)
    {
        var playlist = (RhythmPlaylist)e.ClickedItem;
        ViewModel.NavigateToPlaylist(playlist.PlaylistId);
    }

    private void Album_ItemClick(object sender, ItemClickEventArgs e)
    {
        var album = (RhythmAlbum)e.ClickedItem;
        ViewModel.NavigateToAlbum(album.AlbumId);
    }

    private void Artist_ItemClick(object sender, ItemClickEventArgs e)
    {
        var artist = (RhythmArtist)e.ClickedItem;
        ViewModel.NavigateToArtist(artist.ArtistId);
    }
}
