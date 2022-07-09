using System.IO;
using System.Windows;
using UCH_ImageToLevelConverter.ViewModels;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            MainWindow = new MainWindow();

            var imageSelectorViewModel = new ImageSelectorViewModel();
            var levelEditorViewModel = new LevelEditorViewModel();

            imageSelectorViewModel.NavigateToLevelEditorCommand.ExecuteCalled += _ =>
            {
                levelEditorViewModel.LevelName.Value = Path.GetFileNameWithoutExtension(imageSelectorViewModel.ImageFileName);
                levelEditorViewModel.Blocks = imageSelectorViewModel.Blocks;
                levelEditorViewModel.BackgroundColor.Value = imageSelectorViewModel.BackgroundColor;

                MainWindow.DataContext = levelEditorViewModel;
            };

            levelEditorViewModel.NavigateToImageSelectorCommand.ExecuteCalled += _ =>
            {
                var tmp = imageSelectorViewModel.OriginalImage.Value;
                imageSelectorViewModel.OriginalImage.Value = null;
                imageSelectorViewModel.OriginalImage.Value = tmp;
                imageSelectorViewModel.BackgroundColor.Value = levelEditorViewModel.BackgroundColor;
                MainWindow.DataContext = imageSelectorViewModel;
            };

            MainWindow.DataContext = imageSelectorViewModel;

            MainWindow.Show();
            base.OnStartup(e);
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Ulimate Chicken Horse Level Designer Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
