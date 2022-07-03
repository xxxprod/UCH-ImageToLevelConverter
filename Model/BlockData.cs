using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class BlockData
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    public int Height => Bottom - Top + 1;
    public int Width => Right - Left + 1;
    public int Cells => Height * Width;

    public Property<Color> Color { get; } = new();
}