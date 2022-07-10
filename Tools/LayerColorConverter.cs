using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Tools;

public class LayerColorConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Layer layer)
            return null;

        return layer switch
        {
            0 => Color.FromRgb(0, 0, 255),
            < 0 => Color.FromRgb(255, 0, 0),
            _ => Color.FromRgb(0, 255, 0)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}