using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SistemasDeGestionCitasPeluqueria.Converters;

/// Devuelve la primera letra en mayúscula del nombre. Si está vacío devuelve '?'.
public sealed class NameInitialConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value as string;
        if (string.IsNullOrWhiteSpace(name))
            return "?";
        var first = name.Trim()[0];
        return char.ToUpperInvariant(first).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}