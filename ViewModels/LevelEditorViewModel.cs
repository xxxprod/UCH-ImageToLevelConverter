using System;
using System.Collections.Generic;
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

        MessageBox.Show($"Successfully saved {filePath}", "Save Level", MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private string CreateSnapshotXml()
    {
        var activePixels = Blocks
            .Where(a => a.Color.Value != EmptyColor)
            .ToArray();

        var blocks = activePixels.Select<BlockData, object>((p, i) => new XElement("block",
            new XAttribute("sceneID", i),
            new XAttribute("blockID", 40),
            new XAttribute("pX", p.Left - Blocks.Width / 2),
            new XAttribute("pY", Blocks.Height / 2 - p.Top),
            new XAttribute("colR", p.Color.Value.R / 512.0f),
            new XAttribute("colG", p.Color.Value.G / 512.0f),
            new XAttribute("colB", p.Color.Value.B / 512.0f)
        ));

        var minX = activePixels.Min(a => a.Left);
        var maxX = activePixels.Max(a => a.Left);
        var minY = activePixels.Min(a => a.Top);
        var maxY = activePixels.Max(a => a.Top);


        var standardElements = new[]
        {
            new XElement("moved",
                new XAttribute("path", "Ceiling"),
                new XAttribute("pY", maxY - Blocks.Height / 2 + 6 + WallOffsetTop),
                new XAttribute("rZ", 180)),
            new XElement("moved",
                new XAttribute("placeableID", 7),
                new XAttribute("path", "DeathPit"),
                new XAttribute("pY", minY - Blocks.Height / 2 - 4 - WallOffsetBottom),
                new XAttribute("rZ", 0)),
            new XElement("moved",
                new XAttribute("path", "LeftWall"),
                new XAttribute("pX", minX - Blocks.Width / 2 - 5 - WallOffsetLeft),
                new XAttribute("rZ", 270)),
            new XElement("moved",
                new XAttribute("path", "RightWall"),
                new XAttribute("pX", maxX - Blocks.Width / 2 + 5 + WallOffsetRight),
                new XAttribute("rZ", 90)),
            new XElement("moved",
                new XAttribute("path", "StartPlank"),
                new XAttribute("pX", minX - Blocks.Width / 2 + 0.5),
                new XAttribute("pY", minY - Blocks.Height / 2),
                new XAttribute("rZ", 0))
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