using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class LevelEditorView : Window
    {
        public LevelEditorView() => InitializeComponent();

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

        private void PixelGrid_OnPreviewMouse(object sender, MouseEventArgs e)
        {
            var viewModel = ((LevelEditorViewModel)PixelGrid.DataContext);
            if (viewModel == null) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Mouse.DirectlyOver is not FrameworkElement element)
                    return;

                if (viewModel.PaintBrushEnabled)
                    viewModel.PaintPixelCommand.Execute(element.DataContext);
                else if (viewModel.PixelEraserEnabled || viewModel.MagicEraserEnabled)
                    viewModel.ErasePixelCommand.Execute(element.DataContext);
                else if (viewModel.ColorPickingEnabled)
                    viewModel.PickColorCommand.Execute(element.DataContext);
            }
        }

        private void UpdateGrid()
        {
            var viewModel = ((LevelEditorViewModel)PixelGrid.DataContext);
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
