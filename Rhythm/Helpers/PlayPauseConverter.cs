using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace Rhythm.Helpers;
public class PlayPauseConverter : IValueConverter
{

    public PlayPauseConverter()
    {
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isPlaying)
        {
            return isPlaying ? "Pause" : "Play";
        }
        return "Paused?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
