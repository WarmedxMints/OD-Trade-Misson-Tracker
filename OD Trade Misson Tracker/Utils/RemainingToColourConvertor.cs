using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace OD_Trade_Mission_Tracker.Utils
{
    public sealed class RemainingToColourConvertor : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i = (int)value;

            SolidColorBrush colour = new();

            if (i > 0)
            {
                colour = (SolidColorBrush)Application.Current.Resources["Failed"];

                return colour;
            }
            
            colour = (SolidColorBrush)Application.Current.Resources["Foreground"];

            return colour;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
