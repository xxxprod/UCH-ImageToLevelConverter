using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter.Shader;

public class BaseShaderControl : Panel
{
    protected WriteableBitmap Bitmap;
    protected readonly BlockGridView Owner;
    private readonly GridShaderEffect _gridShader;

    protected BaseShaderControl(BlockGridView owner)
    {
        Owner = owner;
        _gridShader = new GridShaderEffect();
        Effect = _gridShader;
        Background = Brushes.Transparent;
    }

    public virtual void SetSize(int width, int height)
    {
        if (Bitmap != null && Bitmap.PixelWidth == width && Bitmap.PixelHeight == height)
            return;

        Bitmap = BitmapFactory.New(width, height);
        _gridShader.Colors = new ImageBrush(Bitmap);
    }

    protected Color GetColor(BlockData block)
    {
        if (!Owner.ViewModel.LevelEditorTools.Layers[block.Layer].IsVisible)
            return Owner.EmptyBlockColor;
        Color color = block.Color;
        if (color == BlockData.EmptyColor)
            color = Owner.EmptyBlockColor;
        else if (Owner.ViewModel.LevelEditorTools.HighlightLayer &&
                 Owner.ViewModel.LevelEditorTools.HighlightedLayer.Value.Layer != block.Layer)
            color = Color.Multiply(color, 0.4f);
        return color;
    }
}