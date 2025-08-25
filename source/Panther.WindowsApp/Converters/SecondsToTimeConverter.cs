using Microsoft.UI.Xaml.Data;
using System;

namespace Panther.WindowsApp.Converters;

public partial class SecondsToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.Hours > 0
                ? timeSpan.ToString(@"hh\:mm\:ss")
                : timeSpan.ToString(@"mm\:ss");
        }
        return "00:00:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string timeString && TimeSpan.TryParse(timeString, out TimeSpan timeSpan))
        {
            return timeSpan.TotalSeconds;
        }
        return 0;
    }
}
