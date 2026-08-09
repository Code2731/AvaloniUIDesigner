# AvaloniaUIDesigner MVP Spec

## Product Goal
AvaloniaUIDesigner is a dedicated GUI design tool for Avalonia, similar to Qt Designer.
Users should be able to place controls visually, inspect properties, and validate layout quickly.

## Target Users
- Avalonia app developers
- Teams that need faster UI iteration than raw XAML editing

## MVP Scope (v0.5)
- 4-pane layout: Toolbox / Canvas / Object Tree / Property Inspector
- Click-to-place workflow from toolbox to design surface
- Element selection, move, and resize (8 handles)
- Live property editing via PropertyGrid
- Basic AXAML draft export from current design document

## Initial Control Set
- Button
- TextBox
- Grid
- StackPanel

## Core Architecture
- Editor: owns design state and selection state
- Renderer: creates live Avalonia controls from component definitions
- Serializer: converts design document to AXAML draft
- Metadata: component catalog with display name, type, defaults, and factory

## Data Model
- DesignElement: runtime element on canvas (x/y/width/height + visual)
- DesignerCanvasDocument: serializable snapshot model
- DesignerComponentDefinition: metadata for toolbox and rendering defaults

## Non-Functional Requirements
- Startup under 2 seconds on a typical dev machine
- Interactive operations (move/resize/select) should feel immediate
- Crash-safe save path with atomic AXAML writes and recoverable `.bak` snapshots

## Roadmap
1. v0.5
- AXAML save/load command wiring in menu
- Selection sync between object tree and canvas
- Better naming and unique id strategy

2. v0.6
- Drag-and-drop placement
- Multi-select and align/distribute tools
- Undo/redo stack

3. v0.7
- ~~Custom control metadata extension model~~
- ~~Plugin-based component packs~~
- ~~Style and resource editing support~~

4. v0.8
- ~~Component Pack management and safe removal~~

5. v0.9
- ~~Multi-selection common property editing~~

6. v1.0
- ~~Categorized Toolbox ordering, collapsible groups, metadata chips, and category filter~~

7. v1.1
- ~~Toolbox Recent and Favorites with session restore~~

8. v1.2
- ~~Toolbox keyboard navigation and quick placement~~

9. v1.3
- ~~Toolbox search focus shortcuts and explicit placement mode~~

10. v1.4
- ~~Canvas Toolbox placement preview with snapped coordinates~~

11. v1.5
- ~~Toolbox placement target highlighting and click-to-insert containers~~

12. v1.6
- ~~Precise container cell, insertion-line, and slot placement feedback~~

13. v1.7
- ~~Object Tree keyboard navigation and inline editing shortcuts~~

14. v1.8
- ~~Property Inspector category/flat view and expand/collapse navigation controls~~

15. v1.9
- ~~Property Inspector dedicated filter, clear action, and focus shortcut~~

16. v1.10
- ~~Per-document-tab Property Inspector filter, category, and expansion state with session restore~~

17. v1.11
- Qt Designer-style workspace panel visibility, size restore, and layout reset

18. v1.12
- Document tab drag reordering with active-order session restore

19. v1.13
- Document tab keyboard movement and context-menu operations

20. v1.14
- Bulk document tab closing with preflight dirty confirmation

21. v1.15
- Save all dirty document tabs while preserving the active tab

22. v1.16
- Close all document tabs with preflight confirmation and a fresh blank document

23. v1.17
- Reopen recently closed document tabs with their editing state intact

24. v1.18
- Persist and restore the recently closed document tab history

25. v1.19
- Duplicate a document tab into an independent dirty document with its editing state intact

26. v1.20
- Rename document tabs with persistent aliases independent of their saved file paths

27. v1.21
- Close document tabs through middle-click while preserving the existing dirty-document safeguards

28. v1.22
- Activate the first nine document tabs directly with keyboard shortcuts

29. v1.23
- Search open document tabs by alias or saved file path and activate the selected result from a quick switch dialog

30. v1.24
- Provide keyboard navigation for canvas zoom and viewport fitting while preserving per-document zoom state

31. v1.25
- Provide standard canvas zoom presets and validated custom zoom percentage input

32. v1.26
- Provide edge-alignment keyboard shortcuts for multi-selected controls without overriding Object Tree arrow navigation

33. v1.27
- Provide horizontal and vertical center-alignment keyboard shortcuts for multi-selected controls

34. v1.28
- Provide horizontal and vertical distribution keyboard shortcuts for selections of three or more controls

35. v1.29
- Provide keyboard shortcuts for matching the width, height, or full size of multi-selected controls

36. v1.30
- Provide keyboard shortcuts for moving selected controls forward, backward, to front, or to back in z-order

37. v1.31
- Provide keyboard shortcuts for centering root selections horizontally, vertically, or on both axes of the artboard

38. v1.32
- Provide separate keyboard shortcuts for toggling the design grid and grid snapping without changing the design-guide clear shortcut

39. v1.33
- Provide keyboard shortcuts for toggling workspace panels and resetting the panel layout

40. v1.34
- Reveal hidden workspace panels before applying their search or placement focus shortcuts

41. v1.35
- Provide keyboard shortcuts for grouping selections into a Canvas and ungrouping the selected Canvas

42. v1.36
- Use direction-aware marquee selection with containment for left-to-right drags and intersection for right-to-left drags

43. v1.37
- Provide keyboard shortcuts for applying supported selection layouts and breaking the selected layout

44. v1.38
- Exclude locked and hidden controls from marquee selection while preserving direct inspection of locked controls

45. v1.39
- Cycle through visible overlapping controls with Alt-click in z-order, preserving locked controls for inspection and excluding hidden controls
