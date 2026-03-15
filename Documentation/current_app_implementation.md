# Khasi Hymns Presenter - Current Implementation Reference

## 1. Scope Of This Document

This document describes the app exactly as it is currently implemented in this repository.

It covers:

- the active project and what is actually used at runtime
- the windows, panels, controls, and current workflows
- the logic and data flow behind the UI
- the color scheme and theme behavior
- the current project structure
- the current limitations and assumptions

This document does not describe future plans unless they are already present in the code.

## 2. Active Project

The currently active desktop app is:

- `NativeHymnsApp`

The executable currently produced by the build is:

- `NativeHymnsApp/bin/Debug/net8.0-windows/NativeHymnsApp.exe`

Other folders at the repository root exist, but they are not the current implementation of the app:

- `WpfApp1` is an older starter project
- `HymnsDesktopApp` is a scaffold/test project and is not the active runtime app
- `NativeHymnsApp` is the real app that should be considered current

## 3. Technology Stack

The current app uses:

- Framework: `.NET 8`
- UI framework: `WPF`
- Architecture style: `lightweight MVVM`
- Storage for hymn source data: local JSON files copied into the build output
- Storage for user state: local JSON file in `%LocalAppData%\NativeHymnsApp\app-state.json`
- Deployment style currently in repo: local debug build output

There is no web application layer in the current implementation.

There is no database in the current implementation.

There are no external packages added for MVVM or persistence. The app uses its own small internal `ObservableObject` and `RelayCommand` helpers.

## 4. Startup And Runtime Behavior

### 4.1 Startup flow

Startup is handled in `NativeHymnsApp/App.xaml.cs`.

At startup the app:

1. Creates `SongCatalogService` pointing to `Data/Structured` under the app output folder.
2. Creates `AppStateService` for local persisted state.
3. Constructs `MainWindowViewModel`.
4. Loads hymn data and saved state.
5. Opens the operator window (`MainWindow`).

If startup fails:

- the app shows a blocking error message box
- the app then exits

### 4.2 Smoke test mode

If the app is launched with:

```text
--smoke-test
```

it does not open the UI normally. Instead it:

- loads the app data and state
- writes a `smoke-test.txt` summary beside the executable
- exits immediately

This is useful for checking whether the app can start and find data without manually opening the UI.

## 5. Windows And Screens

The current app has three user-facing windows:

- the main operator window
- the presenter window
- the content editor dialog

There are currently no custom icon buttons inside the app UI. The app uses text-labeled buttons instead of icon-only controls.

## 6. Main Operator Window

File:

- `NativeHymnsApp/MainWindow.xaml`
- `NativeHymnsApp/MainWindow.xaml.cs`

### 6.1 Window shell

The main operator window currently uses a standard Windows title bar.

Window behavior:

- title: `Khasi Hymns Presenter`
- startup size: `1360 x 820`
- minimum size: `1024 x 680`
- startup state: maximized
- standard window chrome: yes
- minimize button: yes
- maximize/restore button: yes
- close button: yes
- resizable: yes

On load, the code also clamps the window to the current monitor working area so it does not open off-screen.

### 6.2 Main layout

The operator window is visually divided into five vertical zones, though only three are content zones:

- left content panel: library
- center content panel: preview
- right content panel: tabs for service plan and theme
- top header band
- bottom status band

The entire window uses a dark shell background gradient:

- start: `#0B1015`
- middle: `#142431`
- end: `#162E36`

### 6.3 Top header band

Position:

- docked at the top of the main window

Visual style:

- background: `#CC0D1419`
- border: `#355566`
- rounded corners: `22`

Contents:

- app title on the left
- data source summary under the title
- current presentation summary under the data source summary
- quick action buttons on the right

Title area:

- title font family: `Georgia`
- title size: `30`
- title color: inherited shell foreground `#F4F1E8`
- data source summary color: `#D3C9B3`
- current presentation summary color: `#91BBD1`

Right-side buttons:

- `Open Presenter`
- `Previous`
- `Next`
- `Blank`
- `Black`
- `Clear Overlay`

Functionality:

- `Open Presenter`: opens or focuses the presenter window
- `Previous`: moves to the previous slide in the active presentation
- `Next`: moves to the next slide
- `Blank`: toggles a blank white overlay on the presenter output
- `Black`: toggles a black overlay on the presenter output
- `Clear Overlay`: removes blank/black overlay without changing the active slide

### 6.4 Left panel - Library

Position:

- left column of the main content area

Purpose:

