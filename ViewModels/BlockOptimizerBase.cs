using System;
using System.Collections.Generic;
using System.Linq;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.ViewModels;

public abstract class BlockOptimizerBase
{
    protected readonly int Width;
    protected readonly int Height;

    protected readonly BlockData[,] Blocks;
    protected readonly int ColOffset;
    protected readonly int RowOffset;

    protected BlockOptimizerBase(ICollection<BlockData> blocks)
    {
        ColOffset = blocks.Min(a => a.Left);
        var maxCol = blocks.Max(a => a.Right);
        RowOffset = blocks.Min(a => a.Top);
        var maxRow = blocks.Max(a => a.Bottom);

        Width = maxCol - ColOffset + 1;
        Height = maxRow - RowOffset + 1;

        Blocks = new BlockData[Height, Width];

        foreach (var blockData in blocks)
        {
            var row = blockData.Top - RowOffset;
            var col = blockData.Left - ColOffset;
            Blocks[row, col] = blockData;
        }
    }

    protected IEnumerable<BlockData> GetAllBlocks()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                var block = Blocks[row, col];
                if (block != null)
                    yield return block;
            }
        }
    }

    public abstract IEnumerable<BlockData> Optimize();
}

class RandomBlockOptimizer : BlockOptimizerBase
{
    private readonly Random _ran = new();

    public RandomBlockOptimizer(ICollection<BlockData> blocks) : base(blocks)
    {
    }

    public override IEnumerable<BlockData> Optimize()
    {
        var blocksToOptimize = GetAllBlocks().ToList();

        while (blocksToOptimize.Any())
        {
            var nextBlock = blocksToOptimize[_ran.Next(blocksToOptimize.Count)];

            foreach (var (blockWidth, blockHeight) in BlockData.BlockSizes)
            {
                var foundHole = false;
                for (var deltaRow = 0; deltaRow < blockHeight && !foundHole; deltaRow++)
                    for (var deltaCol = 0; deltaCol < blockWidth && !foundHole; deltaCol++)
                    {
                        var row = nextBlock.Top + deltaRow - RowOffset;
                        var col = nextBlock.Left + deltaCol - ColOffset;
                        if (row >= Height || col >= Width || Blocks[row, col] == null)
                            foundHole = true;
                    }

                if (!foundHole)
                {
                    yield return new BlockData(
                        nextBlock.Top, nextBlock.Left, blockWidth, blockHeight,
                        nextBlock.Layer, nextBlock.Color);

                    for (var deltaRow = 0; deltaRow < blockHeight; deltaRow++)
                        for (var deltaCol = 0; deltaCol < blockWidth; deltaCol++)
                        {
                            var row = nextBlock.Top + deltaRow - RowOffset;
                            var col = nextBlock.Left + deltaCol - ColOffset;
                            blocksToOptimize.Remove(Blocks[row, col]);
                            Blocks[row, col] = null;
                        }

                    break;
                }
            }
        }
    }
}