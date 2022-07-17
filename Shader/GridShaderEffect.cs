using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace UCH_ImageToLevelConverter.Shader;

public class GridShaderEffect : ShaderEffect
{
    private static readonly PixelShader Shader = new() { UriSource = MakePackUri("Shader/PixelShader.fx.ps") };

    public static readonly DependencyProperty ColorsProperty =
        RegisterPixelShaderSamplerProperty("Colors", typeof(GridShaderEffect), 0);

    public GridShaderEffect()
    {
        PixelShader = Shader;
        UpdateShaderValue(ColorsProperty);
    }

    public Brush Colors
    {
        get => (Brush)GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    // MakePackUri is a utility method for computing a pack uri
    // for the given resource. 
    public static Uri MakePackUri(string relativeFile)
    {
        Assembly a = typeof(GridShaderEffect).Assembly;

        // Extract the short name.
        string assemblyShortName = a.ToString().Split(',')[0];

        string uriString = "pack://application:,,,/" +
                           assemblyShortName +
                           ";component/" +
                           relativeFile;

        return new Uri(uriString);
    }
}