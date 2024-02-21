using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

    private void Button_ClickAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var t = Task.Run(() =>
               {
                   var x = 0;
                   var y = 3 / x;
                   return y;
               });
        myButton.Content = t.Result.ToString();
    }
}
