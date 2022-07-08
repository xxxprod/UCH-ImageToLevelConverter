using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace UCH_ImageToLevelConverter.Model;

public static class BlockDataExtensions
{
    public static IEnumerable<BlockData> BreakToCells(this IEnumerable<BlockData> blocks)
    {
        return blocks.ToArray().SelectMany(BreakToCells);
    }

    private static IEnumerable<BlockData> BreakToCells(this BlockData block)
    {
        if (block.Cells == 1)
            yield return block;
        else
        {
            for (int row = block.Top; row <= block.Bottom; row++)
            {
                for (int col = block.Left; col <= block.Right; col++)
                {
                    yield return new BlockData(row, col, block.Layer, block.Color);
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

    public static IEnumerable<BlockData> FindBlocksWithSameColor(this BlockDataCollection blocks, BlockData startBlock, Color color, double maxDistance = 1)
    {
        var queue = new Queue<BlockData>();
        var done = new HashSet<BlockData>();

        queue.Enqueue(startBlock);
        done.Add(startBlock);

        while (queue.Any())
        {
            var block = queue.Dequeue();

            yield return block;

            var neighborIdx = blocks.GetNeighborIndices(block);

            foreach (var idx in neighborIdx)
            {
                if (idx < 0 || idx >= blocks.Width * blocks.Height)
                    continue;
                var neighbor = blocks[idx];
                if (!done.Add(neighbor))
                    continue;


                if (GetColorSimilarity(neighbor.Color, color) <= maxDistance)
                    queue.Enqueue(neighbor);
            }
        }
    }

    private static double GetColorSimilarity(Color a, Color b)
    {
        return Math.Sqrt(Math.Pow(a.R - b.R, 2) + Math.Pow(a.G - b.G, 2) + Math.Pow(a.B - b.B, 2));
    }
}