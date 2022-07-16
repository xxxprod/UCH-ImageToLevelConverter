using System.Windows;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class LevelEditorToolsView
    {
        public LevelEditorToolsView()
        {
            InitializeComponent();
        }

        private void PaintExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
            vm.LevelEditorTools.PaintBrushEnabled.Value = false;
            vm.LevelEditorTools.FillBrushEnabled.Value = false;
            vm.LevelEditorTools.EraseBlockEnabled.Value = false;
            vm.LevelEditorTools.EraseRegionEnabled.Value = false;
        }

        private void OptimizerExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
            vm.LevelEditorTools.OptimizerEnabled.Value = false;
            vm.LevelEditorTools.BreakBlockEnabled.Value = false;
            vm.LevelEditorTools.BreakRegionEnabled.Value = false;
        }

        private void LayerExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            LevelEditorViewModel vm = (LevelEditorViewModel) DataContext;
            vm.LevelEditorTools.MoveToLayerEnabled.Value = false;
            vm.LevelEditorTools.MoveRegionToLayerEnabled.Value = false;
        }
    }
}
