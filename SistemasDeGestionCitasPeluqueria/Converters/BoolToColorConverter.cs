using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SistemasDeGestionCitasPeluqueria.Converters;

public class BoolToColorConverter : IValueConverter
{
    // true -> SaveAccent; false -> TextSecondary
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ok = value as bool? ?? false;

        try
        {
            var res = Application.Current!.Resources;

            if (ok)
            {
                try { return (Color)res["SaveAccent"]; } catch { }
                try { var b = res["SaveAccentBrush"]; if (b is SolidColorBrush scb) return scb.Color; } catch { }
            }
            else
            {
                try { return (Color)res["TextSecondary"]; } catch { }
                try { var b = res["TextSecondaryBrush"]; if (b is SolidColorBrush scb) return scb.Color; } catch { }
            }
        }
        catch { /* ignore */ }

        // Fallback
        return ok ? Colors.Green : Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}