using System.Windows.Input;

namespace Notepad_App.ViewModels.Commands;

public class RelayCommand : ICommand
{
    #region Fields

    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    #endregion

    #region Events

    public event EventHandler? CanExecuteChanged;

    #endregion

    #region Constructor

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    #endregion

    #region ICommand Implementation

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    #endregion

    #region Public Methods

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}