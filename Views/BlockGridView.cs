using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views;

public class BlockGridView : FrameworkElement
{
    private readonly DrawingGroup _backingStore = new();

    private bool _recordingGridActions;

    public event Action BlockDataChanged;

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    public BlockGridView()
    {
        DataContextChanged += OnDataContextChanged;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    public IPixelGridViewModel ViewModel { get; private set; }

    public static readonly DependencyProperty EmptyBlockColorProperty = DependencyProperty.Register(
        "EmptyBlockColor", typeof(Color), typeof(BlockGridView), new FrameworkPropertyMetadata(default(Color), FrameworkPropertyMetadataOptions.None,
            (o, _) => ((BlockGridView)o).OnEmptyColorChanged()));

    private BlockData? _lastRecordedBlock;
    private Orientation? _snapToEdgeOrientation;
    private WriteableBitmap _bitmap;

    private void OnEmptyColorChanged()
    {
        if (ViewModel == null)
            return;
        OnBlocksChanged(ViewModel.Blocks);
    }

    public Color EmptyBlockColor
    {
        get => (Color)GetValue(EmptyBlockColorProperty);
        set => SetValue(EmptyBlockColorProperty, value);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.BlocksChanged -= OnBlocksChanged;

        ViewModel = (IPixelGridViewModel)e.NewValue;
        if (ViewModel == null)
            return;

        ViewModel.BlocksChanged += OnBlocksChanged;
    }

    private void OnBlocksChanged(IEnumerable<BlockData> changedBlocks)
    {
        if (changedBlocks is BlockDataCollection blocks)
        {
            if (_bitmap == null || _bitmap.PixelWidth != blocks.Width || _bitmap.PixelHeight != blocks.Height)
            {
                _bitmap = BitmapFactory.New(blocks.Width, blocks.Height);

                _backingStore.Children.Clear();
                _backingStore.Children.Add(new ImageDrawing(_bitmap,
                    new Rect(new Size(_bitmap.PixelWidth, _bitmap.PixelHeight))));
            }

            BlockDataChanged?.Invoke();
        }

        using (_bitmap.GetBitmapContext())
        {
            foreach (BlockData block in changedBlocks)
            {
                Color color = GetColor(block);
                _bitmap.SetPixel(block.Col, block.Row, color);
            }
        }
    }

    private Color GetColor(BlockData block)
    {
        if (!ViewModel.Layers[block.Layer].IsVisible)
            return EmptyBlockColor;
        Color color = block.Color;
        if (color == BlockData.EmptyColor)
            color = EmptyBlockColor;
        else if (ViewModel.HighlightLayer && ViewModel.HighlightedLayer.Value.Layer != block.Layer)
            color = Color.Multiply(color, 0.4f);
        return color;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (ViewModel == null || !ViewModel.EditorEnabled) return;

        _recordingGridActions |= Mouse.LeftButton == MouseButtonState.Pressed;

        if (!_recordingGridActions) return;

        ViewModel.StartRecordingGridActions();

        BlockData? blockData = GetBlockUnderCursor(e);
        if (blockData != null)
        {
            _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value);
            _lastRecordedBlock = blockData;
            _snapToEdgeOrientation = null;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        _recordingGridActions &= e.LeftButton == MouseButtonState.Pressed;
        if (ViewModel == null || !_recordingGridActions)
            return;

        BlockData? blockData = GetBlockUnderCursor(e);
        if (blockData == null)
            return;

        int deltaRow = blockData.Value.Row - _lastRecordedBlock!.Value.Row;
        int deltaCol = blockData.Value.Col - _lastRecordedBlock!.Value.Col;

        if (deltaRow == 0 && deltaCol == 0)
            return;

        if (ViewModel.SnapToEdgesEnabled)
        {
            _snapToEdgeOrientation ??= Math.Abs(deltaRow) > Math.Abs(deltaCol)
                ? Orientation.Vertical
                : Orientation.Horizontal;

            int row;
            int col;
            if (_snapToEdgeOrientation == Orientation.Horizontal)
            {
                row = _lastRecordedBlock!.Value.Row;
                col = _lastRecordedBlock.Value.Col + (deltaCol > 0 ? 1 : -1);
            }
            else
            {
                row = _lastRecordedBlock!.Value.Row + (deltaRow > 0 ? 1 : -1);
                col = _lastRecordedBlock.Value.Col;
            }

            if (ViewModel.Blocks.IsOutOfBounds(row, col))
                return;

            blockData = ViewModel.Blocks[row, col];

            Point snappedPoint = new(col + 0.5, row + 0.5);
            Point screenPosition = PointToScreen(snappedPoint);
            SetCursorPos((int)screenPosition.X, (int)screenPosition.Y);
        }

        _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value);
        _lastRecordedBlock = blockData;
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            ViewModel.Blocks.Width,
            ViewModel.Blocks.Height
        );
    }


    protected override void OnRender(DrawingContext dc) => dc.DrawDrawing(_backingStore);

    private BlockData? GetBlockUnderCursor(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        int row = (int)position.Y;
        int col = (int)position.X;

        return ViewModel.Blocks.IsOutOfBounds(row, col) ? null : ViewModel.Blocks[row, col];
    }
}