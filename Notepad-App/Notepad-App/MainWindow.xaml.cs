using Notepad_App.Models;
using Notepad_App.Utils;
using Notepad_App.ViewModels;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;

namespace Notepad_App;

public partial class MainWindow : Window
{
    private readonly ConfigManager _configManager = new();

    private MainVM ViewModel => (MainVM)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainVM();
        ViewModel.ResetViewRequested += OnResetViewRequested;

        var config = _configManager.LoadConfig();

        Width = config.WindowWidth;
        Height = config.WindowHeight;

        ViewModel.RestoreFromConfig(config);
    }
    private void OnResetViewRequested()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }

        Width = MainVM.DefaultWindowWidth;
        Height = MainVM.DefaultWindowHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem treeViewItem &&
            treeViewItem.DataContext is TreeItem item)
        {
            ViewModel.ExpandTreeItem(item);
        }
    }

    private void ProjectTreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProjectTreeView.SelectedItem is TreeItem item)
        {
            ViewModel.OpenFileFromTree(item);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!TryCloseAllTabsBeforeExit())
        {
            e.Cancel = true;
            return;
        }

        var config = ViewModel.BuildAppConfig(Width, Height);
        _configManager.SaveConfig(config);

        base.OnClosing(e);
    }

    private bool TryCloseAllTabsBeforeExit()
    {
        var tabs = ViewModel.Tabs.ToArray();

        foreach (var tab in tabs)
        {
            if (tab.IsModified)
            {
                ViewModel.SelectedTab = tab;

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
                    ViewModel.SaveFile();

                    if (tab.IsModified)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}