namespace UCH_ImageToLevelConverter.ViewModels;

public class Property<T> : ViewModelBase
{
    private T _value;

    public Property(T defaultValue = default) => _value = defaultValue;

    public T Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }
    
    public static implicit operator T(Property<T> d) => d.Value;
}