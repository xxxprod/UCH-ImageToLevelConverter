using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class ImageSelectorView: Window
    {
        public ImageSelectorView() => InitializeComponent();


        private void OnPixelGridLoaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl))
                .AddValueChanged(sender, (_, _) => UpdateGrid());

            PixelGrid.SizeChanged += (_, _) => UpdateGrid();
        }

        private void PixelContainer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGrid();
        }

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
    }
}
