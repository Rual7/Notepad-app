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
        ViewModel.SelectTextRequested += OnSelectTextRequested;

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
    private void OnSelectTextRequested(int startIndex, int length)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (EditorTabControl.SelectedItem == null)
            {
                return;
            }

            var contentPresenter = EditorTabControl.Template.FindName("PART_SelectedContentHost", EditorTabControl) as ContentPresenter;

            if (contentPresenter == null)
            {
                return;
            }

            var textBox = EditorTabControl.ContentTemplate.FindName("EditorTextBox", contentPresenter) as TextBox;

            if (textBox == null)
            {
                return;
            }

            textBox.Focus();
            textBox.Select(startIndex, length);
            textBox.CaretIndex = startIndex + length;
            textBox.ScrollToLine(textBox.GetLineIndexFromCharacterIndex(startIndex));
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                return typedChild;
            }

            T? descendant = FindVisualChild<T>(child);

            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}