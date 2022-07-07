using System;
using System.Globalization;
using System.Linq;
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
        private const double CellSize = 30;

        private double _zoomScale = 1;
        private double _sizeScale = 1;

        private IPixelGridViewModel _viewModel;
        private bool _dragEnabled;
        private Point _lastMousePosition;
        private bool _recordingGridActions;

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

            if (_recordingGridActions || _dragEnabled)
                return;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _dragEnabled = true;
                _lastMousePosition = e.GetPosition(ZoomBox);
            }
            else if (Mouse.LeftButton == MouseButtonState.Pressed && _viewModel.EditorEnabled && Mouse.DirectlyOver is FrameworkElement element)
            {
                _recordingGridActions = true;
                _viewModel.StartRecordingGridActions();
                _viewModel.PixelGridActionCommand.Execute(element.DataContext);
            }
        }

        private void Canvas_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            var newPos = e.GetPosition(ZoomBox);
            var delta = newPos - _lastMousePosition;
            if (delta == new Vector())
                return;
            _lastMousePosition = newPos;

            if (_dragEnabled)
            {
                if (e.MiddleButton != MouseButtonState.Pressed)
                {
                    _dragEnabled = false;
                    return;
                }


                ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset - delta.Y);
                ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset - delta.X);
            }
            else if (_recordingGridActions)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    _recordingGridActions = false;
                    return;
                }

                if (Mouse.DirectlyOver is FrameworkElement element)
                {
                    _viewModel.PixelGridActionCommand.Execute(element.DataContext);
                }
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.BlocksChanged -= OnBlocksChanged;

            if (e.NewValue is IPixelGridViewModel vm)
            {
                _viewModel = vm;
                _viewModel.BlocksChanged += OnBlocksChanged;
                OnBlocksChanged();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateCanvasSize();

        private void OnBlocksChanged()
        {
            Canvas.Children.Clear();

            var blocks = _viewModel?.Blocks;
            if (blocks == null)
                return;

            foreach (BlockData block in blocks)
            {
                var brush = new SolidColorBrush();
                var binding = new Binding(nameof(BlockData.Color) + "." + nameof(Property<int>.Value));
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, binding);

                var rectangle = new Rectangle
                {
                    Height = CellSize * block.Height + Space * (block.Height - 1),
                    Width = CellSize * block.Width + Space * (block.Width - 1),
                    Fill = brush,
                    DataContext = block
                };
                
                var layerOpacityBinding = new MultiBinding();
                layerOpacityBinding.Bindings.Add(new Binding(nameof(IPixelGridViewModel.HighlightLayer) + "." + nameof(Property<int>.Value)) { Source = DataContext });
                layerOpacityBinding.Bindings.Add(new Binding(nameof(IPixelGridViewModel.HighlightedLayer) + "." + nameof(Property<int>.Value)) { Source = DataContext });
                layerOpacityBinding.Bindings.Add(new Binding(nameof(IPixelGridViewModel.Layers)) { Source = DataContext });
                layerOpacityBinding.Bindings.Add(new Binding(nameof(BlockData.Layer) + "." + nameof(Property<int>.Value)));
                layerOpacityBinding.Converter = new LayerOpacityConverter();
                BindingOperations.SetBinding(rectangle, OpacityProperty, layerOpacityBinding);

                Canvas.Children.Add(rectangle);

                Canvas.SetLeft(rectangle, block.Left * (CellSize + Space));
                Canvas.SetTop(rectangle, block.Top * (CellSize + Space));
            }

            UpdateCanvasSize();
        }

        private void UpdateCanvasSize()
        {
            if (_viewModel.Blocks == null)
                return;

            Canvas.Width = _viewModel.Blocks.Width * (CellSize + Space);
            Canvas.Height = _viewModel.Blocks.Height * (CellSize + Space);

            _sizeScale = _viewModel.Blocks.Height / (double)_viewModel.Blocks.Width < ZoomBox.ActualHeight / ZoomBox.ActualWidth
                ? ZoomBox.ActualWidth / Canvas.Width
                : ZoomBox.ActualHeight / Canvas.Height;

            UpdateCanvasScale();
        }

        private void UpdateCanvasScale()
        {
            var scale = _sizeScale * _zoomScale;
            Canvas.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }

    public class LayerOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v == DependencyProperty.UnsetValue))
                return 1;

            var highlightLayer = (bool)values[0];
            var highlightedLayer = (LayerViewModel)values[1];
            var allLayers = (LayerViewModel[])values[2];
            var blockLayer = (Layer)values[3];

            if (!allLayers[(int) blockLayer].IsVisible)
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
}
