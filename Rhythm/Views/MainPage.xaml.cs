using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
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
    }

    private async void Button_ClickAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        // page.RhythmPlayer.PlayTrack("24daa65d-0a78-427c-9533-8d14f7ca9c17");
        await page.RhythmPlayer.PlayAlbum("a01f3cb4-6165-4669-8d0c-c48c1bbcff5f");
        // page.RhythmPlayer.TrackId = "24daa65d-0a78-427c-9533-8d14f7ca9c17";
    }
}
