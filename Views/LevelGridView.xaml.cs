using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class LevelGridView
    {
        private const double Space = 1;
        private const double PixelSize = 30;

        private double _zoomScale = 1;
        private double _sizeScale = 1;

        private IPixelGridViewModel _viewModel;
        private bool _dragEnabled;
        private Point _dragStart;

        public LevelGridView() => InitializeComponent();

        private void ZoomBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;

            if (e.Delta > 0) _zoomScale += 0.1;
            else if (_zoomScale > 1) _zoomScale -= 0.1;

            UpdateCanvasScale();

            e.Handled = true;
        }

        private void Canvas_OnPreviewMouseDown(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _dragEnabled = true;
                _dragStart = e.GetPosition(ZoomBox);
            }
            else if (Mouse.LeftButton == MouseButtonState.Pressed && _viewModel.EditorEnabled && Mouse.DirectlyOver is FrameworkElement element)
            {
                _viewModel.PixelGridActionCommand.Execute(element.DataContext);
            }
        }

        private void Canvas_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            if (_dragEnabled)
            {
                if (e.MiddleButton != MouseButtonState.Pressed)
                {
                    _dragEnabled = false;
                    return;
                }

                var newPos = e.GetPosition(ZoomBox);
                var delta = newPos - _dragStart;
                _dragStart = newPos;

                ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset - delta.Y);
                ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset - delta.X);
            }
            else if (Mouse.LeftButton == MouseButtonState.Pressed && _viewModel.EditorEnabled && Mouse.DirectlyOver is FrameworkElement element)
            {
                _viewModel.PixelGridActionCommand.Execute(element.DataContext);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.Blocks.OnChanged -= OnPixelsChanged;

            if (e.NewValue is IPixelGridViewModel vm)
            {
                _viewModel = vm;
                _viewModel.Blocks.OnChanged += OnPixelsChanged;
                OnPixelsChanged(_viewModel.Blocks);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateCanvasSize();

        private void OnPixelsChanged(BlockData[] pixels)
        {
            Canvas.Children.Clear();

            var blocks = _viewModel?.Blocks.Value;
            if (blocks == null)
                return;

            foreach (BlockData block in blocks)
            {
                var brush = new SolidColorBrush();
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty,
                    new Binding(nameof(BlockData.Color) + "." + nameof(Property<int>.Value)));

                var rectangle = new Rectangle
                {
                    Height = PixelSize * block.Height,
                    Width = PixelSize * block.Width,
                    Fill = brush,
                    DataContext = block
                };

                Canvas.Children.Add(rectangle);

                Canvas.SetLeft(rectangle, block.Left * (PixelSize + Space));
                Canvas.SetTop(rectangle, block.Top * (PixelSize + Space));
            }

            UpdateCanvasSize();
        }

        private void UpdateCanvasSize()
        {
            Canvas.Width = _viewModel.Width * (PixelSize + Space);
            Canvas.Height = _viewModel.Height * (PixelSize + Space);

            _sizeScale = _viewModel.Height / (double)_viewModel.Width < ActualHeight / ActualWidth
                ? ActualWidth / Canvas.Width
                : ActualHeight / Canvas.Height;

            UpdateCanvasScale();
        }

        private void UpdateCanvasScale()
        {
            var scale = _sizeScale * _zoomScale;
            Canvas.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }
}
