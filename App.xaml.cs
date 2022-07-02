using System.IO;
using System.Windows;
using UCH_ImageToLevelConverter.ViewModels;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var imageSelectorViewModel = new ImageSelectorViewModel();
            MainWindow = new ImageSelectorView
            {
                DataContext = imageSelectorViewModel
            };

            if (!MainWindow.ShowDialog() ?? false)
            {
                Shutdown();
                return;
            }
            
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            MainWindow = new LevelEditorView
            {
                DataContext = new LevelEditorViewModel
                {
                    LevelName = { Value = Path.GetFileNameWithoutExtension(imageSelectorViewModel.ImageFileName) },
                    Pixels = { Value = imageSelectorViewModel.Pixels },
                    Width = { Value = imageSelectorViewModel.Width },
                    Height = { Value = imageSelectorViewModel.Height },
                }
            };
            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
