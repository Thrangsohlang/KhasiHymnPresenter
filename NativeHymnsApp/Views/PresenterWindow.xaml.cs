using System.Windows;
using System.Windows.Input;
using NativeHymnsApp.ViewModels;

namespace NativeHymnsApp.Views;

public partial class PresenterWindow : Window
{
    public PresenterWindow(PresenterViewModel viewModel, MainWindowViewModel operatorViewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ConfigureInputBindings(operatorViewModel);
    }

    private void PresenterWindow_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
        }
    }

    private void ConfigureInputBindings(MainWindowViewModel operatorViewModel)
    {
        InputBindings.Add(new KeyBinding(operatorViewModel.NextSlideCommand, Key.PageDown, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(operatorViewModel.PreviousSlideCommand, Key.PageUp, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(operatorViewModel.NextSlideCommand, Key.Right, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(operatorViewModel.PreviousSlideCommand, Key.Left, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(operatorViewModel.BlackScreenCommand, Key.B, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(operatorViewModel.BlankScreenCommand, Key.W, ModifierKeys.None));
    }
}
