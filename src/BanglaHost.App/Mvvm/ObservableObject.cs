using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BanglaHost.App.Mvvm;

/// <summary>Minimal INotifyPropertyChanged base for view-models and bindable items.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Set a backing field and raise PropertyChanged only when the value actually changes.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
