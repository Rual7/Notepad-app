using Notepad_App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Notepad_App.Utils;

public class ConfigManager
{
    private readonly string _configPath;

    public ConfigManager()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folder = Path.Combine(appData, "NotepadApp");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        _configPath = Path.Combine(folder, "config.json");
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_configPath, json);
        }
        catch
        {
        }
    }

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
}
