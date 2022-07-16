using System;
using System.Collections.Generic;
using System.Windows;
using UCH_ImageToLevelConverter.ViewModels;

namespace UCH_ImageToLevelConverter.Tools;

public class Property<T> : ViewModelBase
{
    private T _value;

    public Property(T defaultValue = default)
    {
        _value = defaultValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value))
                return;
            Validate(value);
            _value = value;
            OnPropertyChanged();
            OnChanged?.Invoke(value);
        }
    }

    public event Action<T> OnChanged;

    protected virtual void Validate(T value)
    {
    }

    protected bool Equals(Property<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Property<T>) obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(_value);
    }

    public static implicit operator T(Property<T> d)
    {
        return d.Value;
    }
}

public class IntProperty : Property<int>
{
    public IntProperty(int defaultValue = default, int? minValue = default, int? maxValue = default) : base(
        defaultValue)
    {
        if (minValue > maxValue)
            throw new ArgumentException("MinValue must be smaller then MaxValue");
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public int? MinValue { get; }
    public int? MaxValue { get; }

    protected override void Validate(int value)
    {
        if (value < MinValue)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Value = MaxValue ?? default;
                Value = MinValue.Value;
            }));
            throw new Exception("Value must not be lower than MinValue");
        }

        if (value > MaxValue)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Value = MinValue ?? default;
                Value = MaxValue.Value;
            }));
            throw new Exception("Value must not exceed MaxValue");
        }
    }
}

public class NullableIntProperty : Property<int?>
{
    public NullableIntProperty(int? defaultValue = default, int? minValue = default, int? maxValue = default) :
        base(defaultValue)
    {
        if (minValue > maxValue)
            throw new ArgumentException("MinValue must be smaller then MaxValue");
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public int? MinValue { get; }
    public int? MaxValue { get; }

    protected override void Validate(int? value)
    {
        if (value < MinValue)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Value = MaxValue ?? default;
                Value = MinValue.Value;
            }));
            throw new Exception("Value must not be lower than MinValue");
        }

        if (value > MaxValue)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Value = MinValue ?? default;
                Value = MaxValue.Value;
            }));
            throw new Exception("Value must not exceed MaxValue");
        }
    }
}