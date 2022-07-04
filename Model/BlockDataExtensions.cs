using System.Collections.Generic;

namespace UCH_ImageToLevelConverter.Model;

public static class BlockDataExtensions
{
    public static IEnumerable<BlockData> BreakToCells(this BlockData block)
    {
        if(block.Cells == 1)
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