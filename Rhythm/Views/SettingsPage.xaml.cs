using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rhythm.Contracts.Services;
using Rhythm.ViewModels;

namespace Rhythm.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    public static readonly string PageName = "Settings";

    public static readonly bool IsPageHidden = false;

    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedText = ((ComboBoxItem)e.AddedItems[0]).Content.ToString();
        var theme = selectedText switch
        {

            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        ViewModel.SwitchThemeCommand.Execute(theme);
    }


    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var theme = ViewModel.currentTheme;
        switch (theme)
        {
            case "Light":
                ThemeComboBox.SelectedIndex = 0;
                break;
            case "Dark":
                ThemeComboBox.SelectedIndex = 1;
                break;
            default:
                ThemeComboBox.SelectedIndex = 2;
                break;
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<ILocalSettingsService>().ClearAll();
        App.MainWindow.Content = App.GetService<LoginPage>();
    }

}
