using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media;
using NativeHymnsApp.Infrastructure;
using NativeHymnsApp.Models;
using NativeHymnsApp.Services;

namespace NativeHymnsApp.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly SongCatalogService _catalogService;
    private readonly AppStateService _stateService;
    private readonly List<SongDocument> _hymns = new();
    private readonly List<SongDocument> _customSongs = new();

    private SongCatalogLoadResult? _lastLoadResult;
    private SongDocument? _selectedLibrarySong;
    private ServicePlanEntry? _selectedServicePlanEntry;
    private SongDocument? _previewSong;
    private string _searchText = string.Empty;
    private string _hymnNumberLookupText = string.Empty;
    private LibraryFilterMode _selectedLibraryFilter = LibraryFilterMode.All;
    private SongDocument? _quickSelectHymn;
    private SlideSection? _selectedActiveSlide;
    private bool _isFuzzySearchEnabled = true;
    private bool _isSyncingActiveSlideSelection;
    private bool _isLoaded;

    public MainWindowViewModel(SongCatalogService catalogService, AppStateService stateService)
    {
        _catalogService = catalogService;
        _stateService = stateService;

        LibraryItems = new ObservableCollection<SongDocument>();
        ServicePlanItems = new ObservableCollection<ServicePlanEntry>();
        Theme = new ThemeSettings();
        Presentation = new PresentationSession();
        PresenterDisplay = new PresenterViewModel(Presentation, Theme);
        ThemePresets = ThemePresetCatalog.GetPresets();
        FontFamilyOptions = Fonts.SystemFontFamilies.Select(font => font.Source).OrderBy(name => name).ToList();
        LibraryFilters = Enum.GetValues<LibraryFilterMode>();
        AlignmentOptions = Enum.GetValues<ContentAlignmentMode>();

        AddSelectedToPlanCommand = new RelayCommand(AddSelectedSongToPlan, () => SelectedLibrarySong is not null);
        PresentSelectedSongCommand = new RelayCommand(PresentSelectedSong, () => SelectedLibrarySong is not null);
        PresentSelectedPlanEntryCommand = new RelayCommand(PresentSelectedPlanEntry, () => SelectedServicePlanEntry is not null);
        MovePlanItemUpCommand = new RelayCommand(MoveSelectedPlanItemUp, CanMoveSelectedPlanItemUp);
        MovePlanItemDownCommand = new RelayCommand(MoveSelectedPlanItemDown, CanMoveSelectedPlanItemDown);
        RemovePlanItemCommand = new RelayCommand(RemoveSelectedPlanItem, () => SelectedServicePlanEntry is not null);
        ClearPlanCommand = new RelayCommand(ClearPlan, () => ServicePlanItems.Count > 0);
        ReloadCatalogCommand = new RelayCommand(ReloadCatalog);
        ShowSlideshowCommand = new RelayCommand(ShowSlideshow);
        SelectHymnByNumberCommand = new RelayCommand(SelectHymnByNumber, () => QuickSelectHymn is not null || LibraryItems.Count > 0);
        QueueQuickHymnCommand = new RelayCommand(QueueQuickHymn, () => QuickSelectHymn is not null);
        PresentQuickHymnCommand = new RelayCommand(PresentQuickHymn, () => QuickSelectHymn is not null);
        QueueLibrarySongCommand = new RelayCommand<SongDocument>(QueueLibrarySong, song => song is not null);
        PresentLibrarySongCommand = new RelayCommand<SongDocument>(PresentLibrarySong, song => song is not null);
        PresentServicePlanItemCommand = new RelayCommand<ServicePlanEntry>(PresentServicePlanItem, entry => entry is not null);
        RemoveServicePlanItemCommand = new RelayCommand<ServicePlanEntry>(RemoveServicePlanItem, entry => entry is not null);
        JumpToSlideCommand = new RelayCommand<SlideSection>(JumpToSlide, slide => slide is not null && Presentation.HasActiveDeck);
        PreviousSlideCommand = new RelayCommand(MoveToPreviousSlide, () => Presentation.CanMovePrevious);
        NextSlideCommand = new RelayCommand(MoveToNextSlide, () => Presentation.CanMoveNext);
        BlankScreenCommand = new RelayCommand(() => Presentation.ToggleBlank(), () => Presentation.HasActiveDeck);
        BlackScreenCommand = new RelayCommand(() => Presentation.ToggleBlack(), () => Presentation.HasActiveDeck);
        ClearOverlayCommand = new RelayCommand(() => Presentation.ClearOverlay(), () => Presentation.HasActiveDeck);
        ResetThemeCommand = new RelayCommand(() => Theme.ResetToDefault());
        ApplyThemePresetCommand = new RelayCommand<ThemePreset>(preset =>
        {
            if (preset is not null)
            {
                Theme.ApplyPreset(preset);
            }
        });

        ServicePlanItems.CollectionChanged += HandleServicePlanCollectionChanged;
        Theme.PropertyChanged += (_, _) => PersistStateIfReady();
        Presentation.PropertyChanged += (_, _) => HandlePresentationChanged();

        LoadStateAndCatalog();
    }

    public ObservableCollection<SongDocument> LibraryItems { get; }

    public ObservableCollection<ServicePlanEntry> ServicePlanItems { get; }

    public ThemeSettings Theme { get; }

    public PresentationSession Presentation { get; }

    public PresenterViewModel PresenterDisplay { get; }

    public IReadOnlyList<ThemePreset> ThemePresets { get; }

    public IReadOnlyList<string> FontFamilyOptions { get; }

    public IReadOnlyList<LibraryFilterMode> LibraryFilters { get; }

    public IReadOnlyList<ContentAlignmentMode> AlignmentOptions { get; }

    public RelayCommand AddSelectedToPlanCommand { get; }

    public RelayCommand PresentSelectedSongCommand { get; }

    public RelayCommand PresentSelectedPlanEntryCommand { get; }

    public RelayCommand MovePlanItemUpCommand { get; }

    public RelayCommand MovePlanItemDownCommand { get; }

    public RelayCommand RemovePlanItemCommand { get; }

    public RelayCommand ClearPlanCommand { get; }

    public RelayCommand ReloadCatalogCommand { get; }

    public RelayCommand ShowSlideshowCommand { get; }

    public RelayCommand SelectHymnByNumberCommand { get; }

    public RelayCommand QueueQuickHymnCommand { get; }

    public RelayCommand PresentQuickHymnCommand { get; }

    public RelayCommand<SongDocument> QueueLibrarySongCommand { get; }

    public RelayCommand<SongDocument> PresentLibrarySongCommand { get; }

    public RelayCommand<ServicePlanEntry> PresentServicePlanItemCommand { get; }

    public RelayCommand<ServicePlanEntry> RemoveServicePlanItemCommand { get; }

    public RelayCommand<SlideSection> JumpToSlideCommand { get; }

    public RelayCommand PreviousSlideCommand { get; }

    public RelayCommand NextSlideCommand { get; }

    public RelayCommand BlankScreenCommand { get; }

    public RelayCommand BlackScreenCommand { get; }

    public RelayCommand ClearOverlayCommand { get; }

    public RelayCommand ResetThemeCommand { get; }

    public RelayCommand<ThemePreset> ApplyThemePresetCommand { get; }

    public event EventHandler? OpenSlideshowRequested;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyLibraryFilter();
            }
        }
    }

    public string HymnNumberLookupText
    {
        get => _hymnNumberLookupText;
        set
        {
            if (SetProperty(ref _hymnNumberLookupText, value))
            {
                UpdateQuickSelectHymn();
                ApplyLibraryFilter();
            }
        }
    }

    public bool IsFuzzySearchEnabled
    {
        get => _isFuzzySearchEnabled;
        set
        {
            if (SetProperty(ref _isFuzzySearchEnabled, value))
            {
                ApplyLibraryFilter();
            }
        }
    }

    public LibraryFilterMode SelectedLibraryFilter
    {
        get => _selectedLibraryFilter;
        set
        {
            if (SetProperty(ref _selectedLibraryFilter, value))
            {
                ApplyLibraryFilter();
            }
        }
    }

    public SongDocument? QuickSelectHymn
    {
        get => _quickSelectHymn;
        private set
        {
            if (SetProperty(ref _quickSelectHymn, value))
            {
                OnPropertyChanged(nameof(QuickSelectTitle), nameof(QuickSelectSubtitle), nameof(HasQuickSelectHymn));
                RefreshCommandStates();
            }
        }
    }

    public SlideSection? SelectedActiveSlide
    {
        get => _selectedActiveSlide;
        set
        {
            if (SetProperty(ref _selectedActiveSlide, value) && !_isSyncingActiveSlideSelection && value is not null)
            {
                JumpToSlide(value);
            }
        }
    }

    public SongDocument? SelectedLibrarySong
    {
        get => _selectedLibrarySong;
        set
        {
            if (SetProperty(ref _selectedLibrarySong, value))
            {
                if (value is not null)
                {
                    PreviewSong = value;
                }

                RefreshCommandStates();
                OnPropertyChanged(nameof(CanEditSelectedCustomSong));
            }
        }
    }

    public ServicePlanEntry? SelectedServicePlanEntry
    {
        get => _selectedServicePlanEntry;
        set
        {
            if (SetProperty(ref _selectedServicePlanEntry, value))
            {
                if (value is not null)
                {
                    PreviewSong = value.Deck;
                }

                RefreshCommandStates();
                OnPropertyChanged(nameof(SelectedPlanTitle), nameof(SelectedPlanSubtitle));
            }
        }
    }

    public SongDocument? PreviewSong
    {
        get => _previewSong;
        private set
        {
            if (SetProperty(ref _previewSong, value))
            {
                OnPropertyChanged(nameof(PreviewTitle), nameof(PreviewSubtitle), nameof(PreviewSlides));
            }
        }
    }

    public string PreviewTitle => PreviewSong?.DisplayTitle ?? "Select a hymn, custom song, or service item";

    public string PreviewSubtitle => PreviewSong?.DisplaySubtitle ?? "Preview slides before sending them to the presenter display.";

    public IEnumerable<SlideSection> PreviewSlides => PreviewSong?.Slides ?? Enumerable.Empty<SlideSection>();

    public string LibrarySummary => $"{LibraryItems.Count} shown - {_hymns.Count} hymns - {_customSongs.Count} custom songs";

    public string ServicePlanSummary => $"{ServicePlanItems.Count} items queued";

    public string DataSourceSummary
    {
        get
        {
            if (_lastLoadResult is null)
            {
                return "No hymn data loaded";
            }

            return $"{_lastLoadResult.Songs.Count} songs loaded from {string.Join(", ", _lastLoadResult.Files)}";
        }
    }

    public string CurrentPresentationSummary => Presentation.ActiveDeck is null
        ? "Slideshow idle"
        : $"Projecting {Presentation.ActiveDeck.DisplayTitle} - {Presentation.SlideCounterText}";

    public string StorageSummary => $"State saved to {_stateService.StateFilePath}";

    public bool CanEditSelectedCustomSong => SelectedLibrarySong?.Kind == SongKind.CustomSong;

    public bool HasQuickSelectHymn => QuickSelectHymn is not null;

    public string QuickSelectTitle => QuickSelectHymn?.DisplayTitle ?? "Jump To A Hymn Number";

    public string QuickSelectSubtitle => QuickSelectHymn is null
        ? "Type a hymn number to narrow the list. Press Enter to select the closest result."
        : $"{QuickSelectHymn.SlideCountLabel} - ready to queue or present.";

    public string LiveDeckTitle => Presentation.ActiveDeck?.DisplayTitle ?? "Slideshow idle";

    public string LiveDeckSubtitle => Presentation.ActiveDeck?.DisplaySubtitle ?? "Send a service item or library selection to the presenter.";

    public IEnumerable<SlideSection> ActivePresentationSlides => Presentation.ActiveDeck?.Slides ?? Enumerable.Empty<SlideSection>();

    public string CurrentSlideHeading => Presentation.CurrentSlide?.Heading ?? "Current Slide";

    public string CurrentSlideText => Presentation.CurrentSlide?.Text ?? "Present a hymn or service item to start the live output.";

    public string NextSlideHeading => Presentation.UpcomingSlide?.Heading ?? "No Upcoming Slide";

    public string NextSlideText => Presentation.UpcomingSlide?.Text ?? "You are at the end of the current item.";

    public string SelectedPlanTitle => SelectedServicePlanEntry?.DisplayTitle ?? "No service item selected";

    public string SelectedPlanSubtitle => SelectedServicePlanEntry is null
        ? "Choose a queued item to keep the live workflow moving."
        : $"{SelectedServicePlanEntry.KindLabel} - {SelectedServicePlanEntry.SlideCountLabel}";

    private void HandleServicePlanCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ServicePlanSummary));
        RefreshCommandStates();
    }

    private void HandlePresentationChanged()
    {
        SyncSelectedActiveSlide();
        PreviousSlideCommand.RaiseCanExecuteChanged();
        NextSlideCommand.RaiseCanExecuteChanged();
        BlankScreenCommand.RaiseCanExecuteChanged();
        BlackScreenCommand.RaiseCanExecuteChanged();
        ClearOverlayCommand.RaiseCanExecuteChanged();
        JumpToSlideCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(
            nameof(CurrentPresentationSummary),
            nameof(LiveDeckTitle),
            nameof(LiveDeckSubtitle),
            nameof(ActivePresentationSlides),
            nameof(SelectedActiveSlide),
            nameof(CurrentSlideHeading),
            nameof(CurrentSlideText),
            nameof(NextSlideHeading),
            nameof(NextSlideText));
    }

    private void RefreshCommandStates()
    {
        AddSelectedToPlanCommand.RaiseCanExecuteChanged();
        PresentSelectedSongCommand.RaiseCanExecuteChanged();
        PresentSelectedPlanEntryCommand.RaiseCanExecuteChanged();
        SelectHymnByNumberCommand.RaiseCanExecuteChanged();
        QueueQuickHymnCommand.RaiseCanExecuteChanged();
        PresentQuickHymnCommand.RaiseCanExecuteChanged();
        JumpToSlideCommand.RaiseCanExecuteChanged();
        MovePlanItemUpCommand.RaiseCanExecuteChanged();
        MovePlanItemDownCommand.RaiseCanExecuteChanged();
        RemovePlanItemCommand.RaiseCanExecuteChanged();
        ClearPlanCommand.RaiseCanExecuteChanged();
    }

    private void ShowSlideshow()
    {
        OpenSlideshowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MoveToPreviousSlide()
    {
        Presentation.Previous();
    }

    private void MoveToNextSlide()
    {
        Presentation.Next();
    }

    private void SyncSelectedActiveSlide()
    {
        _isSyncingActiveSlideSelection = true;
        SelectedActiveSlide = Presentation.CurrentSlide;
        _isSyncingActiveSlideSelection = false;
    }

    public string RunSmokeTest()
    {
        return string.Join(
            Environment.NewLine,
            $"Songs={_hymns.Count}",
            $"CustomSongs={_customSongs.Count}",
            $"ServicePlanItems={ServicePlanItems.Count}",
            $"ThemeFont={Theme.FontFamilyName}",
            $"DataFiles={_lastLoadResult?.Files.Count ?? 0}",
            $"StateFile={_stateService.StateFilePath}");
    }
}

