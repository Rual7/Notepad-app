using Notepad_App.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Notepad_App.Utils;

public class TreeManager
{
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
            var directories = Directory.GetDirectories(parent.FullPath).OrderBy(x => x);
            foreach (var dir in directories)
            {
                var info = new DirectoryInfo(dir);

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

            var files = Directory.GetFiles(parent.FullPath).OrderBy(x => x);
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
        }
    }
}
