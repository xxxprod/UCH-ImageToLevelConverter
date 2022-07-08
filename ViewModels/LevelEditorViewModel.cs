using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Optimizer;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorViewModel : ViewModelBase, IPixelGridViewModel
{
    private const string SnapshotsDirectory = "snapshots";
    private readonly Stack<BlockData[]> _redoHistory = new();

    private readonly Stack<BlockData[]> _undoHistory = new();
    private BlockDataCollection _blocks;

    public LevelEditorViewModel()
    {
        PixelGridActionCommand = new DelegateCommand(o => OnPixelGridAction((BlockData)o));
        UndoCommand = new DelegateCommand(o => UndoAction());
        RedoCommand = new DelegateCommand(o => RedoAction());
        OptimizeAllCommand = new DelegateCommand(o => OptimizeAll());
        BreakAllCommand = new DelegateCommand(o => BreakAll());
        SaveLevelCommand = new DelegateCommand(o => SaveLevel());
        NavigateToImageSelectorCommand = new DelegateCommand(_ => { });

        var pixelGridActions = new[]
        {
            PixelEraserEnabled,
            MagicEraserEnabled,
            ColorPickingEnabled,
            PaintBrushEnabled,
            FillBrushEnabled,
            OptimizerEnabled,
            BreakBlocksEnabled,
            MoveToLayerEnabled,
            MoveRegionToLayerEnabled
        };

        foreach (var pixelGridAction in pixelGridActions)
        {
            var localAction = pixelGridAction;

            localAction.OnChanged += newValue =>
            {
                EditorEnabled.Value = pixelGridActions.Any(a => a);
                if (!newValue) return;

                foreach (var otherAction in pixelGridActions)
                    if (!ReferenceEquals(otherAction, localAction))
                        otherAction.Value = false;
            };
        }
        
        Layers = Enum.GetValues<Layer>().OrderBy(a => a).ToDictionary(a => a, a => new LayerViewModel(a, true));

        foreach (var layer in Layers.Values)
            layer.IsVisible.OnChanged += _ => OnLayerVisibilityChanged(layer);

        HighlightedLayer.OnChanged += OnHighlightedLayerChanged;
        MoveToLayerEnabled.OnChanged += MoveToLayerChanged;
        MoveRegionToLayerEnabled.OnChanged += MoveToLayerChanged;
        HighlightLayer.OnChanged += highlight =>
        {
            if (!highlight)
            {
                MoveToLayerEnabled.Value = false;
                MoveRegionToLayerEnabled.Value = false;
            }
        };
        HighlightedLayer.Value = Layers[Layer.Default];
    }

    public DelegateCommand SaveLevelCommand { get; }
    public DelegateCommand UndoCommand { get; }
    public DelegateCommand RedoCommand { get; }
    public DelegateCommand OptimizeAllCommand { get; }
    public DelegateCommand BreakAllCommand { get; }
    public DelegateCommand NavigateToImageSelectorCommand { get; }

    public Property<string> LevelName { get; } = new();

    public Property<bool> CanUndo { get; } = new();
    public Property<bool> CanRedo { get; } = new();

    public Property<bool> ColorPickingEnabled { get; } = new();
    public Property<bool> PixelEraserEnabled { get; } = new();
    public Property<bool> MagicEraserEnabled { get; } = new();
    public Property<bool> PaintBrushEnabled { get; } = new();
    public Property<bool> FillBrushEnabled { get; } = new();
    public Property<bool> OptimizerEnabled { get; } = new();
    public Property<bool> BreakBlocksEnabled { get; } = new();
    public Property<bool> MoveToLayerEnabled { get; } = new();
    public Property<bool> MoveRegionToLayerEnabled { get; } = new();

    public Property<Color> SelectedPaintColor { get; } = new(Colors.Black);

    public Property<LayerViewModel> HighlightedLayer { get; } = new();
    public Property<bool> HighlightLayer { get; } = new();

    public Dictionary<Layer, LayerViewModel> Layers { get; }

    public DelegateCommand PixelGridActionCommand { get; }
    public IntProperty LevelFullness { get; } = new();

    public BlockDataCollection Blocks
    {
        get => _blocks;
        set
        {
            _blocks = value;
            OnBlocksChanged();
            _undoHistory.Clear();
            _redoHistory.Clear();
            CanUndo.Value = false;
            CanRedo.Value = false;
        }
    }

    public event Action BlocksChanged;

    public Property<bool> EditorEnabled { get; } = new();

    public void StartRecordingGridActions()
    {
        if (ColorPickingEnabled)
            return;
        PushUndoData(Blocks.CopyBlocks());
    }

    private void MoveToLayerChanged(bool moveToLayerEnabled)
    {
        if (moveToLayerEnabled)
        {
            HighlightedLayer.Value ??= Layers[Layer.Default];
            HighlightLayer.Value = true;
        }
    }


    private void OnLayerVisibilityChanged(LayerViewModel layer)
    {
        if (HighlightedLayer.Value == layer && !layer.IsVisible)
        {
            HighlightedLayer.Value = Layers.Values.FirstOrDefault(a => a.IsVisible) ?? Layers[Layer.Default];
            HighlightedLayer.Value.IsVisible.Value = true;
        }

        OnPropertyChanged(nameof(Layers));
    }

    private void OnHighlightedLayerChanged(LayerViewModel layer)
    {
        if (layer == null || !layer.IsVisible.Value)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                layer ??= Layers[Layer.Default];
                layer.IsVisible.Value = true;
                HighlightedLayer.Value = layer;
            }));
        OnPropertyChanged(nameof(Layers));
    }

    private void RedoAction()
    {
        _undoHistory.Push(Blocks.CopyBlocks().ToArray());

        var blockData = _redoHistory.Pop();

        foreach (var block in blockData)
            Blocks.SetBlock(block);

        CanUndo.Value = _undoHistory.Any();
        CanRedo.Value = _redoHistory.Any();
        OnBlocksChanged();
    }

    private void UndoAction()
    {
        _redoHistory.Push(Blocks.CopyBlocks().ToArray());

        var blockData = _undoHistory.Pop();

        foreach (var block in blockData)
            Blocks.SetBlock(block);

        CanUndo.Value = _undoHistory.Any();
        CanRedo.Value = _redoHistory.Any();
        OnBlocksChanged();
    }

    private void OnPixelGridAction(BlockData blockData)
    {
        if (PixelEraserEnabled || MagicEraserEnabled)
        {
            ErasePixel(blockData);
        }
        else if (ColorPickingEnabled)
        {
            if (blockData.Color != BlockData.EmptyColor)
                SelectedPaintColor.Value = blockData.Color;
        }
        else if (PaintBrushEnabled)
        {
            blockData.Color.Value = SelectedPaintColor.Value;
            blockData.Layer.Value = HighlightedLayer.Value.Layer;
        }
        else if (FillBrushEnabled)
        {
            foreach (var block in Blocks.FindBlocksWithSameColor(blockData, blockData.Color))
            {
                block.Color.Value = SelectedPaintColor.Value;
                block.Layer.Value = HighlightedLayer.Value.Layer;
            }
        }
        else if (OptimizerEnabled)
        {
            OptimizeSection(blockData);
        }
        else if (BreakBlocksEnabled)
        {
            BreakSection(blockData);
        }
        else if (MoveToLayerEnabled)
        {
            blockData.Layer.Value = HighlightedLayer.Value.Layer;
        }
        else if (MoveRegionToLayerEnabled)
        {
            foreach (var block in Blocks.FindBlocksWithSameColor(blockData, blockData.Color))
                block.Layer.Value = HighlightedLayer.Value.Layer;
        }

        UpdateLevelFullness();
    }

    private void BreakSection(BlockData blockData)
    {
        var blocksToOptimize = Blocks.FindBlocksWithSameColor(blockData, blockData.Color)
            .Where(a => a.Color != BlockData.EmptyColor)
            .BreakToCells();

        foreach (var block in blocksToOptimize)
            Blocks.SetBlock(block);

        OnBlocksChanged();
    }


    private void BreakAll()
    {
        PushUndoData(Blocks.CopyBlocks());

        foreach (var block in Blocks.BreakToCells())
            Blocks.SetBlock(block);

        OnBlocksChanged();
    }

    private void OptimizeAll()
    {
        PushUndoData(Blocks.CopyBlocks());

        var blocksByColor = Blocks
            .Where(a => a.Color != BlockData.EmptyColor)
            .GroupBy(a => a.Color)
            .ToDictionary(
                a => a.Key,
                a => a.BreakToCells().ToList()
            );

        foreach ((Color _, var blocks) in blocksByColor)
            OptimizeBlocks(blocks);

        OnBlocksChanged();
    }

    private void OptimizeSection(BlockData block)
    {
        var blocksToOptimize = Blocks.FindBlocksWithSameColor(block, block.Color)
            .Where(a => a.Color != BlockData.EmptyColor)
            .BreakToCells()
            .ToList();

        OptimizeBlocks(blocksToOptimize);

        OnBlocksChanged();
    }

    private void OptimizeBlocks(List<BlockData> blocksToOptimize)
    {
        if (blocksToOptimize.Count <= 1)
            return;

        var optimizer = new RandomBlockOptimizer(blocksToOptimize);

        foreach (var optimizedBlock in optimizer.Optimize())
            Blocks.SetBlock(optimizedBlock);
    }

    private void ErasePixel(BlockData block)
    {
        var blocksWithSameColor = Blocks.FindBlocksWithSameColor(block, block.Color)
            .Where(a => a.Color != BlockData.EmptyColor);
        if (!MagicEraserEnabled)
            blocksWithSameColor = blocksWithSameColor.Take(1);

        var brokenCellsCount = 0;
        foreach (var cell in blocksWithSameColor)
            if (cell.Cells == 1)
            {
                cell.Color.Value = BlockData.EmptyColor;
            }
            else
            {
                Blocks.SetBlock(new BlockData(cell.Top, cell.Left, cell.Layer));
                brokenCellsCount++;
            }

        if (brokenCellsCount > 0)
            OnBlocksChanged();
    }

    private void SaveLevel()
    {
        var snapshotXml = Blocks.CreateSnapshotXml();

        var compressed = SevenZipHelper.Compress(Encoding.UTF8.GetBytes(snapshotXml));

        if (!Directory.Exists(SnapshotsDirectory))
            Directory.CreateDirectory(SnapshotsDirectory);

        var filePath = Path.GetFullPath($"{SnapshotsDirectory}/{LevelName.Value}.c.snapshot");

        if (File.Exists(filePath))
            if (MessageBox.Show(Application.Current.MainWindow!,
                    $"The file '{filePath}' does already exists. Overwrite?",
                    "Save Level", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        File.WriteAllBytes(filePath, compressed);

        if (MessageBox.Show($"Successfully saved {Path.GetFileName(filePath)}\nOpen Output Folder?", "Save Level",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.Yes)
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = Path.GetDirectoryName(filePath),
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }

    protected virtual void OnBlocksChanged()
    {
        BlocksChanged?.Invoke();
        UpdateLevelFullness();
    }

    private void UpdateLevelFullness()
    {
        LevelFullness.Value =
            Blocks == null
                ? 0
                : Blocks.Count(a => a.Color.Value != BlockData.EmptyColor) * 5 + 10; // Add 10 for Start and Goal
    }

    private void PushUndoData(IEnumerable<BlockData> undoData)
    {
        _undoHistory.Push(undoData.ToArray());
        _redoHistory.Clear();
        CanRedo.Value = false;
        CanUndo.Value = true;
    }
}