using NativeHymnsApp.Models;

namespace NativeHymnsApp.Services;

public static class ThemePresetCatalog
{
    public static IReadOnlyList<ThemePreset> GetPresets()
    {
        return new[]
        {
            new ThemePreset
            {
                Name = "Sanctuary",
                Description = "Deep teal and warm gold for classic hymn projection.",
                FontFamilyName = "Georgia",
                TitleFontSize = 34,
                BodyFontSize = 28,
                BackgroundHex = "#0F1A1F",
                SecondaryBackgroundHex = "#193844",
                ForegroundHex = "#FFF7EC",
                AccentHex = "#D8BD7A",
                OverlayHex = "#B3121820",
                ContentAlignment = ContentAlignmentMode.Center,
                FooterText = "Khasi Presbyterian Hymns"
            },
            new ThemePreset
            {
                Name = "Morning Light",
                Description = "A bright parchment look for welcome slides and order-of-service screens.",
                FontFamilyName = "Palatino Linotype",
                TitleFontSize = 36,
                BodyFontSize = 27,
                BackgroundHex = "#F6ECD8",
                SecondaryBackgroundHex = "#D3B98F",
                ForegroundHex = "#27323A",
                AccentHex = "#8D5A2B",
                OverlayHex = "#C7FFFFFF",
                ContentAlignment = ContentAlignmentMode.Left,
                FooterText = "Weekly Service"
            },
            new ThemePreset
            {
                Name = "Harvest Night",
                Description = "A darker cinematic preset for evening worship and custom songs.",
                FontFamilyName = "Trebuchet MS",
                TitleFontSize = 34,
                BodyFontSize = 29,
                BackgroundHex = "#140D14",
                SecondaryBackgroundHex = "#41253D",
                ForegroundHex = "#FFF3E5",
                AccentHex = "#F0A85D",
                OverlayHex = "#B5141118",
                ContentAlignment = ContentAlignmentMode.Center,
                FooterText = "Praise & Worship"
            }
        };
    }
}
