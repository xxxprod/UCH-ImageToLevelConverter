using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Tools;

public class LayerOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Any(v => v == DependencyProperty.UnsetValue))
            return 1;

        var highlightLayer = (bool)values[0];
        var highlightedLayer = (LayerViewModel)values[1];
        var allLayers = (Dictionary<Layer, LayerViewModel>)values[2];
        var blockLayer = (Layer)values[3];

        if (!allLayers[blockLayer].IsVisible)
            return 0.0;

        if (!highlightLayer || blockLayer == highlightedLayer.Layer)
            return 1.0;

        return 0.2;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}