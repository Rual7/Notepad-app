using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Notepad_App.Views;


public partial class FindReplaceWindow : Window
{
    public string FindText => FindBox.Text;
    public string ReplaceText => ReplaceBox.Text;

    public FindReplaceWindow(bool showReplace)
    {
        InitializeComponent();

        if (!showReplace)
        {
            ReplaceLabel.Visibility = Visibility.Collapsed;
            ReplaceBox.Visibility = Visibility.Collapsed;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