- browse hymns
- search hymns
- switch between hymn/custom content filtering
- create and manage custom songs
- start presenting or add content to the service plan

Panel card appearance:

- background: `#101920`
- border: `#29404C`
- rounded corners: `18`

Panel header:

- heading text: `Library`
- heading size: `22`
- summary line color: `#9AB8C8`

Sub-sections in this panel:

- `Find Hymn`
- `Filter`
- library action buttons
- custom song action buttons
- library results list

#### Find Hymn field

Control:

- single-line `TextBox`

Visual style:

- background: `#13202A`
- foreground: `#F4F1E8`
- border: `#34586E`

Behavior:

- updates search immediately as the user types
- matches hymn number exactly
- also matches title text
- also matches slide text/lyric content

#### Filter dropdown

Control:

- `ComboBox`

Available filter modes:

- `All`
- `Hymns`
- `CustomSongs`

Behavior:

- `All` shows built-in hymns plus custom songs
- `Hymns` shows only structured hymn content
- `CustomSongs` shows only user-created songs

#### Library action buttons

Buttons:

- `Add To Plan`
- `Present`
- `Reload Data`

Behavior:

- `Add To Plan`: clones the currently selected library item into the service plan queue
- `Present`: immediately sends the selected library song to the presenter session
- `Reload Data`: reloads hymn JSON files from `Data/Structured`

#### Custom song action buttons

Buttons:

- `Add Custom Song`
- `Edit Custom`
- `Delete Custom`

Behavior:

- `Add Custom Song`: opens the content editor dialog to create a new custom song
- `Edit Custom`: opens the selected custom song in the content editor dialog
- `Delete Custom`: removes the selected custom song from local saved state

Important rule:

- `Edit Custom` and `Delete Custom` are only enabled when the selected library item is a custom song

#### Library results list

Control:

- `ListBox`

Item card appearance:

- background: `#15232B`
- border: `#274553`
- rounded corners: `14`

Each library item shows:

- title or hymn number plus title
- kind label
- slide count

Color usage:

- title: default foreground
- kind label: `#D7C08C`
- slide count: `#8CB3C8`

Selection behavior:

- selecting an item updates the center preview panel
- selecting an item also enables any relevant actions

### 6.5 Center panel - Preview

Position:

- center column of the main content area

Purpose:

- show the currently selected hymn, custom song, or service item before sending it live

Structure:

- preview header row
- preview slide canvas area

Header row contents:

- `PreviewTitle`
- `PreviewSubtitle`
- `Present Selection` button
- `Present Plan Item` button

Behavior:

- `PreviewTitle` reflects the selected library item or selected service plan item
- `PreviewSubtitle` describes the current content type
- `Present Selection` sends the selected library song to the presenter
- `Present Plan Item` sends the selected service queue item to the presenter

Preview canvas appearance:

- uses the current theme background generated by `PresenterViewModel`
- border: `#3C5A6A`
- rounded corners: `24`

Inside the preview canvas:

- title rendered with the theme title font and color
- subtitle rendered with the theme accent color
- scrollable list of preview slides

Each preview slide card:

- background: `#B4172029`
- rounded corners: `18`

Each preview slide shows:

- slide heading such as `Verse 1`
- slide text body

This preview is a design approximation of the presenter style, not a second interactive presenter window.

### 6.6 Right panel - Tabs

Position:

- right column of the main content area

Current tabs:

- `Service Plan`
- `Theme`

#### Service Plan tab

Purpose:

- build a simple order of service / queue of projected items

Top controls:

- `Add Program Slide`
- `Present`
- `Move Up`
- `Move Down`
- `Remove`
- `Clear Plan`

Behavior:

- `Add Program Slide`: opens the content editor dialog to create a free-text slide deck
- `Present`: sends the selected service item to the presenter
- `Move Up`: shifts the selected item earlier in the plan
- `Move Down`: shifts the selected item later in the plan
- `Remove`: removes the selected item
- `Clear Plan`: removes all service plan items

Service plan list:

- uses the same visual card style as the library list
- each item displays title, kind label, and slide count

Data rule:

- service plan items are stored as clones, so changing the original selected library song later does not mutate the already queued copy

#### Theme tab

Purpose:

- adjust the weekly presentation style

Sections shown in order:

- `Presets`
- `Typography`
- `Layout`
- `Colors`
- `Footer And Image`

Preset cards:

- `Sanctuary`
- `Morning Light`
- `Harvest Night`

Each preset card shows:

- preset name
- short description
- `Apply` button

