using System.Windows;
using System.Windows.Input;
using NativeHymnsApp.ViewModels;

namespace NativeHymnsApp.Views;

public partial class SlideshowControlWindow : Window
{
    public SlideshowControlWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SlideshowControlWindow_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Close();
        }
    }
}
