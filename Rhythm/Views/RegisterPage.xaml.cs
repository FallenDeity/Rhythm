using System.Diagnostics.Metrics;
using System.Reflection;
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
        if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Password) || string.IsNullOrEmpty(ConfirmPassword.Password) || string.IsNullOrEmpty(Gender.SelectedValue.ToString()) || string.IsNullOrEmpty(Country.Text))
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

    private void Gender_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ValidateDetails();

        //System.Diagnostics.Debug.WriteLine("Gender " + selectedGender);
    }

    private readonly List<string> Countries = new()
    {
        "Afghanistan",
        "Albania",
        "Algeria",
        "Andorra",
        "Angola",
        "Antigua and Barbuda",
        "Argentina",
        "Armenia",
        "Australia",
        "Austria",
        "Azerbaijan",
        "Bahamas",
        "Bahrain",
        "Bangladesh",
        "Barbados",
        "Belarus",
        "Belgium",
        "Belize",
        "Benin",
        "Bhutan",
        "Bolivia",
        "Bosnia and Herzegovina",
        "Botswana",
        "Brazil",
        "Brunei",
        "Bulgaria",
        "Burkina Faso",
        "Burundi",
        "Cabo Verde",
        "Cambodia",
        "Cameroon",
        "Canada",
        "Central African Republic",
        "Chad",
        "Chile",
        "China",
        "Colombia",
        "Comoros",
        "Congo, Democratic Republic of the",
        "Congo, Republic of the",
        "Costa Rica",
        "Croatia",
        "Cuba",
        "Cyprus",
        "Czech Republic",
        "Denmark",
        "Djibouti",
        "Dominica",
        "Dominican Republic",
        "East Timor (Timor-Leste)",
        "Ecuador",
        "Egypt",
        "El Salvador",
        "Equatorial Guinea",
        "Eritrea",
        "Estonia",
        "Eswatini",
        "Ethiopia",
        "Fiji",
        "Finland",
        "France",
        "Gabon",
        "Gambia",
        "Georgia",
        "Germany",
        "Ghana",
        "Greece",
        "Grenada",
        "Guatemala",
        "Guinea",
        "Guinea-Bissau",
        "Guyana",
        "Haiti",
        "Honduras",
        "Hungary",
        "Iceland",
        "India",
        "Indonesia",
        "Iran",
        "Iraq",
        "Ireland",
        "Israel",
        "Italy",
        "Ivory Coast",
        "Jamaica",
        "Japan",
        "Jordan",
        "Kazakhstan",
        "Kenya",
        "Kiribati",
        "Kosovo",
        "Kuwait",
        "Kyrgyzstan",
        "Laos",
        "Latvia",
        "Lebanon",
        "Lesotho",
        "Liberia",
        "Libya",
        "Liechtenstein",
        "Lithuania",
        "Luxembourg",
        "Madagascar",
        "Malawi",
        "Malaysia",
        "Maldives",
        "Mali",
        "Malta",
        "Marshall Islands",
        "Mauritania",
        "Mauritius",
        "Mexico",
        "Micronesia",
        "Moldova",
        "Monaco",
        "Mongolia",
        "Montenegro",
        "Morocco",
        "Mozambique",
        "Myanmar (Burma)",
        "Namibia",
        "Nauru",
        "Nepal",
        "Netherlands",
        "New Zealand",
        "Nicaragua",
        "Niger",
        "Nigeria",
        "North Korea",
        "North Macedonia (formerly Macedonia)",
        "Norway",
        "Oman",
        "Pakistan",
        "Palau",
        "Palestine",
        "Panama",
        "Papua New Guinea",
        "Paraguay",
        "Peru",
        "Philippines",
        "Poland",
        "Portugal",
        "Qatar",
        "Romania",
        "Russia",
        "Rwanda",
        "Saint Kitts and Nevis",
        "Saint Lucia",
        "Saint Vincent and the Grenadines",
        "Samoa",
        "San Marino",
        "Sao Tome and Principe",
        "Saudi Arabia",
        "Senegal",
        "Serbia",
        "Seychelles",
        "Sierra Leone",
        "Singapore",
        "Slovakia",
        "Slovenia",
        "Solomon Islands",
        "Somalia",
        "South Africa",
        "South Korea",
        "South Sudan",
        "Spain",
        "Sri Lanka",
        "Sudan",
        "Suriname",
        "Sweden",
        "Switzerland",
        "Syria",
        "Taiwan",
        "Tajikistan",
        "Tanzania",
        "Thailand",
        "Togo",
        "Tonga",
        "Trinidad and Tobago",
        "Tunisia",
        "Turkey",
        "Turkmenistan",
        "Tuvalu",
        "Uganda",
        "Ukraine",
        "United Arab Emirates",
        "United Kingdom",
        "United States",
        "Uruguay",
        "Uzbekistan",
        "Vanuatu",
        "Vatican City",
        "Venezuela",
        "Vietnam",
        "Yemen",
        "Zambia",
        "Zimbabwe"
    };

    // Handle text change and present suitable items
    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Since selecting an item will also change the text,
        // only listen to changes caused by user entering text.
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suitableItems = new List<string>();
            var splitText = sender.Text.ToLower().Split(" ");
            foreach (var country in Countries)
            {
                var found = splitText.All((key) =>
                {
                    return country.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(country);
                }
            }
            if (suitableItems.Count == 0)
            {
                suitableItems.Add("No results found");
            }
            sender.ItemsSource = suitableItems;
        }
    }

    // Handle user selecting an item, in our case just output the selected item.
    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {

        Country.Text = args.SelectedItem.ToString();
    }

    private void Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateDetails();
    }

    private void ConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateDetails();
    }

    private void Register(string username, string password, string gender, string country)
    {


        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO users (username, password, gender, country, user_image) VALUES (:username, :password, :gender, :country, EMPTY_BLOB())";
        command.Parameters.Add(new OracleParameter("username", username));
        command.Parameters.Add(new OracleParameter("password", BCrypt.Net.BCrypt.HashPassword(password)));
        command.Parameters.Add(new OracleParameter("gender", gender));
        command.Parameters.Add(new OracleParameter("country", country));
        command.ExecuteNonQuery();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var genderSelected = Gender.SelectedValue.ToString();
        if (Password.Password != ConfirmPassword.Password)
        {
            await App.MainWindow.ShowMessageDialogAsync("Passwords do not match", "Error");
            return;
        }
        if (genderSelected is null)
        {
            await App.MainWindow.ShowMessageDialogAsync("Pick a valid gender", "Error");
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
        var countrySelected = Country.Text.ToString();

        await Task.Run(() => Register(username, password, genderSelected, countrySelected));
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