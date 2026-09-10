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
