using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rhythm.Controls;
using Rhythm.Core.Models;
using Rhythm.Helpers;
using Rhythm.ViewModels;

namespace Rhythm.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel
    {
        get;
    }

    public static readonly string PageName = "Search";

    public static readonly bool IsPageHidden = false;

    private readonly IncrementalLoadingCollection<IncrementalTrackLoader, RhythmTrackItem> trackCollection;
    private readonly IncrementalLoadingCollection<IncrementalArtistLoader, RhythmArtist> artistCollection;
    private readonly IncrementalLoadingCollection<IncrementalAlbumLoader, RhythmAlbum> albumCollection;
    private readonly IncrementalLoadingCollection<IncrementalPlaylistLoader, RhythmPlaylist> playlistCollection;
    private readonly IncrementalLoadingCollection<IncrementalUserLoader, RhythmUser> userCollection;

    private readonly Dictionary<string, ListView> sources = new();


    public SearchPage()
    {
        ViewModel = App.GetService<SearchViewModel>();
        this.InitializeComponent();
        trackCollection = new IncrementalLoadingCollection<IncrementalTrackLoader, RhythmTrackItem>(15, null, null, null);
        artistCollection = new IncrementalLoadingCollection<IncrementalArtistLoader, RhythmArtist>(15, null, null, null);
        albumCollection = new IncrementalLoadingCollection<IncrementalAlbumLoader, RhythmAlbum>(15, null, null, null);
        playlistCollection = new IncrementalLoadingCollection<IncrementalPlaylistLoader, RhythmPlaylist>(15, null, null, null);
        userCollection = new IncrementalLoadingCollection<IncrementalUserLoader, RhythmUser>(15, null, null, null);
    }

    private void TokenItemCreating(object sender, TokenItemAddingEventArgs e)
    {
        e.Item = ViewModel.Tokens.FirstOrDefault((item) => item.Text!.Contains(e.TokenText, StringComparison.CurrentCultureIgnoreCase));
        e.Item ??= new SelectionToken()
        {
            Text = e.TokenText,
            Icon = Symbol.OutlineStar
        };
    }

    private void SearchDetails_Loaded(object sender, RoutedEventArgs e)
    {
        SearchTracks.ItemsSource = trackCollection;
        SearchArtists.ItemsSource = artistCollection;
        SearchAlbums.ItemsSource = albumCollection;
        SearchPlaylists.ItemsSource = playlistCollection;
        SearchUsers.ItemsSource = userCollection;
        sources.Add("Track", SearchTracks);
        sources.Add("Artist", SearchArtists);
        sources.Add("Album", SearchAlbums);
        sources.Add("Playlist", SearchPlaylists);
        sources.Add("User", SearchUsers);
        for (var i = 0; i < sources.Count; i++)
        {
            sources.ElementAt(i).Value.Visibility = Visibility.Collapsed;
        }
        SearchTracks.Visibility = Visibility.Visible;
    }

    private async void TokenBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ViewModel.SelectedTokens.Count == 0)
        {
            IncrementalTrackLoader.queryString = TokenBox.Text;
            await trackCollection.RefreshAsync();
            return;
        }
        var currentToken = ViewModel.SelectedTokens.FirstOrDefault();
        switch (currentToken.Text)
        {

            case "Track":
                IncrementalTrackLoader.queryString = TokenBox.Text;
                await trackCollection.RefreshAsync();
                break;
            case "Artist":
                IncrementalArtistLoader.queryString = TokenBox.Text;
                await artistCollection.RefreshAsync();
                break;
            case "Album":
                IncrementalAlbumLoader.queryString = TokenBox.Text;
                await albumCollection.RefreshAsync();
                break;
            case "Playlist":
                IncrementalPlaylistLoader.queryString = TokenBox.Text;
                await playlistCollection.RefreshAsync();
                break;
            case "User":
                IncrementalUserLoader.queryString = TokenBox.Text;
                await userCollection.RefreshAsync();
                break;
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

    private void ArtistMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var track = (RhythmTrackItem)((FrameworkElement)sender).DataContext;
        if (track.RhythmTrack.Artists is not null && track.RhythmTrack.Artists.Any())
        {
            ViewModel.NavigateToArtist(track.RhythmTrack.Artists[0].ArtistId);
        }
    }

    private void SearchTracks_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = (RhythmTrackItem)e.ClickedItem;
        var page = (ShellPage)App.MainWindow.Content;
        if (page.RhythmPlayer.TrackId != track.RhythmTrack.TrackId)
        {
            page.RhythmPlayer.PlayTrack(track.RhythmTrack.TrackId);
        }
    }

    private void SearchAlbums_ItemClick(object sender, ItemClickEventArgs e)
    {
        var album = (RhythmAlbum)e.ClickedItem;
        ViewModel.NavigateToAlbum(album.AlbumId);
    }

    private void SearchArtists_ItemClick(object sender, ItemClickEventArgs e)
    {
        var artist = (RhythmArtist)e.ClickedItem;
        ViewModel.NavigateToArtist(artist.ArtistId);
    }

    private void SearchPlaylists_ItemClick(object sender, ItemClickEventArgs e)
    {
        var playlist = (RhythmPlaylist)e.ClickedItem;
        ViewModel.NavigateToPlaylist(playlist.PlaylistId);
    }

    private void SearchUsers_ItemClick(object sender, ItemClickEventArgs e)
    {
    }

    private void TokenBox_TokenItemRemoved(TokenizingTextBox sender, object args)
    {
        for (var i = 0; i < sources.Count; i++)
        {
            sources.ElementAt(i).Value.Visibility = Visibility.Collapsed;
        }
        SearchTracks.Visibility = Visibility.Visible;
    }

    private void TokenBox_TokenItemAdded(TokenizingTextBox sender, object args)
    {
        var token = (SelectionToken)args;
        for (var i = 0; i < sources.Count; i++)
        {
            if (sources.ElementAt(i).Key == token.Text)
            {
                sources.ElementAt(i).Value.Visibility = Visibility.Visible;
            }
            else
            {
                sources.ElementAt(i).Value.Visibility = Visibility.Collapsed;
            }
        }
    }
}
