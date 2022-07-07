using System.Windows;
using System.Windows.Controls;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class LayerView
    {
        public static readonly DependencyProperty LayerDataProperty = DependencyProperty.Register(
            "LayerData", typeof(LayerViewModel[]), typeof(LayerView), new FrameworkPropertyMetadata(default(LayerViewModel[]), FrameworkPropertyMetadataOptions.None, OnLayerDataChanged));

        public LayerViewModel[] LayerData
        {
            get => (LayerViewModel[])GetValue(LayerDataProperty);
            set => SetValue(LayerDataProperty, value);
        }

        public LayerView() => InitializeComponent();


        private static void OnLayerDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (LayerView)d;
            var layerViewModels = (LayerViewModel[])e.NewValue;
            if (view == null || layerViewModels == null)
                return;

            var visibility = layerViewModels.Length > 6 ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
            view.LayerList.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, visibility);
            view.DummyScrollBar.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, visibility);
        }
    }
}
