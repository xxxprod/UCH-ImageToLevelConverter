using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Shader;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views;

public class BlockGridView : Grid
{
    public static readonly DependencyProperty EmptyBlockColorProperty = DependencyProperty.Register(
        "EmptyBlockColor", typeof(Color), typeof(BlockGridView), new FrameworkPropertyMetadata(default(Color),
            FrameworkPropertyMetadataOptions.None,
            (o, _) => ((BlockGridView) o).OnEmptyColorChanged()));

    private readonly ColorShaderControl _colorShader;
    private readonly GridShaderControl _gridShader;

    private BlockData? _lastRecordedBlock;
    private bool _recordingGridActions;
    private Orientation? _snapToEdgeOrientation;

    public BlockGridView()
    {
        DataContextChanged += OnDataContextChanged;
        _colorShader = new ColorShaderControl(this);
        _gridShader = new GridShaderControl(this);
        Children.Add(_colorShader);
        Children.Add(_gridShader);
    }

    public LevelEditorViewModel ViewModel { get; private set; }

    public Color EmptyBlockColor
    {
        get => (Color) GetValue(EmptyBlockColorProperty);
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

        ViewModel = (LevelEditorViewModel) e.NewValue;
        if (ViewModel == null)
            return;

        ViewModel.BlocksChanged += OnBlocksChanged;
    }

    private void OnBlocksChanged(IEnumerable<BlockData> changedBlocks)
    {
        if (changedBlocks is BlockDataCollection blocks)
        {
            _colorShader.SetSize(blocks.Width, blocks.Height);
            _gridShader.SetSize(blocks.Width, blocks.Height);

            BlockDataChanged?.Invoke();
        }

        changedBlocks = changedBlocks as IList<BlockData> ?? changedBlocks.ToArray();


        _colorShader.DrawBlocks(changedBlocks);
        _gridShader.DrawBlocks(changedBlocks);
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
            SetCursorPos((int) screenPosition.X, (int) screenPosition.Y);
        }

        _recordingGridActions = ViewModel.OnPixelGridAction(blockData.Value, false);
        _lastRecordedBlock = blockData;
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        if (ViewModel.Blocks == null)
            return base.MeasureOverride(availableSize);

        return new Size(
            ViewModel.Blocks.Width,
            ViewModel.Blocks.Height
        );
    }


    //protected override void OnRender(DrawingContext dc)
    //{
    //    dc.DrawDrawing(_backingStore);
    //}

    private BlockData? GetBlockUnderCursor(MouseEventArgs e)
    {
        Point position = e.GetPosition(this);

        int row = (int) position.Y;
        int col = (int) position.X;

        return ViewModel.Blocks.IsOutOfBounds(row, col) ? null : ViewModel.Blocks[row, col];
    }
}