using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Contracts.ViewModels;
using Rhythm.Core.Models;
using Rhythm.Helpers;
using Rhythm.Views;
using Windows.ApplicationModel;

namespace Rhythm.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    [ObservableProperty]
    private bool _userLoaded;

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public RhythmUser? currentUser
    {
        get;
        set;
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    public string currentTheme => ElementTheme switch
    {

        ElementTheme.Default => "System",
        ElementTheme.Light => "Light",
        ElementTheme.Dark => "Dark",
        _ => "Unknown"
    };

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public async Task LoadUserAsync()
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        var userId = localSettings.Values["UserId"].ToString();
        if (localSettings.Values["IsAuthenticated"] != null && bool.Parse(localSettings.Values["IsAuthenticated"].ToString() ?? "false") && userId is not null)
        {
            var uid = userId.Replace("\"", "");
            var connection = App.GetService<IDatabaseService>().GetOracleConnection();
            var command = new OracleCommand($"SELECT * FROM users WHERE user_id = :userId", connection);
            command.Parameters.Add(new OracleParameter("userId", uid));
            var reader = await command.ExecuteReaderAsync();
            if (reader.Read())
            {
                currentUser = new RhythmUser
                {
                    UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                    UserName = reader.GetString(reader.GetOrdinal("USERNAME")),
                    UserImageURL = reader.IsDBNull(reader.GetOrdinal("USER_IMAGE_URL")) ? null : reader.GetString(reader.GetOrdinal("USER_IMAGE_URL")),
                    Password = reader.GetString(reader.GetOrdinal("PASSWORD")),
                    Gender = reader.GetValue(reader.GetOrdinal("GENDER")) as string,
                    Country = reader.GetValue(reader.GetOrdinal("COUNTRY")) as string,
                    PlaylistCount = reader.GetInt32(reader.GetOrdinal("PLAYLIST_COUNT")),
                    FavoriteSongCount = reader.GetInt32(reader.GetOrdinal("FAVORITE_SONGS_COUNT")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
                };
            }
            else
            {
                App.MainWindow.Content = App.GetService<LoginPage>();
            }
        }
        else
        {
            App.MainWindow.Content = App.GetService<LoginPage>();
        }
    }

    public async Task UpdateUserImage(string image)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE users SET user_image_url = :image WHERE user_id = :userId", connection);
        command.Parameters.Add(new OracleParameter("image", image));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserName(string name)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE users SET username = :name WHERE user_id = :userId", connection);
        command.Parameters.Add(new OracleParameter("name", name));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateUserPassword(string password)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE users SET password = :password WHERE user_id = :userId", connection);
        command.Parameters.Add(new OracleParameter("password", BCrypt.Net.BCrypt.HashPassword(password)));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAccount()
    {
        var supabase = App.GetService<IStorageService>();
        if (currentUser?.UserImageURL is not null) await supabase.DeleteAvatar(currentUser.UserImageFileName);
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"DELETE FROM users WHERE user_id = :userId", connection);
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        await command.ExecuteNonQueryAsync();
    }

    public void OnNavigatedTo(object parameter)
    {
        ShellPage page = (ShellPage)App.MainWindow.Content;
        page.RhythmPlayer.Visibility = Visibility.Collapsed;
    }

    public void OnNavigatedFrom()
    {
        ShellPage page = (ShellPage)App.MainWindow.Content;
        page.RhythmPlayer.Visibility = Visibility.Visible;
    }
}
