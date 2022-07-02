using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class PixelGridView
    {
        private double _scale = 1;

        public PixelGridView()
        {
            InitializeComponent();
        }
        
        private void ZoomBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            if (e.Delta > 0) _scale += 0.1;
            else if (_scale > 1) _scale -= 0.1;

            PixelGrid.LayoutTransform = new ScaleTransform(_scale, _scale);

            e.Handled = true;
        }
        
        private void OnPixelGridLoaded(object sender, RoutedEventArgs e)
        {
            DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl))
                .AddValueChanged(sender, (_, _) => UpdateGrid());

            PixelGrid.SizeChanged += (_, _) => UpdateGrid();
        }


        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            var viewModel = (IPixelGridViewModel)PixelGrid.DataContext;
            if (viewModel == null) return;

            if (viewModel.Height / (double)viewModel.Width < ActualHeight / ActualWidth)
            {
                PixelGrid.Width = ActualWidth;
                PixelGrid.Height = ActualWidth * viewModel.Height / viewModel.Width;
            }
            else
            {
                PixelGrid.Height = ActualHeight;
                PixelGrid.Width = ActualHeight * viewModel.Width / viewModel.Height;
            }
        }
        
        private void PixelGrid_OnPreviewMouse(object sender, MouseEventArgs e) => RaisePixelGridAction();

        private void RaisePixelGridAction()
        {
            var viewModel = ((IPixelGridViewModel) PixelGrid.DataContext);
            if (viewModel == null) return;
            if (!viewModel.EditorEnabled) return;

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (Mouse.DirectlyOver is not FrameworkElement element)
                    return;

                viewModel.PixelGridActionCommand.Execute(element.DataContext);
            }
        }
    }
}
