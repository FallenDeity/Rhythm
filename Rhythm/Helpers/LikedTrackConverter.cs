using Microsoft.UI.Xaml.Data;

namespace Rhythm.Helpers;
public class LikedTrackConverter : IValueConverter
{
    public LikedTrackConverter()
    {
    }

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var accent = Microsoft.UI.Xaml.Application.Current.Resources["AccentAAFillColorDefaultBrush"];
        var normal = Microsoft.UI.Xaml.Application.Current.Resources["SystemControlForegroundBaseHighBrush"];
        return value is not null && (bool)value ? accent : normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
