Khasi Hymn Presenter
====================

Khasi Hymn Presenter is a Windows desktop app for preparing and presenting Khasi hymns during church services. It is built around a live operator workflow: build the service plan, find hymns quickly by number or title, preview content, and run the projector from a dedicated slideshow control window.

Features
--------

- Fast hymn lookup with number-first search and optional fuzzy title matching
- Service plan queue for arranging hymns and program slides
- Live slideshow control window with next/previous, direct slide jump, and service-plan loading
- Fullscreen presenter output for the projector or second display
- Theme editor for fonts, colors, alignment, and background image
- Local persistence for custom songs, service plan state, and presentation theme

Active Project
--------------

The active application in this repository is:

- `NativeHymnsApp/NativeHymnsApp.csproj`

Requirements
------------

- Windows 10 or Windows 11
- .NET 8 SDK
- Structured hymn JSON files in `Data/Structured`

Run The App
-----------

From the repository root:

```powershell
dotnet build NativeHymnsApp\NativeHymnsApp.csproj
dotnet run --project NativeHymnsApp\NativeHymnsApp.csproj
```

The app reads hymn data from JSON files in `Data/Structured`. If that folder is missing or contains no JSON files, startup will fail.

Intended Workflow
-----------------

1. Use the main window to search the library and assemble the service plan.
2. Use the hymn number field for fast hymn lookup.
3. Queue hymns or text slides into the service plan.
4. Open the slideshow control window for live operation.
5. Run the service from the slideshow control window while the presenter window stays fullscreen on the second display when available.

Keyboard Shortcuts
------------------

- `F5`: open slideshow control
- `PageDown` or `Right Arrow`: next slide
- `PageUp` or `Left Arrow`: previous slide
- `B`: black screen overlay
- `W`: blank screen overlay
- `Esc`: clear overlay

Data And Saved State
--------------------

- Hymn source data: `Data/Structured/*.json`
- Saved state: `%LocalAppData%\NativeHymnsApp\app-state.json`

The saved state stores custom songs, the service plan, and current theme settings.

Project Structure
-----------------

- `NativeHymnsApp/`: WPF application source
- `Data/Structured/`: structured hymn JSON data
- `Documentation/`: design and implementation notes
- `scripts/`: helper scripts used during data preparation

Notes
-----

- A second display is optional. If a second display is connected, the presenter window will use it for fullscreen projector output.
- The main operator interface is optimized for live use rather than generic library browsing.
