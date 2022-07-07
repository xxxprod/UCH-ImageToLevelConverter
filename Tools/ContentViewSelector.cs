using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UCH_ImageToLevelConverter.Tools;

public class ContentViewSelector : DataTemplateSelector
{
    public List<DataTemplate> Templates { get; } = new();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return Templates.FirstOrDefault(a => (Type)a.DataType == item?.GetType());
    }
}