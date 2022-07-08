using System;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public struct BlockData
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


    //public void Update(Color color, Layer layer)
    //{
    //    Color = color;
    //    Layer = layer;
    //}

    //public void Update(int top, int bottom, int left, int right, Color color, Layer layer)
    //{
    //    Top = top;
    //    Bottom = bottom;
    //    Left = left;
    //    Right = right;
    //    Update(color, layer);
    //}

    //public void Clear()
    //{
    //    Update(Row, Row, Col, Col, EmptyColor, Layer);
    //}
    public BlockData CopyCleared()
    {
        return new BlockData(Row, Col, Top, Bottom, Left, Right, Layer.Default, EmptyColor);
    }

    public BlockData Copy(Color color, Layer layer)
    {
        return new BlockData(Row, Col, Top, Bottom, Left, Right, layer, color);
    }
}