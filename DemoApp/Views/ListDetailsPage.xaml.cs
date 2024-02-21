using CommunityToolkit.WinUI.UI.Controls;
using DemoApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace DemoApp.Views;

public sealed partial class ListDetailsPage : Page
{
    public static readonly string PageName = "List Details";

    public static readonly bool IsPageHidden = false;

    public ListDetailsViewModel ViewModel
    {
        get;
    }

    public ListDetailsPage()
    {
        ViewModel = App.GetService<ListDetailsViewModel>();
        InitializeComponent();
    }

    private void OnViewStateChanged(object sender, ListDetailsViewState e)
    {
        if (e == ListDetailsViewState.Both)
        {
            ViewModel.EnsureItemSelected();
        }
    }
}
