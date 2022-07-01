using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();


        private void OnPixelGridLoaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl))
                .AddValueChanged(sender, (_, _) => UpdateGrid());

            PixelGrid.SizeChanged += (_, _) => UpdateGrid();
        }

        private void UpdateGrid()
        {
            var viewModel = ((MainWindowViewModel)PixelGrid.DataContext);

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


            //if (viewModel.Height < viewModel.Width)
            //{
            //    PixelGrid.Width = double.NaN;
            //    PixelGrid.UpdateLayout();
            //    var newHeight = PixelGrid.ActualWidth * viewModel.Height / viewModel.Width;
            //    PixelGrid.Height = newHeight;
            //}
            //else
            //{
            //    PixelGrid.Height = double.NaN;
            //    PixelGrid.UpdateLayout();
            //    PixelGrid.Width = PixelGrid.ActualHeight * viewModel.Width / viewModel.Height;
            //}
        }

        private void PixelContainer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGrid();
        }
    }
}
