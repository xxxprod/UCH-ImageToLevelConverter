using System.Collections.Generic;
using System.Linq;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Optimizer;

public abstract class BlockOptimizerBase
{
    protected readonly int Width;
    protected readonly int Height;

    protected readonly BlockData?[,] Blocks;
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

        Blocks = new BlockData?[Height, Width];

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
                    yield return block.Value;
            }
        }
    }

    public abstract IEnumerable<BlockData> Optimize(double colorSimilarityThreshold);
}