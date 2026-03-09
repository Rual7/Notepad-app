using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Notepad_App.Models;

public class TreeItem
{
    public string Name { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public bool IsDirectory { get; set; }

    public ObservableCollection<TreeItem> Children { get; set; } = new();

    public bool IsExpanded { get; set; }

    public bool IsLoaded { get; set; }
}
