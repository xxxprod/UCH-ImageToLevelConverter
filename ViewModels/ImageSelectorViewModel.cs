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
        AcceptCommand = new DelegateCommand(
            _ => OriginalImage.Value != null,
            _ => Application.Current.MainWindow!.DialogResult = true);

        RegisterPropertyChangedCallback(UpdatePreview,
            OriginalImage,
            Width, Height,
            //WallOffsetLeft, WallOffsetRight,
            //WallOffsetTop, WallOffsetBottom,
            MaxColors);
    }

    public DelegateCommand SelectImageToConvertCommand { get; }
    public DelegateCommand AcceptCommand { get; }

    public Property<BitmapSource> OriginalImage { get; } = new();

    public Property<string> ImageFileName { get; } = new();
    public Property<PixelData[]> Pixels { get; } = new();
    public Property<int> Width { get; } = new(70);
    public Property<int> Height { get; } = new(50);
    public Property<int?> MaxColors { get; } = new(null);
    //public Property<int> WallOffsetLeft { get; } = new(5);
    //public Property<int> WallOffsetRight { get; } = new(5);
    //public Property<int> WallOffsetTop { get; } = new(5);
    //public Property<int> WallOffsetBottom { get; } = new(5);


    private void SelectImageToConvert()
    {
        var openFileDialog = new OpenFileDialog
        {
            InitialDirectory = Environment.CurrentDirectory
        };

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
            .Resize(Width.Value, Height.Value)
            .Format(PixelFormats.Rgb24);

        if (MaxColors.Value.HasValue)
            bitmapSource = bitmapSource.KNNReduceColors(MaxColors.Value.Value);

        Pixels.Value = bitmapSource
            .GetPixelData()
            .ToArray();
    }
}