using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorViewModel : ViewModelBase, IPixelGridViewModel
{
    private const string SnapshotsDirectory = "snapshots";
    public readonly Color EmptyColor = new();
    private BlockDataCollection _blocks;

    public LevelEditorViewModel()
    {
        PixelGridActionCommand = new DelegateCommand(o => OnPixelGridAction((BlockData)o));
        OptimizeAllCommand = new DelegateCommand(o => OptimizeAll());
        SaveLevelCommand = new DelegateCommand(o => SaveLevel());
        NavigateToImageSelectorCommand = new DelegateCommand(_ => { });

        var pixelGridActions = new[]
        {
            PixelEraserEnabled,
            MagicEraserEnabled,
            ColorPickingEnabled,
            PaintBrushEnabled,
            OptimizerEnabled
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
    public DelegateCommand OptimizeAllCommand { get; }
    public DelegateCommand NavigateToImageSelectorCommand { get; }

    public Property<string> LevelName { get; } = new();

    public IntProperty WallOffsetLeft { get; } = new(5, 0, 20);
    public IntProperty WallOffsetRight { get; } = new(5, 0, 20);
    public IntProperty WallOffsetTop { get; } = new(5, 0, 20);
    public IntProperty WallOffsetBottom { get; } = new(5, 0, 20);
    public Property<bool> ColorPickingEnabled { get; } = new();
    public Property<bool> PixelEraserEnabled { get; } = new();
    public Property<bool> MagicEraserEnabled { get; } = new();
    public Property<bool> PaintBrushEnabled { get; } = new();
    public Property<bool> OptimizerEnabled { get; } = new();

    public Property<Color> SelectedColor { get; } = new(Colors.Black);

    public DelegateCommand PixelGridActionCommand { get; }
    public IntProperty LevelFullness { get; } = new();

    public BlockDataCollection Blocks
    {
        get => _blocks;
        set { _blocks = value; OnBlocksChanged(); }
    }

    public event Action BlocksChanged;

    public Property<bool> EditorEnabled { get; } = new();


    private void OnPixelGridAction(BlockData blockData)
    {
        if (PixelEraserEnabled || MagicEraserEnabled) ErasePixel(blockData);
        else if (ColorPickingEnabled) SelectedColor.Value = blockData.Color;
        else if (PaintBrushEnabled) blockData.Color.Value = SelectedColor.Value;
        else if (OptimizerEnabled) OptimizeSection(blockData);

        UpdateLevelFullness();
    }


    private void OptimizeAll()
    {
        var blocksByColor = Blocks
            .GroupBy(a => a.Color)
            .ToDictionary(
                a => a.Key,
                a => a.SelectMany(b => b.BreakToCells()).ToList()
            );

        foreach ((Color color, List<BlockData> blocks) in blocksByColor)
            OptimizeBlocks(blocks);

        OnBlocksChanged();
    }

    private void OptimizeSection(BlockData block)
    {
        var blocksToOptimize = FindBlocksWithSameColor(block, block.Color)
            .SelectMany(a => a.BreakToCells())
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
        foreach (var foundBlock in FindBlocksWithSameColor(block, block.Color))
        {
            foundBlock.Color.Value = EmptyColor;

            if (!MagicEraserEnabled)
                break;
        }
    }

    private IEnumerable<BlockData> FindBlocksWithSameColor(BlockData startBlock, Color color)
    {
        if (color == EmptyColor)
            yield break;

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
        var snapshotXml = CreateSnapshotXml();

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

    private string CreateSnapshotXml()
    {
        var activeBlocks = Blocks
            .Where(a => a.Color.Value != EmptyColor)
            .ToArray();

        var blocks = activeBlocks.Select<BlockData, object>(CreateBlockXml).ToArray();

        var minX = activeBlocks.Min(a => a.Left);
        var maxX = activeBlocks.Max(a => a.Right);
        var minY = activeBlocks.Min(a => a.Top);
        var maxY = activeBlocks.Max(a => a.Bottom);


        var standardElements = new[]
        {
            new XElement("moved",
                new XAttribute("placeableID", 7),
                new XAttribute("path", "DeathPit"),
                new XAttribute("pY", minY - Blocks.Height / 2 - 4 - WallOffsetBottom)),
            new XElement("moved",
                new XAttribute("placeableID", 9),
                new XAttribute("path", "LeftWall"),
                new XAttribute("pX", minX - Blocks.Width / 2 - 5 - WallOffsetLeft),
                new XAttribute("rZ", 270)),
            new XElement("moved",
                new XAttribute("placeableID", 8),
                new XAttribute("path", "Ceiling"),
                new XAttribute("pY", maxY - Blocks.Height / 2 + 6 + WallOffsetTop),
                new XAttribute("rZ", 180)),
            new XElement("moved",
                new XAttribute("placeableID", 6),
                new XAttribute("path", "RightWall"),
                new XAttribute("pX", maxX - Blocks.Width / 2 + 5 + WallOffsetRight),
                new XAttribute("rZ", 90)),
            new XElement("block",
                new XAttribute("sceneID", blocks.Length + 5),
                new XAttribute("blockID", 39),
                new XAttribute("pX", maxX - Blocks.Width / 2 + WallOffsetRight -1),
                new XAttribute("pY", minY - Blocks.Height / 2 - WallOffsetBottom + 1),
                new XAttribute("placeableID", 2)),
            new XElement("moved",
                new XAttribute("placeableID", 11),
                new XAttribute("path", "StartPlank"),
                new XAttribute("pX", minX - Blocks.Width / 2 - WallOffsetLeft + 1.5),
                new XAttribute("pY", minY - Blocks.Height / 2 - WallOffsetBottom + 1))
        };


        var saveFileContents = new XElement("scene", blocks.Concat(standardElements)
            .Concat(new[]
            {
                new XAttribute("levelSceneName", "BlankLevel"),
                new XAttribute("saveFormatVersion", 1),
                new XAttribute("customLevelBackground", 2),
                new XAttribute("customLevelMusic", 10),
                new XAttribute("customLevelAmbience", 10)
            })
        ).ToString(SaveOptions.DisableFormatting);
        return saveFileContents;
    }

    private XElement CreateBlockXml(BlockData block, int index)
    {
        int blockId;// = 40;
        var x = block.Left - Blocks.Width / 2;
        var y = Blocks.Height / 2 - block.Top;
        var rot = 0;

        if (block.Cells == 1)
            blockId = 40;
        else if (block.Cells == 2)
        {
            blockId = 41;
            if (block.Width == 1)
            {
                rot = 90;
                y -= 1;
            }
        }
        else if (block.Cells == 3)
        {
            blockId = 42;
            if (block.Height == 1)
                x += 1;
            else
            {
                rot = 90;
                y -= 1;
            }
        }
        else if (block.Cells == 4 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 43;
            if (block.Height == 1)
                x += 1;
            else
            {
                rot = 90;
                y -= 2;
            }
        }
        else if (block.Cells == 8 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 44;
            if (block.Height == 1)
                x += 4;
            else
            {
                rot = 90;
                y -= 3;
            }
        }
        else if (block.Cells == 16 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 45;
            if (block.Height == 1)
                x += 7;
            else
            {
                rot = 90;
                y -= 8;
            }
        }
        else if (block.Cells == 8 && (block.Width == 2 || block.Height == 2))
        {
            blockId = 46;
            if (block.Height == 2)
                x += 1;
            else
            {
                rot = 90;
                y -= 2;
            }
        }
        else if (block.Cells == 4 && block.Width == 2)
            blockId = 47;
        else if (block.Cells == 16 && block.Width == 4)
        {
            blockId = 48;
            x += 1;
            y -= 1;
        }
        else if (block.Cells == 64)
        {
            blockId = 49;
            x += 3;
            y -= 3;
        }
        else if (block.Cells == 96)
        {
            blockId = 50;
            if (block.Width == 6)
            {
                x += 2;
                y -= 7;
            }
            else
            {
                rot = 90;
                x += 7;
                y -= 3;
            }
        }
        else throw new NotSupportedException(
                $"Block with Height {block.Height} and Width {block.Width} is not supported");

        return new XElement("block",
            new XAttribute("sceneID", index),
            new XAttribute("blockID", blockId),
            new XAttribute("pX", x),
            new XAttribute("pY", y),
            new XAttribute("rZ", rot),
            new XAttribute("placeableID", 13 + index * 2),
            new XAttribute("colR", block.Color.Value.R / 512.0f),
            new XAttribute("colG", block.Color.Value.G / 512.0f),
            new XAttribute("colB", block.Color.Value.B / 512.0f)
        );
    }

    protected virtual void OnBlocksChanged()
    {
        BlocksChanged?.Invoke();
        UpdateLevelFullness();
    }

    private void UpdateLevelFullness()
    {
        LevelFullness.Value = Blocks.Count(a => a.Color.Value != new Color()) * 5 + 10; // Add 10 for Start and Goal
    }
}