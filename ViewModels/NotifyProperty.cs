namespace UCH_ImageToLevelConverter.ViewModels;

public class NotifyProperty<T> : ViewModelBase
{
    private T _value;

    public NotifyProperty(T defaultValue = default) => _value = defaultValue;

    public T Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }
    
    public static implicit operator T(NotifyProperty<T> d) => d.Value;
}