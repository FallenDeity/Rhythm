using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rhythm.ViewModels;

namespace Rhythm.Views;

public sealed partial class MainPage : Page
{
    public static readonly string PageName = "Home";

    public static readonly bool IsPageHidden = false;

    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        RecommendedAlbums.ItemContainerStyle = new Style(typeof(GridViewItem))
        {
            Setters =
            {
                new Setter(GridViewItem.MarginProperty, new Thickness(8)),
            }
        };
        RecommendedArtists.ItemContainerStyle = new Style(typeof(GridViewItem))
        {
            Setters =
            {
                new Setter(GridViewItem.MarginProperty, new Thickness(8)),
            }
        };
        RecommendedPlaylists.ItemContainerStyle = new Style(typeof(GridViewItem))
        {
            Setters =
            {
                new Setter(GridViewItem.MarginProperty, new Thickness(8)),
            }
        };
    }

    private async void Button_ClickAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        /*
         * <Grid x:Name="ContentArea">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <Image
            Grid.Row="0"
            Height="400"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Source="/Assets/LargeTile.scale-400.png" />
        <Button
            x:Name="myButton"
            Grid.Row="1"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Click="Button_ClickAsync"
            Content="Standard XAML button" />
    </Grid>
        /*
        // page.RhythmPlayer.PlayTrack("24daa65d-0a78-427c-9533-8d14f7ca9c17");
        // await page.RhythmPlayer.PlayAlbum("cc0cfd78-4171-4fcf-b44d-3d1481afb3e6");
        // page.RhythmPlayer.TrackId = "24daa65d-0a78-427c-9533-8d14f7ca9c17";
        // 64c30c7e-b35c-4a51-a58a-ecc1cc7eb473 playlist 2
        // 41ba0323-1935-4b94-996f-4bc186ebf9f0 playlist 1
        // d2a35489-72eb-4b83-ae81-fbb35e3119d6 playlist 3
        // await page.RhythmPlayer.PlayPlaylist("d2a35489-72eb-4b83-ae81-fbb35e3119d6");
        */
        var page = (ShellPage)App.MainWindow.Content;
        await page.RhythmPlayer.PlayAlbum("a01f3cb4-6165-4669-8d0c-c48c1bbcff5f", "77915f09-a18c-43fe-8189-0204e418e1ad");
    }

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var themeResource = App.Current.Resources["ListViewItemPointerOverBackgroundThemeBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;
        grid.Background = themeResource;
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var themeResource = App.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush;
        grid.Background = themeResource;
    }
}
