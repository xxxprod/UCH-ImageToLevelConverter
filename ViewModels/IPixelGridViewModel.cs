using System;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public interface IPixelGridViewModel
{
    Property<bool> EditorEnabled { get; }
    BlockDataCollection Blocks { get; }
    event Action BlocksChanged;
    DelegateCommand PixelGridActionCommand { get; }
    IntProperty LevelFullness { get; }
}