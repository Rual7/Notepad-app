using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Notepad_App.ViewModels;

public class BaseVM : INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Protected Methods

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}