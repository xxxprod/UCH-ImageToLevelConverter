using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views;

public partial class LevelEditorView
{
    private readonly Regex _filterNumericCharacterRegex = new("[^0-9]+", RegexOptions.Compiled);

    public LevelEditorView()
    {
        InitializeComponent();
    }

    private void LayerExpander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
        vm.MoveToLayerEnabled.Value = false;
        vm.MoveRegionToLayerEnabled.Value = false;
    }

    private void OptimizerExpander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
        vm.OptimizerEnabled.Value = false;
        vm.BreakBlockEnabled.Value = false;
    }

    private void PaintExpander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
        vm.ColorPickingEnabled.Value = false;
        vm.PaintBrushEnabled.Value = false;
        vm.FillBrushEnabled.Value = false;
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = _filterNumericCharacterRegex.IsMatch(e.Text);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (e.Key == Key.Z)
                vm.UndoCommand.Execute(null);
            else if (e.Key == Key.Y)
                vm.RedoCommand.Execute(null);
        }

        base.OnKeyDown(e);
    }
}