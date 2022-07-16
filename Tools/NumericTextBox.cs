using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Tools;

public class NumericTextBox : TextBox
{
    private readonly Regex _filterNumericCharacterRegex = new("[^0-9]+", RegexOptions.Compiled);
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        base.OnPreviewTextInput(e);
        e.Handled = _filterNumericCharacterRegex.IsMatch(e.Text);
    }
}