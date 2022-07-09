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
        UndoCommand = new DelegateCommand(o => UndoAction());
        RedoCommand = new DelegateCommand(o => RedoAction());
        OptimizeAllCommand = new DelegateCommand(o => OptimizeAll());
        BreakAllCommand = new DelegateCommand(o => BreakAll());
        SaveLevelCommand = new DelegateCommand(o => SaveLevel());
        NavigateToImageSelectorCommand = new DelegateCommand(_ => { });

        Property<bool>[] pixelGridActions = new[]
        {
            EraseBlockEnabled,
            EraseRegionEnabled,
            ColorPickingEnabled,
            PaintBrushEnabled,
            FillBrushEnabled,
            OptimizerEnabled,
            BreakBlockEnabled,
            BreakRegionEnabled,
            MoveToLayerEnabled,
            MoveRegionToLayerEnabled
        };

        foreach (Property<bool> pixelGridAction in pixelGridActions)
        {
            Property<bool> localAction = pixelGridAction;

            localAction.OnChanged += newValue =>
            {
                EditorEnabled.Value = pixelGridActions.Any(a => a);
                if (!newValue) return;

                foreach (Property<bool> otherAction in pixelGridActions)
                    if (!ReferenceEquals(otherAction, localAction))
                        otherAction.Value = false;
            };
        }

        Layers = Enum.GetValues<Layer>().OrderBy(a => a).ToDictionary(a => a, a => new LayerViewModel(a, true));

        foreach (LayerViewModel layer in Layers.Values)
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

            OnBlocksChanged(Blocks);
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
    public Property<bool> EraseBlockEnabled { get; } = new();
    public Property<bool> EraseRegionEnabled { get; } = new();
    public Property<bool> PaintBrushEnabled { get; } = new();
    public Property<bool> FillBrushEnabled { get; } = new();
    public Property<bool> OptimizerEnabled { get; } = new();
    public Property<bool> BreakBlockEnabled { get; } = new();
    public Property<bool> BreakRegionEnabled { get; } = new();
    public Property<bool> MoveToLayerEnabled { get; } = new();
    public Property<bool> MoveRegionToLayerEnabled { get; } = new();

    public Property<Color> SelectedPaintColor { get; } = new(Colors.Crimson);
    public Property<int> ColorSimilarityThreshold { get; } = new(30);

    public Property<LayerViewModel> HighlightedLayer { get; } = new();
    public Property<bool> HighlightLayer { get; } = new();

    public Dictionary<Layer, LayerViewModel> Layers { get; }
    public IntProperty LevelFullness { get; } = new();
    public Property<Color> BackgroundColor { get; } = new(Colors.LightSteelBlue);

    public BlockDataCollection Blocks
    {
        get => _blocks;
        set
        {
            _blocks = value;
            _undoHistory.Clear();
            _redoHistory.Clear();
            CanUndo.Value = false;
            CanRedo.Value = false;
            OnBlocksChanged(value);
        }
    }

    public event Action<IEnumerable<BlockData>> BlocksChanged;

    public Property<bool> EditorEnabled { get; } = new();

    public void StartRecordingGridActions()
    {
        if (ColorPickingEnabled)
            return;
        PushUndoData(Blocks);
    }

    public bool OnPixelGridAction(BlockData blockData)
    {
        if (ColorPickingEnabled)
        {
            if (blockData.Color != BlockData.EmptyColor)
                SelectedPaintColor.Value = blockData.Color;
        }
        else if (EraseBlockEnabled || EraseRegionEnabled)
        {
            OnBlocksChanged(EraseBlocks(blockData));
        }
        else if (PaintBrushEnabled || FillBrushEnabled)
        {
            OnBlocksChanged(PaintBlocks(blockData));
        }
        else if (OptimizerEnabled)
        {
            OnBlocksChanged(OptimizeSection(blockData));
            return false;
        }
        else if (BreakBlockEnabled || BreakRegionEnabled)
        {
            OnBlocksChanged(BreakSection(blockData));
        }
        else if (MoveToLayerEnabled || MoveRegionToLayerEnabled)
        {
            OnBlocksChanged(MoveToLayer(blockData));
        }

        return true;
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
        OnBlocksChanged(Blocks);
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

        if (HighlightLayer)
            OnBlocksChanged(Blocks);
    }

    private void RedoAction()
    {
        _undoHistory.Push(Blocks.ToArray());

        Blocks.ReplaceBlocks(_redoHistory.Pop());

        CanUndo.Value = _undoHistory.Any();
        CanRedo.Value = _redoHistory.Any();
        OnBlocksChanged(Blocks);
    }

    private void UndoAction()
    {
        _redoHistory.Push(Blocks.ToArray());

        Blocks.ReplaceBlocks(_undoHistory.Pop());

        CanUndo.Value = _undoHistory.Any();
        CanRedo.Value = _redoHistory.Any();
        OnBlocksChanged(Blocks);
    }

    private void BreakAll()
    {
        PushUndoData(Blocks);

        Blocks.ReplaceBlocks(Blocks.BreakToCells());

        OnBlocksChanged(Blocks);
    }

    private void OptimizeAll()
    {
        PushUndoData(Blocks);

        Dictionary<Color, BlockData[]> blocksByColor = Blocks
            .Where(a => a.Color != BlockData.EmptyColor)
            .GroupBy(a => a.Color)
            .ToDictionary(
                a => a.Key,
                a => a.BreakToCells()
            );

        foreach ((var _, BlockData[] blocks) in blocksByColor)
        {
            BlockData[] _ = OptimizeBlocks(blocks).ToArray();
        }

        OnBlocksChanged(Blocks);
    }

    private IEnumerable<BlockData> BreakSection(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            yield break;

        IEnumerable<BlockData> blocksToOptimize = Blocks.FindBlocksWithSameColor(blockData, blockData.Color, ColorSimilarityThreshold);

        foreach (BlockData block in blocksToOptimize)
        {
            foreach (BlockData updatedBlock in Blocks.ReplaceBlocks(block.BreakToCells()))
                yield return updatedBlock;

            if (!BreakRegionEnabled)
                yield break;
        }
    }

    private IEnumerable<BlockData> OptimizeSection(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            return Enumerable.Empty<BlockData>();

        BlockData[] blocksToOptimize = Blocks.FindBlocksWithSameColor(blockData, blockData.Color, ColorSimilarityThreshold)
            .BreakToCells();

        return OptimizeBlocks(blocksToOptimize);
    }

    private IEnumerable<BlockData> OptimizeBlocks(ICollection<BlockData> blocksToOptimize)
    {
        if (blocksToOptimize.Count <= 1)
            yield break;

        RandomBlockOptimizer optimizer = new(blocksToOptimize);

        foreach (BlockData optimizedBlock in optimizer.Optimize())
            foreach (BlockData updatedBlock in Blocks.ReplaceBlock(optimizedBlock))
                yield return updatedBlock;
    }

    private IEnumerable<BlockData> MoveToLayer(BlockData blockData)
    {
        return UpdateBlocks(blockData, !MoveRegionToLayerEnabled,
            block => Blocks.Update(block, block.Color, HighlightedLayer.Value.Layer));
    }

    private IEnumerable<BlockData> PaintBlocks(BlockData blockData)
    {
        return UpdateBlocks(blockData, !FillBrushEnabled,
            block => Blocks.Update(block, SelectedPaintColor, HighlightedLayer.Value.Layer));
    }

    private IEnumerable<BlockData> EraseBlocks(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            return Enumerable.Empty<BlockData>();

        return UpdateBlocks(blockData, !EraseRegionEnabled, block => Blocks.ClearBlock(block));
    }

    private IEnumerable<BlockData> UpdateBlocks(BlockData origin, bool onlyFirstBlock,
        Func<BlockData, IEnumerable<BlockData>> updateBlock)
    {
        IEnumerable<BlockData> blocksWithSameColor = Blocks.FindBlocksWithSameColor(origin, origin.Color, ColorSimilarityThreshold);

        foreach (BlockData block in blocksWithSameColor)
        {
            foreach (BlockData updated in updateBlock(block))
                yield return updated;

            if (onlyFirstBlock)
                yield break;
        }
    }

    private void SaveLevel()
    {
        string snapshotXml = Blocks.CreateSnapshotXml();

        byte[] compressed = SevenZipHelper.Compress(Encoding.UTF8.GetBytes(snapshotXml));

        if (!Directory.Exists(SnapshotsDirectory))
            Directory.CreateDirectory(SnapshotsDirectory);

        string filePath = Path.GetFullPath($"{SnapshotsDirectory}/{LevelName.Value}.c.snapshot");

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
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = Path.GetDirectoryName(filePath),
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }

    protected virtual void OnBlocksChanged(IEnumerable<BlockData> changedBlocks)
    {
        BlocksChanged?.Invoke(changedBlocks);
        UpdateLevelFullness();
    }

    private void UpdateLevelFullness()
    {
        LevelFullness.Value = Blocks == null
            ? 0 : Blocks.GetDistinctNonEmptyBlocks().Count() * 5 + 10; // Add 10 for Start and Goal
    }

    private void PushUndoData(IEnumerable<BlockData> undoData)
    {
        _undoHistory.Push(undoData.ToArray());
        _redoHistory.Clear();
        CanRedo.Value = false;
        CanUndo.Value = true;
    }
}