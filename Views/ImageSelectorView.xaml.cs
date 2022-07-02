using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class ImageSelectorView
    {
        private readonly HashSet<string> _numericKeyCodes = Enumerable.Range(0, 9)
            .SelectMany(i => new[] { $"D{i}", $"NumPad{i}" })
            .ToHashSet();

        public ImageSelectorView() => InitializeComponent();


        private void OnNumberTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Delete or Key.Back or Key.Left or Key.Right or Key.Enter or Key.Home or Key.End)
                return;

            e.Handled = !_numericKeyCodes.Contains(e.Key.ToString());
        }
    }
}
