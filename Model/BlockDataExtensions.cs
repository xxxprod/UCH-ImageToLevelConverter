using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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

    public static IEnumerable<BlockData> FindBlocksWithSameColor(this BlockDataCollection blocks, BlockData startBlock, int minSimilarity, IEnumerable<Layer> activeLayers)
    {
        HashSet<Layer> layerFilter = new(activeLayers);
        Queue<BlockData> queue = new();
        HashSet<(int Top, int Bottom, int Left, int Right)> done = new();

        queue.Enqueue(startBlock);
        done.Add((startBlock.Top, startBlock.Bottom, startBlock.Left, startBlock.Right));

        while (queue.Any())
        {
            BlockData block = queue.Dequeue();
            if (!layerFilter.Contains(block.Layer))
                continue;

            yield return block;

            IEnumerable<int> neighborIdx = blocks.GetNeighborIndices(block);

            foreach (int idx in neighborIdx)
            {
                if (idx < 0 || idx >= blocks.Width * blocks.Height)
                    continue;
                BlockData neighbor = blocks[idx];
                if (!done.Add((neighbor.Top, neighbor.Bottom, neighbor.Left, neighbor.Right)))
                    continue;

                if (AreColorsSimilar(neighbor.Color, startBlock.Color, minSimilarity))
                    queue.Enqueue(neighbor);
            }
        }
    }

    public static bool AreColorsSimilar(Color a, Color b, int minSimilarity)
    {
        if ((a == BlockData.EmptyColor) ^ (b == BlockData.EmptyColor))
            return false;

        double maxSquaredError = Math.Pow((100 - minSimilarity) / 100.0, 2);
        double colorDistance = GetNormalizedDistance(a, b);

        return colorDistance < maxSquaredError;
    }

    private static double GetNormalizedDistance(Color a, Color b)
    {
        Vector3D a1 = new(a.R, a.G, a.B);
        Vector3D b1 = new(b.R, b.G, b.B);
        Vector3D distanceVector = a1 - b1;

        return distanceVector.Length / 441.67295593006372; // sqrt(255^2 * 3)
    }
}