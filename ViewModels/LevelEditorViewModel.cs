using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorViewModel : ViewModelBase, IPixelGridViewModel
{
    private const string SnapshotsDirectory = "snapshots";
    private BlockDataCollection _blocks;

    private readonly Stack<BlockData[]> _undoHistory = new();
    private readonly Stack<BlockData[]> _redoHistory = new();

    public LevelEditorViewModel()
    {
        PixelGridActionCommand = new DelegateCommand(o => OnPixelGridAction((BlockData)o));
        UndoCommand = new DelegateCommand(o =>
        {
            _redoHistory.Push(Blocks.CopyBlocks().ToArray());

            BlockData[] blockData = _undoHistory.Pop();

            foreach (BlockData block in blockData)
                Blocks.SetBlock(block);

            CanUndo.Value = _undoHistory.Any();
            CanRedo.Value = _redoHistory.Any();
            OnBlocksChanged();
        });
        RedoCommand = new DelegateCommand(o =>
        {
            _undoHistory.Push(Blocks.CopyBlocks().ToArray());

            BlockData[] blockData = _redoHistory.Pop();

            foreach (BlockData block in blockData)
                Blocks.SetBlock(block);

            CanUndo.Value = _undoHistory.Any();
            CanRedo.Value = _redoHistory.Any();
            OnBlocksChanged();
        });
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
            BreakBlocksEnabled
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
    }

    public DelegateCommand SaveLevelCommand { get; }
    public DelegateCommand UndoCommand { get; }
    public DelegateCommand RedoCommand { get; }
    public DelegateCommand OptimizeAllCommand { get; }
    public DelegateCommand BreakAllCommand { get; }
    public DelegateCommand NavigateToImageSelectorCommand { get; }

    public Property<string> LevelName { get; } = new();

    public Property<bool> CanUndo { get; } = new(false);
    public Property<bool> CanRedo { get; } = new(false);

    public Property<bool> ColorPickingEnabled { get; } = new();
    public Property<bool> PixelEraserEnabled { get; } = new();
    public Property<bool> MagicEraserEnabled { get; } = new();
    public Property<bool> PaintBrushEnabled { get; } = new();
    public Property<bool> FillBrushEnabled { get; } = new();
    public Property<bool> OptimizerEnabled { get; } = new();
    public Property<bool> BreakBlocksEnabled { get; } = new();

    public Property<Color> SelectedColor { get; } = new(Colors.Black);

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

    private void OnPixelGridAction(BlockData blockData)
    {
        if (PixelEraserEnabled || MagicEraserEnabled) ErasePixel(blockData);
        else if (ColorPickingEnabled)
        {
            if (blockData.Color != BlockData.EmptyColor)
                SelectedColor.Value = blockData.Color;
        }
        else if (PaintBrushEnabled) blockData.Color.Value = SelectedColor.Value;
        else if (FillBrushEnabled)
        {
            foreach (BlockData block in FindBlocksWithSameColor(blockData, blockData.Color))
                block.Color.Value = SelectedColor.Value;
        }
        else if (OptimizerEnabled) OptimizeSection(blockData);
        else if (BreakBlocksEnabled) BreakSection(blockData);

        UpdateLevelFullness();
    }

    private void BreakSection(BlockData blockData)
    {
        var blocksToOptimize = FindBlocksWithSameColor(blockData, blockData.Color)
            .Where(a => a.Color != BlockData.EmptyColor)
            .BreakToCells();

        foreach (BlockData block in blocksToOptimize)
            Blocks.SetBlock(block);

        OnBlocksChanged();
    }


    private void BreakAll()
    {
        PushUndoData(Blocks.CopyBlocks());

        foreach (BlockData block in Blocks.BreakToCells())
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

        foreach ((Color _, List<BlockData> blocks) in blocksByColor)
            OptimizeBlocks(blocks);

        OnBlocksChanged();
    }

    private void OptimizeSection(BlockData block)
    {
        var blocksToOptimize = FindBlocksWithSameColor(block, block.Color)
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

        var minCol = blocksToOptimize.Min(a => a.Left);
        var maxCol = blocksToOptimize.Max(a => a.Left);
        var minRow = blocksToOptimize.Min(a => a.Top);
        var maxRow = blocksToOptimize.Max(a => a.Top);

        var width = maxCol - minCol + 1;
        var height = maxRow - minRow + 1;

        BlockData[,] blockGrid = new BlockData[height, width];

        foreach (BlockData blockData in blocksToOptimize)
        {
            var row = blockData.Top - minRow;
            var col = blockData.Left - minCol;
            blockGrid[row, col] = blockData;
        }

        (int Width, int Height)[] blockSizes =
        {
            (16, 6), (6, 16),
            (8, 8),
            (1, 16), (16, 1),
            (4, 4),
            (2, 4), (4, 2),
            (1, 8), (8, 1),
            (2, 2),
            (1, 4), (4, 1),
            (1, 3), (3, 1),
            (1, 2), (2, 1),
            (1, 1)
        };

        var ran = new Random();

        var optimizedBlocks = new List<BlockData>();

        while (blocksToOptimize.Any())
        {
            var nextBlock = blocksToOptimize[ran.Next(blocksToOptimize.Count)];

            foreach ((int blockWidth, int blockHeight) in blockSizes)
            {
                bool foundHole = false;
                for (int deltaRow = 0; deltaRow < blockHeight && !foundHole; deltaRow++)
                {
                    for (int deltaCol = 0; deltaCol < blockWidth && !foundHole; deltaCol++)
                    {
                        var row = nextBlock.Top + deltaRow - minRow;
                        var col = nextBlock.Left + deltaCol - minCol;
                        if (row >= height || col >= width || blockGrid[row, col] == null)
                            foundHole = true;
                    }
                }

                if (!foundHole)
                {
                    optimizedBlocks.Add(new BlockData(nextBlock.Top, nextBlock.Left, blockWidth, blockHeight,
                        nextBlock.Color));

                    for (int deltaRow = 0; deltaRow < blockHeight; deltaRow++)
                    {
                        for (int deltaCol = 0; deltaCol < blockWidth; deltaCol++)
                        {
                            var row = nextBlock.Top + deltaRow - minRow;
                            var col = nextBlock.Left + deltaCol - minCol;
                            blocksToOptimize.Remove(blockGrid[row, col]);
                            blockGrid[row, col] = null;
                        }
                    }

                    break;
                }
            }
        }

        foreach (var optimizedBlock in optimizedBlocks)
            Blocks.SetBlock(optimizedBlock);
    }

    private void ErasePixel(BlockData block)
    {
        var blocksWithSameColor = FindBlocksWithSameColor(block, block.Color)
            .Where(a => a.Color != BlockData.EmptyColor);
        if (!MagicEraserEnabled)
            blocksWithSameColor = blocksWithSameColor.Take(1);

        int brokenCellsCount = 0;
        foreach (BlockData cell in blocksWithSameColor)
        {
            if (cell.Cells == 1)
            {
                cell.Color.Value = BlockData.EmptyColor;
            }
            else
            {
                Blocks.SetBlock(new BlockData(cell.Top, cell.Left));
                brokenCellsCount++;
            }
        }

        if (brokenCellsCount > 0)
            OnBlocksChanged();
    }

    private IEnumerable<BlockData> FindBlocksWithSameColor(BlockData startBlock, Color color)
    {
        var queue = new Queue<BlockData>();
        var done = new HashSet<BlockData>();
        queue.Enqueue(startBlock);
        done.Add(startBlock);

        while (queue.Any())
        {
            var block = queue.Dequeue();

            yield return block;

            var neighborIdx = Blocks.GetNeighborIndices(block);

            foreach (var idx in neighborIdx)
            {
                if (idx < 0 || idx >= Blocks.Width * Blocks.Height)
                    continue;
                var neighbor = Blocks[idx];
                if (neighbor.Color == color && done.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
    }

    private void SaveLevel()
    {
        string snapshotXml = Blocks.CreateSnapshotXml();

        byte[] compressed = SevenZipHelper.Compress(Encoding.UTF8.GetBytes(snapshotXml));

        if (!Directory.Exists(SnapshotsDirectory))
            Directory.CreateDirectory(SnapshotsDirectory);

        var filePath = Path.GetFullPath($"{SnapshotsDirectory}/{LevelName.Value}.c.snapshot");

        if (File.Exists(filePath))
            if (MessageBox.Show(Application.Current.MainWindow!,
                    $"The file '{filePath}' does already exists. Overwrite?",
                    "Save Level", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        File.WriteAllBytes(filePath, compressed);

        if (MessageBox.Show($"Successfully saved {Path.GetFileName(filePath)}\nOpen Output Folder?", "Save Level", MessageBoxButton.YesNo,
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

    protected virtual void OnBlocksChanged()
    {
        BlocksChanged?.Invoke();
        UpdateLevelFullness();
    }

    private void UpdateLevelFullness()
    {
        LevelFullness.Value = Blocks.Count(a => a.Color.Value != BlockData.EmptyColor) * 5 + 10; // Add 10 for Start and Goal
    }

    private void PushUndoData(IEnumerable<BlockData> undoData)
    {
        _undoHistory.Push(undoData.ToArray());
        _redoHistory.Clear();
        CanRedo.Value = false;
        CanUndo.Value = true;
    }
}