Editable theme fields:

- font family
- title font size
- body font size
- content alignment
- background hex
- secondary background hex
- foreground hex
- accent hex
- overlay hex
- footer text
- background image path

Theme action buttons:

- `Browse Image`
- `Clear Image`
- `Reset Theme`

Behavior:

- `Browse Image` chooses a local image file
- `Clear Image` removes the current image path
- `Reset Theme` restores hard-coded default theme values

### 6.7 Bottom status band

Position:

- docked at the bottom of the main window

Appearance:

- background: `#C010171D`
- border: `#2F4B5B`
- rounded corners: `18`

Contents:

- left side: storage summary showing where state is saved
- right side: fixed label `Desktop WPF app | offline-first data`

## 7. Presenter Window

Files:

- `NativeHymnsApp/Views/PresenterWindow.xaml`
- `NativeHymnsApp/Views/PresenterWindow.xaml.cs`
- `NativeHymnsApp/ViewModels/PresenterViewModel.cs`
- `NativeHymnsApp/Services/PresentationSession.cs`

### 7.1 Presenter shell

The presenter window is intentionally different from the main window.

Properties:

- borderless window
- no minimize/maximize/close buttons
- maximized
- topmost
- not shown in taskbar

Close behavior:

- pressing `Escape` while the presenter window is focused closes it

Monitor behavior:

- if a second display exists, the presenter window is moved to the second screen
- otherwise it is shown on the primary screen

### 7.2 Presenter layout

Main zones:

- title/subtitle area at the top
- central slide content area
- footer/counter area at the bottom

Default presenter behavior when idle:

- title: `Ready To Present`
- helper text: `Choose a hymn or service item from the operator window.`

When content is active, the presenter shows:

- `DeckTitle`
- `DeckSubtitle`
- `SlideHeading`
- `SlideText`
- footer text
- slide counter

### 7.3 Presenter overlays

The presenter supports three visual states:

- normal presentation
- blank screen
- black screen

Normal presentation:

- theme background is visible
- slide content is visible

Blank screen:

- full-window white overlay
- color: `#FFF8F3`

Black screen:

- full-window black overlay

Overlay state is controlled by `PresentationSession`.

## 8. Content Editor Dialog

Files:

- `NativeHymnsApp/Views/ContentEditorWindow.xaml`
- `NativeHymnsApp/Views/ContentEditorWindow.xaml.cs`

Purpose:

- create a custom song
- edit a custom song
- create a free-text program slide deck

Fields:

- title
- content

Buttons:

- `Save`
- `Cancel`

Validation:

- both title and content are required
- if either is empty, a warning message box is shown

Content entry rule:

- blank lines split the content into separate slides
- the first line can optionally be a section heading like `Verse 1`, `Chorus`, `Bridge`, `Refrain`, `Intro`, `Outro`, `Ending`, or `Slide`

## 9. Keyboard Shortcuts

These are currently defined in `MainWindow.xaml`.

- `F5` -> open presenter
- `PageDown` -> next slide
- `PageUp` -> previous slide
- `Right Arrow` -> next slide
- `Left Arrow` -> previous slide
- `B` -> toggle black screen
- `W` -> toggle blank screen
- `Escape` -> clear overlay

Presenter-specific shortcut:

- `Escape` inside the presenter window closes the presenter window itself

## 10. Color Scheme

### 10.1 App-wide shell colors

| Element | Color |
|---|---|
| Main background gradient start | `#0B1015` |
| Main background gradient middle | `#142431` |
| Main background gradient end | `#162E36` |
| Default foreground text | `#F4F1E8` |
| Secondary warm text | `#D3C9B3` |
| Accent gold label text | `#D7C08C` |
| Informational blue text | `#9AB8C8` |
| Status blue text | `#91BBD1` |

### 10.2 Card and panel colors

| Element | Color |
|---|---|
| Standard panel card background | `#101920` |
| Standard panel card border | `#29404C` |
| Header band background | `#CC0D1419` |
| Header band border | `#355566` |
| Footer band background | `#C010171D` |
| Footer band border | `#2F4B5B` |
| Library/service item card background | `#15232B` |
| Library/service item card border | `#274553` |
| Preview slide sample card background | `#B4172029` |

### 10.3 Input colors

| Element | Color |
|---|---|
| TextBox and ComboBox background | `#13202A` |
| TextBox and ComboBox border | `#34586E` |
| ListBox background | `#101920` |
| ListBox border | `#2B4658` |

### 10.4 Button colors

