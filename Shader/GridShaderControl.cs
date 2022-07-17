using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter.Shader;

public class GridShaderControl : BaseShaderControl
{
    private const int BlockSize = 20;

    public GridShaderControl(BlockGridView owner) : base(owner) { }

    public override void SetSize(int width, int height)
    {
        base.SetSize(width * BlockSize, height * BlockSize);
    }

    public void DrawBlocks(IEnumerable<BlockData> blocks)
    {
        using (Bitmap.GetBitmapContext())
        {
            HashSet<BlockData> seenBlocks = new(new BlockDataCoordinatesComparer());

            foreach (BlockData block in blocks)
            {
                if (!seenBlocks.Add(block))
                    continue;

                ClearBorders(block);
                DrawBorders(block);
            }
        }
    }

    private void DrawBorders(BlockData block)
    {
        Color color = GetColor(block);
        color = Color.Multiply(color, 0.4f);

        int minX = block.Left * BlockSize;
        int maxX = (block.Right + 1) * BlockSize - 1;
        int minY = block.Top * BlockSize;
        int maxY = (block.Bottom + 1) * BlockSize - 1;

        for (int col = block.Left; col <= block.Right; col++)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                int x = col * BlockSize + i;

                Bitmap.SetPixel(x, minY, color);
                Bitmap.SetPixel(x, maxY, color);
            }
        }

        for (int row = block.Top; row <= block.Bottom; row++)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                int y = row * BlockSize + i;

                Bitmap.SetPixel(minX, y, color);
                Bitmap.SetPixel(maxX, y, color);
            }
        }
    }

    private void ClearBorders(BlockData block)
    {
        for (int row = block.Top; row <= block.Bottom; row++)
        {
            int y1 = row * BlockSize;
            int y2 = (row + 1) * BlockSize - 1;

            for (int col = block.Left; col <= block.Right; col++)
            {
                for (int i = 0; i < BlockSize; i++)
                {
                    int x = col * BlockSize + i;

                    Bitmap.SetPixel(x, y1, Colors.Transparent);
                    Bitmap.SetPixel(x, y2, Colors.Transparent);
                }
            }
        }

        for (int col = block.Left; col <= block.Right; col++)
        {
            int x1 = col * BlockSize;
            int x2 = (col + 1) * BlockSize - 1;

            for (int row = block.Top; row <= block.Bottom; row++)
            {
                for (int i = 0; i < BlockSize; i++)
                {
                    int y = row * BlockSize + i;

                    Bitmap.SetPixel(x1, y, Colors.Transparent);
                    Bitmap.SetPixel(x2, y, Colors.Transparent);
                }
            }
        }
    }
}