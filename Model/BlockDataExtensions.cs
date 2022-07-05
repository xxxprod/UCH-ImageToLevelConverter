using System.Collections.Generic;
using System.Linq;

namespace UCH_ImageToLevelConverter.Model;

public static class BlockDataExtensions
{
    public static IEnumerable<BlockData> BreakToCells(this IEnumerable<BlockData> blocks)
    {
        foreach (var block in blocks.ToArray())
        {
            if (block.Cells == 1)
                yield return block;
            else
            {
                for (int row = block.Top; row <= block.Bottom; row++)
                {
                    for (int col = block.Left; col <= block.Right; col++)
                    {
                        yield return new BlockData(row, col, block.Color);
                    }
                }
            }
        }
    }

    public static IEnumerable<BlockData> CopyBlocks(this IEnumerable<BlockData> blocks)
    {
        foreach (BlockData block in blocks)
        {
            yield return new BlockData(block);
        }
    }
}