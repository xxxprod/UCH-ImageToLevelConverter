using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Optimizer;
using UCH_ImageToLevelConverter.Tools;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorViewModel : ViewModelBase
{
    private const string SnapshotsDirectory = "snapshots";
    private readonly Stack<BlockData[]> _redoHistory = new();

    private readonly Stack<BlockData[]> _undoHistory = new();
    private BlockDataCollection _blocks;
    private readonly NewLevelPromptViewModel _newLevelPromptViewModel;

    public LevelEditorViewModel()
    {
        UndoCommand = new DelegateCommand(o => UndoAction());
        RedoCommand = new DelegateCommand(o => RedoAction());
        CreateNewLevelCommand = new DelegateCommand(o => CreateNewLevel());
        LoadImageCommand = new DelegateCommand(o => LoadImage());
        SaveLevelCommand = new DelegateCommand(o => SaveLevel());

        LevelEditorTools = new LevelEditorToolsViewModel();
        LevelEditorTools.OptimizeAllCommand.ExecuteCalled += _ => OptimizeAll();
        LevelEditorTools.BreakAllCommand.ExecuteCalled += _ => BreakAll();
        LevelEditorTools.ToolsUpdated += LevelEditorTools_ToolsUpdated;

        Blocks = new BlockDataCollection(70, 50);

        _newLevelPromptViewModel = new NewLevelPromptViewModel();
    }

    public LevelEditorToolsViewModel LevelEditorTools { get; }

    public DelegateCommand SaveLevelCommand { get; }
    public DelegateCommand CreateNewLevelCommand { get; }
    public DelegateCommand LoadImageCommand { get; }
    public DelegateCommand UndoCommand { get; }
    public DelegateCommand RedoCommand { get; }

    public Property<string> LevelName { get; } = new();

    public Property<bool> CanUndo { get; } = new();
    public Property<bool> CanRedo { get; } = new();


    public IntProperty LevelFullness { get; } = new();

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
            OnPropertyChanged();
        }
    }

    public event Action<IEnumerable<BlockData>> BlocksChanged;

    private void LevelEditorTools_ToolsUpdated()
    {
        OnBlocksChanged(Blocks);
    }

    public bool OnPixelGridAction(BlockData blockData, bool saveCurrentState)
    {
        if (!LevelEditorTools.PixelGridActionsEnabled)
            return false;

        if (saveCurrentState && !LevelEditorTools.ColorPickingEnabled)
            PushUndoData(Blocks);

        if (LevelEditorTools.ColorPickingEnabled)
        {
            if (blockData.Color != BlockData.EmptyColor)
                LevelEditorTools.SelectedPaintColor.Value = blockData.Color;
        }
        else if (LevelEditorTools.EraseBlockEnabled || LevelEditorTools.EraseRegionEnabled)
        {
            OnBlocksChanged(EraseBlocks(blockData));
        }
        else if (LevelEditorTools.PaintBrushEnabled || LevelEditorTools.FillBrushEnabled)
        {
            OnBlocksChanged(PaintBlocks(blockData));
        }
        else if (LevelEditorTools.OptimizerEnabled)
        {
            OnBlocksChanged(OptimizeSection(blockData));
            return false;
        }
        else if (LevelEditorTools.BreakBlockEnabled || LevelEditorTools.BreakRegionEnabled)
        {
            OnBlocksChanged(BreakSection(blockData));
        }
        else if (LevelEditorTools.MoveToLayerEnabled || LevelEditorTools.MoveRegionToLayerEnabled)
        {
            OnBlocksChanged(MoveToLayer(blockData));
        }

        return true;
    }


    private void RedoAction()
    {
        if (!_redoHistory.Any())
            return;
        _undoHistory.Push(Blocks.ToArray());

        Blocks.ReplaceBlocks(_redoHistory.Pop());

        CanUndo.Value = _undoHistory.Any();
        CanRedo.Value = _redoHistory.Any();
        OnBlocksChanged(Blocks);
    }

    private void UndoAction()
    {
        if (!_undoHistory.Any())
            return;
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

        RandomBlockOptimizer optimizer = new(Blocks.ToArray());

        OnBlocksChanged(Blocks.ReplaceBlocks(optimizer.Optimize(LevelEditorTools.ColorSimilarityPercentage)));
    }

    private IEnumerable<BlockData> MoveToLayer(BlockData blockData)
    {
        return UpdateBlocks(blockData, !LevelEditorTools.MoveRegionToLayerEnabled,
            block => Blocks.Update(block, block.Color, LevelEditorTools.HighlightedLayer.Value.Layer));
    }

    private IEnumerable<BlockData> PaintBlocks(BlockData blockData)
    {
        return UpdateBlocks(blockData, !LevelEditorTools.FillBrushEnabled,
            block => Blocks.Update(block, LevelEditorTools.SelectedPaintColor,
                LevelEditorTools.HighlightedLayer.Value.Layer));
    }

    private IEnumerable<BlockData> EraseBlocks(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            return Enumerable.Empty<BlockData>();

        return UpdateBlocks(blockData, !LevelEditorTools.EraseRegionEnabled, block => Blocks.ClearBlock(block));
    }

    private IEnumerable<BlockData> BreakSection(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            yield break;

        IEnumerable<BlockData> blocksToOptimize = GetRegionBlocks(blockData);

        foreach (BlockData block in blocksToOptimize)
        {
            foreach (BlockData updatedBlock in Blocks.ReplaceBlocks(block.BreakToCells()))
                yield return updatedBlock;

            if (!LevelEditorTools.BreakRegionEnabled)
                yield break;
        }
    }

    private IEnumerable<BlockData> OptimizeSection(BlockData blockData)
    {
        if (blockData.Color == BlockData.EmptyColor)
            return Enumerable.Empty<BlockData>();

        IEnumerable<BlockData> blocksToOptimize = GetRegionBlocks(blockData);

        RandomBlockOptimizer optimizer = new(blocksToOptimize.ToArray());

        return Blocks.ReplaceBlocks(optimizer.Optimize(LevelEditorTools.ColorSimilarityPercentage));
    }

    private IList<BlockData> UpdateBlocks(BlockData origin, bool onlyFirstBlock,
        Func<BlockData, IEnumerable<BlockData>> updateBlock)
    {
        IEnumerable<BlockData> blocksWithSameColor = GetRegionBlocks(origin);

        List<BlockData> updatedBlocks = new();
        foreach (BlockData block in blocksWithSameColor)
        {
            updatedBlocks.AddRange(updateBlock(block));

            if (onlyFirstBlock)
                break;
        }
        return updatedBlocks;
    }

    private IEnumerable<BlockData> GetRegionBlocks(BlockData blockData)
    {
        return Blocks.FindBlocksWithSameColor(blockData, LevelEditorTools.ColorSimilarityPercentage,
            LevelEditorTools.GetActiveLayers());
    }

    private void CreateNewLevel()
    {
        NewLevelPromptView view = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = _newLevelPromptViewModel
        };

        if (view.ShowDialog() == true)
        {
            Blocks = new BlockDataCollection(
                _newLevelPromptViewModel.Width,
                _newLevelPromptViewModel.Height
            );
        }
    }

    private void LoadImage()
    {
        OpenFileDialog openFileDialog = new();
        if (openFileDialog.ShowDialog() != true)
            return;

        BitmapImage image = new(new Uri(openFileDialog.FileName));

        BitmapSource bitmapSource = image
            .Resize(100, 100)
            .Format(PixelFormats.Rgb24);

        Blocks = new BlockDataCollection(100, 100, bitmapSource.GetColorData().ToArray());
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
            ProcessStartInfo startInfo = new()
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
            ? 0
            : Blocks.GetDistinctNonEmptyBlocks().Count() * 5 + 10; // Add 10 for Start and Goal
    }

    private void PushUndoData(IEnumerable<BlockData> undoData)
    {
        _undoHistory.Push(undoData.ToArray());
        _redoHistory.Clear();
        CanRedo.Value = false;
        CanUndo.Value = true;
    }
}