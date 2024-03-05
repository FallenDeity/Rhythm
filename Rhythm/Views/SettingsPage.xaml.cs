using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Helpers;
using Rhythm.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

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

    private async void PickAPhotoButton_Click(object sender, RoutedEventArgs e)
    {
        var openPicker = new FileOpenPicker();
        var window = App.MainWindow;
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        openPicker.FileTypeFilter.Add(".jpg");
        openPicker.FileTypeFilter.Add(".jpeg");
        openPicker.FileTypeFilter.Add(".png");
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            // read file as byte array
            var buffer = await FileIO.ReadBufferAsync(file);
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
            var bytes = new byte[buffer.Length];
            dataReader.ReadBytes(bytes);
            UserImage.Source = await BitmapHelper.GetBitmapAsync(bytes);
            if (ViewModel.currentUser is not null)
            {

                ViewModel.currentUser.UserImage = bytes;
                await Task.Run(() => ViewModel.UpdateUserImage(bytes));
            }
        }
    }

    public static string relativize(DateTime date)
    {
        var span = DateTime.Now - date;
        if (span.Days > 365)
        {
            var years = span.Days / 365;
            if (span.Days % 365 != 0)
            {
                years += 1;
            }
            return $"about {years} {(years == 1 ? "year" : "years")} ago";
        }
        if (span.Days > 30)
        {
            var months = span.Days / 30;
            if (span.Days % 31 != 0)
            {
                months += 1;
            }
            return $"about {months} {(months == 1 ? "month" : "months")} ago";
        }
        if (span.Days > 0)
        {
            return $"about {span.Days} {(span.Days == 1 ? "day" : "days")} ago";
        }
        if (span.Hours > 0)
        {
            return $"about {span.Hours} {(span.Hours == 1 ? "hour" : "hours")} ago";
        }
        if (span.Minutes > 0)
        {
            return $"about {span.Minutes} {(span.Minutes == 1 ? "minute" : "minutes")} ago";
        }
        if (span.Seconds > 5)
        {
            return $"about {span.Seconds} seconds ago";
        }
        return "just now";
    }

    private async Task LoadUserData()
    {
        await Task.Run(() => ViewModel.LoadUser());
        if (ViewModel.currentUser is not null)
        {
            if (ViewModel.currentUser.UserImage.Length > 0)
            {
                var bitmap = await BitmapHelper.GetBitmapAsync(ViewModel.currentUser.UserImage);
                UserImage.Source = bitmap;
            }
            Username.Text = ViewModel.currentUser.UserName;
            CreatedAt.Text = "joined " + relativize(ViewModel.currentUser.CreatedAt);
            ViewModel.UserLoaded = true;
        }
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await LoadUserData();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.currentUser is not null)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(ViewModel.currentUser.UserId);
            Clipboard.SetContent(dataPackage);
            App.GetService<IAppNotificationService>().Show("UserIdCopiedNotification".GetLocalized());
        }
        else
        {
            App.GetService<IAppNotificationService>().Show("User ID not found");
        }
    }

    private async void SaveUsernameButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsernameTextBox.Text.Length == 0)
        {
            await App.MainWindow.ShowMessageDialogAsync("Username cannot be empty");
            return;
        }
        if (ViewModel.currentUser is not null)
        {
            var name = UsernameTextBox.Text;
            await Task.Run(() => ViewModel.UpdateUserName(name));
            ViewModel.currentUser.UserName = UsernameTextBox.Text;
            Username.Text = ViewModel.currentUser.UserName;
            UsernameTextBox.Text = "";
        }
    }

    private async void SavePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.currentUser is not null)
        {
            var password = NewPasswordBox.Password;
            await Task.Run(() => ViewModel.UpdateUserPassword(password));
            ViewModel.currentUser.Password = NewPasswordBox.Password;
            NewPasswordBox.Password = "";
            await App.MainWindow.ShowMessageDialogAsync("Password updated successfully");

        }
        else
        {

            await App.MainWindow.ShowMessageDialogAsync("User not found");
        }
    }
}
