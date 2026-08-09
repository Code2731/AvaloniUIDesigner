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
