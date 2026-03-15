using System.Windows;
using System.Windows.Interop;
using NativeHymnsApp.Models;
using NativeHymnsApp.ViewModels;
using NativeHymnsApp.Views;
using Screen = System.Windows.Forms.Screen;

namespace NativeHymnsApp;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private PresenterWindow? _presenterWindow;
    private SlideshowControlWindow? _slideshowWindow;
    private ThemeEditorWindow? _themeEditorWindow;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.OpenSlideshowRequested += HandleOpenSlideshowRequested;
    }

    private void HandleOpenSlideshowRequested(object? sender, EventArgs e)
    {
        EnsureSlideshowWindow();
        EnsurePresenterWindow();
    }

    private void EnsurePresenterWindow()
    {
        if (Screen.AllScreens.Length < 2)
        {
            _presenterWindow?.Hide();
            return;
        }

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

    private void EnsureSlideshowWindow()
    {
        if (_slideshowWindow is { IsLoaded: true })
        {
            if (!_slideshowWindow.IsVisible)
            {
                _slideshowWindow.Show();
            }

            _slideshowWindow.Activate();
            return;
        }

        _slideshowWindow = new SlideshowControlWindow(_viewModel)
        {
            Owner = this
        };
        _slideshowWindow.Closed += (_, _) => _slideshowWindow = null;
        PositionWorkbenchWindow(_slideshowWindow, 0.9, 0.88);
        _slideshowWindow.Show();
        _slideshowWindow.Activate();
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

    private void PositionWorkbenchWindow(Window window, double widthRatio, double heightRatio)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var screen = Screen.FromHandle(handle);
        var workingArea = screen.WorkingArea;
        var width = Math.Min(workingArea.Width - 48, (int)(workingArea.Width * widthRatio));
        var height = Math.Min(workingArea.Height - 48, (int)(workingArea.Height * heightRatio));

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = Math.Max(window.MinWidth, width);
        window.Height = Math.Max(window.MinHeight, height);
        window.Left = workingArea.Left + Math.Max(24, (workingArea.Width - window.Width) / 2);
        window.Top = workingArea.Top + Math.Max(24, (workingArea.Height - window.Height) / 2);
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

        EditCustomSong(song);
    }

    private void EditLibrarySong_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: SongDocument { Kind: SongKind.CustomSong } song })
        {
            return;
        }

        _viewModel.SelectedLibrarySong = song;
        EditCustomSong(song);
    }

    private void EditCustomSong(SongDocument song)
    {
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

    private void DeleteLibrarySong_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: SongDocument { Kind: SongKind.CustomSong } song })
        {
            return;
        }

        _viewModel.SelectedLibrarySong = song;
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

    private void OpenThemeEditor_Click(object sender, RoutedEventArgs e)
    {
        if (_themeEditorWindow is { IsLoaded: true })
        {
            if (!_themeEditorWindow.IsVisible)
            {
                _themeEditorWindow.Show();
            }

            _themeEditorWindow.Activate();
            return;
        }

        _themeEditorWindow = new ThemeEditorWindow(_viewModel)
        {
            Owner = this
        };
        _themeEditorWindow.Closed += (_, _) => _themeEditorWindow = null;
        PositionWorkbenchWindow(_themeEditorWindow, 0.45, 0.82);
        _themeEditorWindow.Show();
        _themeEditorWindow.Activate();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.OpenSlideshowRequested -= HandleOpenSlideshowRequested;
        _slideshowWindow?.Close();
        _themeEditorWindow?.Close();
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
