using System.Collections.Generic;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class BlockData
{
    public BlockData(int row, int col, Color color = default) : this(row, col, 1, 1, color)
    {
    }

    public BlockData(int row, int col, int width, int height, Color color = default)
    {
        Left = col;
        Right = col + width - 1;
        Top = row;
        Bottom = row + height - 1;
        Color.Value = color;
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

public static class BlockDataExtensions
{
    public static IEnumerable<BlockData> BreakToCells(this BlockData block)
    {
        if(block.Cells == 1)
            yield return block;
        else
        {
            for (int row = block.Top; row <= block.Bottom; row++)
            {
                for (int col = block.Left; col <= block.Right; col++)
                {
                    yield return new BlockData(row, col, block.Color);
                }
            }
        }
    }
}