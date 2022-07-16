using System.Windows;
using System.Windows.Threading;
using UCH_ImageToLevelConverter.ViewModels;
using UCH_ImageToLevelConverter.Views;

namespace UCH_ImageToLevelConverter;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        MainWindow = new LevelEditorView();
        MainWindow.DataContext = new LevelEditorViewModel();

        MainWindow.Show();
        base.OnStartup(e);
    }

    private static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Ulimate Chicken Horse Level Designer Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}