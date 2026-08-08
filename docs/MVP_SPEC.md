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
- ~~Categorized Toolbox ordering, metadata chips, and category filter~~
