using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Core.Models;
using Rhythm.Helpers;
using Rhythm.Views;
using Windows.ApplicationModel;

namespace Rhythm.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

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

    public void LoadUser()
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        var userId = localSettings.Values["UserId"].ToString();
        if (localSettings.Values["IsAuthenticated"] != null && bool.Parse(localSettings.Values["IsAuthenticated"].ToString() ?? "false") && userId is not null)
        {
            var uid = userId.Replace("\"", "");
            var connection = App.GetService<IDatabaseService>().GetOracleConnection();
            var command = new OracleCommand($"SELECT * FROM USERS WHERE USER_ID = :userId", connection);
            command.Parameters.Add(new OracleParameter("userId", uid));
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                currentUser = new RhythmUser
                {
                    UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                    UserName = reader.GetString(reader.GetOrdinal("USERNAME")),
                    Password = reader.GetString(reader.GetOrdinal("PASSWORD")),
                    Gender = reader.GetString(reader.GetOrdinal("GENDER")),
                    Country = reader.GetString(reader.GetOrdinal("COUNTRY")),
                    UserImage = reader.GetValue(reader.GetOrdinal("USER_IMAGE")) as byte[],
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

    public void UpdateUserImage(byte[] image)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE USERS SET USER_IMAGE = :image WHERE USER_ID = :userId", connection);
        command.Parameters.Add(new OracleParameter("image", image));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        command.ExecuteNonQuery();
    }

    public void UpdateUserName(string name)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE USERS SET USERNAME = :name WHERE USER_ID = :userId", connection);
        command.Parameters.Add(new OracleParameter("name", name));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        command.ExecuteNonQuery();
    }

    public void UpdateUserPassword(string password)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = new OracleCommand($"UPDATE USERS SET PASSWORD = :password WHERE USER_ID = :userId", connection);
        command.Parameters.Add(new OracleParameter("password", password));
        command.Parameters.Add(new OracleParameter("userId", currentUser?.UserId));
        command.ExecuteNonQuery();
    }
}
