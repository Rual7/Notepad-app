using System;
using System.Collections.Generic;
using System.Text;

namespace Notepad_App.Models;

public class EditorTab
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public bool IsModified { get; set; }
    public bool IsUntitled { get; set; }
}
