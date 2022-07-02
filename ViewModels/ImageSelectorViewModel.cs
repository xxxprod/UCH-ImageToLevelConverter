using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class ImageSelectorViewModel : ViewModelBase
{
    public ImageSelectorViewModel()
    {
        SelectImageToConvertCommand = new DelegateCommand(_ => SelectImageToConvert());
        NavigateToLevelEditorCommand = new DelegateCommand(_ => OriginalImage.Value != null, _ => { });

        RegisterPropertyChangedCallback(UpdatePreview,
            OriginalImage, Width, Height, MaxColors);
    }

    public DelegateCommand SelectImageToConvertCommand { get; }
    public DelegateCommand NavigateToLevelEditorCommand { get; }

    public Property<BitmapSource> OriginalImage { get; } = new();

    public Property<string> ImageFileName { get; } = new();
    public Property<PixelData[]> Pixels { get; } = new();
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

        Pixels.Value = bitmapSource
            .GetPixelData()
            .ToArray();
    }
}