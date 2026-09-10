# Round Verification

Run from the repository root in PowerShell:

```powershell
./scripts/Verify.ps1
```

The script runs these checks sequentially and stops on the first failure:

1. Release build of the application solution.
2. Layout validation regression checks.
3. File persistence regression checks using isolated temporary files.
4. Avalonia Headless Preview behavior regression checks.

The Headless suite also exercises the editor's AXAML round trips, save
preflight, session serialization, and movement undo/redo history. History
checks include redo after session restoration, no-op edits, and replacing a
redo branch with a new edit. These call editor commands rather than OS input.
Movement checks reject non-finite distances and distinguish zero-distance and
canvas-boundary no-ops without adding history or document-change events.
The same suite verifies that design-only custom controls retain interaction
and explicitly edited accessibility state through Draft AXAML and Undo/Redo
without exporting placeholder-only chrome or keyboard defaults.
Custom-control checks also cover transforms and visual effects across two-step
Undo/Redo and Draft re-import, allowing normal matrix and color quantization.
Common event handlers are verified through edit, Undo/Redo, Draft re-import,
Preview wiring, and an actual Avalonia routed focus event.
Common-property bindings on design-only custom controls are verified through
edit, Undo/Redo, sample-data application, Draft re-import, Headless Preview,
and Preview Reset without serializing the temporary sampled value.
Custom-control declared properties, style classes, and explicit color-resource
references are also covered through Undo/Redo, Draft re-import, Headless
Preview, and Preview Reset while excluding the design placeholder's default
background.

Persistence checks cover initial UTF-8 saves, replacement backups, repeated
backup rotation, invalid backup paths, temporary-file cleanup, and retry after
a failed replacement. They do not simulate power loss or hardware failure.

The script resolves paths relative to its own location, so it can also be
invoked by absolute path from another directory. It restores the caller's
working directory even on failure. An unhandled failure returns a nonzero
exit status when invoked with `pwsh -File`.

After dependencies have been restored, use `-NoRestore` for offline checks:

```powershell
./scripts/Verify.ps1 -NoRestore
```

Requirements: a .NET SDK supporting the repository's `.slnx` solution format,
the .NET 8 runtime, and NuGet access for the first restore. The script disables
Avalonia build telemetry through `UsedAvaloniaProducts`.

Headless checks do not verify visual rendering quality or operating-system
input. Manually exercise the editor and Preview for UI-facing changes before
claiming those behaviors are verified. Commit and push only after all required
checks pass; the script itself never changes Git state.
