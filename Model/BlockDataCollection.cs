using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;

namespace UCH_ImageToLevelConverter.Model;

public class BlockDataCollection : IEnumerable<BlockData>
{
    private readonly BlockData[,] _blocks;
    private readonly Dictionary<(int Top, int Bottom, int Left, int Right), BlockData> _uniqueNonEmptyBlocks = new();

    public BlockDataCollection(int width, int height)
    {
        Width = width;
        Height = height;

        _blocks = new BlockData[Height, Width];

        for (int row = 0; row < Height; row++)
            for (int col = 0; col < Width; col++)
                _blocks[row, col] = new BlockData(row, col, Layer.Default);
    }

    public BlockDataCollection(int width, int height, IList<Color> colors)
    {
        Width = width;
        Height = height;

        if (colors.Count != Count)
            throw new ArgumentException(
                $"Passed colors must match Width*Height={Width * Height} but was {colors.Count}");

        _blocks = new BlockData[Height, Width];

        for (int row = 0; row < Height; row++)
            for (int col = 0; col < Width; col++)
            {
                BlockData block = new(row, col, Layer.Default, colors[GetIndex(row, col)]);
                _blocks[row, col] = block;
                _uniqueNonEmptyBlocks.Add((row, row, col, col), block);
            }
    }

    public int Width { get; }
    public int Height { get; }
    public int Count => Width * Height;

    public BlockData this[int idx] => _blocks[idx / Width, idx % Width];
    public BlockData this[int row, int col] => _blocks[row, col];

    public IEnumerator<BlockData> GetEnumerator()
    {
        return new BlockEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public List<BlockData> ReplaceBlock(BlockData block)
    {
        return ReplaceBlocks(new[] { block });
    }

    public List<BlockData> ReplaceBlocks(IEnumerable<BlockData> blocks)
    {
        List<BlockData> replacedBlocks = new();
        HashSet<(int top, int bottom, int left, int right)> removedBlocks = new();

        foreach (BlockData block in blocks)
        {
            for (int row = block.Top; row <= block.Bottom; row++)
                for (int col = block.Left; col <= block.Right; col++)
                {
                    BlockData blockData = _blocks[row, col];

                    _uniqueNonEmptyBlocks.Remove((blockData.Top, blockData.Bottom, blockData.Left, blockData.Right));

                    replacedBlocks.Add(_blocks[row, col] = new BlockData(row, col, block.Top, block.Bottom, block.Left,
                        block.Right, block.Layer, block.Color));
                }

            if (block.Color != BlockData.EmptyColor)
                _uniqueNonEmptyBlocks.Add((block.Top, block.Bottom, block.Left, block.Right), block);
        }

        return replacedBlocks;
    }

    public IEnumerable<int> GetNeighborIndices(BlockData block)
    {
        for (int row = block.Top; row <= block.Bottom; row++)
        {
            yield return GetIndex(row, block.Left - 1);
            yield return GetIndex(row, block.Right + 1);
        }

        for (int col = block.Left; col <= block.Right; col++)
        {
            yield return GetIndex(block.Top - 1, col);
            yield return GetIndex(block.Bottom + 1, col);
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

    public IEnumerable<BlockData> GetDistinctNonEmptyBlocks()
    {
        return _uniqueNonEmptyBlocks.Values;
    }

    public bool IsOutOfBounds(int row, int col)
    {
        return row < 0 || col < 0 || row >= Height || col >= Width;
    }

    private class BlockEnumerator : IEnumerator<BlockData>
    {
        private int _index = -1;
        private BlockDataCollection _owner;

        public BlockEnumerator(BlockDataCollection owner)
        {
            _owner = owner;
        }

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

        public void Reset()
        {
            _index = -1;
        }

        public BlockData Current => _owner[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _owner = null;
        }
    }
}