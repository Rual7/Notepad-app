using System.Windows;

namespace Notepad_App.Views;

public partial class FindReplaceWindow : Window
{
    #region Properties

    public string FindText => FindBox.Text;
    public string ReplaceText => ReplaceBox.Text;

    #endregion

    #region Constructor

    public FindReplaceWindow(bool showReplace)
    {
        InitializeComponent();

        if (!showReplace)
        {
            ReplaceLabel.Visibility = Visibility.Collapsed;
            ReplaceBox.Visibility = Visibility.Collapsed;
        }
    }

    #endregion

    #region Event Handlers

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    #endregion
}