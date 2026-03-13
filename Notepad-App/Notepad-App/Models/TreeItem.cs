using System.Collections.ObjectModel;
using Notepad_App.ViewModels;

namespace Notepad_App.Models;

public class TreeItem : BaseVM
{
    #region Fields

    private string _name = string.Empty;
    private string _fullPath = string.Empty;
    private bool _isDirectory;
    private bool _isExpanded;
    private bool _isLoaded;

    #endregion

    #region Properties

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }
    public string FullPath
    {
        get => _fullPath;
        set => SetField(ref _fullPath, value);
    }
    public bool IsDirectory
    {
        get => _isDirectory;
        set => SetField(ref _isDirectory, value);
    }
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }
    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetField(ref _isLoaded, value);
    }
    public ObservableCollection<TreeItem> Children { get; set; } = new();

    #endregion
}