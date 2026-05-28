# Opt-out guide

Every check in this template can be disabled, but the path is intentionally
narrow: each disable carries a justification that names an issue, an owner, and
a review date. The aim is auditability — six months later, anyone reading the
config knows why a check is off and when it should be revisited.

The three layers below match where the check lives. Pick the one that fits the
check you want to disable; the per-check catalog in
[`practices.md`](practices.md) points each check at its layer.

## Justification shape

Every opt-out — regardless of layer — must include this text, verbatim or
substituting the bracketed parts:

```
Justification: [ISSUE-1234], owner @[handle], review [YYYY-MM-DD]
```

- **ISSUE-1234** — link to the tracking issue. No ticket = no opt-out.
- **owner @handle** — the person accountable for re-enabling the check.
- **review YYYY-MM-DD** — the date by which the opt-out should be revisited.
  `dotnet quality status` flags entries past their review date.

Anything shorter ("not relevant", "noisy", "TODO") fails the
`bypass-directive-check` hook (Layer C) or the `.quality.toml` validator
(Layer B).

---

## Layer A — Roslyn rules: `.editorconfig`

Use this layer for any `CAxxxx`, `SAxxxx`, `MAxxxx`, `RCSxxxx`, or other
Roslyn-analyzer rule listed in [`practices.md`](practices.md).

### Syntax

Downgrade severity in `.editorconfig`. Default is `error`; lower to
`warning`, `suggestion`, `silent`, or `none`. Path-specific sections let you
scope the opt-out to a subtree.

```ini
# Repo-wide downgrade (rarely justified):
[*.cs]
dotnet_diagnostic.CAxxxx.severity = suggestion

# Scoped to a single folder (preferred):
[tests/**/*.cs]
dotnet_diagnostic.CAxxxx.severity = none
```

### Required justification

Add a comment above the rule line. Example:

```ini
# Justification: ISSUE-1234, owner @alice, review 2026-07-01
# CA1303 localization noise — not relevant for an internal template.
dotnet_diagnostic.CA1303.severity = suggestion
```

### Worked example

```ini
# Justification: ISSUE-1234, owner @alice, review 2026-07-01
# SA1633 file-header rule disabled because this repo is internal and
# copyright headers add churn without compliance value.
[*.cs]
dotnet_diagnostic.SA1633.severity = none
```

### Anchors

This section is referenced from `practices.md` for every Roslyn check:

<a id="ca2254"></a>`#ca2254` ·
<a id="ca1727"></a>`#ca1727` ·
<a id="ca1848"></a>`#ca1848` ·
<a id="ca2017"></a>`#ca2017` ·
<a id="ca1873"></a>`#ca1873` ·
<a id="ma0026"></a>`#ma0026`

---

## Layer B — Pre-commit hooks: `.quality.toml`

Use this layer for any check listed in [`practices.md`](practices.md) whose
home is `tools/Quality.Cli`, `.pre-commit-config.yaml`, or `.semgrep/*.yml`.

### Syntax

Flip `enabled = true` to `enabled = false` and add a `reason` field. The
`reason` must be non-empty — the `.quality.toml` validator (part of
`dotnet quality status` / `pr-check`) fails otherwise.

```toml
[phase4]
trivy_fs = { enabled = false, reason = "Justification: ISSUE-1234, owner @alice, review 2026-07-01" }
```

### Required justification

The `reason` field must contain the full justification shape (issue, owner,
review date). Examples of insufficient `reason` values that the validator
rejects:

- `reason = ""`
- `reason = "noisy"`
- `reason = "TODO fix later"`

### Worked example

```toml
# Disable trivy-fs while we migrate the pipeline; tracked in ISSUE-1234.
# Re-enable when ISSUE-1234 closes — review date below.
[phase4]
trivy_fs = { enabled = false, reason = "Justification: ISSUE-1234, owner @alice, review 2026-07-01" }
```

After disabling, run `dotnet quality status` — the table shows the disabled
check, its reason, and how many days since the disable. Stale opt-outs (past
review date) are surfaced there.

### Anchors

<a id="gitleaks"></a>`#gitleaks` ·
<a id="max-lines"></a>`#max-lines` ·
<a id="env-exhaustiveness"></a>`#env-exhaustiveness` ·
<a id="unused-nuget-packages"></a>`#unused-nuget-packages` ·
<a id="ef-migrations-drift"></a>`#ef-migrations-drift` ·
<a id="lockfile-integrity"></a>`#lockfile-integrity` ·
<a id="dotnet-list-vulnerable"></a>`#dotnet-list-vulnerable` ·
<a id="license-check"></a>`#license-check` ·
<a id="osv-scanner"></a>`#osv-scanner` ·
<a id="trivy"></a>`#trivy` ·
<a id="semgrep-arch"></a>`#semgrep-arch` ·
<a id="semgrep-security"></a>`#semgrep-security` ·
<a id="semgrep-logging"></a>`#semgrep-logging` ·
<a id="jscpd"></a>`#jscpd` ·
<a id="dotnet-build-locked"></a>`#dotnet-build-locked` ·
<a id="dotnet-test"></a>`#dotnet-test`

---

## Layer C — Bypass directives in code

Use this layer when a single callsite needs an exception to an otherwise-active
Roslyn rule. The `bypass-directive-check` hook enforces that every
`#pragma warning disable` and `[SuppressMessage]` carries a justification.

### Syntax: `#pragma warning disable`

Pair every `#pragma warning disable` with a `// Justification:` comment on the
preceding line. Always re-enable the rule at the end of the scoped block.

```csharp
// Justification: ISSUE-1234, owner @alice, review 2026-07-01
#pragma warning disable CA1303
var label = "internal-only literal";
#pragma warning restore CA1303
```

A disable without the adjacent `// Justification:` line fails the hook.

### Syntax: `[SuppressMessage]`

Pass a non-empty `Justification` argument in the full shape:

```csharp
[SuppressMessage(
    "Performance",
    "CA1848:Use LoggerMessage delegates",
    Justification = "ISSUE-1234, owner @alice, review 2026-07-01")]
public void Probe() => _logger.LogInformation("probe");
```

An attribute without `Justification = "…"` (or with an empty string) fails the
hook. The `MA0091` analyzer also enforces this at build time as a safety net.

### Worked example

```csharp
// Justification: ISSUE-1234, owner @alice, review 2026-07-01
// CA2254 disabled here because the template *is* a constant in this codegen
// path; the analyzer can't see through the source-generator boundary.
#pragma warning disable CA2254
_logger.LogInformation(generatedTemplate, generatedArgs);
#pragma warning restore CA2254
```

### `--no-verify` is not an opt-out

`git commit --no-verify` skips the local hook pipeline, but CI re-runs the
identical `.pre-commit-config.yaml`. Local bypass only delays failure to PR
time; it is not a supported mechanism for disabling a check, and the
`AGENTS.md` quality stance forbids its use without explicit user confirmation
in the conversation.

### Anchors

<a id="bypass-directive-check"></a>`#bypass-directive-check` — the check
itself; disable in `.quality.toml` (Layer B) if it must be silenced.
