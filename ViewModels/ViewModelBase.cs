using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UCH_ImageToLevelConverter.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void RegisterPropertyChangedCallback(Action changeCallback, params INotifyPropertyChanged[] sources)
    {
        foreach (INotifyPropertyChanged source in sources) 
            source.PropertyChanged += (_, _) => changeCallback();
    }
}