using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Activation;
using Rhythm.Contracts.Services;
using Rhythm.Core.Contracts.Services;
using Rhythm.Core.Models;
using Rhythm.Core.Services;
using Rhythm.Helpers;
using Rhythm.Models;
using Rhythm.Notifications;
using Rhythm.Services;
using Rhythm.ViewModels;
using Rhythm.Views;

namespace Rhythm;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging

    public IHost Host
    {
        get;
    }

    public static RhythmUser? currentUser
    {
        get;
        set;
    }

    public static string[] LikedSongIds
    {
        get;
        set;
    } = Array.Empty<string>();

    public static string[] FollowedArtistIds
    {
        get;
        set;
    } = Array.Empty<string>();

    public static string[] LikedPlaylistIds
    {
        get;
        set;
    } = Array.Empty<string>();

    public static string[] FollowedPlaylistIds
    {
        get;
        set;
    } = Array.Empty<string>();

    public static string[] SavedAlbumIds
    {
        get;
        set;
    } = Array.Empty<string>();

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IStorageService, StorageService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<LoginPage>();
            services.AddTransient<RegisterPage>();
            services.AddTransient<AlbumDetailViewModel>();
            services.AddTransient<AlbumDetailPage>();
            services.AddTransient<PlaylistDetailViewModel>();
            services.AddTransient<PlaylistDetailPage>();
            services.AddTransient<ArtistDetailViewModel>();
            services.AddTransient<ArtistDetailPage>();
            services.AddTransient<TrackDetailViewModel>();
            services.AddTransient<TrackDetailPage>();
            services.AddTransient<SearchViewModel>();
            services.AddTransient<SearchPage>();
            services.AddTransient<LibraryViewModel>();
            services.AddTransient<LibraryPage>();


            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    [STAThread]
    static async Task Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        var isRedirect = await DecideRedirection();
        if (!isRedirect)
        {
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
    }

    public static async Task LoadUser(string userId)
    {
        var db = App.GetService<IDatabaseService>().GetOracleConnection();
        var cmd = new OracleCommand("SELECT * FROM users WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        var reader = await cmd.ExecuteReaderAsync();
        if (reader.Read())
        {
            currentUser = new RhythmUser
            {
                UserId = reader.GetString(reader.GetOrdinal("USER_ID")),
                UserName = reader.GetString(reader.GetOrdinal("USERNAME")),
                Password = reader.GetString(reader.GetOrdinal("PASSWORD")),
                UserImageURL = reader.IsDBNull(reader.GetOrdinal("USER_IMAGE_URL")) ? null : reader.GetString(reader.GetOrdinal("USER_IMAGE_URL")),
                Gender = reader.GetValue(reader.GetOrdinal("GENDER")) as string,
                Country = reader.GetValue(reader.GetOrdinal("COUNTRY")) as string,
                PlaylistCount = reader.GetInt32(reader.GetOrdinal("PLAYLIST_COUNT")),
                FavoriteSongCount = reader.GetInt32(reader.GetOrdinal("FAVORITE_SONGS_COUNT")),
                SavedAlbumCount = reader.GetInt32(reader.GetOrdinal("SAVED_ALBUMS_COUNT")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CREATED_AT")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UPDATED_AT"))
            };
        }
        cmd = new OracleCommand("SELECT track_id FROM user_favorite_songs WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        reader = await cmd.ExecuteReaderAsync();
        var likedSongs = new List<string>();
        while (reader.Read())
        {
            likedSongs.Add(reader.GetString(0));
        }
        LikedSongIds = likedSongs.ToArray();
        cmd = new OracleCommand("SELECT artist_id FROM artist_followers WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        reader = await cmd.ExecuteReaderAsync();
        var followedArtists = new List<string>();
        while (reader.Read())
        {
            followedArtists.Add(reader.GetString(0));
        }
        FollowedArtistIds = followedArtists.ToArray();
        cmd = new OracleCommand("SELECT playlist_id FROM playlist_likes WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        reader = await cmd.ExecuteReaderAsync();
        var likedPlaylists = new List<string>();
        while (reader.Read())
        {
            likedPlaylists.Add(reader.GetString(0));
        }
        LikedPlaylistIds = likedPlaylists.ToArray();
        cmd = new OracleCommand("SELECT playlist_id FROM playlist_followers WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        reader = await cmd.ExecuteReaderAsync();
        var followedPlaylists = new List<string>();
        while (reader.Read())
        {
            followedPlaylists.Add(reader.GetString(0));
        }
        FollowedPlaylistIds = followedPlaylists.ToArray();
        cmd = new OracleCommand("SELECT album_id FROM user_saved_albums WHERE user_id = :userId", db);
        cmd.Parameters.Add(new OracleParameter("userId", userId));
        reader = await cmd.ExecuteReaderAsync();
        var savedAlbums = new List<string>();
        while (reader.Read())
        {
            savedAlbums.Add(reader.GetString(0));
        }
        SavedAlbumIds = savedAlbums.ToArray();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine(e.Exception);
        var exceptionString = e.Exception.Message + Environment.NewLine + e.Exception.StackTrace;
        App.MainWindow.ShowMessageDialogAsync(exceptionString, "Unhandled Exception");
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        App.MainWindow.BringToFront();
    }

    private static async Task<bool> DecideRedirection()
    {
        var instance = AppInstance.FindOrRegisterForKey("RhythmInstance");
        if (!instance.IsCurrent)
        {
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            await instance.RedirectActivationToAsync(activationArgs);
            return true;
        }
        instance.Activated += OnActivated;
        return false;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
