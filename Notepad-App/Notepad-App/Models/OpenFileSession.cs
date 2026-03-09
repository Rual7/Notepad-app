using System;
using System.Collections.Generic;
using System.Text;

namespace Notepad_App.Models;

public class OpenFileSession
{
    public string? FilePath { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsUntitled { get; set; }
    public string Content { get; set; } = string.Empty;
}
