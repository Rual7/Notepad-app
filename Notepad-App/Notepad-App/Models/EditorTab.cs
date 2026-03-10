using System;
using System.Collections.Generic;
using System.Text;

using Notepad_App.ViewModels;

namespace Notepad_App.Models;

public class EditorTab : BaseVM
{
    private string _title = string.Empty;
    private string _content = string.Empty;
    private string? _filePath;
    private bool _isModified;
    private bool _isUntitled;

    public string Title
    {
        get => _title;
        set
        {
            if (SetField(ref _title, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (SetField(ref _content, value))
            {
                IsModified = true;
            }
        }
    }

    public string? FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (SetField(ref _isModified, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public bool IsUntitled
    {
        get => _isUntitled;
        set => SetField(ref _isUntitled, value);
    }

    public string DisplayTitle => IsModified ? $"*{Title}" : Title;
}