using NativeHymnsApp.Infrastructure;
using NativeHymnsApp.Models;

namespace NativeHymnsApp.Services;

public sealed class PresentationSession : ObservableObject
{
    private SongDocument? _activeDeck;
    private int _currentSlideIndex;
    private PresentationOverlayMode _overlayMode;

    public SongDocument? ActiveDeck => _activeDeck;

    public SlideSection? CurrentSlide => _activeDeck is not null && _activeDeck.Slides.Count > 0
        ? _activeDeck.Slides[Math.Clamp(_currentSlideIndex, 0, _activeDeck.Slides.Count - 1)]
        : null;

    public SlideSection? UpcomingSlide => _activeDeck is not null && _currentSlideIndex < _activeDeck.Slides.Count - 1
        ? _activeDeck.Slides[_currentSlideIndex + 1]
        : null;

    public int CurrentSlideIndex => _currentSlideIndex;

    public PresentationOverlayMode OverlayMode => _overlayMode;

    public bool HasActiveDeck => _activeDeck is not null;

    public bool CanMoveNext => _activeDeck is not null && _currentSlideIndex < _activeDeck.Slides.Count - 1;

    public bool CanMovePrevious => _activeDeck is not null && _currentSlideIndex > 0;

    public bool IsBlankScreen => _overlayMode == PresentationOverlayMode.Blank;

    public bool IsBlackScreen => _overlayMode == PresentationOverlayMode.Black;

    public string SlideCounterText => _activeDeck is null ? "0/0" : $"{_currentSlideIndex + 1}/{_activeDeck.Slides.Count}";

    public void PresentSong(SongDocument deck)
    {
        _activeDeck = deck.DeepClone();
        _currentSlideIndex = 0;
        _overlayMode = PresentationOverlayMode.None;
        RaiseDeckChanged();
    }

    public void Next()
    {
        if (!CanMoveNext)
        {
            return;
        }

        _currentSlideIndex++;
        RaiseDeckChanged();
    }

    public void Previous()
    {
        if (!CanMovePrevious)
        {
            return;
        }

        _currentSlideIndex--;
        RaiseDeckChanged();
    }

    public void GoToSlide(int index)
    {
        if (_activeDeck is null || _activeDeck.Slides.Count == 0)
        {
            return;
        }

        var targetIndex = Math.Clamp(index, 0, _activeDeck.Slides.Count - 1);
        if (targetIndex == _currentSlideIndex)
        {
            return;
        }

        _currentSlideIndex = targetIndex;
        RaiseDeckChanged();
    }

    public void ToggleBlank()
    {
        _overlayMode = _overlayMode == PresentationOverlayMode.Blank
            ? PresentationOverlayMode.None
            : PresentationOverlayMode.Blank;
        RaiseDeckChanged();
    }

    public void ToggleBlack()
    {
        _overlayMode = _overlayMode == PresentationOverlayMode.Black
            ? PresentationOverlayMode.None
            : PresentationOverlayMode.Black;
        RaiseDeckChanged();
    }

    public void ClearOverlay()
    {
        if (_overlayMode == PresentationOverlayMode.None)
        {
            return;
        }

        _overlayMode = PresentationOverlayMode.None;
        RaiseDeckChanged();
    }

    private void RaiseDeckChanged()
    {
        OnPropertyChanged(
            nameof(ActiveDeck),
            nameof(CurrentSlide),
            nameof(UpcomingSlide),
            nameof(CurrentSlideIndex),
            nameof(OverlayMode),
            nameof(HasActiveDeck),
            nameof(CanMoveNext),
            nameof(CanMovePrevious),
            nameof(IsBlankScreen),
            nameof(IsBlackScreen),
            nameof(SlideCounterText));
    }
}
