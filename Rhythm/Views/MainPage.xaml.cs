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
        // await page.RhythmPlayer.PlayAlbum("a01f3cb4-6165-4669-8d0c-c48c1bbcff5f");
        // await page.RhythmPlayer.PlayAlbum("cc0cfd78-4171-4fcf-b44d-3d1481afb3e6");
        // page.RhythmPlayer.TrackId = "24daa65d-0a78-427c-9533-8d14f7ca9c17";
        // 64c30c7e-b35c-4a51-a58a-ecc1cc7eb473 playlist 2
        // 41ba0323-1935-4b94-996f-4bc186ebf9f0 playlist 1
        // d2a35489-72eb-4b83-ae81-fbb35e3119d6 playlist 3
        await page.RhythmPlayer.PlayPlaylist("41ba0323-1935-4b94-996f-4bc186ebf9f0");
    }
}
