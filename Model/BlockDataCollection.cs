using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UCH_ImageToLevelConverter.Model;

public class BlockDataCollection : IEnumerable<BlockData>
{
    private readonly int[] _blockRefs;
    private readonly List<BlockData> _blocks;

    public BlockDataCollection(int width, int height, IEnumerable<BlockData> blocks)
    {
        Width = width;
        Height = height;
        _blockRefs = new int[Width * Height];
        for (int i = 0; i < _blockRefs.Length; i++) _blockRefs[i] = i;
        _blocks = blocks.ToList();
    }

    public int Width { get; }
    public int Height { get; }

    public BlockData this[int idx]
    {
        get
        {
            var blockRef = _blockRefs[idx];
            return _blocks[blockRef];
        }
    }

    public void SetBlock(BlockData block)
    {
        var cellIdx = GetCellIndices(block).ToArray();
        var blockRefs = cellIdx
            .Select(i => _blockRefs[i])
            .Where(r => r >= 0)
            .Distinct()
            .ToArray();
        var placedBlocks = blockRefs
            .Select(r => _blocks[r])
            .ToArray();

        foreach (var placedBlock in placedBlocks)
            RemoveBlock(placedBlock);

        _blocks.Add(block);

        foreach (var idx in cellIdx)
            _blockRefs[idx] = _blocks.Count - 1;

        for (int i = 0; i < _blockRefs.Length; i++)
        {
            if (_blockRefs[i] == -1)
            {
                int row = i / Width;
                int col = i % Width;

                _blocks.Add(new BlockData(row, col));
                _blockRefs[i] = _blocks.Count - 1;
            }
        }
    }

    private void RemoveBlock(BlockData block)
    {
        var cellIdx = GetCellIndices(block).ToArray();
        foreach (var idx in cellIdx)
            _blockRefs[idx] = -1;

        var blockRef = _blocks.IndexOf(block);

        for (int i = 0; i < _blockRefs.Length; i++)
        {
            if (_blockRefs[i] > blockRef)
                _blockRefs[i]--;
        }

        _blocks.Remove(block);
    }

    private IEnumerable<int> GetCellIndices(BlockData block)
    {
        for (var row = 0; row < block.Height; row++)
        {
            for (int col = 0; col < block.Width; col++)
            {
                yield return GetIndex(block.Top + row, block.Left + col);
            }
        }
    }

    public IEnumerable<int> GetNeighborIndices(BlockData block)
    {
        for (var row = -1; row <= block.Height; row++)
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
        if (row >= Height) row = Height;
        if (col >= Width) col = Width;
        return row * Width + col;
    }

    public IEnumerator<BlockData> GetEnumerator()
    {
        return new BlockEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class BlockEnumerator : IEnumerator<BlockData>
    {
        private readonly BlockDataCollection _owner;
        private int _index = -1;
        private readonly bool[] _visitedRefs;

        public BlockEnumerator(BlockDataCollection owner)
        {
            _owner = owner;
            _visitedRefs = new bool[_owner._blocks.Count];
        }

        public bool MoveNext()
        {
            while(true)
            {
                if (_index == _owner._blockRefs.Length - 1)
                    return false;
                
                _index++;
                
                var blockRef = _owner._blockRefs[_index];
                if(_visitedRefs[blockRef])
                    continue;
                _visitedRefs[blockRef] = true;

                return true;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public BlockData Current => _owner[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}