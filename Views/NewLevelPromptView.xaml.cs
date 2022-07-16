using System.Windows;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Views
{
    public partial class NewLevelPromptView
    {
        public NewLevelPromptView()
        {
            InitializeComponent();
        }

        private void OkButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            DialogResult = e.Key switch
            {
                Key.Escape => false,
                Key.Enter => true,
                _ => DialogResult
            };

            base.OnKeyDown(e);
        }
    }
}
