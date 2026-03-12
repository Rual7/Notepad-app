using System.Collections.Generic;

namespace Notepad_App.Models;

public class AppConfig
{
    public bool IsTreeViewVisible { get; set; } = true;
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 700;
    public int SelectedTabIndex { get; set; } = 0;
    public List<EditorTab> OpenTabs { get; set; } = new();
}