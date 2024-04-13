using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rhythm.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Controls;
public sealed partial class LyricsControl : UserControl
{
    public static readonly DependencyProperty LyricsProperty = DependencyProperty.Register("Lyrics", typeof(string), typeof(LyricsControl), null);
    public LyricsControl()
    {
        this.InitializeComponent();
    }

    public string LyricsText
    {
        get => (string)GetValue(LyricsProperty);
        set => SetValue(LyricsProperty, value);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (LyricsText != null && !string.IsNullOrEmpty(LyricsText))
        {
            Lyrics.Text = LyricsText;
        }
        else
        {
            Lyrics.Text = "No lyrics available";
        }
    }
}
