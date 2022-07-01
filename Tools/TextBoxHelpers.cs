using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Tools;

public class TextBoxHelpers
{
    public static readonly DependencyProperty UpdateSourceDelayProperty = DependencyProperty.RegisterAttached(
        "UpdateSourceDelay", typeof(double), typeof(TextBoxHelpers), new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.None, UpdateSourceDelayChangedCallback));

    private static void UpdateSourceDelayChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = (TextBox)d;

        var timer = new Timer(Convert.ToDouble(e.NewValue) * 1000)
        {
            AutoReset = false
        };

        timer.Elapsed += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(UpdateSourceBinding);
        };

        textBox.KeyDown += (_, e1) =>
        {
            if (e1.Key == Key.Enter) UpdateSourceBinding();
        };

        textBox.TextChanged += (_, _) =>
        {
            timer.Stop();
            timer.Start();
        };

        void UpdateSourceBinding()
        {
            timer.Stop();
            var textPropertyBinding = textBox.GetBindingExpression(TextBox.TextProperty);
            textPropertyBinding?.UpdateSource();
        }
    }

    public static void SetUpdateSourceDelay(DependencyObject element, double value) => element.SetValue(UpdateSourceDelayProperty, value);
}