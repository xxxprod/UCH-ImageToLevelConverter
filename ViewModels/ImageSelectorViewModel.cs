using System;
using System.Linq;
using System.Windows;
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
        SelectImageToConvertCommand = new DelegateCommand(_ => SelectImageToConvert());
        NavigateToLevelEditorCommand = new DelegateCommand(_ => OriginalImage.Value != null, _ => { });
        PixelGridActionCommand = new DelegateCommand(_ => throw new NotImplementedException());

        RegisterPropertyChangedCallback(UpdatePreview,
            OriginalImage, Width, Height, MaxColors);
    }

    public DelegateCommand SelectImageToConvertCommand { get; }
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


    private void SelectImageToConvert()
    {
        var openFileDialog = new OpenFileDialog();

        if (openFileDialog.ShowDialog() == true)
        {
            ImageFileName.Value = openFileDialog.FileName;
            OriginalImage.Value = new BitmapImage(new Uri(openFileDialog.FileName));
        }
    }

    private void UpdatePreview()
    {
        if (OriginalImage.Value == null)
            return;

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
        LevelFullness.Value = Blocks.Count(a => a.Color.Value != new Color()) * 5 + 10; // Add 10 for Start and Goal
    }
}