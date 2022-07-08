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
        var blocksToOptimize = new Stack<BlockData>(GetAllBlocks().OrderBy(a=>_ran.Next()));

        while (blocksToOptimize.Any())
        {
            var nextBlock = blocksToOptimize.Pop();

            foreach (var (blockWidth, blockHeight) in BlockData.BlockSizes)
            {
                var foundHole = false;
                for (var deltaRow = 0; deltaRow < blockHeight && !foundHole; deltaRow++)
                    for (var deltaCol = 0; deltaCol < blockWidth && !foundHole; deltaCol++)
                    {
                        var row = nextBlock.Row + deltaRow - RowOffset;
                        var col = nextBlock.Col + deltaCol - ColOffset;
                        if (row >= Height || col >= Width || Blocks[row, col] == null)
                            foundHole = true;
                    }

                if (!foundHole)
                {
                    yield return new BlockData(0, 0,
                        nextBlock.Top, nextBlock.Top + blockHeight - 1, nextBlock.Left, nextBlock.Left + blockWidth - 1,
                        nextBlock.Layer, nextBlock.Color);

                    for (var deltaRow = 0; deltaRow < blockHeight; deltaRow++)
                        for (var deltaCol = 0; deltaCol < blockWidth; deltaCol++)
                        {
                            var row = nextBlock.Row + deltaRow - RowOffset;
                            var col = nextBlock.Col + deltaCol - ColOffset;
                            Blocks[row, col] = null;
                        }

                    break;
                }
            }
        }
    }
}