using System.Text.Json.Serialization;

namespace NativeHymnsApp.Models;

public enum SongKind
{
    Hymn,
    CustomSong,
    TextSlide
}

public enum LibraryFilterMode
{
    All,
    Hymns,
    CustomSongs
}

public enum PresentationOverlayMode
{
    None,
    Blank,
    Black
}

public sealed class SlideSection
{
    public int Order { get; set; }

    public string Heading { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

public sealed class SongDocument
{
    public string Id { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SongKind Kind { get; set; }

    public int? HymnNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? SourceFile { get; set; }

    public List<SlideSection> Slides { get; set; } = new();

    [JsonIgnore]
    public string DisplayTitle => HymnNumber.HasValue ? $"{HymnNumber}. {Title}" : Title;

    [JsonIgnore]
    public string DisplaySubtitle => Kind switch
    {
        SongKind.Hymn => HymnNumber.HasValue ? $"Khasi Presbyterian Hymn {HymnNumber}" : "Khasi Presbyterian Hymn",
        SongKind.CustomSong => "Custom Song Library",
        SongKind.TextSlide => "Program Slide",
        _ => string.Empty
    };

    [JsonIgnore]
    public string KindLabel => Kind switch
    {
        SongKind.Hymn => "Hymn",
        SongKind.CustomSong => "Custom Song",
        SongKind.TextSlide => "Text Slide",
        _ => string.Empty
    };

    [JsonIgnore]
    public string SlideCountLabel => $"{Slides.Count} slide{(Slides.Count == 1 ? string.Empty : "s")}";

    public SongDocument DeepClone()
    {
        return new SongDocument
        {
            Id = Id,
            Kind = Kind,
            HymnNumber = HymnNumber,
            Title = Title,
            SourceFile = SourceFile,
            Slides = Slides
                .OrderBy(section => section.Order)
                .Select(section => new SlideSection
                {
                    Order = section.Order,
                    Heading = section.Heading,
                    Text = section.Text
                })
                .ToList()
        };
    }
}

public sealed class ServicePlanEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SongDocument Deck { get; set; } = new();

    [JsonIgnore]
    public string DisplayTitle => Deck.DisplayTitle;

    [JsonIgnore]
    public string KindLabel => Deck.KindLabel;

    [JsonIgnore]
    public string SlideCountLabel => Deck.SlideCountLabel;

    public ServicePlanEntry DeepClone()
    {
        return new ServicePlanEntry
        {
            Id = Id,
            Deck = Deck.DeepClone()
        };
    }
}

public sealed class AppStateSnapshot
{
    public List<SongDocument> CustomSongs { get; set; } = new();

    public List<ServicePlanEntry> ServicePlan { get; set; } = new();

    public ThemeSettingsSnapshot Theme { get; set; } = ThemeSettingsSnapshot.CreateDefault();
}

public sealed class StructuredHymnFile
{
    public string? SourceFile { get; set; }

    public int HymnCount { get; set; }

    public List<StructuredHymn> Hymns { get; set; } = new();
}

public sealed class StructuredHymn
{
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<StructuredVerse> Verses { get; set; } = new();
}

public sealed class StructuredVerse
{
    public int Number { get; set; }

    public string Text { get; set; } = string.Empty;
}

public sealed class SongCatalogLoadResult
{
    public string DataDirectory { get; set; } = string.Empty;

    public List<string> Files { get; set; } = new();

    public List<SongDocument> Songs { get; set; } = new();
}
