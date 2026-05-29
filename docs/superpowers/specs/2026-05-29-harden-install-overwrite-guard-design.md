# Harden `dotnet quality install` — overwrite guard, atomic writes, precise XML anchor

**Date:** 2026-05-29
**Status:** Approved (design)
**Findings addressed:** #4, #7, #11 (`tools/Quality.Cli/Commands/InstallCommand.cs`)

## Problem

`InstallCommand.Run` bootstraps a target repo by copying 9 embedded template files
to disk and then attaching `SerilogAnalyzer` to `Directory.Packages.props`. Three
defects make re-running it unsafe:

- **#4 (line 37):** `File.Create(target)` truncates unconditionally. Re-running
  `install` to pick up a framework update silently destroys any user customization
  to `.semgrep/*.yml`, `.quality.toml`, `Directory.Build.props`, etc. There is no
  `--force` flag, prompt, backup, or skip-if-exists guard. The user's mental model
  (install is idempotent) does not match the implementation (install overwrites).
- **#7 (line 41):** `AutoAttachSerilogAnalyzer` runs after all 9 writes. If the
  target `Directory.Packages.props` has no `</Project>`, the string `Replace` is a
  silent no-op — the package is never pinned and no error is raised. If a later
  write throws, the 9 template files are already on disk with no rollback and no
  guidance to the user.
- **#11 (line 70):** `text.Replace("</Project>", injection, Ordinal)` replaces
  *every* occurrence of `</Project>`, including one inside an XML comment, producing
  invalid XML with the injection block emitted twice.

## Goals

- Re-running `install` is always safe: existing files are never destroyed by default.
- A user can intentionally take the framework's version of a file via `--force`.
- Install fails loudly and early on packaging defects and malformed target files,
  rather than silently no-op'ing or leaving a half-written file.
- Keep the diff focused on `InstallCommand`; preserve the existing public entry point
  shape used by tests.

## Non-goals

- Full transactional rollback across all 9 files (over-engineering; re-run safety +
  atomic per-file writes cover the realistic failure modes — see YAGNI note below).
- Three-way merge of user customizations with framework updates (no merge exists;
  the choice is skip-or-overwrite).
- Interactive prompting (bad for CI / non-interactive use).

## Design

### 1. Public surface & wiring

`InstallCommand.Run` gains an optional `force` parameter defaulting to `false`, so
the existing call sites and tests compile unchanged:

```csharp
public static int Run(string targetRoot, bool force = false)
```

`Program.cs` adds a `--force` boolean option to the `install` command, wired
alongside the existing `--into`:

```csharp
var forceOpt = new Option<bool>(
    "--force",
    () => false,
    "Overwrite existing files with the framework's version");
install.AddOption(forceOpt);
install.SetHandler(
    (into, force) => Environment.Exit(Commands.InstallCommand.Run(into, force)),
    targetOpt,
    forceOpt);
```

### 2. Plan-then-apply structure

Internal model:

```csharp
private enum FileAction { Write, Skip, Overwrite }
private sealed record FilePlan(string Resource, string Target, FileAction Action);
```

**Plan phase** (no disk writes): for each of the 9 `(resource, relativePath)`
entries, resolve the embedded resource stream up front. If any stream is null,
throw the existing `InvalidOperationException` ("missing embedded resource …
packaging defect") *before any file is written*. Then classify each target:

| Condition                       | Action      |
|---------------------------------|-------------|
| target does not exist           | `Write`     |
| target exists and `!force`      | `Skip`      |
| target exists and `force`       | `Overwrite` |

**Apply phase**: for `Write` and `Overwrite`, ensure the parent directory exists,
copy the embedded resource to a sibling temp file (`target + ".tmp-install"`), then
`File.Move(temp, target, overwrite: true)`. The move is atomic per file, so a target
is never left half-written if the process dies mid-copy. `Skip` does nothing. The
temp file is cleaned up on a failed copy.

### 3. SerilogAnalyzer attach hardening (#7 + #11)

`AppendPackageVersionToPackagesProps` changes:

- The existing idempotency guard stays: if `Include="{id}"` is already present,
  return without modifying the file.
- Locate the closing tag with `text.LastIndexOf("</Project>", StringComparison.Ordinal)`
  and splice the injection in before it, instead of `text.Replace(...)`. Only the
  real (last) closing tag is affected — a `</Project>` inside a comment is left
  alone. (Fixes #11.)
- If `LastIndexOf` returns `-1` (no closing tag at all), throw an
  `InvalidOperationException` naming the file path, instead of silently doing
  nothing. (Fixes #7's silent-failure path.)

This attach step still runs after the file writes, but it is now the only post-write
operation and it fails loudly rather than silently. Combined with skip-by-default,
a failure here is recoverable: the user fixes the props file and re-runs, and the
already-written template files are skipped rather than re-truncated.

### 4. Reporting & exit code

After the apply phase, print one line per file plus a footer, using the plain
`Console.WriteLine` style the other commands use:

```
  wrote    .semgrep/security.yml
  skipped  .quality.toml (exists)
  ...
3 written, 6 skipped. Re-run with --force to overwrite skipped files.
```

Exit code stays `0` on success — skips are normal and expected, not errors. Only
genuine failures (packaging defect, missing `</Project>`, IO exception) take a
thrown / non-zero path.

### YAGNI note on rollback

Finding #7 mentions "no rollback". Full transactional rollback of 9 files is
deliberately out of scope. Two properties make it unnecessary: (a) each file is
written via atomic temp-file + move, so no individual file is ever half-written;
and (b) skip-by-default means re-running after an interrupted install completes it
rather than corrupting it. The remaining failure window (process killed between two
files) leaves a set of individually-complete files that a re-run finishes — an
acceptable, self-healing outcome.

## Testing

Existing tests are kept (they exercise the `force = false` default; the
"is idempotent" test now additionally proves skip-not-overwrite). New tests:

1. **Re-run preserves customization (core #4 regression):** install, customize a
   written file, install again with `force = false` → the customization is intact.
2. **`force = true` overwrites:** after customizing, install with `force = true` →
   the file matches the embedded template again.
3. **Comment containing `</Project>` (#11):** a `Directory.Packages.props` with
   `<!-- </Project> -->` before the real closing tag, plus a Serilog reference,
   gets the injection appended exactly once at the end and remains valid XML.
4. **Missing `</Project>` (#7):** a props file with no closing tag, plus a Serilog
   reference, causes `Run` to throw a clear `InvalidOperationException` naming the
   file.

## Follow-up bookkeeping

After implementation lands, mark findings #4, #7, and #11 `resolved` in
`findings.json` with the commit ref.
