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
Property-specific custom binding checks cover an unbound editor state, invalid
path rejection, case-insensitive declaration lookup, Add/Edit field recovery,
same-binding no-op history, and Undo/Redo for both binding creation and removal.
They also verify that removing only a binding reveals its preserved local value
while unrelated bindings remain untouched. The actual Inspector row template
must expose a Bind action beside the independent Reset action.
Typed custom-property checks load a backward-compatible Component Pack with
String, Boolean, Integer, Double, Color, and Enum definitions. They reject
invalid Enum definitions and out-of-range defaults, normalize every default,
enforce the same validation for inline and bulk local edits, style setters, and
binding fallbacks, and preserve ranges/options through Draft re-import,
Headless Preview, selected-pack export, and in-use pack removal. The actual
Inspector template is built to verify that Boolean values use a choice editor,
Integer and Double values use their declared ranges and increments in spin
editors, and Color values expose their effective color as a swatch. Event-level
checks cover canonical numeric commits, fractional Integer rejection and
restoration, Undo back to a style value, render-time no-op behavior, and the
text-editor fallback for Double values outside the decimal editor range.
Metadata checks reject multiline category labels and preserve display names,
categories, and descriptions through Draft re-import, Headless Preview,
selected-pack export, and in-use pack removal. Inspector-state and template
checks cover category/display-name ordering, uncategorized fallback placement,
category headers, technical-name/type labels, and display-name, category, and
description filtering with header recomputation.
The structured bulk custom-property editor is exercised as a real Avalonia
control tree. Checks cover category headers, Local toggles, String, choice,
numeric, and alpha-color editors, per-property accessibility names, invalid
Integer rejection, canonical result generation, multi-value apply and unset in
one Undo action, pending RESET and
LOCAL source labels, Clear local behavior, retained bindings and their lower
local values, binding replacement, and Undo restoration of both bulk values
and bindings.

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
