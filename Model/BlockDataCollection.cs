using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.Model;

public class BlockDataCollection : IEnumerable<BlockData>
{
    private readonly BlockData[,] _blocks;

    public int Width { get; }
    public int Height { get; }
    public int Count => Width * Height;

    public BlockDataCollection(int width, int height)
    {
        Width = width;
        Height = height;

        _blocks = new BlockData[Height, Width];

        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _blocks[row, col] = new BlockData(row, col, Layer.Default);
            }
        }
    }

    public BlockDataCollection(int width, int height, IList<Color> colors)
    {
        Width = width;
        Height = height;

        if (colors.Count != Width * Height)
            throw new ArgumentException($"Passed colors must match Width*Height={Width * Height} but was {colors.Count}");

        _blocks = new BlockData[Height, Width];

        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _blocks[row, col] = new BlockData(row, col, Layer.Default, colors[GetIndex(row, col)]);
            }
        }
    }

    public BlockData this[int idx] => _blocks[idx / Width, idx % Width];
    public BlockData this[int row, int col] => _blocks[row, col];

    public IEnumerable<BlockData> ReplaceBlock(BlockData block) => ReplaceBlock(new[] { block });
    public IEnumerable<BlockData> ReplaceBlock(IEnumerable<BlockData> blocks)
    {
        foreach (BlockData block in blocks)
        {
            for (var row = block.Top; row <= block.Bottom; row++)
            {
                for (int col = block.Left; col <= block.Right; col++)
                {
                    yield return _blocks[row, col] = new BlockData(row, col, block.Top, block.Bottom, block.Left, block.Right, block.Layer, block.Color);
                }
            }
        }
    }

    private IEnumerable<BlockData> GetCells(BlockData block)
    {
        for (var row = block.Top; row <= block.Bottom; row++)
        {
            for (int col = block.Left; col <= block.Right; col++)
            {
                yield return _blocks[row, col];
            }
        }
    }

    public IEnumerable<int> GetNeighborIndices(BlockData block)
    {
        for (var row = 0; row < block.Height; row++)
        {
            yield return GetIndex(block.Top + row, block.Left - 1);
            yield return GetIndex(block.Top + row, block.Right + 1);
        }

        for (var col = 0; col < block.Width; col++)
        {
            yield return GetIndex(block.Top - 1, block.Left + col);
            yield return GetIndex(block.Top + 1, block.Right + col);
        }
    }

    private int GetIndex(int row, int col)
    {
        if (row < 0) row = 0;
        if (col < 0) col = 0;
        if (row >= Height) row = Height - 1;
        if (col >= Width) col = Width - 1;
        return row * Width + col;
    }

    public IEnumerator<BlockData> GetEnumerator() => new BlockEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class BlockEnumerator : IEnumerator<BlockData>
    {
        private BlockDataCollection _owner;
        private int _index = -1;

        public BlockEnumerator(BlockDataCollection owner) => _owner = owner;

        public bool MoveNext()
        {
            while (true)
            {
                if (_index == _owner.Count - 1)
                    return false;

                _index++;

                return true;
            }
        }

        public void Reset() => _index = -1;

        public BlockData Current => _owner[_index];

        object IEnumerator.Current => Current;

        public void Dispose() => _owner = null;
    }

    public IEnumerable<BlockData> ClearBlock(BlockData block)
    {
        return Update(block, BlockData.EmptyColor, Layer.Default);
    }

    public IEnumerable<BlockData> Update(BlockData block, Color newColor, Layer newLayer)
    {
        return ReplaceBlock(new BlockData(
            block.Row, block.Col,
            block.Top, block.Bottom,
            block.Left, block.Right,
            newLayer, newColor));
    }
}