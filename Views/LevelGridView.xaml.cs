using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Views;

public partial class LevelGridView
{
    private const double MinZoomScale = 1;
    private const double MaxZoomScale = 10;

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

        ZoomGrid(e.Delta > 0, e.GetPosition(BlockGridView));

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

        if (delta == new Vector())
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

    private void UpdateCanvasSize()
    {
        BlockDataCollection blocks = BlockGridView.ViewModel.Blocks;

        int gridWidth = blocks.Width * BlockGridView.DefaultCellSize;
        int gridHeight = blocks.Height * BlockGridView.DefaultCellSize;

        double zoomBoxHeight = ZoomBox.ActualHeight - 3;
        double zoomBoxWidth = ZoomBox.ActualWidth - 3;

        _sizeScale = blocks.Height / (double)blocks.Width < zoomBoxHeight / zoomBoxWidth
            ? zoomBoxWidth / gridWidth
            : zoomBoxHeight / gridHeight;

        UpdateCanvasScale();
    }

    private void ZoomGrid(bool zoomIn, Point center = default)
    {
        if (center == default)
            center = new Point(ZoomBox.ActualWidth / 2, ZoomBox.ActualHeight / 2);

        double zoomFactor = zoomIn ? 1.1 : 1 / 1.1;

        _zoomScale *= zoomFactor;
        if (_zoomScale < MinZoomScale)
        {
            _zoomScale = MinZoomScale;
            zoomFactor = 1;
        }
        else if (_zoomScale > MaxZoomScale)
        {
            _zoomScale = MaxZoomScale;
            zoomFactor = 1;
        }

        UpdateCanvasScale();

        double scrollDeltaX = center.X - center.X * zoomFactor;
        double scrollDeltaY = center.Y - center.Y * zoomFactor;

        ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset - scrollDeltaY);
        ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset - scrollDeltaX);
    }

    private void UpdateCanvasScale()
    {
        BlockGridView.Scale = _sizeScale * _zoomScale;
        ZoomLabel.Content = $"{_zoomScale * 100:0} %";
    }
}