using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public interface IPixelGridViewModel
{
    Property<bool> EditorEnabled { get; }
    Property<BlockData[]> Pixels { get; }
    IntProperty Height { get; }
    IntProperty Width { get; }
    DelegateCommand PixelGridActionCommand { get; }
}