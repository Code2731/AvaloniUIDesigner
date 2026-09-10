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
Declared design-only properties are exposed by the binding editor and their
binding expressions are retained through rejection safety, Undo/Redo, Draft
re-import, Headless Preview, and Preview Reset. The placeholder does not claim
to execute properties that require the external custom-control assembly.
Document styles for design-only custom controls are matched against the
original Avalonia type rather than the placeholder type. Regression coverage
includes local-default conflict removal, pseudo-class simulation, Undo/Redo,
Draft re-import, live disabled-state changes in Headless Preview, and Preview
Reset without serializing style-managed values as local attributes.
The declared custom-property editor is covered for common-property filtering,
case-insensitive canonical names, undeclared-property rejection, value edits
and removal, selective binding replacement, Undo/Redo, Draft re-import, and
Headless Preview metadata preservation. Binding choices continue to come from
the component declaration even when a style removes a local value, and an
in-use placeholder retains those declarations after its component pack is
removed and its history is rebuilt.
Component Pack checks also cover explicit declarations without default values,
invalid CLR property-name rejection, binding-to-local transitions, selected
component export, and reloading the exported pack without turning unset
declarations into default AXAML attributes.
Declared custom-property style setters are verified for base and disabled
pseudo-class calculation, undeclared-setter rejection, Draft re-import,
Headless Preview and Reset, local-value and binding precedence, and Undo/Redo.
Calculated values remain separate from local AXAML attributes while explicit
local values survive style-aware history capture.
The Property Inspector summary uses the same runtime state to distinguish
local, binding, calculated style, and unset declarations. Regression checks
cover source precedence, binding path/mode/fallback formatting, pseudo-class
updates, and transitions back to an unset value. A Headless MainWindow check
also verifies source filtering, no-match hiding, locked read-only visibility,
unlocking, and immediate refresh after custom metadata changes.
Inline custom-property checks build the actual Inspector row template and
exercise its LostFocus commit and Reset button events. Command-level coverage
includes case-insensitive property names, undeclared and invalid-XML rejection,
same-value no-op history, Set/Undo/Redo, local and binding Reset, preservation
of unrelated local values and bindings, and restoration of style or unset
states.

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
