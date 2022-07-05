using System.Text.RegularExpressions;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class ImageSelectorView
    {
        private readonly Regex _filterNumericCharacterRegex = new("[^0-9]+", RegexOptions.Compiled);

        public ImageSelectorView() => InitializeComponent();

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e) => 
            e.Handled = _filterNumericCharacterRegex.IsMatch(e.Text);
    }
}