| State | Background | Border |
|---|---|---|
| Default | `#243848` | `#34586E` |
| Hover | `#2F4E60` | `#4A7A92` |
| Pressed | `#1B2E39` | inherited |
| Disabled | same base, opacity `0.45` | same base |

Button text color:

- `#F4F1E8`

### 10.5 Default presentation theme colors

The default theme is defined in `ThemeSettingsSnapshot.CreateDefault()`.

| Theme property | Value |
|---|---|
| Background | `#0F1A1F` |
| Secondary background | `#193844` |
| Foreground | `#FFF7EC` |
| Accent | `#D8BD7A` |
| Overlay | `#B3121820` |
| Footer text | `Khasi Presbyterian Hymns` |
| Content alignment | `Center` |
| Default font family | `Georgia` |
| Title font size | `34` |
| Body font size | `28` |

## 11. Current Theme Presets

### 11.1 Sanctuary

- font: `Georgia`
- title size: `34`
- body size: `28`
- background: `#0F1A1F`
- secondary background: `#193844`
- foreground: `#FFF7EC`
- accent: `#D8BD7A`
- overlay: `#B3121820`
- alignment: `Center`
- footer: `Khasi Presbyterian Hymns`

### 11.2 Morning Light

- font: `Palatino Linotype`
- title size: `36`
- body size: `27`
- background: `#F6ECD8`
- secondary background: `#D3B98F`
- foreground: `#27323A`
- accent: `#8D5A2B`
- overlay: `#C7FFFFFF`
- alignment: `Left`
- footer: `Weekly Service`

### 11.3 Harvest Night

- font: `Trebuchet MS`
- title size: `34`
- body size: `29`
- background: `#140D14`
- secondary background: `#41253D`
- foreground: `#FFF3E5`
- accent: `#F0A85D`
- overlay: `#B5141118`
- alignment: `Center`
- footer: `Praise & Worship`

## 12. Core Workflow

### 12.1 Hymn projection workflow

1. App starts and loads structured hymn JSON.
2. User searches by hymn number, title, or lyric text in the left panel.
3. User selects a hymn from the library list.
4. The selected hymn appears in the center preview panel.
5. User clicks `Present` or `Present Selection`.
6. The hymn is cloned into the presentation session.
7. The presenter window is opened or focused.
8. The operator moves slide by slide using buttons or keyboard shortcuts.

### 12.2 Service plan workflow

1. User selects a hymn or custom song from the library.
2. User clicks `Add To Plan`.
3. The selected content is cloned into the service plan.
4. User may add additional program slides using `Add Program Slide`.
5. User reorders the queue with `Move Up` and `Move Down`.
6. User selects a queue item and clicks `Present`.

### 12.3 Custom song workflow

1. User clicks `Add Custom Song`.
2. Content editor opens.
3. User enters title and content.
4. Blank lines split content into multiple slides.
5. Optional section names such as `Verse 1` or `Chorus` are recognized.
6. On save, the song is stored in local app state.
7. The custom song becomes visible in the library.

### 12.4 Program slide workflow

1. User clicks `Add Program Slide` in the service plan tab.
2. Content editor opens.
3. User enters the title and body.
4. Blank lines create multiple slides if desired.
5. The result is inserted directly into the service queue as a `TextSlide`.

### 12.5 Theme workflow

1. User opens the `Theme` tab.
2. User either applies a preset or changes fields manually.
3. The preview panel updates based on the current theme.
4. The presenter window also reflects the theme through `PresenterViewModel`.
5. State is saved locally whenever the theme changes.

## 13. Data Flow And Logic

### 13.1 Hymn data loading

The app expects JSON files in:

```text
Data/Structured
```

At build time, JSON files from the repository root `Data/Structured` folder are copied into the output folder:

```text
NativeHymnsApp/bin/Debug/net8.0-windows/Data/Structured
```

`SongCatalogService`:

- scans for all `*.json` files in that folder
- deserializes `StructuredHymnFile`
- converts each hymn into a `SongDocument`
- maps each verse to one `SlideSection`
- sets the slide heading to `Verse N`

### 13.2 In-memory content model

The main runtime model is `SongDocument`.

It represents:

- a structured hymn
- a custom song
- or a text slide deck

Each `SongDocument` contains:

- an ID
- a kind
- an optional hymn number
- a title
- an optional source file
- a list of slides

### 13.3 Slide generation logic for custom content

`ParseSlides()` in `MainWindowViewModel.Content.cs` turns raw editor content into slides.

Rules:

