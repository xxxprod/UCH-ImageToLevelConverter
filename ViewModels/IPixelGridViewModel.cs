﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public interface IPixelGridViewModel
{
    Property<bool> EditorEnabled { get; }
    Property<bool> HighlightLayer { get; }
    Property<LayerViewModel> HighlightedLayer { get; }
    Dictionary<Layer, LayerViewModel> Layers { get; }
    BlockDataCollection Blocks { get; }
    event Action<IEnumerable<BlockData>> BlocksChanged;
    bool OnPixelGridAction(BlockData blockData);
    IntProperty LevelFullness { get; }
    Property<Color> BackgroundColor { get; }
    void StartRecordingGridActions();
}