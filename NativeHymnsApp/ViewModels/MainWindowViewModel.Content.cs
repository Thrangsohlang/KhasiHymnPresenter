using System.Text.RegularExpressions;
using NativeHymnsApp.Models;

namespace NativeHymnsApp.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly record struct LibraryMatch(SongDocument Song, int Priority);

    private static readonly Regex SectionHeadingRegex = new(
        @"^(Verse|Chorus|Bridge|Refrain|Intro|Outro|Ending|Slide)\s*([0-9IVX-]*)\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumberedLineRegex = new(
        @"^(\d+)[\.\)]\s*(.+)$",
        RegexOptions.Compiled);

    public void AddOrUpdateCustomSong(SongDocument song)
    {
        var existingIndex = _customSongs.FindIndex(item => string.Equals(item.Id, song.Id, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            _customSongs[existingIndex] = song;
        }
        else
        {
            _customSongs.Add(song);
        }

        ApplyLibraryFilter(song.Id);
        PersistStateIfReady();
    }

    public SongDocument BuildCustomSong(string title, string rawContent, string? existingId = null)
    {
        return new SongDocument
        {
            Id = existingId ?? $"custom-{Guid.NewGuid():N}",
            Kind = SongKind.CustomSong,
            Title = RequireValue(title, "A custom song needs a title."),
            Slides = ParseSlides(rawContent, SongKind.CustomSong)
        };
    }

    public ServicePlanEntry BuildTextSlideEntry(string title, string rawContent)
    {
        return new ServicePlanEntry
        {
            Deck = new SongDocument
            {
                Id = $"text-{Guid.NewGuid():N}",
                Kind = SongKind.TextSlide,
                Title = RequireValue(title, "A program slide needs a title."),
                Slides = ParseSlides(rawContent, SongKind.TextSlide)
            }
        };
    }

    public void AddTextSlideToPlan(ServicePlanEntry entry)
    {
        ServicePlanItems.Add(entry);
        SelectedServicePlanEntry = entry;
        PersistStateIfReady();
    }

    public void DeleteSelectedCustomSong()
    {
        if (SelectedLibrarySong is not { Kind: SongKind.CustomSong } song)
        {
            return;
        }

        _customSongs.RemoveAll(item => string.Equals(item.Id, song.Id, StringComparison.OrdinalIgnoreCase));
        ApplyLibraryFilter();
        PersistStateIfReady();
    }

    public string FormatSongForEditing(SongDocument song)
    {
        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            song.Slides.Select(slide =>
            {
                if (slide.Heading.StartsWith("Slide ", StringComparison.OrdinalIgnoreCase))
                {
                    return slide.Text;
                }

                return $"{slide.Heading}{Environment.NewLine}{slide.Text}";
            }));
    }

    public void SetBackgroundImagePath(string path)
    {
        Theme.BackgroundImagePath = path;
    }

    public void ClearBackgroundImagePath()
    {
        Theme.BackgroundImagePath = string.Empty;
    }

    private void LoadStateAndCatalog()
    {
        _lastLoadResult = _catalogService.LoadCatalog();
        _hymns.Clear();
        _hymns.AddRange(_lastLoadResult.Songs.Where(song => song.Kind == SongKind.Hymn));

        var state = _stateService.Load();
        Theme.ApplySnapshot(state.Theme);

        _customSongs.Clear();
        _customSongs.AddRange(
            state.CustomSongs
                .Where(song => song.Kind == SongKind.CustomSong)
                .Select(song => song.DeepClone()));

        ServicePlanItems.Clear();
        foreach (var entry in state.ServicePlan.Select(plan => plan.DeepClone()))
        {
            ServicePlanItems.Add(entry);
        }

        _isLoaded = true;
        UpdateQuickSelectHymn();
        ApplyLibraryFilter();

        if (LibraryItems.Count > 0)
        {
            SelectedLibrarySong = LibraryItems[0];
        }
        else if (ServicePlanItems.Count > 0)
        {
            SelectedServicePlanEntry = ServicePlanItems[0];
        }

        OnPropertyChanged(nameof(DataSourceSummary), nameof(StorageSummary), nameof(ServicePlanSummary));
        RefreshCommandStates();
    }

    private void ReloadCatalog()
    {
        _lastLoadResult = _catalogService.LoadCatalog();
        _hymns.Clear();
        _hymns.AddRange(_lastLoadResult.Songs.Where(song => song.Kind == SongKind.Hymn));
        UpdateQuickSelectHymn();
        ApplyLibraryFilter(SelectedLibrarySong?.Id ?? QuickSelectHymn?.Id);
        OnPropertyChanged(nameof(DataSourceSummary), nameof(LibrarySummary));
    }

    private void ApplyLibraryFilter(string? preferredSelectionId = null)
    {
        IEnumerable<SongDocument> source = SelectedLibraryFilter switch
        {
            LibraryFilterMode.Hymns => _hymns,
            LibraryFilterMode.CustomSongs => _customSongs,
            _ => _hymns.Concat(_customSongs)
        };

        var hymnNumberLookup = GetHymnNumberLookup();
        if (!string.IsNullOrWhiteSpace(hymnNumberLookup))
        {
            source = source.Where(song => MatchesHymnNumberLookup(song, hymnNumberLookup));
        }

        var query = SearchText.Trim();
        var hasSearchQuery = !string.IsNullOrWhiteSpace(query);
        var filtered = source
            .Select(song => new LibraryMatch(song, hasSearchQuery ? GetSearchPriority(song, query, IsFuzzySearchEnabled) : 0))
            .Where(match => !hasSearchQuery || match.Priority < int.MaxValue)
            .OrderBy(match => GetHymnNumberLookupPriority(match.Song, hymnNumberLookup))
            .ThenBy(match => match.Priority)
            .ThenBy(match => match.Song.Kind == SongKind.Hymn ? 0 : 1)
            .ThenBy(match => match.Song.HymnNumber ?? int.MaxValue)
            .ThenBy(match => match.Song.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(match => match.Song)
            .ToList();

        LibraryItems.Clear();
        foreach (var song in filtered)
        {
            LibraryItems.Add(song);
        }

        if (filtered.Count == 0)
        {
            SelectedLibrarySong = null;
            OnPropertyChanged(nameof(LibrarySummary));
            return;
        }

        var selectionId = preferredSelectionId ?? SelectedLibrarySong?.Id;
        SelectedLibrarySong = filtered.FirstOrDefault(song => string.Equals(song.Id, selectionId, StringComparison.OrdinalIgnoreCase))
            ?? filtered[0];

        OnPropertyChanged(nameof(LibrarySummary));
    }

    private void SelectHymnByNumber()
    {
        if (SelectedLibraryFilter == LibraryFilterMode.CustomSongs)
        {
            SelectedLibraryFilter = LibraryFilterMode.Hymns;
        }

        var song = QuickSelectHymn ?? LibraryItems.FirstOrDefault();
        if (song is null)
        {
            return;
        }

        ApplyLibraryFilter(song.Id);
    }

    private void QueueQuickHymn()
    {
        if (QuickSelectHymn is null)
        {
            return;
        }

        SelectHymnByNumber();
        AddDeckToPlan(QuickSelectHymn);
    }

    private void PresentQuickHymn()
    {
        if (QuickSelectHymn is null)
        {
            return;
        }

        SelectHymnByNumber();
        PresentDeck(QuickSelectHymn);
    }

    private void AddSelectedSongToPlan()
    {
        if (SelectedLibrarySong is null)
        {
            return;
        }

        AddDeckToPlan(SelectedLibrarySong);
    }

    private void PresentSelectedSong()
    {
        if (SelectedLibrarySong is null)
        {
            return;
        }

        PresentDeck(SelectedLibrarySong);
    }

    private void PresentSelectedPlanEntry()
    {
        if (SelectedServicePlanEntry is null)
        {
            return;
        }

        PresentDeck(SelectedServicePlanEntry.Deck);
    }

    private void MoveSelectedPlanItemUp()
    {
        if (SelectedServicePlanEntry is null)
        {
            return;
        }

        var index = ServicePlanItems.IndexOf(SelectedServicePlanEntry);
        if (index <= 0)
        {
            return;
        }

        ServicePlanItems.Move(index, index - 1);
        PersistStateIfReady();
    }

    private bool CanMoveSelectedPlanItemUp()
    {
        return SelectedServicePlanEntry is not null && ServicePlanItems.IndexOf(SelectedServicePlanEntry) > 0;
    }

    private void MoveSelectedPlanItemDown()
    {
        if (SelectedServicePlanEntry is null)
        {
            return;
        }

        var index = ServicePlanItems.IndexOf(SelectedServicePlanEntry);
        if (index < 0 || index >= ServicePlanItems.Count - 1)
        {
            return;
        }

        ServicePlanItems.Move(index, index + 1);
        PersistStateIfReady();
    }

    private bool CanMoveSelectedPlanItemDown()
    {
        return SelectedServicePlanEntry is not null
               && ServicePlanItems.IndexOf(SelectedServicePlanEntry) >= 0
               && ServicePlanItems.IndexOf(SelectedServicePlanEntry) < ServicePlanItems.Count - 1;
    }

    private void RemoveSelectedPlanItem()
    {
        if (SelectedServicePlanEntry is null)
        {
            return;
        }

        var index = ServicePlanItems.IndexOf(SelectedServicePlanEntry);
        ServicePlanItems.Remove(SelectedServicePlanEntry);
        SelectedServicePlanEntry = ServicePlanItems.ElementAtOrDefault(Math.Min(index, ServicePlanItems.Count - 1));
        PersistStateIfReady();
    }

    private void ClearPlan()
    {
        ServicePlanItems.Clear();
        SelectedServicePlanEntry = null;
        PersistStateIfReady();
    }

    private void AddDeckToPlan(SongDocument deck)
    {
        var entry = new ServicePlanEntry
        {
            Deck = deck.DeepClone()
        };

        ServicePlanItems.Add(entry);
        SelectedServicePlanEntry = entry;
        PersistStateIfReady();
    }

    private void PresentDeck(SongDocument deck)
    {
        Presentation.PresentSong(deck);
        ShowPresenter();
    }

    private void UpdateQuickSelectHymn()
    {
        var lookup = GetHymnNumberLookup();
        if (string.IsNullOrWhiteSpace(lookup) || !int.TryParse(lookup, out var hymnNumber))
        {
            QuickSelectHymn = null;
            return;
        }

        QuickSelectHymn = _hymns.FirstOrDefault(song => song.HymnNumber == hymnNumber);
    }

    private string GetHymnNumberLookup()
    {
        return new string(HymnNumberLookupText.Where(char.IsDigit).ToArray());
    }

    private static bool MatchesHymnNumberLookup(SongDocument song, string hymnNumberLookup)
    {
        return song.HymnNumber.HasValue
               && song.HymnNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                   .StartsWith(hymnNumberLookup, StringComparison.Ordinal);
    }

    private static int GetHymnNumberLookupPriority(SongDocument song, string hymnNumberLookup)
    {
        if (string.IsNullOrWhiteSpace(hymnNumberLookup) || !song.HymnNumber.HasValue)
        {
            return 0;
        }

        var hymnNumber = song.HymnNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return hymnNumber == hymnNumberLookup ? 0 : 1;
    }

    private static int GetSearchPriority(SongDocument song, string query, bool fuzzySearchEnabled)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return 0;
        }

        if (song.HymnNumber.HasValue)
        {
            var hymnNumber = song.HymnNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (string.Equals(hymnNumber, query, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (hymnNumber.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
        }

        if (string.Equals(song.Title, query, StringComparison.CurrentCultureIgnoreCase)
            || string.Equals(song.DisplayTitle, query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 2;
        }

        if (song.Title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 3;
        }

        if (song.DisplayTitle.Contains(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 4;
        }

        if (song.Slides.Any(slide => slide.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
        {
            return 5;
        }

        return fuzzySearchEnabled && MatchesFuzzySearch(song, query) ? 6 : int.MaxValue;
    }

    private static bool MatchesFuzzySearch(SongDocument song, string query)
    {
        var normalizedQuery = NormalizeSearchValue(query);
        if (normalizedQuery.Length < 3)
        {
            return false;
        }

        return MatchesFuzzyCandidate(song.Title, normalizedQuery)
               || MatchesFuzzyCandidate(song.DisplayTitle, normalizedQuery);
    }

    private static bool MatchesFuzzyCandidate(string candidate, string normalizedQuery)
    {
        var normalizedCandidate = NormalizeSearchValue(candidate);
        if (normalizedCandidate.Length == 0)
        {
            return false;
        }

        if (normalizedCandidate.Contains(normalizedQuery, StringComparison.Ordinal)
            || IsSubsequenceMatch(normalizedQuery, normalizedCandidate))
        {
            return true;
        }

        return candidate
            .Split(new[] { ' ', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeSearchValue)
            .Any(token => token.Length > 0
                          && GetLevenshteinDistance(token, normalizedQuery) <= GetAllowedFuzzyDistance(normalizedQuery.Length));
    }

    private static string NormalizeSearchValue(string value)
    {
        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static bool IsSubsequenceMatch(string query, string candidate)
    {
        var queryIndex = 0;
        foreach (var character in candidate)
        {
            if (queryIndex < query.Length && character == query[queryIndex])
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }

    private static int GetLevenshteinDistance(string source, string target)
    {
        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

        var costs = new int[target.Length + 1];
        for (var index = 0; index <= target.Length; index++)
        {
            costs[index] = index;
        }

        for (var sourceIndex = 1; sourceIndex <= source.Length; sourceIndex++)
        {
            var previousDiagonal = costs[0];
            costs[0] = sourceIndex;

            for (var targetIndex = 1; targetIndex <= target.Length; targetIndex++)
            {
                var previousValue = costs[targetIndex];
                var substitutionCost = source[sourceIndex - 1] == target[targetIndex - 1] ? 0 : 1;
                costs[targetIndex] = Math.Min(
                    Math.Min(costs[targetIndex] + 1, costs[targetIndex - 1] + 1),
                    previousDiagonal + substitutionCost);
                previousDiagonal = previousValue;
            }
        }

        return costs[target.Length];
    }

    private static int GetAllowedFuzzyDistance(int queryLength)
    {
        return queryLength >= 7 ? 2 : 1;
    }

    private void PersistStateIfReady()
    {
        if (!_isLoaded)
        {
            return;
        }

        _stateService.Save(new AppStateSnapshot
        {
            Theme = Theme.ToSnapshot(),
            CustomSongs = _customSongs.Select(song => song.DeepClone()).ToList(),
            ServicePlan = ServicePlanItems.Select(item => item.DeepClone()).ToList()
        });

        OnPropertyChanged(nameof(StorageSummary));
    }

    private static List<SlideSection> ParseSlides(string rawContent, SongKind kind)
    {
        var content = RequireValue(rawContent, "Slide content cannot be empty.");
        var blocks = Regex.Split(content.Trim(), @"(?:\r?\n){2,}")
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        var slides = new List<SlideSection>();
        var order = 1;

        foreach (var block in blocks)
        {
            var lines = block
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count == 0)
            {
                continue;
            }

            var heading = GetDefaultHeading(kind, order);
            var bodyLines = new List<string>(lines);

            var sectionHeadingMatch = SectionHeadingRegex.Match(lines[0]);
            if (sectionHeadingMatch.Success)
            {
                heading = NormalizeSectionHeading(sectionHeadingMatch.Groups[1].Value, sectionHeadingMatch.Groups[2].Value);
                bodyLines = lines.Skip(1).ToList();
                if (bodyLines.Count == 0)
                {
                    bodyLines = new List<string> { lines[0] };
                }
            }
            else
            {
                var numberedLineMatch = NumberedLineRegex.Match(lines[0]);
                if (numberedLineMatch.Success)
                {
                    heading = $"Verse {numberedLineMatch.Groups[1].Value}";
                    bodyLines = new List<string> { numberedLineMatch.Groups[2].Value.Trim() };
                    bodyLines.AddRange(lines.Skip(1));
                }
            }

            slides.Add(new SlideSection
            {
                Order = order,
                Heading = heading,
                Text = string.Join(Environment.NewLine, bodyLines)
            });
            order++;
        }

        if (slides.Count == 0)
        {
            throw new InvalidOperationException("No valid slides were created from the provided content.");
        }

        return slides;
    }

    private static string GetDefaultHeading(SongKind kind, int order)
    {
        return kind switch
        {
            SongKind.TextSlide => $"Slide {order}",
            SongKind.CustomSong => $"Slide {order}",
            _ => $"Verse {order}"
        };
    }

    private static string NormalizeSectionHeading(string label, string suffix)
    {
        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        var heading = textInfo.ToTitleCase(label.ToLowerInvariant());
        return string.IsNullOrWhiteSpace(suffix) ? heading : $"{heading} {suffix.Trim()}";
    }

    private static string RequireValue(string value, string errorMessage)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(errorMessage)
            : value.Trim();
    }
}
