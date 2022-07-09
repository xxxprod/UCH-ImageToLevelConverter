using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class ImageSelectorViewModel : ViewModelBase, IPixelGridViewModel
{
    public ImageSelectorViewModel()
    {
        OpenFileCommand = new DelegateCommand(_ => OpenFile());
        NavigateToLevelEditorCommand = new DelegateCommand(_ => { });
        Layers = Enum.GetValues<Layer>().ToDictionary(a => a, a => new LayerViewModel(a, true));
        RegisterPropertyChangedCallback(UpdatePreview,
            ImageFileName, Width, Height, MaxColors);
        UpdatePreview();
    }

    public DelegateCommand OpenFileCommand { get; }
    public DelegateCommand NavigateToLevelEditorCommand { get; }

    public Property<BitmapSource> OriginalImage { get; } = new();

    public Property<string> ImageFileName { get; } = new();
    public IntProperty Width { get; } = new(100, 0, 150);
    public IntProperty Height { get; } = new(100, 0, 150);
    public NullableIntProperty MaxColors { get; } = new(null, null, 256);
    public Property<bool> EditorEnabled { get; } = new();
    public Property<bool> HighlightLayer { get; } = new();
    public Property<LayerViewModel> HighlightedLayer { get; } = new();
    public Dictionary<Layer, LayerViewModel> Layers { get; }
    public BlockDataCollection Blocks { get; private set; }
    public IntProperty LevelFullness { get; } = new();

    public event Action<IEnumerable<BlockData>> BlocksChanged;

    public bool OnPixelGridAction(BlockData blockData)
    {
        throw new NotImplementedException();
    }

    public void StartRecordingGridActions()
    {
        throw new NotImplementedException();
    }


    private void OpenFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
            ImageFileName.Value = openFileDialog.FileName;
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrWhiteSpace(ImageFileName.Value))
        {
            Blocks = new BlockDataCollection(Width, Height);
        }
        else
        {
            OriginalImage.Value = new BitmapImage(new Uri(ImageFileName.Value));

            BitmapSource bitmapSource = OriginalImage.Value
                .Resize(Width, Height)
                .Format(PixelFormats.Rgb24);

            if (MaxColors.Value.HasValue)
                bitmapSource = bitmapSource.KNNReduceColors(MaxColors.Value.Value);

            Blocks = new BlockDataCollection(Width, Height, bitmapSource.GetColorData().ToArray());
        }

        OnBlocksChanged();
    }

    protected virtual void OnBlocksChanged()
    {
        BlocksChanged?.Invoke(Blocks);
        UpdateLevelFullness();
    }


    private void UpdateLevelFullness()
    {
        LevelFullness.Value = Blocks.Count(a => a.Color != BlockData.EmptyColor) * 5 + 10; // Add 10 for Start and Goal
    }
}