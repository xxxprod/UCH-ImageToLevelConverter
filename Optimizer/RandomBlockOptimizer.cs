using System;
using System.Collections.Generic;
using System.Linq;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Optimizer;

internal class RandomBlockOptimizer : BlockOptimizerBase
{
    private readonly Random _ran = new();

    public RandomBlockOptimizer(ICollection<BlockData> blocks) : base(blocks)
    {
    }

    public override IEnumerable<BlockData> Optimize(double colorSimilarityThreshold)
    {
        Stack<BlockData> blocksToOptimize = new(GetAllBlocks().OrderBy(a => _ran.Next()));

        while (blocksToOptimize.Any())
        {
            BlockData nextBlock = blocksToOptimize.Pop();

            foreach ((int blockWidth, int blockHeight) in BlockData.BlockSizes)
            {
                bool foundHole = false;
                for (int deltaRow = 0; deltaRow < blockHeight && !foundHole; deltaRow++)
                    for (int deltaCol = 0; deltaCol < blockWidth && !foundHole; deltaCol++)
                    {
                        int row = nextBlock.Row + deltaRow - RowOffset;
                        int col = nextBlock.Col + deltaCol - ColOffset;
                        if (row >= Height || col >= Width || Blocks[row, col] == null ||
                            !BlockDataExtensions.AreColorsSimilar(nextBlock.Color, Blocks[row, col].Value.Color, colorSimilarityThreshold))
                            foundHole = true;
                    }

                if (!foundHole)
                {
                    yield return new BlockData(0, 0,
                        nextBlock.Top, nextBlock.Top + blockHeight - 1, nextBlock.Left, nextBlock.Left + blockWidth - 1,
                        nextBlock.Layer, nextBlock.Color);

                    for (int deltaRow = 0; deltaRow < blockHeight; deltaRow++)
                        for (int deltaCol = 0; deltaCol < blockWidth; deltaCol++)
                        {
                            int row = nextBlock.Row + deltaRow - RowOffset;
                            int col = nextBlock.Col + deltaCol - ColOffset;
                            Blocks[row, col] = null;
                        }

                    break;
                }
            }
        }
    }
}