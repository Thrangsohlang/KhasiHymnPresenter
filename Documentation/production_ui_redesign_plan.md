# Khasi Hymns Presenter - Production UI Redesign Plan

## Objectives

- Make hymn lookup fast during live use, especially by hymn number.
- Separate planning tasks from live slideshow control.
- Reduce visual clutter by replacing repetitive text buttons with icon actions and hover tooltips.
- Keep projector output and operator controls clearly separated.
- Move secondary functionality out of the main workspace when it does not need to stay visible.

## Concrete Features

### 1. Planning Workspace

Purpose:

- build and reorder the service plan
- search and queue hymns quickly
- preview the currently selected library or plan item
- manage custom songs and theme settings through secondary windows

Core features:

- hymn number quick entry
- ranked library search with fuzzy title matching
- compact result list with per-row icon actions
- service plan queue with item actions
- selected-item preview panel
- top menu for file, library, slideshow, and appearance actions

### 2. Slideshow Control Window

Purpose:

- run the live service from a dedicated control surface
- show the operator what is currently on the projector
- allow direct slide selection and deck loading without returning to the planning workspace

Core features:

- projector preview panel on the right
- current deck information and transport controls on the left
- direct slide list with jump-to-slide actions
- service plan load list for switching to the next item quickly
- projector overlay controls: blank, black, clear

### 3. Presenter Window

Purpose:

- full-screen projector output only

Core features:

- no operator controls
- follows the active deck and slide index
- uses the selected presentation theme

### 4. Theme Editor Window

Purpose:

- keep appearance editing available without consuming main window space

Core features:

- preset selection
- font and color editing
- background image controls

## Window Strategy

### Main Window

- Left: service plan queue and queue controls
- Right top: quick hymn number entry, search, and library filters
- Right middle: large compact hymn results list
- Right bottom: preview of the currently selected deck
- Top bar: menu plus primary actions

### Slideshow Control Window

- Left top: live deck title and slide transport controls
- Left middle: active slide list
- Left bottom: service plan load list
- Right: large projector preview mirroring the presenter screen

### Theme Editor Window

- Single-column settings window
- modeless, so operators can tune presentation styling without losing the main workspace

## Interaction Plan

### Library Results

- Compact row layout instead of tall cards
- Actions shown as icons with tooltips:
  - queue hymn
  - present hymn
  - edit custom song
  - delete custom song
- selecting a row updates preview
- action icons do not require selecting the row first

### Slideshow Flow

1. Operator builds the service plan in the main window.
2. Operator opens slideshow control.
3. Slideshow control opens projector output on the second screen when available.
4. Operator runs the service from the slideshow window:
   - next / previous
   - jump directly to a slide
   - load another queued deck
5. Main window stays available for planning and library work.

## Wireframe

### Main Window

```text
+--------------------------------------------------------------------------------------+
| File  Library  Slideshow  View                                                      |
+--------------------------------------------------------------------------------------+
| Khasi Hymns Presenter                           [Open Slideshow] [Theme] [Reload]    |
+--------------------------------------+-----------------------------------------------+
| Service Plan                          | Hymn Lookup                                  |
| - queue list                          | [ 123 ] [search.........................]     |
| - move / present / remove             | [All v] [Fuzzy on]                          |
|                                      +-----------------------------------------------+
|                                      | Library Results                              |
|                                      | 123  U Blei          [queue] [play]          |
|                                      | 124  Jingrwai        [queue] [play]          |
|                                      | custom song          [queue] [play] [edit]   |
|                                      | ...                                           |
|                                      +-----------------------------------------------+
|                                      | Preview                                        |
|                                      | selected deck title / slide preview           |
+--------------------------------------+-----------------------------------------------+
```

### Slideshow Control Window

```text
+--------------------------------------------------------------------------------------+
| Live Deck Title                                         [Prev] [Next] [Blank] [Black]|
+--------------------------------------+-----------------------------------------------+
| Current Deck Slides                  | Projector Preview                            |
| 1 Verse 1                [go]        |                                               |
| 2 Chorus                 [go]        | mirrored presenter output                     |
| 3 Verse 2                [go]        |                                               |
|                                      |                                               |
+--------------------------------------+-----------------------------------------------+
| Service Plan Load List               | Footer / projector status                     |
| hymn 123                  [load]     |                                               |
| announcement              [load]     |                                               |
| benediction               [load]     |                                               |
+--------------------------------------+-----------------------------------------------+
```

## Color Scheme

The operator-facing UI will use a warm studio palette rather than a pure dark dashboard.

- Canvas: `#F5F0E6`
- Surface: `#FFFDFC`
- Surface Alt: `#E8DECF`
- Ink: `#1F2A30`
- Muted Ink: `#5F6D72`
- Accent: `#A6652B`
- Accent Soft: `#E6C39B`
- Border: `#CDBCA7`
- Highlight: `#2F5B6A`
- Danger: `#A54545`

Rationale:

- warm light backgrounds keep dense planning tasks readable
- darker preview surfaces keep projector content visually distinct
- brass and slate accents support a church-service presentation context without looking generic

## Implementation Notes

- Keep the existing MVVM structure and presentation session logic.
- Add parameterized commands for list-row actions instead of depending only on selection.
- Add direct slide navigation to the presentation session.
- Reuse the existing presenter theme settings in both projector output and slideshow preview.
