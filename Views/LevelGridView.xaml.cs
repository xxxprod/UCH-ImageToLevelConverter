using System.Windows;
using System.Windows.Input;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Views;

public partial class LevelGridView
{
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

        if (e.Delta > 0) _zoomScale *= 1.1;
        else if (_zoomScale > 1) _zoomScale /= 1.1;

        UpdateCanvasScale();

        e.Handled = true;
    }

    private void Canvas_OnPreviewMouseDown(object sender, MouseEventArgs e)
    {
        _dragEnabled |= e.MiddleButton == MouseButtonState.Pressed;

        if (_dragEnabled)
            _lastMousePosition = e.GetPosition(ZoomBox);
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

        _lastMousePosition = newPos;

        ZoomBox.ScrollToVerticalOffset(ZoomBox.VerticalOffset - delta.Y);
        ZoomBox.ScrollToHorizontalOffset(ZoomBox.HorizontalOffset - delta.X);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        BlockDataCollection blocks = BlockGridView.ViewModel.Blocks;

        int gridWidth = blocks.Width * BlockGridView.DefaultCellSize;
        int gridHeight = blocks.Height * BlockGridView.DefaultCellSize;

        double zoomBoxHeight = ZoomBox.ActualHeight - 3;
        double zoomBoxWidth = ZoomBox.ActualWidth - 3;

        _sizeScale = blocks.Height / (double) blocks.Width < zoomBoxHeight / zoomBoxWidth
            ? zoomBoxWidth / gridWidth
            : zoomBoxHeight / gridHeight;

        UpdateCanvasScale();
    }

    private void UpdateCanvasScale()
    {
        BlockGridView.Scale = _sizeScale * _zoomScale;
    }
}