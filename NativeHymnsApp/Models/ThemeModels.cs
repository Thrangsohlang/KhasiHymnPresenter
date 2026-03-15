using System.Text.Json.Serialization;
using NativeHymnsApp.Infrastructure;

namespace NativeHymnsApp.Models;

public enum ContentAlignmentMode
{
    Center,
    Left
}

public sealed class ThemePreset
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FontFamilyName { get; set; } = string.Empty;

    public double TitleFontSize { get; set; }

    public double BodyFontSize { get; set; }

    public string BackgroundHex { get; set; } = string.Empty;

    public string SecondaryBackgroundHex { get; set; } = string.Empty;

    public string ForegroundHex { get; set; } = string.Empty;

    public string AccentHex { get; set; } = string.Empty;

    public string OverlayHex { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentAlignmentMode ContentAlignment { get; set; }

    public string FooterText { get; set; } = string.Empty;
}

public sealed class ThemeSettingsSnapshot
{
    public string FontFamilyName { get; set; } = "Georgia";

    public double TitleFontSize { get; set; } = 34;

    public double BodyFontSize { get; set; } = 28;

    public string BackgroundHex { get; set; } = "#0F1A1F";

    public string SecondaryBackgroundHex { get; set; } = "#193844";

    public string ForegroundHex { get; set; } = "#FFF7EC";

    public string AccentHex { get; set; } = "#D8BD7A";

    public string OverlayHex { get; set; } = "#B3121820";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentAlignmentMode ContentAlignment { get; set; } = ContentAlignmentMode.Center;

    public string FooterText { get; set; } = "Khasi Presbyterian Hymns";

    public string BackgroundImagePath { get; set; } = string.Empty;

    public static ThemeSettingsSnapshot CreateDefault()
    {
        return new ThemeSettingsSnapshot();
    }
}

public sealed class ThemeSettings : ObservableObject
{
    private string _fontFamilyName = ThemeSettingsSnapshot.CreateDefault().FontFamilyName;
    private double _titleFontSize = ThemeSettingsSnapshot.CreateDefault().TitleFontSize;
    private double _bodyFontSize = ThemeSettingsSnapshot.CreateDefault().BodyFontSize;
    private string _backgroundHex = ThemeSettingsSnapshot.CreateDefault().BackgroundHex;
    private string _secondaryBackgroundHex = ThemeSettingsSnapshot.CreateDefault().SecondaryBackgroundHex;
    private string _foregroundHex = ThemeSettingsSnapshot.CreateDefault().ForegroundHex;
    private string _accentHex = ThemeSettingsSnapshot.CreateDefault().AccentHex;
    private string _overlayHex = ThemeSettingsSnapshot.CreateDefault().OverlayHex;
    private ContentAlignmentMode _contentAlignment = ThemeSettingsSnapshot.CreateDefault().ContentAlignment;
    private string _footerText = ThemeSettingsSnapshot.CreateDefault().FooterText;
    private string _backgroundImagePath = ThemeSettingsSnapshot.CreateDefault().BackgroundImagePath;

    public string FontFamilyName
    {
        get => _fontFamilyName;
        set => SetProperty(ref _fontFamilyName, value);
    }

    public double TitleFontSize
    {
        get => _titleFontSize;
        set => SetProperty(ref _titleFontSize, value);
    }

    public double BodyFontSize
    {
        get => _bodyFontSize;
        set => SetProperty(ref _bodyFontSize, value);
    }

    public string BackgroundHex
    {
        get => _backgroundHex;
        set => SetProperty(ref _backgroundHex, value);
    }

    public string SecondaryBackgroundHex
    {
        get => _secondaryBackgroundHex;
        set => SetProperty(ref _secondaryBackgroundHex, value);
    }

    public string ForegroundHex
    {
        get => _foregroundHex;
        set => SetProperty(ref _foregroundHex, value);
    }

    public string AccentHex
    {
        get => _accentHex;
        set => SetProperty(ref _accentHex, value);
    }

    public string OverlayHex
    {
        get => _overlayHex;
        set => SetProperty(ref _overlayHex, value);
    }

    public ContentAlignmentMode ContentAlignment
    {
        get => _contentAlignment;
        set => SetProperty(ref _contentAlignment, value);
    }

    public string FooterText
    {
        get => _footerText;
        set => SetProperty(ref _footerText, value);
    }

    public string BackgroundImagePath
    {
        get => _backgroundImagePath;
        set => SetProperty(ref _backgroundImagePath, value);
    }

    public ThemeSettingsSnapshot ToSnapshot()
    {
        return new ThemeSettingsSnapshot
        {
            FontFamilyName = FontFamilyName,
            TitleFontSize = TitleFontSize,
            BodyFontSize = BodyFontSize,
            BackgroundHex = BackgroundHex,
            SecondaryBackgroundHex = SecondaryBackgroundHex,
            ForegroundHex = ForegroundHex,
            AccentHex = AccentHex,
            OverlayHex = OverlayHex,
            ContentAlignment = ContentAlignment,
            FooterText = FooterText,
            BackgroundImagePath = BackgroundImagePath
        };
    }

    public void ApplySnapshot(ThemeSettingsSnapshot snapshot)
    {
        FontFamilyName = snapshot.FontFamilyName;
        TitleFontSize = snapshot.TitleFontSize;
        BodyFontSize = snapshot.BodyFontSize;
        BackgroundHex = snapshot.BackgroundHex;
        SecondaryBackgroundHex = snapshot.SecondaryBackgroundHex;
        ForegroundHex = snapshot.ForegroundHex;
        AccentHex = snapshot.AccentHex;
        OverlayHex = snapshot.OverlayHex;
        ContentAlignment = snapshot.ContentAlignment;
        FooterText = snapshot.FooterText;
        BackgroundImagePath = snapshot.BackgroundImagePath;
    }

    public void ApplyPreset(ThemePreset preset)
    {
        ApplySnapshot(new ThemeSettingsSnapshot
        {
            FontFamilyName = preset.FontFamilyName,
            TitleFontSize = preset.TitleFontSize,
            BodyFontSize = preset.BodyFontSize,
            BackgroundHex = preset.BackgroundHex,
            SecondaryBackgroundHex = preset.SecondaryBackgroundHex,
            ForegroundHex = preset.ForegroundHex,
            AccentHex = preset.AccentHex,
            OverlayHex = preset.OverlayHex,
            ContentAlignment = preset.ContentAlignment,
            FooterText = preset.FooterText,
            BackgroundImagePath = string.Empty
        });
    }

    public void ResetToDefault()
    {
        ApplySnapshot(ThemeSettingsSnapshot.CreateDefault());
    }
}
