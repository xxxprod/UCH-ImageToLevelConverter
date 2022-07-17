using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace UCH_ImageToLevelConverter.Model;

public class BlockDataCoordinatesComparer : IEqualityComparer<BlockData>
{
    public bool Equals(BlockData x, BlockData y)
    {
        return x.Top == y.Top && x.Bottom == y.Bottom && x.Left == y.Left && x.Right == y.Right;
    }

    public int GetHashCode(BlockData obj)
    {
        return HashCode.Combine(obj.Top, obj.Bottom, obj.Left, obj.Right);
    }
}

public readonly struct BlockData
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

    public static readonly Color EmptyColor = new();

    public BlockData(int row, int col, Layer layer, Color? color = default) : this(row, col, row, row, col, col, layer, color ?? EmptyColor)
    {
    }

    public BlockData(int row, int col, int top, int bottom, int left, int right, Layer layer, Color? color = default)
    {
        Row = row;
        Col = col;

        Top = top;
        Bottom = bottom;

        Left = left;
        Right = right;

        Color = color ?? EmptyColor;
        Layer = layer;
    }

    public int Row { get; }
    public int Col { get; }

    public int Top { get; }
    public int Bottom { get; }
    public int Left { get; }
    public int Right { get; }
    public Color Color { get; }
    public Layer Layer { get; }

    public int Width => Right - Left + 1;
    public int Height => Bottom - Top + 1;
    public int Cells => Height * Width;
}