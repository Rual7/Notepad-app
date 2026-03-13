using Notepad_App.Models;
using System.IO;
using System.Text.Json;

namespace Notepad_App.Utils;

public class ConfigManager
{
    #region Fields

    private readonly string _configPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    #endregion

    #region Constructor

    public ConfigManager()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folderPath = Path.Combine(appData, "NotepadApp");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        _configPath = Path.Combine(folderPath, "config.json");
    }

    #endregion

    #region Save Configuration

    public void SaveConfig(AppConfig config)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Ignored intentionally
        }
    }

    #endregion

    #region Load Configuration

    public AppConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                return new AppConfig();
            }

            string json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json);

            return config ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    #endregion
}
