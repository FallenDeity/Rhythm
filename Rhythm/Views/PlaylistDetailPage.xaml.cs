using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

    public ObservableCollection<RhythmTrackItem> SearchedTracks = new ObservableCollection<RhythmTrackItem>();

    public PlaylistDetailPage()
    {
        ViewModel = App.GetService<PlaylistDetailViewModel>();
        InitializeComponent();
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

    private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

        if (args.ChosenSuggestion != null)
        {
            SearchedTracks = ViewModel.GetSearchPlaylist(sender.Text);
            PlaylistTracks.ItemsSource = SearchedTracks;
        }
        else
        {
            PlaylistTracks.ItemsSource = ViewModel.Tracks;
        }
    }

    private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var suggestions = ViewModel.GetSearchPlaylist(sender.Text);
        var suggestionsList = new List<string>();
        foreach (var suggestion in suggestions)
        {
            suggestionsList.Add(suggestion.RhythmTrack.TrackName);
        }
        if (suggestionsList.Count > 0)
        {
            PlaylistTracks.ItemsSource = suggestions;
            sender.ItemsSource = suggestionsList;
        }
        else
        {
            sender.ItemsSource = new string[] { "No results found" };
        }
    }

    [RelayCommand]
    public void ShufflePlaylist()
    {
        var page = (ShellPage)App.MainWindow.Content;
        page.RhythmPlayer.Shuffle();
        VisualStateManager.GoToState(this, page.RhythmPlayer.IsShuffled ? "ShuffleStateOn" : "ShuffleStateOff", true);
    }

    [RelayCommand]
    public async Task PlayAll()
    {
        var page = (ShellPage)App.MainWindow.Content;
        await page.RhythmPlayer.PlayPlaylist(ViewModel.Item?.PlaylistId!);
    }

    private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        controlsSearchBox.Focus(FocusState.Programmatic);
    }

    private void UpdateButtons()
    {
        var tooltip = new ToolTip();
        var text = App.FollowedPlaylistIds.Contains(ViewModel.Item!.PlaylistId!) ? "Unfollow" : "Follow";
        tooltip.Content = text;
        ToolTipService.SetToolTip(FollowButton, tooltip);
        var accent = Application.Current.Resources["AccentAAFillColorDefaultBrush"] as SolidColorBrush;
        var normal = Application.Current.Resources["SystemControlForegroundBaseHighBrush"] as SolidColorBrush;
        FollowButton.Content = new FontIcon
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = App.FollowedPlaylistIds.Contains(ViewModel.Item!.PlaylistId!) ? accent : normal,
            Glyph = "\uE8FA"
        };
        tooltip = new ToolTip();
        text = App.LikedPlaylistIds.Contains(ViewModel.Item!.PlaylistId!) ? "Dislike" : "Like";
        tooltip.Content = text;
        ToolTipService.SetToolTip(LikeButton, tooltip);
        LikeButton.Content = new FontIcon
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = App.LikedPlaylistIds.Contains(ViewModel.Item!.PlaylistId!) ? accent : normal,
            Glyph = App.LikedPlaylistIds.Contains(ViewModel.Item!.PlaylistId!) ? "\uEB52" : "\uEB51"
        };
    }

    private async void PlaylistDetails_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Item is not null && ViewModel.Item.PlaylistOwner == App.currentUser!.UserId)
        {
            LikeButton.Visibility = Visibility.Collapsed;
            FollowButton.Visibility = Visibility.Collapsed;
        }
        while (ViewModel.Item is null) await Task.Delay(10);
        if (ViewModel.Item is not null) UpdateButtons();
    }

    [RelayCommand]
    public async Task FollowPlaylist()
    {
        await ViewModel.TogglePlaylistFollow(ViewModel.Item!);
        UpdateButtons();
    }

    [RelayCommand]
    public async Task LikePlaylist()
    {
        await ViewModel.TogglePlaylistLike(ViewModel.Item!);
        UpdateButtons();
    }
}
