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
        else if (OptimizerEnabled) OptimizeColor(blockData);
    }

    private void OptimizeColor(BlockData block)
    {
        Blocks.SetBlock(new BlockData(block.Top, block.Left, 2, 2, Colors.Red));

        OnBlocksChanged();

        //OptimizerEnabled.Value = false;
    }

    private void ErasePixel(BlockData block)
    {
        var matchingColor = block.Color.Value;

        if (matchingColor == EmptyColor)
            return;

        var queue = new Queue<BlockData>();
        var done = new HashSet<BlockData>();
        queue.Enqueue(block);

        while (queue.Any())
        {
            block = queue.Dequeue();

            block.Color.Value = EmptyColor;

            if (!MagicEraserEnabled)
                continue;

            HashSet<int> neighborIdx = new();

            for (var r = -1; r <= block.Height; r++)
            {
                neighborIdx.Add(GetIndex(block.Top + r, block.Left - 1));
                neighborIdx.Add(GetIndex(block.Top + r, block.Right + 1));
            }

            for (var c = 0; c < block.Width; c++)
            {
                neighborIdx.Add(GetIndex(block.Top - 1, block.Left + c));
                neighborIdx.Add(GetIndex(block.Top + 1, block.Right + c));
            }

            foreach (var idx in neighborIdx)
            {
                if (idx < 0 || idx >= Blocks.Height * Blocks.Width)
                    continue;

                var neighbor = Blocks[idx];
                if (neighbor.Color == matchingColor && done.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
    }
    private int GetIndex(int row, int col)
    {
        if (row < 0) row = 0;
        if (col < 0) col = 0;
        if (row >= Blocks.Height) row = Blocks.Height;
        if (col >= Blocks.Width) col = Blocks.Width;
        return row * Blocks.Width + col;
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
    }
}