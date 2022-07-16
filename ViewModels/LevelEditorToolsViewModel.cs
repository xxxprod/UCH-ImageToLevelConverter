using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using UCH_ImageToLevelConverter.Model;
using UCH_ImageToLevelConverter.Tools;

namespace UCH_ImageToLevelConverter.ViewModels;

public class LevelEditorToolsViewModel : ViewModelBase
{
    private readonly Property<bool>[] _pixelGridActions;

    public event Action ToolsUpdated;

    public LevelEditorToolsViewModel()
    {
        OptimizeAllCommand = new DelegateCommand(o => { });
        BreakAllCommand = new DelegateCommand(o => { });

        _pixelGridActions = new[]
        {
            EraseBlockEnabled,
            EraseRegionEnabled,
            ColorPickingEnabled,
            PaintBrushEnabled,
            FillBrushEnabled,
            OptimizerEnabled,
            BreakBlockEnabled,
            BreakRegionEnabled,
            MoveToLayerEnabled,
            MoveRegionToLayerEnabled
        };

        foreach (Property<bool> pixelGridAction in _pixelGridActions)
            pixelGridAction.OnChanged += _ => PixelGridActionChanged(pixelGridAction);

        MoveToLayerEnabled.OnChanged += OnMoveToLayerChanged;
        MoveRegionToLayerEnabled.OnChanged += OnMoveToLayerChanged;

        Layers = Enum.GetValues<Layer>().OrderBy(a => a).ToDictionary(a => a, a => new LayerViewModel(a, true));

        foreach (LayerViewModel layer in Layers.Values)
            layer.IsVisible.OnChanged += _ => OnLayerVisibilityChanged(layer);

        HighlightedLayer.OnChanged += OnHighlightedLayerChanged;
        HighlightLayer.OnChanged += highlight =>
        {
            if (!highlight)
            {
                MoveToLayerEnabled.Value = false;
                MoveRegionToLayerEnabled.Value = false;
            }

            OnToolsUpdated();
        };
        HighlightedLayer.Value = Layers[Layer.Default];
    }

    private void PixelGridActionChanged(Property<bool> pixelGridAction)
    {
        if (!pixelGridAction) return;

        foreach (Property<bool> otherAction in _pixelGridActions)
            if (!ReferenceEquals(otherAction, pixelGridAction))
                otherAction.Value = false;
    }

    public DelegateCommand OptimizeAllCommand { get; }
    public DelegateCommand BreakAllCommand { get; }

    public bool PixelGridActionsEnabled => _pixelGridActions.Any(a => a.Value);

    public Property<bool> ColorPickingEnabled { get; } = new();
    public Property<bool> EraseBlockEnabled { get; } = new();
    public Property<bool> EraseRegionEnabled { get; } = new();
    public Property<bool> PaintBrushEnabled { get; } = new();
    public Property<bool> FillBrushEnabled { get; } = new();
    public Property<bool> OptimizerEnabled { get; } = new();
    public Property<bool> BreakBlockEnabled { get; } = new();
    public Property<bool> BreakRegionEnabled { get; } = new();
    public Property<bool> MoveToLayerEnabled { get; } = new();
    public Property<bool> MoveRegionToLayerEnabled { get; } = new();
    public Property<bool> SnapToEdgesEnabled { get; } = new();
    public Property<bool> HighlightLayer { get; } = new();

    public Property<Color> BackgroundColor { get; } = new(Colors.LightSteelBlue);
    public Property<Color> SelectedPaintColor { get; } = new(Colors.Crimson);
    public IntProperty ColorSimilarityPercentage { get; } = new(80, 0, 100);

    public Dictionary<Layer, LayerViewModel> Layers { get; }
    public Property<LayerViewModel> HighlightedLayer { get; } = new();

    public IEnumerable<Layer> GetActiveLayers()
    {
        return Layers.Where(a => a.Value.IsVisible).Select(a => a.Value.Layer);
    }

    private void OnMoveToLayerChanged(bool moveToLayerEnabled)
    {
        if (moveToLayerEnabled)
        {
            HighlightedLayer.Value ??= Layers[Layer.Default];
            HighlightLayer.Value = true;
        }
    }

    private void OnLayerVisibilityChanged(LayerViewModel layer)
    {
        OnToolsUpdated();
    }

    private void OnHighlightedLayerChanged(LayerViewModel layer)
    {
        if (layer == null || !layer.IsVisible.Value)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                layer ??= Layers[Layer.Default];
                layer.IsVisible.Value = true;
                HighlightedLayer.Value = layer;
            }));

        if (HighlightLayer)
            OnToolsUpdated();
    }

    private void OnToolsUpdated() => ToolsUpdated?.Invoke();
}