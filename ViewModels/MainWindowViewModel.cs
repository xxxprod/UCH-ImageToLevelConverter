using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Win32;
using SevenZip.Compression.LZMA;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class PixelData
{
    public int Row { get; set; }
    public int Col { get; set; }
    public Color Color { get; set; }
}

public class MainWindowViewModel : ViewModelBase
{
    private BitmapSource _originalImage;
    private List<PixelData> _pixels;
    private int _width = 70;
    private int _height = 50;
    private int _maxColors = 16;

    public MainWindowViewModel()
    {
        SelectImageToConvertCommand = new DelegateCommand(_ => SelectImageToConvert());
        SelectTransparentColorCommand = new DelegateCommand(o => RemoveSelectedColor((PixelData)o));
        SaveLevelCommand = new DelegateCommand(o => SavePixelData());
    }

    public DelegateCommand SelectImageToConvertCommand { get; }
    public DelegateCommand SelectTransparentColorCommand { get; }
    public DelegateCommand SaveLevelCommand { get; }

    public BitmapSource OriginalImage
    {
        get => _originalImage;
        set { _originalImage = value; OnPropertyChanged(); UpdatePreview(); }
    }

    public List<PixelData> Pixels
    {
        get => _pixels;
        set { _pixels = value; OnPropertyChanged(); }
    }

    public int MaxColors
    {
        get => _maxColors;
        set { _maxColors = value; OnPropertyChanged(); UpdatePreview(); }
    }

    public int Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); UpdatePreview(); }
    }

    public int Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); UpdatePreview(); }
    }

    private void SelectImageToConvert()
    {
        OriginalImage = new BitmapImage(new Uri(Path.GetFullPath("testimage.jpg")));
        return;

        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            OriginalImage = new BitmapImage(new Uri(openFileDialog.FileName));
        }
    }

    private void RemoveSelectedColor(PixelData pixel)
    {
        Pixels = Pixels.Where(a => a.Color != pixel.Color).ToList();
    }

    private void UpdatePreview()
    {
        //WriteTransformedBitmapToFile<BmpBitmapEncoder>(resizedImage, "test.bmp");


        Pixels = OriginalImage
            .Resize(Width, Height)
            .Format(PixelFormats.Rgb24)
            .KNNReduceColors(MaxColors)
            .GetPixelData()
            .ToList();
    }

    private void SavePixelData()
    {
        var blocks = Pixels.Select<PixelData, object>((p, i) => new XElement("block",
            new XAttribute("sceneID", i),
            new XAttribute("blockID", 40),
            new XAttribute("pX", p.Col - Width / 2),
            new XAttribute("pY", Height / 2 - p.Row),
            new XAttribute("pZ", 0),
            new XAttribute("rX", 0), new XAttribute("rY", 0), new XAttribute("rZ", 0),
            new XAttribute("sX", 1f), new XAttribute("sY", 1f), new XAttribute("sZ", 1),
            new XAttribute("placeableID", i * 10),
            new XAttribute("colR", p.Color.R / 512.0f),
            new XAttribute("colG", p.Color.G / 512.0f),
            new XAttribute("colB", p.Color.B / 512.0f)
        ));

        var minX = Pixels.Min(a => a.Col);
        var maxX = Pixels.Max(a => a.Col);
        var minY = Pixels.Min(a => a.Row);
        var maxY = Pixels.Max(a => a.Row);


        var moved = new[]
        {
            new XElement("moved",
                new XAttribute("placeableID", 8),
                new XAttribute("path", "Ceiling"),
                new XAttribute("pX", 0),
                new XAttribute("pY", maxY - Height / 2 + 6),
                new XAttribute("pZ", 0),
                new XAttribute("rX", 0), new XAttribute("rY", 0), new XAttribute("rZ", 180),
                new XAttribute("sX", 1f), new XAttribute("sY", 1f), new XAttribute("sZ", 1)),
            new XElement("moved",
                new XAttribute("placeableID", 7),
                new XAttribute("path", "DeathPit"),
                new XAttribute("pX", 0),
                new XAttribute("pY", minY - Height / 2 - 4),
                new XAttribute("pZ", 0),
                new XAttribute("rX", 0), new XAttribute("rY", 0), new XAttribute("rZ", 0),
                new XAttribute("sX", 1f), new XAttribute("sY", 1f), new XAttribute("sZ", 1)),
            new XElement("moved",
                new XAttribute("placeableID", 9),
                new XAttribute("path", "LeftWall"),
                new XAttribute("pX", minX - Width / 2 - 5),
                new XAttribute("pY", 0),
                new XAttribute("pZ", 0),
                new XAttribute("rX", 0), new XAttribute("rY", 0), new XAttribute("rZ", 270),
                new XAttribute("sX", 1f), new XAttribute("sY", 1f), new XAttribute("sZ", 1)),
            new XElement("moved",
                new XAttribute("placeableID", 6),
                new XAttribute("path", "RightWall"),
                new XAttribute("pX", maxX - Width / 2 + 5),
                new XAttribute("pY", 0),
                new XAttribute("pZ", 0),
                new XAttribute("rX", 0), new XAttribute("rY", 0), new XAttribute("rZ", 90),
                new XAttribute("sX", 1f), new XAttribute("sY", 1f), new XAttribute("sZ", 1))
        };

        var saveFileContents = new XElement("scene", blocks.Concat(moved)
            .Concat(new[]
            {
                new XAttribute("levelSceneName", "BlankLevel"),
                new XAttribute("saveFormatVersion", 1),
                new XAttribute("customLevelBackground", 2),
                new XAttribute("customLevelMusic", 10),
                new XAttribute("customLevelAmbience", 10)
            })
        ).ToString(SaveOptions.None);

        var compressed = SevenZipHelper.Compress(Encoding.UTF8.GetBytes(saveFileContents));

        File.WriteAllBytes("TestLevel.c.snapshot", compressed);
    }

    public void WriteTransformedBitmapToFile<T>(BitmapSource bitmapSource, string fileName) where T : BitmapEncoder, new()
    {
        var frame = BitmapFrame.Create(bitmapSource); ;
        var encoder = new T();
        encoder.Frames.Add(frame);
        using var fs = new FileStream(fileName, FileMode.Create);
        encoder.Save(fs);
    }
}