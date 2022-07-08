using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Data;
using System.Windows.Markup;

namespace UCH_ImageToLevelConverter.Tools;

public class EnumMemberConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value.ToString();
        var field = value.GetType().GetField(name);

        var customAttributes = (EnumMemberAttribute[])field.GetCustomAttributes(typeof(EnumMemberAttribute), false);
        return customAttributes.Select(a => a.Value).FirstOrDefault() ?? name;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}