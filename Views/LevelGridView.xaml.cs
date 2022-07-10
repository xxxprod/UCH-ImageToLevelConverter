using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Views;

public partial class LevelGridView
{
    private const double MinZoomScale = 1;
    private const double MaxZoomScale = 50;

    private bool _dragEnabled;
    private Point _lastMousePosition;
    private double _sizeScale = 1;
    private double _zoomScale = 1;

    public LevelGridView()
    {
        InitializeComponent();
    }

    private void ZoomBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
            return;

        Point position = e.GetPosition(BlockGridView);
        Point center = new(position.X / BlockGridView.ActualWidth, position.Y / BlockGridView.ActualHeight);

        ZoomGrid(e.Delta > 0, center);

        e.Handled = true;
    }

    private void Canvas_OnPreviewMouseDown(object sender, MouseEventArgs e)
    {
        _dragEnabled |= e.MiddleButton == MouseButtonState.Pressed;

        if (_dragEnabled) _lastMousePosition = e.GetPosition(ZoomBox);
    }

    private void Canvas_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        _dragEnabled &= e.MiddleButton == MouseButtonState.Pressed;

        if (!_dragEnabled)
            return;

        Point newPos = e.GetPosition(ZoomBox);
        Vector delta = newPos - _lastMousePosition;

        if (Math.Abs(delta.Length) < 1)
            return;

        if (_dragEnabled)
        {
            ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset - delta.Y);
            ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset - delta.X);
        }

        _lastMousePosition = newPos;
    }

    private void ZoomInClicked(object sender, RoutedEventArgs e) => ZoomGrid(true);
    private void ZoomOutClicked(object sender, RoutedEventArgs e) => ZoomGrid(false);
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateCanvasSize();
    private void BlockGridView_OnBlockDataChanged() => UpdateCanvasSize();

    private void UpdateCanvasSize()
    {
        BlockDataCollection blocks = BlockGridView.ViewModel.Blocks;

        double zoomBoxHeight = ZoomBox.ActualHeight - 3;
        double zoomBoxWidth = ZoomBox.ActualWidth - 3;

        _sizeScale = blocks.Height / (double)blocks.Width < zoomBoxHeight / zoomBoxWidth
            ? zoomBoxWidth / blocks.Width
            : zoomBoxHeight / blocks.Height;

        UpdateCanvasScale();
    }

    private void ZoomGrid(bool zoomIn, Point center = default)
    {
        if (center == default)
            center = new Point(0.5, 0.5);

        double zoomFactor = zoomIn ? 1.1 : 1 / 1.1;

        _zoomScale *= zoomFactor;
        switch (_zoomScale)
        {
            case < MinZoomScale:
                _zoomScale = MinZoomScale;
                zoomFactor = 1;
                break;
            case > MaxZoomScale:
                _zoomScale = MaxZoomScale;
                zoomFactor = 1;
                break;
        }

        UpdateCanvasScale();

        double scrollDeltaX = center.X * zoomFactor - center.X;
        double scrollDeltaY = center.Y * zoomFactor - center.Y;

        ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset + scrollDeltaY * ZoomBox.ExtentHeight);
        ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset + scrollDeltaX * ZoomBox.ExtentWidth);
    }

    private void UpdateCanvasScale()
    {
        double scale = _sizeScale * _zoomScale;
        BlockGridView.LayoutTransform = new ScaleTransform(scale, scale);
        ZoomLabel.Content = $"{_zoomScale * 100:0} %";
    }
}