using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ArtistDetailPage : Page
{
    public static readonly string PageName = "Artist Detail";

    public static readonly bool IsPageHidden = true;

    public ArtistDetailViewModel ViewModel
    {
        get;
    }

    public ArtistDetailPage()
    {
        ViewModel = App.GetService<ArtistDetailViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyContentGrid", itemHero);
        var page = (ShellPage)App.MainWindow.Content;
        VisualStateManager.GoToState(this, page.RhythmPlayer.IsShuffled ? "ShuffleStateOn" : "ShuffleStateOff", true);
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

    private async void ArtistTracks_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = (RhythmTrackItem)e.ClickedItem;
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.RhythmTrack.TrackId)
        {
            await page.RhythmPlayer.PlayAlbum(track.RhythmTrack.TrackAlbumId, track.RhythmTrack.TrackId);
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
    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var themeResource = App.Current.Resources["ListViewItemPointerOverBackgroundThemeBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;
        grid.Background = themeResource;
    }

    [RelayCommand]
    public void ShuffleArtist()
    {
        var page = (ShellPage)App.MainWindow.Content;
        page.RhythmPlayer.Shuffle();
        VisualStateManager.GoToState(this, page.RhythmPlayer.IsShuffled ? "ShuffleStateOn" : "ShuffleStateOff", true);
    }

    [RelayCommand]
    public void PlayAll()
    {
        var page = (ShellPage)App.MainWindow.Content;
        var tracks = ViewModel.Tracks.Select(t => t.RhythmTrack.TrackId).ToList();
        page.RhythmPlayer.PlayTracks(tracks.ToArray());
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var themeResource = App.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;
        grid.Background = themeResource;
    }
}
