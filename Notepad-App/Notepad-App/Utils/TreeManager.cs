using Notepad_App.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace Notepad_App.Utils;

public class TreeManager
{
    #region Tree Loading

    public ObservableCollection<TreeItem> LoadRoots()
    {
        var items = new ObservableCollection<TreeItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            var item = new TreeItem
            {
                Name = drive.Name,
                FullPath = drive.RootDirectory.FullName,
                IsDirectory = true,
                IsLoaded = false
            };

            AddPlaceholder(item);
            items.Add(item);
        }

        return items;
    }
    public void LoadChildren(TreeItem parent)
    {
        if (parent == null || !parent.IsDirectory || parent.IsLoaded)
        {
            return;
        }

        parent.Children.Clear();

        try
        {
            var directories = Directory.GetDirectories(parent.FullPath).OrderBy(path => path);

            foreach (var directory in directories)
            {
                var info = new DirectoryInfo(directory);

                var child = new TreeItem
                {
                    Name = info.Name,
                    FullPath = info.FullName,
                    IsDirectory = true,
                    IsLoaded = false
                };

                AddPlaceholder(child);
                parent.Children.Add(child);
            }

            var files = Directory.GetFiles(parent.FullPath).OrderBy(path => path);

            foreach (var file in files)
            {
                var info = new FileInfo(file);

                parent.Children.Add(new TreeItem
                {
                    Name = info.Name,
                    FullPath = info.FullName,
                    IsDirectory = false,
                    IsLoaded = true
                });
            }

            parent.IsLoaded = true;
        }
        catch
        {
            parent.Children.Clear();
            parent.IsLoaded = true;
        }
    }

    #endregion

    #region File Creation

    public string CreateNewFile(string directoryPath)
    {
        const string baseName = "NewFile";
        const string extension = ".txt";

        string filePath = Path.Combine(directoryPath, $"{baseName}{extension}");
        int counter = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(directoryPath, $"{baseName}{counter}{extension}");
            counter++;
        }

        File.WriteAllText(filePath, string.Empty);
        return filePath;
    }

    #endregion

    #region Folder Copy

    public async Task CopyFolderAsync(string sourceFolderPath, string destinationParentFolderPath)
    {
        await Task.Run(() =>
        {
            string source = Path.GetFullPath(sourceFolderPath);
            string destinationParent = Path.GetFullPath(destinationParentFolderPath);

            string sourceFolderName = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFolder = Path.Combine(destinationParent, sourceFolderName);

            if (Directory.Exists(destinationFolder))
            {
                destinationFolder = GetUniqueFolderPath(destinationParent, sourceFolderName);
            }

            string[] directories = Directory.GetDirectories(source, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

            Directory.CreateDirectory(destinationFolder);

            foreach (var directory in directories)
            {
                string relativePath = Path.GetRelativePath(source, directory);
                string newDirectory = Path.Combine(destinationFolder, relativePath);

                Directory.CreateDirectory(newDirectory);
            }

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(source, file);
                string newFile = Path.Combine(destinationFolder, relativePath);
                string? parentDirectory = Path.GetDirectoryName(newFile);

                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory!);
                }

                File.Copy(file, newFile, true);
            }
        });
    }

    #endregion

    #region Helper Methods

    private string GetUniqueFolderPath(string parentFolder, string baseFolderName)
    {
        int counter = 1;
        string newPath;

        do
        {
            newPath = Path.Combine(parentFolder, $"{baseFolderName}_Copy{counter}");
            counter++;
        }
        while (Directory.Exists(newPath));

        return newPath;
    }
    private void AddPlaceholder(TreeItem item)
    {
        try
        {
            if (Directory.EnumerateFileSystemEntries(item.FullPath).Any())
            {
                item.Children.Add(new TreeItem
                {
                    Name = "Loading...",
                    FullPath = string.Empty,
                    IsDirectory = false,
                    IsLoaded = true
                });
            }
        }
        catch
        {
            // Ignored intentionally
        }
    }

    #endregion
}