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
public sealed partial class AlbumDetailPage : Page
{
    public static readonly string PageName = "Album Detail";

    public static readonly bool IsPageHidden = true;

    public AlbumDetailViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<RhythmTrackItem> SearchedTracks = new();

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<AlbumDetailViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyArtistGrid", itemHero);
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

    private async void AlbumTracks_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = (RhythmTrackItem)e.ClickedItem;
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.RhythmTrack.TrackId)
        {
            await page.RhythmPlayer.PlayAlbum(track.RhythmTrack.TrackAlbumId, track.RhythmTrack.TrackId);
        }
    }

    private void AddToQueueMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        page.RhythmPlayer.AddToQueue(track.RhythmTrack.TrackId);
    }

    private void ArtistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        if (track.RhythmTrack.Artists is not null && track.RhythmTrack.Artists.Any())
        {
            ViewModel.NavigateToArtist(track.RhythmTrack.Artists[0].ArtistId);
        }
    }

    private async void ToggleLikeButton_Click(object sender, RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        var button = (Button)sender;
        button.IsEnabled = false;
        await ViewModel.ToggleLike(track.RhythmTrack);
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
        button.IsEnabled = true;
    }

    private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

        if (args.ChosenSuggestion != null)
        {
            SearchedTracks = ViewModel.GetSearchAlbums(sender.Text);
            AlbumTracks.ItemsSource = SearchedTracks;
        }
        else
        {
            AlbumTracks.ItemsSource = ViewModel.Tracks;
        }
    }

    private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var suggestions = ViewModel.GetSearchAlbums(sender.Text);
        List<string> suggestionsList = new List<string>();
        foreach (var suggestion in suggestions)
        {
            suggestionsList.Add(suggestion.RhythmTrack.TrackName);
        }
        if (suggestionsList.Count > 0)
        {
            AlbumTracks.ItemsSource = suggestions;
            sender.ItemsSource = suggestionsList;
        }
        else
        {
            sender.ItemsSource = new string[] { "No results found" };
        }
    }

    [RelayCommand]
    public void ShuffleAlbum()
    {
        var page = (ShellPage)App.MainWindow.Content;
        page.RhythmPlayer.Shuffle();
        VisualStateManager.GoToState(this, page.RhythmPlayer.IsShuffled ? "ShuffleStateOn" : "ShuffleStateOff", true);
    }

    [RelayCommand]
    public async Task PlayAll()
    {
        var page = (ShellPage)App.MainWindow.Content;
        await page.RhythmPlayer.PlayAlbum(ViewModel.Item?.AlbumId!);
    }

    [RelayCommand]
    public async Task SaveAlbum()
    {
        await ViewModel.ToggleSave(ViewModel.Item!);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var tooltip = new ToolTip();
        var text = App.SavedAlbumIds.Contains(ViewModel.Item!.AlbumId!) ? "Discard" : "Save";
        tooltip.Content = text;
        ToolTipService.SetToolTip(SaveButton, tooltip);
        var accent = Application.Current.Resources["AccentAAFillColorDefaultBrush"] as SolidColorBrush;
        var normal = Application.Current.Resources["SystemControlForegroundBaseHighBrush"] as SolidColorBrush;
        SaveButton.Content = new FontIcon
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            Foreground = App.SavedAlbumIds.Contains(ViewModel.Item!.AlbumId!) ? accent : normal,
            Glyph = "\uE74E"
        };
    }

    private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        controlsSearchBox.Focus(FocusState.Programmatic);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        while (ViewModel.Item is null) await Task.Delay(10);
        if (ViewModel.Item is not null) UpdateButtons();
    }
}
