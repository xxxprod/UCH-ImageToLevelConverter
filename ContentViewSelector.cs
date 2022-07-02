using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter;

public class ContentViewSelector : DataTemplateSelector
{
    public List<DataTemplate> Templates { get; } = new();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return Templates.FirstOrDefault(a => (Type)a.DataType == item?.GetType());
    }
}