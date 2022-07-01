using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Linq;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorViewModel : ViewModelBase
{
    public LevelEditorViewModel()
    {
        SelectTransparentColorCommand = new DelegateCommand(o => RemoveSelectedColor((PixelData)o));
        SaveLevelCommand = new DelegateCommand(o => SavePixelData());
    }
    
    public DelegateCommand SelectTransparentColorCommand { get; }
    public DelegateCommand SaveLevelCommand { get; }
    
    public Property<string> LevelName { get; } = new();

    public Property<PixelData[]> Pixels { get; } = new();
    public Property<int> Width { get; } = new(70);
    public Property<int> Height { get; } = new(50);
    public Property<int?> MaxColors { get; } = new(null);
    public Property<int> WallOffsetLeft { get; } = new(5);
    public Property<int> WallOffsetRight { get; } = new(5);
    public Property<int> WallOffsetTop { get; } = new(5);
    public Property<int> WallOffsetBottom { get; } = new(5);

    

    private void RemoveSelectedColor(PixelData pixel)
    {
        Pixels.Value = Pixels.Value.Where(a => a.Color != pixel.Color).ToArray();
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
        ).ToString(SaveOptions.DisableFormatting);

        var compressed = SevenZipHelper.Compress(Encoding.UTF8.GetBytes(saveFileContents));

        File.WriteAllBytes("TestLevel.c.snapshot", compressed);
    }
}