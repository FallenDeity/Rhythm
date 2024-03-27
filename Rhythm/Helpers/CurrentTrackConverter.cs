using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Rhythm.Views;

namespace Rhythm.Helpers;
internal class CurrentTrackConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var page = (ShellPage)App.MainWindow.Content;
        if (value is string currentTrack)
        {
            var selected = App.Current.Resources["ButtonPointerOverBackgroundThemeBrush"] as SolidColorBrush;
            return page.RhythmPlayer.TrackId == currentTrack ? selected! : new SolidColorBrush(Colors.Transparent);
        }
        return new SolidColorBrush(Colors.Transparent);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
