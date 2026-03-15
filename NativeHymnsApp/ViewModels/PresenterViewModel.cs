using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NativeHymnsApp.Infrastructure;
using NativeHymnsApp.Models;
using NativeHymnsApp.Services;

using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;

namespace NativeHymnsApp.ViewModels;

public sealed class PresenterViewModel : ObservableObject
{
    private readonly PresentationSession _session;
    private readonly ThemeSettings _theme;

    public PresenterViewModel(PresentationSession session, ThemeSettings theme)
    {
        _session = session;
        _theme = theme;
        _session.PropertyChanged += HandleSourceChanged;
        _theme.PropertyChanged += HandleSourceChanged;
    }

    public string DeckTitle => _session.ActiveDeck?.DisplayTitle ?? "Select a hymn to begin";

    public string DeckSubtitle => _session.ActiveDeck?.DisplaySubtitle ?? "Load a hymn, custom song, or service item from the main window.";

    public string SlideHeading => _session.CurrentSlide?.Heading ?? string.Empty;

    public string SlideText => _session.CurrentSlide?.Text ?? string.Empty;

    public string SlideCounterText => _session.SlideCounterText;

    public bool HasActiveDeck => _session.HasActiveDeck;

    public bool IsIdle => !_session.HasActiveDeck;

    public bool IsBlankScreen => _session.IsBlankScreen;

    public bool IsBlackScreen => _session.IsBlackScreen;

    public MediaBrush DisplayBackgroundBrush => BuildBackgroundBrush();

    public MediaBrush ForegroundBrush => BuildSolidBrush(_theme.ForegroundHex, Colors.WhiteSmoke);

    public MediaBrush AccentBrush => BuildSolidBrush(_theme.AccentHex, MediaColor.FromRgb(216, 189, 122));

    public MediaBrush OverlayBrush => BuildSolidBrush(_theme.OverlayHex, MediaColor.FromArgb(178, 18, 24, 32));

    public MediaFontFamily TitleFontFamily => CreateFontFamily(_theme.FontFamilyName);

    public MediaFontFamily BodyFontFamily => CreateFontFamily(_theme.FontFamilyName);

    public double TitleFontSize => _theme.TitleFontSize;

    public double BodyFontSize => _theme.BodyFontSize;

    public TextAlignment DisplayTextAlignment => _theme.ContentAlignment == ContentAlignmentMode.Left
        ? TextAlignment.Left
        : TextAlignment.Center;

    public string FooterText => string.IsNullOrWhiteSpace(_theme.FooterText)
        ? "Khasi Presbyterian Hymns"
        : _theme.FooterText;

    private void HandleSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(
            nameof(DeckTitle),
            nameof(DeckSubtitle),
            nameof(SlideHeading),
            nameof(SlideText),
            nameof(SlideCounterText),
            nameof(HasActiveDeck),
            nameof(IsIdle),
            nameof(IsBlankScreen),
            nameof(IsBlackScreen),
            nameof(DisplayBackgroundBrush),
            nameof(ForegroundBrush),
            nameof(AccentBrush),
            nameof(OverlayBrush),
            nameof(TitleFontFamily),
            nameof(BodyFontFamily),
            nameof(TitleFontSize),
            nameof(BodyFontSize),
            nameof(DisplayTextAlignment),
            nameof(FooterText));
    }

    private MediaBrush BuildBackgroundBrush()
    {
        if (!string.IsNullOrWhiteSpace(_theme.BackgroundImagePath) && File.Exists(_theme.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(_theme.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                var imageBrush = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
                imageBrush.Freeze();
                return imageBrush;
            }
            catch
            {
                // Fall back to the gradient background.
            }
        }

        var start = GetColor(_theme.BackgroundHex, MediaColor.FromRgb(15, 26, 31));
        var end = GetColor(_theme.SecondaryBackgroundHex, MediaColor.FromRgb(25, 56, 68));
        var brush = new LinearGradientBrush(start, end, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
        brush.Freeze();
        return brush;
    }

    private static MediaBrush BuildSolidBrush(string value, MediaColor fallbackColor)
    {
        var brush = new SolidColorBrush(GetColor(value, fallbackColor));
        brush.Freeze();
        return brush;
    }

    private static MediaColor GetColor(string value, MediaColor fallbackColor)
    {
        try
        {
            var converted = System.Windows.Media.ColorConverter.ConvertFromString(value);
            return converted is MediaColor color ? color : fallbackColor;
        }
        catch
        {
            return fallbackColor;
        }
    }

    private static MediaFontFamily CreateFontFamily(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? new MediaFontFamily("Georgia")
            : new MediaFontFamily(name);
    }
}
