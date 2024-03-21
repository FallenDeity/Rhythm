using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Activation;
using Rhythm.Contracts.Services;
using Rhythm.Core.Models;
using Rhythm.Views;

namespace Rhythm.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Connect to database
        var connected = App.GetService<IDatabaseService>().ConnectToOracle();
        if (!connected)
        {
            App.MainWindow.Content = App.GetService<LoginPage>();
            App.MainWindow.Activate();
            await StartupAsync();
            return;
        }

        // Connect to storage
        connected = await App.GetService<IStorageService>().ConnectToSupabase();
        if (!connected)
        {
            App.MainWindow.Content = App.GetService<LoginPage>();
            App.MainWindow.Activate();
            await StartupAsync();
            return;
        }

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var user = localSettings.Values["UserId"];
            var auth = localSettings.Values["IsAuthenticated"];
            if (auth != null && bool.Parse(auth.ToString() ?? "false") && user != null)
            {
                var userId = user.ToString()?.Replace("\"", "");
                var connection = App.GetService<IDatabaseService>().GetOracleConnection();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE user_id = :userId";
                command.Parameters.Add(new OracleParameter("userId", userId));
                var reader = await command.ExecuteReaderAsync();
                if (reader.Read())
                {
                    var userData = new RhythmUser
                    {
                        UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                        UserName = reader.GetString(reader.GetOrdinal("USERNAME")),
                        Password = reader.GetString(reader.GetOrdinal("PASSWORD")),
                        UserImageURL = reader.IsDBNull(reader.GetOrdinal("USER_IMAGE_URL")) ? null : reader.GetString(reader.GetOrdinal("USER_IMAGE_URL")),
                        Gender = reader.GetValue(reader.GetOrdinal("GENDER")) as string,
                        Country = reader.GetValue(reader.GetOrdinal("COUNTRY")) as string,
                        PlaylistCount = reader.GetInt32(reader.GetOrdinal("PLAYLIST_COUNT")),
                        FavoriteSongCount = reader.GetInt32(reader.GetOrdinal("FAVORITE_SONGS_COUNT")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
                    };
                    App.currentUser = userData;
                    _shell = App.GetService<ShellPage>();
                }
                else
                {
                    await App.GetService<ILocalSettingsService>().ClearAll();
                    _shell = App.GetService<LoginPage>();
                }
                _shell = App.GetService<ShellPage>();
            }
            else
            {
                _shell = App.GetService<LoginPage>();
            }
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }
}
