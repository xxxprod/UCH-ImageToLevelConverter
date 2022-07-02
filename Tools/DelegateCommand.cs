using System;
using System.Windows.Input;

namespace UCH_ImageToLevelConverter.Tools;

public class DelegateCommand : ICommand
{
    private readonly Func<object, bool> _canExecute;
    private readonly Action<object> _execute;

    public DelegateCommand(Action<object> execute) : this(o => true, execute) { }
    public DelegateCommand(Func<object, bool> canExecute, Action<object> execute)
    {
        _canExecute = canExecute;
        _execute = execute;
    }

    public bool CanExecute(object parameter) => _canExecute(parameter);
    public void Execute(object parameter)
    {
        _execute(parameter);
        ExecuteCalled?.Invoke(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public event Action<object> ExecuteCalled;
}