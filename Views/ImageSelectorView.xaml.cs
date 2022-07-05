using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class ImageSelectorView
    {
        private readonly HashSet<string> _numericKeyCodes = Enumerable.Range(0, 9)
            .SelectMany(i => new[] { $"D{i}", $"NumPad{i}" })
            .ToHashSet();

        private readonly Regex _filterNumericCharacterRegex = new("[^0-9]+", RegexOptions.Compiled);

        public ImageSelectorView() => InitializeComponent();

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e) => 
            e.Handled = _filterNumericCharacterRegex.IsMatch(e.Text);
    }
}
