using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Win32;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        SelectImageToConvertCommand = new DelegateCommand(_ => SelectImageToConvert());
        SelectTransparentColorCommand = new DelegateCommand(o => RemoveSelectedColor((PixelData)o));
        SaveLevelCommand = new DelegateCommand(o => SavePixelData());

        Width.PropertyChanged += MaxColors_PropertyChanged;
        Height.PropertyChanged += MaxColors_PropertyChanged;
        MaxColors.PropertyChanged += MaxColors_PropertyChanged;
        WallOffsetLeft.PropertyChanged += MaxColors_PropertyChanged;
        WallOffsetRight.PropertyChanged += MaxColors_PropertyChanged;
        WallOffsetTop.PropertyChanged += MaxColors_PropertyChanged;
        WallOffsetBottom.PropertyChanged += MaxColors_PropertyChanged;
    }

    public DelegateCommand SelectImageToConvertCommand { get; }
    public DelegateCommand SelectTransparentColorCommand { get; }
    public DelegateCommand SaveLevelCommand { get; }

    public BitmapSource OriginalImage { get; set; }

    public NotifyProperty<PixelData[]> Pixels { get; } = new();
    public NotifyProperty<int> Width { get; } = new(70);
    public NotifyProperty<int> Height { get; } = new(50);
    public NotifyProperty<int> MaxColors { get; } = new(64);
    public NotifyProperty<int> WallOffsetLeft { get; } = new(5);
    public NotifyProperty<int> WallOffsetRight { get; } = new(5);
    public NotifyProperty<int> WallOffsetTop { get; } = new(5);
    public NotifyProperty<int> WallOffsetBottom { get; } = new(5);


    private void SelectImageToConvert()
    {
        OriginalImage = new BitmapImage(new Uri(Path.GetFullPath("testimage.jpg")));
        UpdatePreview();
        return;

        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            OriginalImage = new BitmapImage(new Uri(openFileDialog.FileName));
        }
    }

    private void RemoveSelectedColor(PixelData pixel)
    {
        Pixels.Value = Pixels.Value.Where(a => a.Color != pixel.Color).ToArray();
    }

    private void MaxColors_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Pixels.Value = OriginalImage
            .Resize(Width.Value, Height.Value)
            .Format(PixelFormats.Rgb24)
            .KNNReduceColors(MaxColors)
            .GetPixelData()
            .ToArray();
    }

    private void SavePixelData()
    {
        var blocks = Pixels.Value.Select<PixelData, object>((p, i) => new XElement("block",
            new XAttribute("sceneID", i),
            new XAttribute("blockID", 40),
            new XAttribute("pX", p.Col - Width / 2),
            new XAttribute("pY", Height / 2 - p.Row),
            new XAttribute("colR", p.Color.R / 512.0f),
            new XAttribute("colG", p.Color.G / 512.0f),
            new XAttribute("colB", p.Color.B / 512.0f)
        ));

        var minX = Pixels.Value.Min(a => a.Col);
        var maxX = Pixels.Value.Max(a => a.Col);
        var minY = Pixels.Value.Min(a => a.Row);
        var maxY = Pixels.Value.Max(a => a.Row);


        var moved = new[]
        {
            new XElement("moved",
                new XAttribute("path", "Ceiling"),
                new XAttribute("pY", maxY - Height / 2 + 6 + WallOffsetTop),
                new XAttribute("rZ", 180)),
            new XElement("moved",
                new XAttribute("placeableID", 7),
                new XAttribute("path", "DeathPit"),
                new XAttribute("pY", minY - Height / 2 - 4 - WallOffsetBottom),
                new XAttribute("rZ", 0)),
            new XElement("moved",
                new XAttribute("path", "LeftWall"),
                new XAttribute("pX", minX - Width / 2 - 5 - WallOffsetLeft),
                new XAttribute("rZ", 270)),
            new XElement("moved",
                new XAttribute("path", "RightWall"),
                new XAttribute("pX", maxX - Width / 2 + 5 + WallOffsetRight),
                new XAttribute("rZ", 90))
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
}