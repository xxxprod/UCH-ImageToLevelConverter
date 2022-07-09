using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Tools;

public class LayerToTextConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        var layer = (Layer)value;

        return $"{layer} ({(int) layer})";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}