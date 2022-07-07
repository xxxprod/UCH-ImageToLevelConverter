using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LayerViewModel : ViewModelBase
{
    public LayerViewModel(Layer layer, bool isVisible)
    {
        Layer = layer;
        IsVisible.Value = isVisible;
    }
    
    public Layer Layer { get; }
    public Property<bool> IsVisible { get; } = new();
}