using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RegisterPage : Page
{
    public RegisterPage()
    {
        this.InitializeComponent();

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "Rhythm - Register";
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
        ValidateDetails();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText as UIElement;
    }


    private void ValidateDetails()
    {
        if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Password) || string.IsNullOrEmpty(ConfirmPassword.Password))
        {
            RegisterButton.IsEnabled = false;
        }
        else
        {
            RegisterButton.IsEnabled = true;
        }
    }

    private void Username_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateDetails();
    }

    private void Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateDetails();
    }

    private void ConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateDetails();
    }

    private void Register(string username, string password)
    {

        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO users (username, password, gender, country, user_image) VALUES (:username, :password, 'male', 'USA', EMPTY_BLOB())";
        command.Parameters.Add(new OracleParameter("username", username));
        command.Parameters.Add(new OracleParameter("password", BCrypt.Net.BCrypt.HashPassword(password)));
        command.ExecuteNonQuery();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        if (Password.Password != ConfirmPassword.Password)
        {
            await App.MainWindow.ShowMessageDialogAsync("Passwords do not match", "Error");
            return;
        }
        ProgressRing p = new ProgressRing();
        p.IsActive = true;
        p.Width = p.Height = 20;
        p.Margin = new Thickness(0, 0, 10, 0);
        RegisterButtonStackPanel.Children.Insert(0, p);
        RegisterButton.IsEnabled = false;
        var username = Username.Text;
        var password = Password.Password;
        await Task.Run(() => Register(username, password));
        await App.MainWindow.ShowMessageDialogAsync("User registered successfully", "Success");
        RegisterButtonStackPanel.Children.RemoveAt(0);
        RegisterButton.IsEnabled = true;
        App.MainWindow.Content = App.GetService<LoginPage>();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Content = App.GetService<LoginPage>();
    }
}
