using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class ImageSelectorView : Window
    {
        private readonly HashSet<string> _numericKeyCodes = Enumerable.Range(0, 9)
            .SelectMany(i => new[] { $"D{i}", $"NumPad{i}" })
            .ToHashSet();

        public ImageSelectorView() => InitializeComponent();
        
        private void OnPixelGridLoaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl))
                .AddValueChanged(sender, (_, _) => UpdateGrid());

            PixelGrid.SizeChanged += (_, _) => UpdateGrid();
        }

        private void PixelContainer_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateGrid();

        private void UpdateGrid()
        {
            var viewModel = (ImageSelectorViewModel)PixelGrid.DataContext;
            if (viewModel == null) return;

            if (viewModel.Height / (double)viewModel.Width < PixelContainer.ActualHeight / PixelContainer.ActualWidth)
            {
                PixelGrid.Width = PixelContainer.ActualWidth;
                PixelGrid.Height = PixelContainer.ActualWidth * viewModel.Height / viewModel.Width;
            }
            else
            {
                PixelGrid.Height = PixelContainer.ActualHeight;
                PixelGrid.Width = PixelContainer.ActualHeight * viewModel.Width / viewModel.Height;
            }
        }

        private void OnNumberTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Delete or Key.Back or Key.Left or Key.Right or Key.Enter or Key.Home or Key.End)
                return;
            
            e.Handled = !_numericKeyCodes.Contains(e.Key.ToString());
        }
    }
}
