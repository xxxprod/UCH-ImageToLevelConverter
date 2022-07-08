using System.Windows;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class LevelEditorView
    {
        public LevelEditorView() => InitializeComponent();

        private void LayerExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            var vm = (LevelEditorViewModel)DataContext;
            vm.MoveToLayerEnabled.Value = false;
            vm.MoveRegionToLayerEnabled.Value = false;
        }

        private void OptimizerExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            var vm = (LevelEditorViewModel)DataContext;
            vm.OptimizerEnabled.Value = false;
            vm.BreakBlocksEnabled.Value = false;
        }

        private void PaintExpander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            var vm = (LevelEditorViewModel)DataContext;
            vm.ColorPickingEnabled.Value = false;
            vm.PaintBrushEnabled.Value = false;
            vm.FillBrushEnabled.Value = false;
        }
    }
}
