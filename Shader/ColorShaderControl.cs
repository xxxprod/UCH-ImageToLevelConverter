using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter.Shader;

public class ColorShaderControl : BaseShaderControl
{
    public ColorShaderControl(BlockGridView owner) : base(owner)
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    public void DrawBlocks(IEnumerable<BlockData> blocks)
    {
        using (Bitmap.GetBitmapContext())
        {
            foreach (BlockData block in blocks)
            {
                Color color = GetColor(block);
                for (int row = block.Top; row <= block.Bottom; row++)
                {
                    for (int col = block.Left; col <= block.Right; col++)
                    {
                        Bitmap.SetPixel(col, row, color);
                    }
                }
            }
        }
    }
}