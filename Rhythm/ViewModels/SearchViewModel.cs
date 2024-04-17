using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;

namespace Rhythm.ViewModels;

public struct SelectionToken
{
    public string Text
    {
        get; set;
    }
    public Symbol Icon
    {
        get; set;
    }
}

public partial class SearchViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    public ObservableCollection<SelectionToken> SelectedTokens
    {
        get; set;
    } = new ObservableCollection<SelectionToken>();

    public readonly List<SelectionToken> Tokens = new()
    {
        new SelectionToken { Text = "Track", Icon = Symbol.MusicInfo },
        new SelectionToken { Text = "Artist", Icon = Symbol.People },
        new SelectionToken { Text = "Album", Icon = Symbol.Folder },
        new SelectionToken { Text = "Playlist", Icon = Symbol.Library },
        new SelectionToken { Text = "User", Icon = Symbol.Account },
    };

    public SearchViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }

    public void NavigateToAlbum(string albumId)
    {
        _navigationService.NavigateTo(typeof(AlbumDetailViewModel).FullName!, albumId);
    }

    public void NavigateToArtist(string artistId)
    {
        _navigationService.NavigateTo(typeof(ArtistDetailViewModel).FullName!, artistId);
    }

    public void NavigateToPlaylist(string playlistId)
    {
        _navigationService.NavigateTo(typeof(PlaylistDetailViewModel).FullName!, playlistId);
    }
}
