using System.IO;

namespace Notepad_App.Utils;

public class FileManager
{
    #region File Operations (READ/WRITE/EXISTS)

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

    #endregion
}