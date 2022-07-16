using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class NewLevelPromptViewModel : ViewModelBase
{
    public IntProperty Width { get; } = new(70, 0, 200);
    public IntProperty Height { get; } = new(50, 0, 200);
}