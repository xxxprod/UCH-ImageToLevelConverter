using System;
using System.Collections.Generic;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Tools;

public class Property<T> : ViewModelBase
{
    protected bool Equals(Property<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Property<T>) obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(_value);
    }

    private T _value;
    public event Action<T> OnChanged;

    public Property(T defaultValue = default) => _value = defaultValue;

    public T Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value))
                return;
            _value = value;
            OnPropertyChanged();
            OnChanged?.Invoke(value);
        }
    }

    public static implicit operator T(Property<T> d) => d.Value;
    public static bool operator ==(Property<T> a, Property<T> b) => Equals(a, b);
    public static bool operator !=(Property<T> a, Property<T> b) => !Equals(a, b);
}