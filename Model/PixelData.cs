using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class PixelData
{
    public int Row { get; set; }
    public int Col { get; set; }
    public Property<Color> Color { get; } = new();
}