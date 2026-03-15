using System.Windows;
using Microsoft.Win32;
using NativeHymnsApp.ViewModels;

namespace NativeHymnsApp.Views;

public partial class ThemeEditorWindow : Window
{
    public ThemeEditorWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void BrowseBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "Choose Background Image"
        };

        if (dialog.ShowDialog(this) == true && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetBackgroundImagePath(dialog.FileName);
        }
    }

    private void ClearBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ClearBackgroundImagePath();
        }
    }
}
