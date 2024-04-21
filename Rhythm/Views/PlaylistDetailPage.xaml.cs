using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.ViewModels;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;


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

    private void AlbumMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
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

    private async void AddToPlaylistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var userId = App.currentUser!.UserId;
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        var userPlaylists = await Task.Run(() => App.GetService<IDatabaseService>().GetUserPlaylists(userId));
        var dialog = new AddToPlaylistDialog(new List<RhythmPlaylist>(userPlaylists), track.RhythmTrack);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private async void RemoveFromPlaylistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        if (ViewModel.Item!.PlaylistOwner != App.currentUser!.UserId)
        {
            var info = new InfoBar
            {
                Message = "You can only remove tracks from your own playlists",
                Severity = InfoBarSeverity.Warning,
                IsOpen = true,
            };
            page.InfoBarStackPanel.Children.Add(info);
            return;
        }
        var track = ((RhythmTrackItem)((FrameworkElement)sender).DataContext).RhythmTrack;
        var playlistId = ViewModel.Item!.PlaylistId;
        var trackId = track.TrackId;
        await Task.Run(() => App.GetService<IDatabaseService>().RemoveTrackFromPlaylist(playlistId!, trackId));
        var copy = new List<RhythmTrackItem>(ViewModel.Tracks);
        ViewModel.Tracks.Clear();
        var count = 1;
        foreach (var t in copy)
        {
            if (t.RhythmTrack.TrackId != trackId)
            {
                t.RhythmTrack.Count = count++;
                ViewModel.Tracks.Add(t);
            }
        }
        var infoBar = new InfoBar
        {
            Message = $"{track.TrackName} removed from {ViewModel.Item!.PlaylistName}",
            Severity = InfoBarSeverity.Error,
            IsOpen = true,
        };
        page.InfoBarStackPanel.Children.Add(infoBar);
    }
}

public class AddToPlaylistDialog : ContentDialog
{
    public AddToPlaylistDialog(List<RhythmPlaylist> playlists, RhythmTrack track)
    {
        Title = "Add to Playlist";
        var comboBox = new ComboBox
        {
            MaxDropDownHeight = 200,
            Width = 300,
            PlaceholderText = "Select a playlist",
            ItemsSource = playlists,
            DisplayMemberPath = "PlaylistName",
            SelectedValuePath = "PlaylistId",
            Margin = new Thickness(0, 10, 0, 0)
        };
        Content = comboBox;
        PrimaryButtonText = "Add";
        PrimaryButtonClick += async (sender, e) =>
        {
            var playlistId = (string)comboBox.SelectedValue;
            var playlist = playlists.First(p => p.PlaylistId == playlistId);
            var trackId = track.TrackId;
            await Task.Run(() => App.GetService<IDatabaseService>().AddTrackToPlaylist(playlistId, trackId));
            var page = (ShellPage)App.MainWindow.Content;
            var info = new InfoBar
            {
                Message = $"{track.TrackName} added to {playlist.PlaylistName}",
                Severity = InfoBarSeverity.Success,
                IsOpen = true,
            };
            page.InfoBarStackPanel.Children.Add(info);
            Hide();
        };
        SecondaryButtonText = "Cancel";
        SecondaryButtonClick += (sender, e) => Hide();
    }
}

public class CreatePlaylistDialog : ContentDialog
{
    public bool uploadedImage
    {
        get;
        set;
    }

    public byte[] ImageBytes
    {
        get;
        set;
    }

    public CreatePlaylistDialog()
    {
        Title = "Create Playlist";
        var grid = new Grid();
        grid.ColumnSpacing = 20;
        grid.Margin = new Thickness(0, 20, 0, 0);
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(leftStackPanel);
        Grid.SetColumn(leftStackPanel, 0);

        var rightStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        grid.Children.Add(rightStackPanel);
        Grid.SetColumn(rightStackPanel, 1);

        var imageGrid = new Grid()
        {
            Width = 120,
            Height = 120,
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var playlistImage = new Image
        {
            Source = new BitmapImage(new Uri("ms-appx:///Assets/Track.jpeg")),
            Width = 120,
            Height = 120,
        };
        imageGrid.Children.Add(playlistImage);
        leftStackPanel.Children.Add(imageGrid);

        var selectImageButton = new Button
        {
            Content = "Select Image",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        selectImageButton.Click += async (sender, e) =>
        {
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var dialog = new FileOpenPicker()
            {
                FileTypeFilter = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" },
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            WinRT.Interop.InitializeWithWindow.Initialize(dialog, hwnd);
            var file = await dialog.PickSingleFileAsync();
            if (file is not null)
            {
                var bitmap = new BitmapImage();
                bitmap.SetSource(await file.OpenAsync(FileAccessMode.Read));
                playlistImage.Source = bitmap;
                var buffer = await FileIO.ReadBufferAsync(file);
                ImageBytes = new byte[buffer.Length];
                using (var reader = DataReader.FromBuffer(buffer))
                {
                    reader.ReadBytes(ImageBytes);
                }
                uploadedImage = true;
            }
        };
        leftStackPanel.Children.Add(selectImageButton);

        var playlistNameTextBox = new TextBox
        {
            VerticalAlignment = VerticalAlignment.Top,
            PlaceholderText = "Playlist Name",
            Width = 300,
            Margin = new Thickness(0, 0, 0, 10)
        };
        rightStackPanel.Children.Add(playlistNameTextBox);

        var playlistDescriptionTextBox = new TextBox
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            PlaceholderText = "Playlist Description",
            Width = 300,
            Height = 100,
            Margin = new Thickness(0, 20, 0, 0)
        };
        rightStackPanel.Children.Add(playlistDescriptionTextBox);

        Content = grid;
        PrimaryButtonText = "Create";
        PrimaryButtonClick += async (sender, e) =>
        {
            var page = (ShellPage)App.MainWindow.Content;
            if (string.IsNullOrWhiteSpace(playlistNameTextBox.Text) || string.IsNullOrWhiteSpace(playlistDescriptionTextBox.Text))
            {
                var infoBar = new InfoBar
                {
                    Severity = InfoBarSeverity.Error,
                    Message = "Playlist name and description are required",
                    IsOpen = true
                };
                page.InfoBarStackPanel.Children.Add(infoBar);
                return;
            }

            IsPrimaryButtonEnabled = false;
            var playlistId = Guid.NewGuid().ToString();
            var playlistImageURL = "ms-appx:///Assets/Track.jpeg";
            if (uploadedImage && ImageBytes is not null)
            {
                playlistImageURL = await Task.Run(() => App.GetService<IStorageService>().UploadPlaylistImage(ImageBytes, playlistId));
            }
            var playlistOwner = App.currentUser!.UserId;
            var playlist = new RhythmPlaylist
            {
                PlaylistId = playlistId,
                PlaylistName = playlistNameTextBox.Text,
                PlaylistDescription = playlistDescriptionTextBox.Text,
                PlaylistImageURL = playlistImageURL,
                PlaylistOwner = playlistOwner
            };
            await Task.Run(() => App.GetService<IDatabaseService>().CreatePlaylist(playlist));
            var info = new InfoBar
            {
                Message = $"Playlist '{playlistNameTextBox.Text}' created",
                Severity = InfoBarSeverity.Success,
                IsOpen = true,
            };
            page.InfoBarStackPanel.Children.Add(info);
            IsPrimaryButtonEnabled = true;
            Hide();
        };
        SecondaryButtonText = "Cancel";
        SecondaryButtonClick += (sender, e) => Hide();
    }
}
