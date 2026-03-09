using System;
using System.Collections.Generic;
using System.Text;

namespace Notepad_App.Models;

public class AppConfig
{
    public bool IsTreeViewVisible { get; set; } = true;
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 700;
    public int SelectedTabIndex { get; set; } = 0;
    public List<OpenFileSession> OpenFiles { get; set; } = new();
}
