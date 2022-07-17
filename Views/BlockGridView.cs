using System;
using System.Collections.Generic;
using System.Linq;
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
    private const int BlockSize = 20;

    public static readonly DependencyProperty EmptyBlockColorProperty = DependencyProperty.Register(
        "EmptyBlockColor", typeof(Color), typeof(BlockGridView), new FrameworkPropertyMetadata(default(Color),
            FrameworkPropertyMetadataOptions.None,
            (o, _) => ((BlockGridView)o).OnEmptyColorChanged()));

    private WriteableBitmap _bitmap;
    private readonly DrawingGroup _backingStore = new();
    private BlockData? _lastRecordedBlock;
    private bool _recordingGridActions;
    private Orientation? _snapToEdgeOrientation;

    public BlockGridView()
    {
        DataContextChanged += OnDataContextChanged;
        _backingStore.Transform = new ScaleTransform(1.0 / BlockSize, 1.0 / BlockSize);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    public LevelEditorViewModel ViewModel { get; private set; }

    public Color EmptyBlockColor
    {
        get => (Color)GetValue(EmptyBlockColorProperty);
        set => SetValue(EmptyBlockColorProperty, value);
    }

    public event Action BlockDataChanged;

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    private void OnEmptyColorChanged()
    {
        if (ViewModel?.Blocks == null)
            return;
        OnBlocksChanged(ViewModel.Blocks);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
            ViewModel.BlocksChanged -= OnBlocksChanged;

        ViewModel = (LevelEditorViewModel)e.NewValue;
        if (ViewModel == null)
            return;

        ViewModel.BlocksChanged += OnBlocksChanged;
    }

    private void OnBlocksChanged(IEnumerable<BlockData> changedBlocks)
    {
        if (changedBlocks is BlockDataCollection blocks)
        {
            int width = blocks.Width * BlockSize;
            int height = blocks.Height * BlockSize;

            if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            {
                _bitmap = BitmapFactory.New(width, height);
                _backingStore.Children.Clear();
                _backingStore.Children.Add(new ImageDrawing(_bitmap, new Rect(0, 0, width, height)));

                BlockDataChanged?.Invoke();
            }
        }

        using (_bitmap.GetBitmapContext())
        {
            HashSet<BlockData> seenBlocks = new(new BlockDataCoordinatesComparer());

            foreach (BlockData block in changedBlocks.Where(seenBlocks.Add))
            {
                Color color = GetColor(block);

                int x1 = block.Left * BlockSize;
                int x2 = (block.Right + 1) * BlockSize;
                int y1 = block.Top * BlockSize;
                int y2 = (block.Bottom + 1) * BlockSize;

                _bitmap.FillRectangle(x1, y1, x2, y2, color);
                
                color = Color.Multiply(color, 0.8f);

                _bitmap.FillRectangle(x1, y1, x2, y1 + 1, color);
                _bitmap.FillRectangle(x1, y2 - 1, x2, y2, color);
                _bitmap.FillRectangle(x1, y1, x1 + 1, y2, color);
                _bitmap.FillRectangle(x2 - 1, y1, x2, y2, color);
            }
        }
    }

    protected Color GetColor(BlockData block)
    {
        if (!ViewModel.LevelEditorTools.Layers[block.Layer].IsVisible)
            return EmptyBlockColor;
        Color color = block.Color;
        if (color == BlockData.EmptyColor)
            color = EmptyBlockColor;
        else if (ViewModel.LevelEditorTools.HighlightLayer &&
                 ViewModel.LevelEditorTools.HighlightedLayer.Value.Layer != block.Layer)
            color = Color.Multiply(color, 0.4f);
        return color;
    }

    protected override void OnRender(DrawingContext dc) => dc.DrawDrawing(_backingStore);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (ViewModel.Blocks == null)
            return base.MeasureOverride(availableSize);

        return new Size(
            ViewModel.Blocks.Width,
            ViewModel.Blocks.Height
        );
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (ViewModel == null) return;

        _recordingGridActions |= Mouse.LeftButton == MouseButtonState.Pressed;

        if (!_recordingGridActions) return;

        BlockData? blockData = GetBlockUnderCursor(e);
        if (blockData != null)
        {
            _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value, true);
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

        if (ViewModel.LevelEditorTools.SnapToEdgesEnabled)
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

        _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value, false);
        _lastRecordedBlock = blockData;
    }

    private BlockData? GetBlockUnderCursor(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        int row = (int)position.Y;
        int col = (int)position.X;

        return ViewModel.Blocks.IsOutOfBounds(row, col) ? null : ViewModel.Blocks[row, col];
    }
}