using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views;

public class BlockGridView : FrameworkElement
{
    public const int DefaultCellSize = 30;
    private readonly DrawingGroup _backingStore = new();

    private bool _recordingGridActions;
    private double _scale = 1;

    private WriteableBitmap _writeableBmp;
    private Point _lastMousePosition;

    public BlockGridView()
    {
        DataContextChanged += OnDataContextChanged;
    }

    public double ScaledCellSize => DefaultCellSize * _scale;

    public IPixelGridViewModel ViewModel { get; private set; }

    public double Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            OnBlocksChanged(ViewModel.Blocks);
            InvalidateMeasure();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.BlocksChanged -= OnBlocksChanged;

        ViewModel = (IPixelGridViewModel)e.NewValue;
        if (ViewModel == null)
            return;

        ViewModel.BlocksChanged += OnBlocksChanged;

        OnBlocksChanged(ViewModel.Blocks);
    }

    private void OnBlocksChanged(IEnumerable<BlockData> changedBlocks)
    {
        if (changedBlocks is BlockDataCollection blocks)
            _writeableBmp = BitmapFactory.New(blocks.Width * DefaultCellSize, blocks.Height * DefaultCellSize);

        DrawBlocks(changedBlocks);
    }

    private void DrawBlocks(IEnumerable<BlockData> blocks)
    {
        using DrawingContext drawingContext = _backingStore.Open();
        using (_writeableBmp.GetBitmapContext())
        {
            foreach (BlockData block in blocks)
            {
                if (!ViewModel.Layers[block.Layer].IsVisible)
                    continue;

                Color color = block.Color;
                if (ViewModel.HighlightLayer && ViewModel.HighlightedLayer.Value.Layer != block.Layer)
                    color = Color.Multiply(color, 0.4f);

                int x1 = block.Col * DefaultCellSize;
                int y1 = block.Row * DefaultCellSize;
                int x2 = x1 + DefaultCellSize;
                int y2 = y1 + DefaultCellSize;

                _writeableBmp.FillRectangle(x1, y1, x2, y2, color);

                if (block.Left == block.Col)
                    _writeableBmp.DrawLine(x1, y1, x1, y2, BlockData.EmptyColor);
                if (block.Right == block.Col)
                    _writeableBmp.DrawLine(x2, y1, x2, y2, BlockData.EmptyColor);

                if (block.Top == block.Row)
                    _writeableBmp.DrawLine(x1, y1, x2, y1, BlockData.EmptyColor);
                if (block.Bottom == block.Row)
                    _writeableBmp.DrawLine(x1, y2, x2, y2, BlockData.EmptyColor);
            }
        }

        if (_scale >= 1)
            drawingContext.DrawImage(_writeableBmp, new Rect(new Size(_writeableBmp.PixelWidth * _scale, _writeableBmp.PixelHeight * _scale)));
        else
        {
            TransformedBitmap scaledBitmap = new(_writeableBmp.Clone(), new ScaleTransform(_scale, _scale));
            drawingContext.DrawImage(scaledBitmap, new Rect(new Size(scaledBitmap.PixelWidth, scaledBitmap.PixelHeight)));
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        _recordingGridActions |= Mouse.LeftButton == MouseButtonState.Pressed;
        if (ViewModel == null || !ViewModel.EditorEnabled || !_recordingGridActions) return;

        ViewModel.StartRecordingGridActions();

        BlockData? blockData = GetBlockUnderCursor(e);
        if (blockData != null)
            _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        _recordingGridActions &= e.LeftButton == MouseButtonState.Pressed;
        if (ViewModel == null || !_recordingGridActions) return;

        Point newPos = e.GetPosition(this);
        Vector delta = newPos - _lastMousePosition;

        if (delta == new Vector())
            return;

        _lastMousePosition = newPos;

        BlockData? blockData = GetBlockUnderCursor(e);
        if (blockData != null)
            _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value);
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            ViewModel.Blocks.Width * ScaledCellSize,
            ViewModel.Blocks.Height * ScaledCellSize
        );
    }


    protected override void OnRender(DrawingContext dc) => dc.DrawDrawing(_backingStore);

    private BlockData? GetBlockUnderCursor(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        int row = (int)(position.Y / ScaledCellSize);
        int col = (int)(position.X / ScaledCellSize);

        if (row < 0 || col < 0) return null;
        if (row >= ViewModel.Blocks.Height || col >= ViewModel.Blocks.Width) return null;

        return ViewModel.Blocks[row, col];
    }
}