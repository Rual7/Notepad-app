using Microsoft.Win32;
using Notepad_App.Models;
using Notepad_App.Utils;
using Notepad_App.ViewModels.Commands;
using Notepad_App.Views;
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

        private bool _isTreeViewVisible = false;
        public GridLength TreeColumnWidth =>
            IsTreeViewVisible ? new GridLength(250) : new GridLength(0);

        public GridLength SplitterColumnWidth =>
            IsTreeViewVisible ? new GridLength(3) : new GridLength(0);

        public const double DefaultWindowWidth = 1100;
        public const double DefaultWindowHeight = 700;

        private string? _copiedFolderPath;
        private bool _searchAllTabs;

        public bool SearchAllTabs
        {
            get => _searchAllTabs;
            set
            {
                if (SetField(ref _searchAllTabs, value))
                {
                    OnPropertyChanged(nameof(SearchSelectedTab));
                }
            }
        }

        public bool SearchSelectedTab
        {
            get => !SearchAllTabs;
            set
            {
                if (value) // când bifezi Selected tab
                {
                    SearchAllTabs = false;
                }
                else // dacă îl debifezi explicit, activează All tabs
                {
                    SearchAllTabs = true;
                }
            }
        }

        public ObservableCollection<EditorTab> Tabs { get; } = new();
        public ObservableCollection<TreeItem> TreeItems { get; } = new();

        public event Action<int, int>? SelectTextRequested;
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
            set
            {
                if (SetField(ref _isTreeViewVisible, value))
                {
                    OnPropertyChanged(nameof(TreeColumnWidth));
                    OnPropertyChanged(nameof(SplitterColumnWidth));
                }
            }
        }

        public RelayCommand NewFileCommand { get; }
        public RelayCommand OpenFileCommand { get; }
        public RelayCommand SaveFileCommand { get; }
        public RelayCommand SaveFileAsCommand { get; }
        public RelayCommand CloseTabCommand { get; }
        public RelayCommand CloseAllTabsCommand { get; }
        public RelayCommand ResetViewCommand { get; }
        public RelayCommand ShowAboutCommand { get; }
        public RelayCommand ExitCommand { get; }

        public bool HasCopiedFolder => !string.IsNullOrWhiteSpace(_copiedFolderPath);

        public RelayCommand NewTreeFileCommand { get; }
        public RelayCommand CopyPathCommand { get; }
        public RelayCommand CopyFolderCommand { get; }
        public RelayCommand PasteFolderCommand { get; }

        public RelayCommand FindCommand { get; }
        public RelayCommand ReplaceCommand { get; }
        public RelayCommand ReplaceAllCommand { get; }

        public MainVM()
        {
            SearchAllTabs = false;
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
            ResetViewCommand = new RelayCommand(_ => ResetView());
            ShowAboutCommand = new RelayCommand(_ => ShowAbout());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            FindCommand = new RelayCommand(_ => FindText());
            ReplaceCommand = new RelayCommand(_ => ReplaceText());
            ReplaceAllCommand = new RelayCommand(_ => ReplaceAllText());

            NewTreeFileCommand = new RelayCommand(
                parameter => CreateNewFileInTree(parameter as TreeItem),
                parameter => parameter is TreeItem item && item.IsDirectory);

            CopyPathCommand = new RelayCommand(
                parameter => CopyTreePath(parameter as TreeItem),
                parameter => parameter is TreeItem item && item.IsDirectory);

            CopyFolderCommand = new RelayCommand(
                parameter => CopyTreeFolder(parameter as TreeItem),
                parameter => parameter is TreeItem item && item.IsDirectory);

            PasteFolderCommand = new RelayCommand(
                parameter => PasteTreeFolder(parameter as TreeItem),
                parameter => parameter is TreeItem item && item.IsDirectory && HasCopiedFolder);

            LoadTreeRoots();
            CreateNewTab();
        }

        public event Action? ResetViewRequested;

        private void ResetView()
        {
            IsTreeViewVisible = false;
            ResetViewRequested?.Invoke();
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


        public void CreateNewFileInTree(TreeItem? item)
        {
            if (item == null || !item.IsDirectory)
            {
                return;
            }

            try
            {
                string newFile = _treeManager.CreateNewFile(item.FullPath);

                ReloadTreeItem(item);

                OpenFileInTab(newFile);
            }
            catch
            {
                MessageBox.Show(
                    "The file could not be created.",
                    "New file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        public void CopyTreePath(TreeItem? item)
        {
            if (item == null || !item.IsDirectory)
            {
                return;
            }

            Clipboard.SetText(item.FullPath);
        }

        public void CopyTreeFolder(TreeItem? item)
        {
            if (item == null || !item.IsDirectory)
            {
                return;
            }

            _copiedFolderPath = item.FullPath;
            PasteFolderCommand.RaiseCanExecuteChanged();
        }

        public async void PasteTreeFolder(TreeItem? item)
        {
            if (item == null || !item.IsDirectory || string.IsNullOrWhiteSpace(_copiedFolderPath))
            {
                return;
            }

            try
            {
                await _treeManager.CopyFolderAsync(_copiedFolderPath, item.FullPath);

                ReloadTreeItem(item);
            }
            catch
            {
                MessageBox.Show(
                    "The folder could not be pasted.",
                    "Paste folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ReloadTreeItem(TreeItem item)
        {
            item.IsLoaded = false;
            item.Children.Clear();
            ExpandTreeItem(item);
            item.IsExpanded = true;
        }

        private void FindText()
        {
            var dialog = new FindReplaceWindow(false)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string textToFind = dialog.FindText;

            if (string.IsNullOrWhiteSpace(textToFind))
            {
                MessageBox.Show("Please enter text to find.");
                return;
            }

            if (SearchAllTabs)
            {
                var results = Tabs
                    .Select(tab => new
                    {
                        Tab = tab,
                        Count = CountOccurrences(tab.Content, textToFind)
                    })
                    .Where(x => x.Count > 0)
                    .ToList();

                if (results.Count == 0)
                {
                    MessageBox.Show("Text not found in any open tab.");
                    return;
                }

                SelectedTab = results.First().Tab;

                string message = string.Join(
                    "\n",
                    results.Select(x => $"{x.Tab.Title}: {x.Count} occurrence(s)"));

                MessageBox.Show(
                    $"Found in {results.Count} tab(s):\n\n{message}",
                    "Find",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                if (SelectedTab == null)
                {
                    return;
                }

                int index = FindFirstIndex(SelectedTab.Content, textToFind);

                if (index >= 0)
                {
                    SelectTextRequested?.Invoke(index, textToFind.Length);
                }
                else
                {
                    MessageBox.Show(
                        "Text not found in current tab.",
                        "Find",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void ReplaceText()
        {
            var dialog = new FindReplaceWindow(true)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string find = dialog.FindText;
            string replace = dialog.ReplaceText;

            if (string.IsNullOrWhiteSpace(find))
            {
                MessageBox.Show("Please enter text to replace.");
                return;
            }

            int count = 0;

            if (SearchAllTabs)
            {
                foreach (var tab in Tabs)
                {
                    count += ReplaceFirstInTab(tab, find, replace);
                }
            }
            else
            {
                if (SelectedTab == null)
                {
                    return;
                }

                count = ReplaceFirstInTab(SelectedTab, find, replace);
            }

            MessageBox.Show(
                $"Replaced {count} occurrence(s).",
                "Replace",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ReplaceAllText()
        {
            var dialog = new FindReplaceWindow(true)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string find = dialog.FindText;
            string replace = dialog.ReplaceText;

            if (string.IsNullOrWhiteSpace(find))
            {
                MessageBox.Show("Please enter text to replace.");
                return;
            }

            if (SearchAllTabs)
            {
                var results = new List<string>();
                int totalCount = 0;

                foreach (var tab in Tabs)
                {
                    int count = CountOccurrences(tab.Content, find);

                    if (count > 0)
                    {
                        tab.Content = tab.Content.Replace(find, replace);
                        tab.IsModified = true;

                        results.Add($"{tab.Title}: {count} replacement(s)");
                        totalCount += count;
                    }
                }

                if (results.Count == 0)
                {
                    MessageBox.Show("No occurrences found.");
                    return;
                }

                string message = string.Join("\n", results);

                MessageBox.Show(
                    $"Total replacements: {totalCount}\n\n{message}",
                    "Replace All",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                if (SelectedTab == null)
                {
                    return;
                }

                int count = CountOccurrences(SelectedTab.Content, find);

                if (count == 0)
                {
                    MessageBox.Show("No occurrences found.");
                    return;
                }

                SelectedTab.Content = SelectedTab.Content.Replace(find, replace);
                SelectedTab.IsModified = true;

                MessageBox.Show(
                    $"{SelectedTab.Title}: {count} replacement(s)",
                    "Replace All",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private int CountOccurrences(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int count = 0;
            int startIndex = 0;

            while (true)
            {
                int index = source.IndexOf(value, startIndex);

                if (index < 0)
                {
                    break;
                }

                count++;
                startIndex = index + value.Length;
            }

            return count;
        }

        private int FindFirstIndex(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return -1;
            }

            return source.IndexOf(value);
        }
        private bool ContainsText(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int ReplaceFirstInTab(EditorTab tab, string find, string replace)
        {
            int index = tab.Content.IndexOf(find, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                return 0;
            }

            tab.Content = tab.Content.Remove(index, find.Length).Insert(index, replace);
            tab.IsModified = true;
            return 1;
        }

        private int ReplaceAllInTab(EditorTab tab, string find, string replace)
        {
            if (string.IsNullOrEmpty(find))
            {
                return 0;
            }

            int count = 0;
            int startIndex = 0;

            while (true)
            {
                int index = tab.Content.IndexOf(find, startIndex, StringComparison.OrdinalIgnoreCase);

                if (index < 0)
                {
                    break;
                }

                tab.Content = tab.Content.Remove(index, find.Length).Insert(index, replace);
                startIndex = index + replace.Length;
                count++;
            }

            if (count > 0)
            {
                tab.IsModified = true;
            }

            return count;
        }
    }

}