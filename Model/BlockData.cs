using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class BlockData
{
    public static readonly (int Width, int Height)[] BlockSizes =
    {
        (16, 6), (6, 16),
        (8, 8),
        (1, 16), (16, 1),
        (4, 4),
        (2, 4), (4, 2),
        (1, 8), (8, 1),
        (2, 2),
        (1, 4), (4, 1),
        (1, 3), (3, 1),
        (1, 2), (2, 1),
        (1, 1)
    };

    public static readonly Color EmptyColor = System.Windows.Media.Color.FromArgb(20, 255, 255, 255);

    public BlockData(int row, int col, Layer layer, Color? color = default) : this(row, col, 1, 1, layer, color ?? EmptyColor)
    {
    }

    public BlockData(int row, int col, int width, int height, Layer layer, Color? color = default)
    {
        Left = col;
        Right = col + width - 1;
        Top = row;
        Bottom = row + height - 1;
        Color.Value = color ?? EmptyColor;
        Layer.Value = layer;
    }

    public BlockData(BlockData block)
    {
        Left = block.Left;
        Right = block.Right;
        Top = block.Top;
        Bottom = block.Bottom;
        Layer.Value = block.Layer;
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
    public Property<Layer> Layer { get; } = new();
}