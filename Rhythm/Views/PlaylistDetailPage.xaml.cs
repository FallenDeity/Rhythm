using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Controls;
using Rhythm.ViewModels;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlaylistDetailPage : Page
{
    public static readonly string PageName = "Playlist Detail";

    public static readonly bool IsPageHidden = true;

    public PlaylistDetailViewModel ViewModel
    {
        get;
    }

    public PlaylistDetailPage()
    {
        ViewModel = App.GetService<PlaylistDetailViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyContentGrid", itemHero);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back)
        {
            var navigationService = App.GetService<INavigationService>();

            if (ViewModel.Item != null)
            {
                navigationService.SetListDataItemForNextConnectedAnimation(ViewModel.Item);
            }
        }
    }

    private async void PlaylistTracks_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = (RhythmTrackItem)e.ClickedItem;
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.RhythmTrack.TrackId)
        {
            await page.RhythmPlayer.PlayPlaylist(ViewModel.Item?.PlaylistId!, track.RhythmTrack.TrackId);
        }
    }

    private void AlbumMenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        ViewModel.NavigateToAlbum(track.RhythmTrack.TrackAlbumId);
    }

    private void AddToQueueMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        page.RhythmPlayer.AddToQueue(track.RhythmTrack.TrackId);
    }

    private async void ToggleLikeButton_Click(object sender, RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        await ViewModel.ToggleLike(track.RhythmTrack);
        var button = (Button)sender;
        var glyph = track.RhythmTrack.TrackLiked();
        var accent = Application.Current.Resources["AccentAAFillColorDefaultBrush"] as SolidColorBrush;
        var normal = Application.Current.Resources["SystemControlForegroundBaseHighBrush"] as SolidColorBrush;
        button.Content = new FontIcon
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = (bool)track.RhythmTrack.Liked! ? accent : normal,
            Glyph = glyph
        };
    }

    private void ArtistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        if (track.RhythmTrack.Artists is not null && track.RhythmTrack.Artists.Any())
        {
            ViewModel.NavigateToArtist(track.RhythmTrack.Artists[0].ArtistId);
        }
    }
}
