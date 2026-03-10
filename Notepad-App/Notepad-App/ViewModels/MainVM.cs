using Microsoft.Win32;
using Notepad_App.Models;
using Notepad_App.Utils;
using Notepad_App.ViewModels.Commands;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Notepad_App.ViewModels
{
    public class MainVM : BaseVM
    {
        private readonly FileManager _fileManager;
        private readonly TreeManager _treeManager;
        private readonly ConfigManager _configManager;

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
                    RefreshCommandStates();
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
            _fileManager = new FileManager();
            _treeManager = new TreeManager();
            _configManager = new ConfigManager();

            NewFileCommand = new RelayCommand(_ => CreateNewTab());
            OpenFileCommand = new RelayCommand(_ => OpenFile());
            SaveFileCommand = new RelayCommand(_ => SaveFile(), _ => SelectedTab != null);
            SaveFileAsCommand = new RelayCommand(_ => SaveFileAs(), _ => SelectedTab != null);
            CloseTabCommand = new RelayCommand(
                parameter => CloseTab(parameter as EditorTab ?? SelectedTab),
                _ => SelectedTab != null);
            CloseAllTabsCommand = new RelayCommand(_ => CloseAllTabs(), _ => Tabs.Any());
            ToggleTreeViewCommand = new RelayCommand(_ => IsTreeViewVisible = !IsTreeViewVisible);
            ShowAboutCommand = new RelayCommand(_ => ShowAbout());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            LoadTreeRoots();
            CreateNewTab();
        }

        public void LoadTreeRoots()
        {
            TreeItems.Clear();

            var roots = _treeManager.LoadRoots();

            foreach (var item in roots)
            {
                TreeItems.Add(item);
            }
        }

        public void ExpandTreeItem(TreeItem? item)
        {
            if (item == null || !item.IsDirectory)
            {
                return;
            }

            _treeManager.LoadChildren(item);
        }

        public void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                OpenFileInTab(dialog.FileName);
            }
        }

        public void OpenFileFromTree(TreeItem? item)
        {
            if (item == null || item.IsDirectory || string.IsNullOrWhiteSpace(item.FullPath))
            {
                return;
            }

            OpenFileInTab(item.FullPath);
        }

        public void OpenFileInTab(string filePath)
        {
            var existingTab = Tabs.FirstOrDefault(tab =>
                !string.IsNullOrWhiteSpace(tab.FilePath) &&
                string.Equals(tab.FilePath, filePath, System.StringComparison.OrdinalIgnoreCase));

            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            try
            {
                string content = _fileManager.ReadFile(filePath);

                var tab = new EditorTab
                {
                    Title = Path.GetFileName(filePath),
                    FilePath = filePath,
                    IsUntitled = false,
                    IsModified = false
                };

                RemoveSingleEmptyUntitledTabIfNeeded();

                tab.Content = content;
                tab.IsModified = false;

                Tabs.Add(tab);
                SelectedTab = tab;

                RefreshCommandStates();
            }
            catch
            {
                MessageBox.Show(
                    "The selected file could not be opened.",
                    "Open file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public void SaveFile()
        {
            if (SelectedTab == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTab.FilePath))
            {
                SaveFileAs();
                return;
            }

            try
            {
                _fileManager.SaveFile(SelectedTab.FilePath, SelectedTab.Content);
                SelectedTab.IsModified = false;
                SelectedTab.IsUntitled = false;
            }
            catch
            {
                MessageBox.Show(
                    "The file could not be saved.",
                    "Save file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public void SaveFileAs()
        {
            if (SelectedTab == null)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = SelectedTab.IsUntitled
                    ? SelectedTab.Title
                    : Path.GetFileName(SelectedTab.FilePath)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _fileManager.SaveFile(dialog.FileName, SelectedTab.Content);

                SelectedTab.FilePath = dialog.FileName;
                SelectedTab.Title = Path.GetFileName(dialog.FileName);
                SelectedTab.IsModified = false;
                SelectedTab.IsUntitled = false;
            }
            catch
            {
                MessageBox.Show(
                    "The file could not be saved.",
                    "Save file as",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public void CreateNewTab()
        {
            var newTab = new EditorTab
            {
                Title = "New File",
                Content = string.Empty,
                FilePath = null,
                IsModified = false,
                IsUntitled = true
            };

            Tabs.Add(newTab);
            SelectedTab = newTab;

            RefreshCommandStates();
        }

        public void CloseTab(EditorTab? tab)
        {
            if (tab == null)
            {
                return;
            }

            if (!ConfirmCloseTab(tab))
            {
                return;
            }

            Tabs.Remove(tab);

            if (Tabs.Count == 0)
            {
                CreateNewTab();
            }
            else
            {
                SelectedTab = Tabs.LastOrDefault();
            }

            RefreshCommandStates();
        }

        public void CloseAllTabs()
        {
            var tabsToClose = Tabs.ToList();

            foreach (var tab in tabsToClose)
            {
                if (!ConfirmCloseTab(tab))
                {
                    return;
                }

                Tabs.Remove(tab);
            }

            if (Tabs.Count == 0)
            {
                CreateNewTab();
            }

            RefreshCommandStates();
        }

        private bool ConfirmCloseTab(EditorTab tab)
        {
            if (!tab.IsModified)
            {
                return true;
            }

            var result = MessageBox.Show(
                $"Do you want to save changes to {tab.Title}?",
                "Unsaved changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return false;
            }

            if (result == MessageBoxResult.Yes)
            {
                SelectedTab = tab;
                SaveFile();

                if (tab.IsModified)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveSingleEmptyUntitledTabIfNeeded()
        {
            if (Tabs.Count != 1)
            {
                return;
            }

            var firstTab = Tabs[0];

            if (firstTab.IsUntitled &&
                string.IsNullOrEmpty(firstTab.Content) &&
                !firstTab.IsModified)
            {
                Tabs.Clear();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "WPF NotepadApp\n\nStudent: Oncioiu Ionut-Raul\nGroup: 10LF243\nEmail: ionut.oncioiu@student.unitbv.ro",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RefreshCommandStates()
        {
            SaveFileCommand.RaiseCanExecuteChanged();
            SaveFileAsCommand.RaiseCanExecuteChanged();
            CloseTabCommand.RaiseCanExecuteChanged();
            CloseAllTabsCommand.RaiseCanExecuteChanged();
        }

        public AppConfig BuildAppConfig(double windowWidth, double windowHeight)
        {
            return new AppConfig
            {
                IsTreeViewVisible = IsTreeViewVisible,
                WindowWidth = windowWidth,
                WindowHeight = windowHeight,
                SelectedTabIndex = SelectedTab != null ? Tabs.IndexOf(SelectedTab) : 0,
                OpenTabs = Tabs.Select(CloneTabForConfig).ToList()
            };
        }

        public void RestoreFromConfig(AppConfig config)
        {
            Tabs.Clear();

            IsTreeViewVisible = config.IsTreeViewVisible;

            if (config.OpenTabs != null && config.OpenTabs.Count > 0)
            {
                foreach (var savedTab in config.OpenTabs)
                {
                    var restoredTab = new EditorTab
                    {
                        Title = savedTab.Title,
                        FilePath = savedTab.FilePath,
                        IsUntitled = savedTab.IsUntitled,
                        IsModified = false,
                        Content = string.Empty
                    };

                    if (!string.IsNullOrWhiteSpace(savedTab.FilePath) &&
                        _fileManager.FileExists(savedTab.FilePath))
                    {
                        string content = _fileManager.ReadFile(savedTab.FilePath);
                        restoredTab.Content = content;
                        restoredTab.IsModified = false;
                        restoredTab.Title = Path.GetFileName(savedTab.FilePath);
                        restoredTab.IsUntitled = false;
                    }
                    else
                    {
                        restoredTab.Content = savedTab.Content;
                        restoredTab.IsModified = false;
                    }

                    Tabs.Add(restoredTab);
                }
            }

            if (Tabs.Count == 0)
            {
                CreateNewTab();
            }
            else if (config.SelectedTabIndex >= 0 && config.SelectedTabIndex < Tabs.Count)
            {
                SelectedTab = Tabs[config.SelectedTabIndex];
            }
            else
            {
                SelectedTab = Tabs.FirstOrDefault();
            }

            RefreshCommandStates();
        }

        private EditorTab CloneTabForConfig(EditorTab source)
        {
            return new EditorTab
            {
                Title = source.Title,
                Content = source.Content,
                FilePath = source.FilePath,
                IsModified = source.IsModified,
                IsUntitled = source.IsUntitled
            };
        }

    }
}