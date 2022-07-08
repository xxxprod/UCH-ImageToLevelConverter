using System;
using System.Collections.Generic;
using System.Linq;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Optimizer;

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