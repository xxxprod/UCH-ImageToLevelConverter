using System;
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
        PixelGridActionCommand = new DelegateCommand(_ => throw new NotImplementedException());

        RegisterPropertyChangedCallback(UpdatePreview,
            ImageFileName, Width, Height, MaxColors);

        Blocks = new BlockDataCollection(Width, Height);
    }

    public DelegateCommand OpenFileCommand { get; }
    public DelegateCommand NavigateToLevelEditorCommand { get; }
    public DelegateCommand PixelGridActionCommand { get; }

    public Property<BitmapSource> OriginalImage { get; } = new();

    public Property<string> ImageFileName { get; } = new();
    public Property<bool> EditorEnabled { get; } = new();
    public BlockDataCollection Blocks { get; private set; }
    public IntProperty LevelFullness { get; } = new();

    public event Action BlocksChanged;
    public IntProperty Width { get; } = new(70, 0, 150);
    public IntProperty Height { get; } = new(50, 0, 150);
    public NullableIntProperty MaxColors { get; } = new(null, null, 256);


    private void OpenFile()
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true) 
            ImageFileName.Value = openFileDialog.FileName;
    }

    private void UpdatePreview()
    {
        if (ImageFileName.Value == null)
            return;
        
        OriginalImage.Value = new BitmapImage(new Uri(ImageFileName.Value));

        var bitmapSource = OriginalImage.Value
            .Resize(Width, Height)
            .Format(PixelFormats.Rgb24);

        if (MaxColors.Value.HasValue)
            bitmapSource = bitmapSource.KNNReduceColors(MaxColors.Value.Value);

        Blocks = new BlockDataCollection(Width, Height, bitmapSource.GetPixelData());
        OnBlocksChanged();
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

    public void StartRecordingGridActions() => throw new NotImplementedException();
}