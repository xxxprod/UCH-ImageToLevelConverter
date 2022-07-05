using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class BlockData
{
    public static readonly Color EmptyColor = System.Windows.Media.Color.FromArgb(20, 255, 255, 255);

    public BlockData(int row, int col, Color? color = default) : this(row, col, 1, 1, color ?? EmptyColor)
    {
    }

    public BlockData(int row, int col, int width, int height, Color? color = default)
    {
        Left = col;
        Right = col + width - 1;
        Top = row;
        Bottom = row + height - 1;
        Color.Value = color ?? EmptyColor;
    }

    public BlockData(BlockData block)
    {
        Left = block.Left;
        Right = block.Right;
        Top = block.Top;
        Bottom = block.Bottom;
        Color.Value = block.Color;
    }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public int Height => Bottom - Top + 1;
    public int Width => Right - Left + 1;
    public int Cells => Height * Width;

    public Property<Color> Color { get; } = new();
}