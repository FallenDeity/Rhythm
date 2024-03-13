using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rhythm.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Controls;
public sealed partial class LyricsControl : UserControl
{
    public LyricsControl()
    {
        this.InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        var page = (ShellPage)App.MainWindow.Content;
        var lyrics = page.RhythmPlayer.GetTrackLyrics();
        if (lyrics != null && !string.IsNullOrEmpty(lyrics))
        {
            Lyrics.Text = lyrics;
        }
        else
        {
            Lyrics.Text = "No lyrics available";
        }
    }
}
