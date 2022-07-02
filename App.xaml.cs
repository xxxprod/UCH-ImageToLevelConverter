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
            MainWindow = new MainWindow();

            var imageSelectorViewModel = new ImageSelectorViewModel();
            var levelEditorViewModel = new LevelEditorViewModel();

            imageSelectorViewModel.NavigateToLevelEditorCommand.ExecuteCalled += _ =>
            {
                levelEditorViewModel.LevelName.Value = Path.GetFileNameWithoutExtension(imageSelectorViewModel.ImageFileName);
                levelEditorViewModel.Pixels.Value = imageSelectorViewModel.Pixels;
                levelEditorViewModel.Width.Value = imageSelectorViewModel.Width;
                levelEditorViewModel.Height.Value = imageSelectorViewModel.Height;

                MainWindow.DataContext = levelEditorViewModel;
            };

            levelEditorViewModel.NavigateToImageSelectorCommand.ExecuteCalled += _ =>
            {
                var tmp = imageSelectorViewModel.OriginalImage.Value;
                imageSelectorViewModel.OriginalImage.Value = null;
                imageSelectorViewModel.OriginalImage.Value = tmp;
                MainWindow.DataContext = imageSelectorViewModel;
            };

            MainWindow.DataContext = imageSelectorViewModel;

            MainWindow.Show();
            base.OnStartup(e);
        }
    }
}
