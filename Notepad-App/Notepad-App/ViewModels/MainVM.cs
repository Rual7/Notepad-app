using Notepad_App.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Notepad_App.ViewModels.Commands;

namespace Notepad_App.ViewModels;
public class MainVM : BaseVM
{
    private EditorTab? _selectedTab;
    private bool _isTreeViewVisible = true;
    private int _untitledCounter = 1;

    public ObservableCollection<EditorTab> Tabs { get; } = new();

    public ObservableCollection<TreeItem> TreeItems { get; } = new();

    public EditorTab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value))
            {
                CloseTabCommand.RaiseCanExecuteChanged();
                SaveFileCommand.RaiseCanExecuteChanged();
                SaveFileAsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsTreeViewVisible
    {
        get => _isTreeViewVisible;
        set => SetField(ref _isTreeViewVisible, value);
    }

    public RelayCommand NewFileCommand { get; }
    public RelayCommand OpenFileCommand { get; }
    public RelayCommand SaveFileCommand { get; }
    public RelayCommand SaveFileAsCommand { get; }
    public RelayCommand CloseTabCommand { get; }
    public RelayCommand CloseAllTabsCommand { get; }
    public RelayCommand ToggleTreeViewCommand { get; }
    public RelayCommand ShowAboutCommand { get; }
    public RelayCommand ExitCommand { get; }

    public MainVM()
    {
        NewFileCommand = new RelayCommand(_ => CreateNewTab());

        OpenFileCommand = new RelayCommand(_ =>
        {
            MessageBox.Show("Open file will be implemented next.");
        });

        SaveFileCommand = new RelayCommand(
            _ => MessageBox.Show("Save file will be implemented next."),
            _ => SelectedTab != null);

        SaveFileAsCommand = new RelayCommand(
            _ => MessageBox.Show("Save file as will be implemented next."),
            _ => SelectedTab != null);

        CloseTabCommand = new RelayCommand(
            parameter => CloseTab(parameter as EditorTab ?? SelectedTab),
            _ => SelectedTab != null);

        CloseAllTabsCommand = new RelayCommand(
            _ => CloseAllTabs(),
            _ => Tabs.Any());

        ToggleTreeViewCommand = new RelayCommand(
            _ => IsTreeViewVisible = !IsTreeViewVisible);

        ShowAboutCommand = new RelayCommand(_ => ShowAbout());

        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

        CreateNewTab();
    }

    public void CreateNewTab()
    {
        var newTab = new EditorTab
        {
            Title = $"File {_untitledCounter++}",
            Content = string.Empty,
            FilePath = null,
            IsModified = false,
            IsUntitled = true
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;

        CloseAllTabsCommand.RaiseCanExecuteChanged();
    }

    public void CloseTab(EditorTab? tab)
    {
        if (tab == null)
        {
            return;
        }

        Tabs.Remove(tab);

        if (SelectedTab == tab)
        {
            SelectedTab = Tabs.LastOrDefault();
        }

        if (!Tabs.Any())
        {
            CreateNewTab();
        }

        CloseTabCommand.RaiseCanExecuteChanged();
        CloseAllTabsCommand.RaiseCanExecuteChanged();
    }

    public void CloseAllTabs()
    {
        Tabs.Clear();
        CreateNewTab();

        CloseTabCommand.RaiseCanExecuteChanged();
        CloseAllTabsCommand.RaiseCanExecuteChanged();
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "WPF Notepad\n\nStudent: Oncioiu Ionut-Raul\nGroup: 10LF243\nEmail: ionut.oncioiu@student.unitbv.ro",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
