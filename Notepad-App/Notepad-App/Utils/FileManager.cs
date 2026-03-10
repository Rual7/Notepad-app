using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Notepad_App.Utils;

public class FileManager
{
    public string ReadFile(string path)
    {
        return File.ReadAllText(path);
    }

    public void SaveFile(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }   
}
