using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Core.Models;
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

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<AlbumDetailViewModel>();
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

    private async void AlbumTracks_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = (RhythmTrack)e.ClickedItem;
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.TrackId)
        {
            await page.RhythmPlayer.PlayAlbum(track.TrackAlbumId, track.TrackId);
        }
    }
}
