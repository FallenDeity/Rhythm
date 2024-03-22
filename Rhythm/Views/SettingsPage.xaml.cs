using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Rhythm.Contracts.Services;
using Rhythm.Helpers;
using Rhythm.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Rhythm.Views;


public sealed partial class SettingsPage : Page
{
    private const string DefaultUserImage = "ms-appx:///Assets/User.png";
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
        SaveUsernameButton.IsEnabled = false;
        SavePasswordButton.IsEnabled = false;
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<ILocalSettingsService>().ClearAll();
        App.currentUser = null;
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
        openPicker.FileTypeFilter.Add(".tiff");
        openPicker.FileTypeFilter.Add(".bmp");
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
            var bytes = new byte[buffer.Length];
            dataReader.ReadBytes(bytes);
            if (bytes.Length > 5000000)
            {
                await App.MainWindow.ShowMessageDialogAsync("Image size must be less than 5MB");
                return;
            }
            var format = ImageHelper.GetImageFormat(file.FileType);
            if (format != null)
            {
                var compressed = ImageHelper.CompressImage(bytes, 92, format);
                bytes = bytes.Length > compressed.Length ? compressed : bytes;
            }
            if (ViewModel.currentUser is not null)
            {
                var oldName = ViewModel.currentUser.UserImageFileName;
                var name = $"{ViewModel.currentUser.UserId}{file.FileType}";
                if (!string.IsNullOrEmpty(oldName) && !oldName.Equals(name)) await Task.Run(() => App.GetService<IStorageService>().DeleteAvatar(oldName));
                var url = await Task.Run(() => App.GetService<IStorageService>().UploadAvatar(bytes, name));
                ViewModel.currentUser.UserImageURL = url;
                await Task.Run(() => ViewModel.UpdateUserImage(url));
                UserImage.Source = new BitmapImage(new Uri(url));
            }
        }
    }

    public static string Relativize(DateTime date)
    {
        var span = DateTime.Now - date;
        if (span.Days > 365) return $"about {span.Days / 365} year{(span.Days / 365 == 1 ? "" : "s")} ago";
        if (span.Days > 30) return $"about {span.Days / 30} month{(span.Days / 30 == 1 ? "" : "s")} ago";
        if (span.Days > 0) return $"about {span.Days} day{(span.Days == 1 ? "" : "s")} ago";
        if (span.Hours > 0) return $"about {span.Hours} hour{(span.Hours == 1 ? "" : "s")} ago";
        if (span.Minutes > 0) return $"about {span.Minutes} minute{(span.Minutes == 1 ? "" : "s")} ago";
        return span.Seconds > 5 ? $"about {span.Seconds} seconds ago" : "just now";
    }


    private async Task LoadUserData()
    {
        await Task.Run(() => ViewModel.LoadUserAsync());
        if (ViewModel.currentUser is not null)
        {
            Username.Text = ViewModel.currentUser.UserName;
            CreatedAt.Text = "joined " + Relativize(ViewModel.currentUser.CreatedAt);
            UsernameTextBox.Text = ViewModel.currentUser.UserName;
            ViewModel.UserLoaded = true;
            var url = ViewModel.currentUser.UserImageURL is null ? DefaultUserImage : ViewModel.currentUser.UserImageURL;
            UserImage.Source = new BitmapImage(new Uri(url));
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
        if (ViewModel.currentUser is not null)
        {
            var name = UsernameTextBox.Text;
            await Task.Run(() => ViewModel.UpdateUserName(name));
            Username.Text = ViewModel.currentUser.UserName;
            await App.MainWindow.ShowMessageDialogAsync("Username updated successfully");
        }
    }

    private async void SavePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.currentUser is not null)
        {
            var password = NewPasswordBox.Password;
            await Task.Run(() => ViewModel.UpdateUserPassword(password));
            await App.MainWindow.ShowMessageDialogAsync("Password updated successfully");

        }
    }

    private void UsernameTextChanged(object sender, TextChangedEventArgs e)
    {
        if (UsernameStatus.Text.Contains("invalid"))
        {
            UsernameStatus.Visibility = Visibility.Visible;
            SaveUsernameButton.IsEnabled = false;
        }
    }

    private void PasswordContentChanged(object sender, RoutedEventArgs e)
    {
        if (PasswordStatus.Text.Contains("invalid"))
        {
            PasswordStatus.Visibility = Visibility.Visible;
            SavePasswordButton.IsEnabled = false;
        }
    }

    private async void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog deleteAccountDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Delete Account",
            Content = "Are you sure you want to delete your account? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel"
        };
        ContentDialogResult result = await deleteAccountDialog.ShowAsync();
        if (result == ContentDialogResult.Primary && ViewModel.currentUser is not null)
        {
            await Task.Run(() => ViewModel.DeleteAccount());
            await App.GetService<ILocalSettingsService>().ClearAll();
            App.currentUser = null;
            App.MainWindow.Content = App.GetService<LoginPage>();
        }
    }
}
