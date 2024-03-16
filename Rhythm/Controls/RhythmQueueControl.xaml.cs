using System.Text.RegularExpressions;
using CommunityToolkit.Labs.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Rhythm.Contracts.Services;
using Rhythm.Helpers;
using Rhythm.Views;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Controls;
public sealed partial class RhythmQueueControl : UserControl
{
    public RhythmQueueControl()
    {
        this.InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        var currentTrack = page.RhythmPlayer.TrackId;
        var trackData = await Task.Run(() => App.GetService<IDatabaseService>().GetTrack(currentTrack));
        if (trackData is null) return;
        var album = await Task.Run(() => App.GetService<IDatabaseService>().GetAlbum(trackData.TrackAlbumId));
        NowPlayingCover.Source = album?.AlbumImageURL is null ? null : new BitmapImage(new Uri(album.AlbumImageURL));
        NowPlayingTitle.Text = trackData.TrackName;
        NowPlayingArtist.Text = page.RhythmPlayer.GetTrackArtist();
        var queue = page.RhythmPlayer.GetQueue();
        QueueScrollViewer.Visibility = queue.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        QueueSeparator.Visibility = queue.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpcomingTitle.Visibility = queue.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        var listBox = new ListBox
        {
            Background = new SolidColorBrush(Colors.Transparent),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            SelectionMode = (SelectionMode)ListViewSelectionMode.None
        };
        listBox.ItemContainerStyle = new Style(typeof(ListBoxItem))
        {
            Setters =
            {
                new Setter(ListBoxItem.AllowFocusOnInteractionProperty, false),
            }
        };
        if (queue.Length > 0)
        {
            foreach (var track in queue)
            {
                var skeleton = Skeleton();
                listBox.Items.Add(skeleton);
            }
            QueueScrollViewer.Content = listBox;
        }
        // await App.GetService<IDatabaseService>().GetTracks(queue);
        var idx = 0;
        foreach (var track in queue)
        {
            var trackComponent = await BuildTrackComponent(track);
            if (trackComponent is not null)
            {
                listBox.Items[idx++] = trackComponent;
                QueueScrollViewer.Content = listBox;
            }
        }
        QueueScrollViewer.Content = listBox;
    }

    private async Task<StackPanel?> BuildTrackComponent(string trackId)
    {
        var trackData = await Task.Run(() => App.GetService<IDatabaseService>().GetTrack(trackId));
        if (trackData is null) return null;
        var albumData = await Task.Run(() => App.GetService<IDatabaseService>().GetAlbum(trackData.TrackAlbumId));
        if (albumData is null) return null;
        // var cover = await App.GetService<IDatabaseService>().GetAlbumCover(trackData.TrackAlbumId, true);
        // var img = await Task.Run(() => App.GetService<IDatabaseService>().GetAlbumCover(trackData.TrackAlbumId));
        var color = (Color)Application.Current.Resources["LayerOnAcrylicFillColorDefault"];
        var imgGrid = new Grid
        {
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(6),
            Width = 64,
            Height = 64
        };
        var imgSource = albumData.AlbumImageURL is null ? null : new BitmapImage(new Uri(albumData.AlbumImageURL));
        // new BitmapImage(new Uri("ms-appx:///Assets/track.jpeg"));
        var imgControl = new Image
        {
            Source = imgSource,
            Width = 64,
            Height = 64
        };
        imgGrid.Children.Add(imgControl);
        var infoStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 180
        };
        var title = new TextBlock
        {
            Text = trackData.TrackName,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        var duration = Regex.Match(trackData.TrackDuration, @"(\d+):(\d+):(\d+).(\d+)").Groups;
        var dText = $"{duration[2]}:{duration[3]} - {albumData.AlbumName}";
        var durationText = new TextBlock
        {
            Text = dText,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.Light,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0),
        };
        infoStackPanel.Children.Add(title);
        infoStackPanel.Children.Add(durationText);
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stackPanel.Children.Add(imgGrid);
        stackPanel.Children.Add(infoStackPanel);
        return stackPanel;
    }

    public StackPanel Skeleton()
    {
        var shimmerImage = new Shimmer
        {
            Width = 64,
            Height = 64,
            Margin = new Thickness(0, 0, 10, 0),
            CornerRadius = new CornerRadius(6),
        };
        var infoStackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 180
        };
        var random = new Random();
        var title = new Shimmer
        {
            Width = random.Next(100, 180),
            Height = 15,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        var durationText = new Shimmer
        {
            Width = random.Next(50, 100),
            Height = 12,
            Margin = new Thickness(0, 5, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        infoStackPanel.Children.Add(title);
        infoStackPanel.Children.Add(durationText);
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stackPanel.Children.Add(shimmerImage);
        stackPanel.Children.Add(infoStackPanel);
        return stackPanel;
    }
}
