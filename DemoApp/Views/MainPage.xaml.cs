using DemoApp.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace DemoApp.Views;

public sealed partial class MainPage : Page
{
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
