using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using NativeHymnsApp.Models;
using NativeHymnsApp.ViewModels;
using NativeHymnsApp.Views;
using Screen = System.Windows.Forms.Screen;

namespace NativeHymnsApp;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private PresenterWindow? _presenterWindow;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.OpenPresenterRequested += HandleOpenPresenterRequested;
    }

    private void HandleOpenPresenterRequested(object? sender, EventArgs e)
    {
        EnsurePresenterWindow();
    }

    private void EnsurePresenterWindow()
    {
        if (_presenterWindow is { IsLoaded: true })
        {
            if (!_presenterWindow.IsVisible)
            {
                _presenterWindow.Show();
            }

            return;
        }

        _presenterWindow = new PresenterWindow(_viewModel.PresenterDisplay, _viewModel)
        {
            ShowActivated = false
        };
        _presenterWindow.Closed += (_, _) => _presenterWindow = null;
        PositionPresenterWindow(_presenterWindow);
        _presenterWindow.Show();
    }

    private static void PositionPresenterWindow(Window window)
    {
        var screens = Screen.AllScreens;
        var target = screens.Length > 1 ? screens[1] : screens[0];
        var bounds = target.Bounds;

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = bounds.Left;
        window.Top = bounds.Top;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        window.WindowState = WindowState.Normal;
        window.WindowState = WindowState.Maximized;
    }

    private void AddCustomSong_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentEditorWindow(
            "Add Custom Song",
            "Separate slides with a blank line. Optional first lines like Verse 1 or Chorus become slide headings.");

        if (dialog.ShowDialog() == true)
        {
            var song = _viewModel.BuildCustomSong(dialog.EntryTitle, dialog.EntryContent);
            _viewModel.AddOrUpdateCustomSong(song);
        }
    }

    private void EditCustomSong_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedLibrarySong is not { Kind: SongKind.CustomSong } song)
        {
            return;
        }

        var dialog = new ContentEditorWindow(
            "Edit Custom Song",
            "Separate slides with a blank line. Optional first lines like Verse 1 or Chorus become slide headings.",
            song.Title,
            _viewModel.FormatSongForEditing(song));

        if (dialog.ShowDialog() == true)
        {
            var updatedSong = _viewModel.BuildCustomSong(dialog.EntryTitle, dialog.EntryContent, song.Id);
            _viewModel.AddOrUpdateCustomSong(updatedSong);
        }
    }

    private void DeleteCustomSong_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteSelectedCustomSong();
    }

    private void AddTextSlide_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentEditorWindow(
            "Add Program Slide",
            "Use blank lines to create multiple slides for the order of service, sermon notes, announcements, or benediction.");

        if (dialog.ShowDialog() == true)
        {
            var planEntry = _viewModel.BuildTextSlideEntry(dialog.EntryTitle, dialog.EntryContent);
            _viewModel.AddTextSlideToPlan(planEntry);
        }
    }

    private void BrowseBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "Choose Background Image"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.SetBackgroundImagePath(dialog.FileName);
        }
    }

    private void ClearBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearBackgroundImagePath();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.OpenPresenterRequested -= HandleOpenPresenterRequested;
        _presenterWindow?.Close();
        base.OnClosed(e);
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var screen = Screen.FromHandle(handle);
        var workingArea = screen.WorkingArea;

        MaxWidth = workingArea.Width;
        MaxHeight = workingArea.Height;

        if (WindowState != WindowState.Maximized)
        {
            Width = Math.Min(Width, workingArea.Width);
            Height = Math.Min(Height, workingArea.Height);
            Left = workingArea.Left;
            Top = workingArea.Top;
        }
    }
}