- blank lines split slides
- leading headings like `Verse 1`, `Chorus`, `Bridge`, `Refrain`, `Intro`, `Outro`, `Ending`, `Slide` are used as slide headings
- if a slide begins with `1.` or `1)` style numbering, the number becomes a `Verse N` heading
- otherwise headings default to `Slide N` for custom/text decks and `Verse N` for hymn-like content

### 13.4 Presentation logic

`PresentationSession` holds the live projection state.

It manages:

- the active deck
- current slide index
- overlay mode
- navigation state

Important implementation detail:

- `PresentSong()` deep-clones the source deck before presenting it

This avoids accidental mutation of the original library or plan content during live presentation.

### 13.5 Presenter visual logic

`PresenterViewModel` turns the current presentation session plus the theme into display-ready properties.

It decides:

- current title
- current subtitle
- current heading
- current slide text
- slide counter
- background brush
- foreground brush
- accent brush
- overlay brush
- selected font family
- selected font sizes
- text alignment
- footer text

Background selection logic:

- if a valid background image path exists, use the image
- otherwise create a linear gradient from the theme's background and secondary background colors

### 13.6 Persistence logic

`AppStateService` stores local state in:

```text
%LocalAppData%\NativeHymnsApp\app-state.json
```

Saved state currently includes:

- custom songs
- service plan
- theme settings

Not saved in state:

- built-in hymn JSON files
- current slide index after app close
- live presenter overlay state after app close

## 14. Current Project Structure

Relevant repository structure:

```text
hymns_app/
  Data/
    Consolidated/
      KHB_Consolidated.docx
    Raw/
    Structured/
      khb_sample_001_010.json
      khb_sample_001_010.csv
      khb_sample_001_010_review.md
  Documentation/
    current_app_implementation.md
  NativeHymnsApp/
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    NativeHymnsApp.csproj
    Infrastructure/
      ObservableObject.cs
      RelayCommand.cs
    Models/
      ContentModels.cs
      ThemeModels.cs
    Services/
      AppStateService.cs
      PresentationSession.cs
      SongCatalogService.cs
      ThemePresetCatalog.cs
    ViewModels/
      MainWindowViewModel.cs
      MainWindowViewModel.Content.cs
      PresenterViewModel.cs
    Views/
      ContentEditorWindow.xaml
      ContentEditorWindow.xaml.cs
      PresenterWindow.xaml
      PresenterWindow.xaml.cs
  scripts/
    structure_khb_sample.py
  WpfApp1/
  HymnsDesktopApp/
```

### 14.1 Folder responsibilities

- `Data/Structured`: structured hymn content currently used by the app
- `scripts`: data extraction and conversion helpers
- `NativeHymnsApp/Infrastructure`: lightweight MVVM support classes
- `NativeHymnsApp/Models`: app state and content models
- `NativeHymnsApp/Services`: loading, persistence, and presentation services
- `NativeHymnsApp/ViewModels`: UI-facing logic and state orchestration
- `NativeHymnsApp/Views`: dialog and presenter windows

## 15. Current Limitations

The app is usable, but it still has important limitations.

- There is no database yet.
- Only JSON hymn files are loaded automatically.
- Only one sample hymn JSON file is currently present.
- The main window uses a fixed three-column layout without dynamic responsive collapse.
- There are no custom icons or toolbar glyphs yet.
- The presenter window is intentionally borderless, so it has no system buttons.
- There is no import UI yet for CSV/Word inside the app.
- There is no dedicated release packaging yet.
- There is no explicit validation UI for malformed hymn JSON files beyond startup failure.
- Search is simple text matching, not fuzzy search.

## 16. Practical Runtime Paths

Executable:

```text
NativeHymnsApp/bin/Debug/net8.0-windows/NativeHymnsApp.exe
```

Structured hymn data copied to runtime:

```text
NativeHymnsApp/bin/Debug/net8.0-windows/Data/Structured
```

Saved local state:

```text
%LocalAppData%\NativeHymnsApp\app-state.json
```

## 17. Summary

The current implementation is a native Windows hymn presenter built with WPF on .NET 8.

Today it already supports:

- loading structured Khasi hymn data from JSON
- searching and previewing hymns
- creating custom songs
- creating free-text program slides
- building a service queue
- opening a separate presenter window
- navigating live slides
- blank and black presenter overlays
- weekly theme customization
- local persistence of theme, custom songs, and service queue

The active implementation lives entirely in `NativeHymnsApp`, and that folder should be treated as the current app codebase.
