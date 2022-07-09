using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace UCH_ImageToLevelConverter.Model;

public static class BlockDataExtensions
{
    public static BlockData[] BreakToCells(this IEnumerable<BlockData> blocks)
    {
        return blocks.SelectMany(BreakToCells).ToArray();
    }

    public static IEnumerable<BlockData> BreakToCells(this BlockData block)
    {
        if (block.Cells == 1)
            yield return block;
        else
            for (int row = block.Top; row <= block.Bottom; row++)
            for (int col = block.Left; col <= block.Right; col++)
                yield return new BlockData(row, col, block.Layer, block.Color);
    }

    public static IEnumerable<BlockData> FindBlocksWithSameColor(this BlockDataCollection blocks, BlockData startBlock,
        Color color, double colorSimilarityThreshold)
    {
        Queue<BlockData> queue = new();
        HashSet<(int Top, int Bottom, int Left, int Right)> done = new();

        queue.Enqueue(startBlock);
        done.Add((startBlock.Top, startBlock.Bottom, startBlock.Left, startBlock.Right));

        while (queue.Any())
        {
            BlockData block = queue.Dequeue();

            yield return block;

            IEnumerable<int> neighborIdx = blocks.GetNeighborIndices(block);

            foreach (int idx in neighborIdx)
            {
                if (idx < 0 || idx >= blocks.Width * blocks.Height)
                    continue;
                BlockData neighbor = blocks[idx];
                if (!done.Add((neighbor.Top, neighbor.Bottom, neighbor.Left, neighbor.Right)))
                    continue;

                if (AreColorsSimilar(neighbor.Color, color, colorSimilarityThreshold))
                    queue.Enqueue(neighbor);
            }
        }
    }

    public static bool AreColorsSimilar(Color a, Color b, double threshold)
    {
        if ((a == BlockData.EmptyColor) ^ (b == BlockData.EmptyColor))
            return false;

        double similarity = Math.Sqrt(
            Math.Pow(a.R - b.R, 2) +
            Math.Pow(a.G - b.G, 2) +
            Math.Pow(a.B - b.B, 2)
        );

        return similarity < threshold;
    }
}