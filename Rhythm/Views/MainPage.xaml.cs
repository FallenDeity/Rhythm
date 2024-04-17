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
