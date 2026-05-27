# Design: .NET Quality Framework Template

**Date:** 2026-05-27
**Status:** Approved (pending implementation plan)
**Author:** Brainstorming session with Martin Oehlert

## Context

The TypeScript template in this repo (`myprojectclone-typescript`) ships a strict
10-phase pre-commit pipeline (~50 hooks) wrapping a NestJS + Next.js + Prisma stack.
The pipeline enforces: type coverage at 100%, zero duplication, zero TODOs, zero
floating promises, license compliance, supply-chain scanning, architecture rules,
and 90%+ test coverage. The project rule is **"fix the code, not the rule"** —
thresholds never relax to make a check pass.

This spec describes a **reusable .NET equivalent**: a quality framework template
that ships the same level of rigor for any ASP.NET Core solution, expressed in
.NET-idiomatic mechanisms.

## Goals

1. Stack-agnostic — works on any ASP.NET Core solution layout, not a specific app.
2. Hybrid delivery — GitHub template repo for greenfield + `install.sh`/`install.ps1`
   for retrofit into existing solutions.
3. Strict by default with documented per-check opt-out (every disable must carry
   a justification; empty justification fails the config-validation hook).
4. Truly cross-platform — Windows/macOS/Linux as first-class citizens.
5. Build-time enforcement wherever possible so violations surface in the IDE,
   not just at commit time.

## Non-goals

- Opinionated app architecture (DDD, vertical slices, etc.) — sample is illustrative only.
- Logging/observability defaults beyond `ILogger` usage rules.
- Authentication scaffolding.
- Ratchet/baseline mode (strict-with-opt-out only).
- NuGet publication of the `dotnet-quality` tool in v1 (local `.nupkg` only).
- `dotnet new` template packaging in v1 (GitHub-template path covers v1).
- Azure DevOps / GitLab CI templates in v1 (GitHub Actions only).
- Phase 10 (Playwright E2E) in v1 — docs only.

## Approach

**Roslyn-native, not shell-script-native.** ~70% of the TS framework's checks
move into `Directory.Build.props` + `.editorconfig` + analyzer NuGet packages,
so they fire at build time and in the IDE. Pre-commit hooks become a thin layer
(~15 hooks) for the cross-cutting checks Roslyn legitimately cannot see —
secrets, supply-chain scanning, cross-file duplication, architecture rules
expressed as semgrep patterns, env-key exhaustiveness, license compliance,
sensitive-data logging.

**Hook implementation language: C#, not bash.** All custom hooks live in a
single `dotnet quality` CLI tool (`tools/Quality.Cli/`) packed as a local
`dotnet tool`. Pre-commit hooks invoke it via `dotnet quality <subcommand>`.
External scanners (gitleaks, OSV, trivy, semgrep) are invoked via their
upstream official pre-commit repos — already cross-platform, already maintained.

## Repository layout

Target repo: `MO2k4/myprojectclone-dotnet` (public, standalone — sibling of
`myprojectclone-typescript`, not a sub-directory of it).

```
myprojectclone-dotnet/
├── .config/
│   └── dotnet-tools.json          # pinned tool manifest (Quality.Cli + externals)
├── .editorconfig                   # style + analyzer severities (default: error)
├── .pre-commit-config.yaml         # 10-phase pipeline (~15 hooks)
├── .gitleaks.toml
├── .semgrep/                       # arch + security + logging rules (C# patterns)
├── .quality.toml                   # per-check opt-out config
├── Directory.Build.props           # global MSBuild defaults (nullable, WaE, analysis)
├── Directory.Packages.props        # central package mgmt; analyzers pinned here
├── Directory.Build.targets         # build-time hooks (coverage threshold, lock check)
├── global.json                     # SDK pin
├── .github/workflows/quality.yml   # CI: native + Docker jobs
├── docker/Dockerfile.quality       # pinned tool image for hermetic CI
├── install.sh                      # ~20 lines, bootstraps SDK + Quality.Cli
├── install.ps1                     # Windows equivalent
├── tools/
│   └── Quality.Cli/                # the dotnet quality tool (C# console)
│       ├── Quality.Cli.csproj      # PackAsTool=true
│       ├── Commands/               # one class per phase/check
│       └── Program.cs              # System.CommandLine + Spectre.Console
├── src/                            # sample solution: 1 lib + 1 api
├── tests/
│   ├── UnitTests/                  # xUnit + coverlet
│   └── ArchitectureTests/          # NetArchTest rules as xUnit cases
├── docs/
│   ├── practices.md                # best-practices catalog (one entry per check)
│   └── opt-out-guide.md
├── AGENTS.md / CLAUDE.md
└── README.md
```

## Build-time enforcement (the Roslyn layer)

This is where ~70% of the framework's rules live. Everything fires both in the
IDE and at `dotnet build` time.

### `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors></WarningsNotAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <AnalysisLevel>latest-all</AnalysisLevel>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RestoreLockedMode Condition="'$(ContinuousIntegrationBuild)'=='true'">true</RestoreLockedMode>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DeterministicSourcePaths>true</DeterministicSourcePaths>
  </PropertyGroup>
</Project>
```

`AnalysisMode=AllEnabledByDefault` (not `All`) is Microsoft's curated strict set —
gives ~95% of value without pedantic noise (e.g. CA1303 localization).
Individual rules can be promoted/demoted in `.editorconfig`.

### Analyzer packages (centralized in `Directory.Packages.props`)

| Package | Role |
|---|---|
| `Microsoft.CodeAnalysis.NetAnalyzers` (SDK built-in) | base correctness (CAxxxx) |
| `StyleCop.Analyzers` | style consistency |
| `SonarAnalyzer.CSharp` | bug patterns + code smells |
| `Roslynator.Analyzers` | refactoring + dead-code suggestions |
| `Meziantou.Analyzer` | async correctness, MA0026 (TODO ban), pragma justification |
| `SecurityCodeScan.VS2019` | OWASP/injection patterns |
| `Microsoft.VisualStudio.Threading.Analyzers` | async/floating-task detection |
| `IDisposableAnalyzers` | resource leaks |
| `ErrorProne.NET.CoreAnalyzers` | misc correctness |
| `coverlet.msbuild` | coverage with threshold attrs |
| `SerilogAnalyzer` | **auto-attached if Serilog detected** in `*.csproj` |

### `.editorconfig` severities

Every rule defaults to `error`. Opt-out is the .NET-native gesture of editing
`.editorconfig` to downgrade a rule severity, scoped per-file or per-folder.

### Structured-logging enforcement (pinned at `error`)

| Rule | What it catches |
|---|---|
| **CA2254** | Interpolated/dynamic message templates (e.g. `$"User {id} logged in"`) — the single most common structured-logging mistake. |
| **CA1727** | Placeholder naming consistency (PascalCase). |
| **CA1848** | Allocations on hot paths — recommends `[LoggerMessage]` source generators. |
| **CA2017** | Mismatched count of placeholders vs arguments. |
| **CA1873** | Avoid `.ToString()` materialization when level may be filtered. |

`dotnet quality install` inspects `*.csproj`; if `Serilog.*` is referenced,
`SerilogAnalyzer` is added to `Directory.Packages.props` automatically.

### Maps to TS phases

| TS Phase | Covered by Roslyn? |
|---|---|
| Phase 2 (formatting) | Mostly — `dotnet format --verify-no-changes` thin hook |
| Phase 3 (dead code) | Yes — IDE0051/52/60, CA1812, IDE0005 |
| Phase 5 (most static analysis) | Yes — Sonar + Sec + Meziantou + Roslynator |
| Phase 6 (type safety) | Fully — Nullable + WaE *is* the 100% type coverage equivalent |
| Phase 7 (architecture) | Yes — NetArchTest as xUnit cases |
| Phase 8 (build) | Yes — `dotnet build` runs everything above |
| Phase 9 (tests + coverage) | Yes — coverlet + threshold |

## Pre-commit hook catalog (what Roslyn can't see)

Ten-phase shape preserved for familiarity. ~15 hooks total.

### Phase 1 — Safety & syntax (language-agnostic)
- `trailing-whitespace`, `end-of-file-fixer`
- `check-json`, `check-yaml`, `check-toml`, `check-xml` (csproj/props/targets)
- `check-merge-conflict`, `check-case-conflict`
- `no-commit-to-main`
- `gitleaks` (upstream's official pre-commit repo)
- TODO/FIXME ban for non-`.cs` files (md/sh/yml); `.cs` covered by MA0026

### Phase 2 — Formatting & schema
- `dotnet-format-verify` → `dotnet format --verify-no-changes --severity error`
- `editorconfig-checker` (catches files Roslyn doesn't process)
- `ef-migrations-drift` → enabled only if `Microsoft.EntityFrameworkCore.Design`
  referenced; runs `dotnet ef migrations has-pending-model-changes`

### Phase 3 — Dead code
- `unused-nuget-packages` → `dotnet-outdated` + custom check for `<ProjectReference>`
  entries unused at any callsite

### Phase 4 — Dependency & supply-chain audits
- `dotnet-list-vulnerable` → `dotnet list package --vulnerable --include-transitive`
- `osv-scan` (works on `packages.lock.json`)
- `trivy-fs` (filesystem scan, MEDIUM+)
- `nuget-license-check` (copyleft detection — denies GPL/AGPL transitive deps)
- `lockfile-integrity` → `dotnet restore --locked-mode` verifies `packages.lock.json`

### Phase 5 — Static analysis (the residual)
- `max-lines-per-file` (custom; 400 LOC default)
- `bypass-directive-check` → bans `#pragma warning disable` and `[SuppressMessage]`
  without an adjacent `// Justification:` comment (non-empty)
- `semgrep-arch` → cross-project boundary violations (Domain → Infrastructure, etc.)
- `semgrep-security` → patterns Roslyn doesn't catch (deserialization, reflection,
  `Process.Start` with user input)
- `semgrep-logging` → **sensitive-data leakage in log calls**:
  - Pattern-matches `$LOGGER.Log*` calls where the template or any argument
    contains identifiers matching `(?i)(password|secret|token|apikey|api_key|
    bearer|authorization|credit.?card|ssn|pii)`
  - Bans `Console.WriteLine` / `Console.Error.WriteLine` outside `Program.cs`
    and `tools/Quality.Cli/`
- `env-exhaustiveness` → `appsettings.json` keys ↔ `IOptions<T>` strongly-typed
  config classes ↔ `.env.example`

### Phase 6 — Type safety
Fully covered by build. No hook.

### Phase 7 — Module architecture
- `jscpd-csharp` → cross-file duplication (jscpd supports C#); 0% threshold
- NetArchTest rules execute as part of Phase 9 `dotnet test`

### Phase 8 — Build
- `dotnet-build-locked` → `dotnet build --no-restore -warnaserror` with
  locked-mode restore

### Phase 9 — Tests + coverage
- `dotnet-test` → `dotnet test` with coverlet thresholds (line/branch ≥ 90%,
  configurable in `.quality.toml`)
- Architecture tests live in `tests/ArchitectureTests/` and run in the same
  `dotnet test` invocation

### Phase 10 — E2E (deferred; docs-only in v1)
- Documented opt-in extension: `playwright-dotnet` hook auto-starts the API
  project under test, runs `Microsoft.Playwright.NUnit` / xUnit fixture.

### Threshold defaults (strict tier; all overridable in `.quality.toml`)

| Metric | Default |
|---|---|
| Line/branch coverage | 90% |
| Max file length | 400 LOC |
| Duplication | 0% |
| Vulnerable deps | 0 (any severity) |
| Roslyn warnings | 0 (errors) |
| TODOs in `.cs` | 0 (MA0026) |
| Architecture violations | 0 |

## Per-check opt-out mechanism

Three layers, each idiomatic to where the check lives.

### Layer A — Roslyn rules: `.editorconfig`

```ini
[*.cs]
# Default: every analyzer rule is error.
# Opt-out: downgrade severity, scoped per-file via path-specific section.
dotnet_diagnostic.CA1303.severity = suggestion
```

### Layer B — Pre-commit hooks: `.quality.toml`

```toml
# Strict by default; flip enabled = false to disable.
# Every disable MUST carry a non-empty `reason`.
# Empty reason = config-validation hook fails.

[phase4]
trivy_fs        = { enabled = true, severity = "MEDIUM" }
license_check   = { enabled = true, denylist = ["GPL-3.0", "AGPL-3.0"] }

[phase5]
max_lines       = { enabled = true, threshold = 400 }
semgrep_logging = { enabled = true }

[phase9]
coverage        = { enabled = true, line = 90, branch = 90 }

# Example disable:
# [phase4]
# trivy_fs = { enabled = false, reason = "Pipeline migration in progress; tracked in ISSUE-1234, owner @alice, review 2026-07-01" }
```

A `quality-config` validator hook reads the TOML and refuses to proceed if any
`enabled = false` entry lacks a non-empty `reason`.

### Layer C — Bypass directive enforcement

- `#pragma warning disable CAxxxx` without an adjacent `// Justification: …`
  → hook fails
- `[SuppressMessage(...)]` without `Justification = "…"` (non-empty) → hook fails
- `git commit --no-verify` is unblockable client-side; CI re-runs the identical
  `.pre-commit-config.yaml` so local bypass only delays failure to PR time.

### Reporting

`dotnet quality status` prints a table of every check + enabled/disabled + (if
disabled) the reason and days-since-disabled — making drift visible without
grepping config.

## The `dotnet quality` tool + bootstrap

### Project shape

`tools/Quality.Cli/Quality.Cli.csproj` with `<PackAsTool>true</PackAsTool>`,
distributed as a local `.nupkg` in v1. NuGet publication deferred to v2.

**Dependencies:**
- `System.CommandLine` — argument parsing
- `Spectre.Console` — readable terminal output (tables, colors, progress)
- `Tomlyn` — read `.quality.toml`
- `LibGit2Sharp` — git staged-files inspection without shelling out
- `Microsoft.Build.Locator` + `Microsoft.Build` — load MSBuild projects for
  hooks like `unused-nuget-packages`, `ef-migrations-drift`, `SerilogAnalyzer`
  auto-attach

### Command surface

```
dotnet quality install            # writes .editorconfig, Directory.Build.props,
                                  # .pre-commit-config.yaml, etc.
dotnet quality fmt                # dotnet format + verify
dotnet quality check <phase|all>  # run one phase or everything
dotnet quality check max-lines    # run a single named check
dotnet quality status             # table: check / enabled / reason / days-off
dotnet quality pr-check           # everything (mirrors AVM ./avm pr-check)
dotnet quality doctor             # diagnose: SDK ver, tools restored,
                                  # pre-commit installed, docker present
```

### Bootstrap flow

`install.sh` (macOS/Linux) + `install.ps1` (Windows) — ~20 lines each, identical logic:

1. Verify `dotnet --version` matches `global.json` (call `dotnet-install` if missing)
2. `dotnet new tool-manifest --force` if `.config/dotnet-tools.json` absent
3. `dotnet tool install --local dotnet-quality`
4. `dotnet tool restore`
5. `dotnet quality install` — does the rest in cross-platform C#

Everything past step 5 is platform-neutral .NET code: file copies, MSBuild
project edits, git config, pre-commit hook installation.

### Pre-commit config example (uniform shape)

```yaml
- id: max-lines
  name: phase5 — max lines per file
  entry: dotnet quality check max-lines
  language: system
  pass_filenames: false
  always_run: true

- repo: https://github.com/gitleaks/gitleaks
  rev: v8.21.0
  hooks:
    - id: gitleaks   # upstream's official cross-platform entry
```

### Testability

Because hooks are C# code, they get unit-tested in `tests/UnitTests/Quality.Cli.Tests/`
like normal code — a property the bash scripts in the TS template don't have.

## CI integration

Single `.github/workflows/quality.yml`; same `dotnet quality pr-check` devs run
locally — no CI-only drift.

```yaml
jobs:
  quality-native:        # primary path — fast, uses local dotnet tools
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      - run: dotnet tool restore
      - uses: pre-commit/action@v3.0.1
      - run: dotnet quality pr-check

  quality-docker:        # hermetic path — pinned image, slower
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4
      - run: docker run --rm -v "$PWD:/repo" -w /repo quality-tools:pinned dotnet quality pr-check
```

`docker/Dockerfile.quality` — pinned image with SDK from `global.json`, tool
manifest restored, external scanners (gitleaks/trivy/osv/semgrep) at pinned
versions. Built and published from the template repo itself, tagged by template
version.

**Cache strategy:** NuGet + `~/.dotnet/tools` keyed by `dotnet-tools.json` hash;
pre-commit cache keyed by `.pre-commit-config.yaml` hash. Cold CI ~3–4 min;
warm ~45s.

**Cross-OS CI:** optional matrix knob runs the same workflow on `windows-latest`
and `macos-latest` to catch platform-specific bugs in `dotnet quality` itself.

**Bypass loophole closed:** `git commit --no-verify` locally is overridden by CI
running the identical hook set via `pre-commit/action@v3.0.1`.

## Scope: v1 vs deferred

### IN — v1 ships with

1. Repo skeleton (`Directory.Build.props`, `Directory.Packages.props`,
   `.editorconfig`, `global.json`, `.config/dotnet-tools.json`, sample
   `src/` + `tests/`)
2. `dotnet quality` CLI (`tools/Quality.Cli/`) with commands: `install`, `fmt`,
   `check`, `pr-check`, `status`, `doctor`. Packed as `dotnet tool`,
   distributed as a local `.nupkg`.
3. Build-time enforcement: nullable on, warnings-as-errors,
   `AnalysisLevel=latest-all` + `AnalysisMode=AllEnabledByDefault`, all 8
   analyzer NuGets wired centrally.
4. ~15 pre-commit hooks from the catalog above, all 10 phase labels present.
5. `.quality.toml` opt-out config + bypass-directive enforcement + justification
   requirement.
6. Bootstrap scripts: `install.sh` + `install.ps1` (~20 lines each); rest in C#.
7. CI: `quality.yml` with native + docker jobs; pinned `Dockerfile.quality`.
8. Sample architecture tests (`tests/ArchitectureTests/`) using NetArchTest —
   3–4 example rules (no cyclic deps, domain → infrastructure ban, etc.).
9. Documentation: `README.md`, `docs/practices.md` (one entry per check),
   `docs/opt-out-guide.md`, `AGENTS.md`/`CLAUDE.md`.
10. Unit tests for `dotnet quality` itself in `tests/UnitTests/`.
11. **Structured-logging enforcement**: CA2254 / CA1727 / CA1848 / CA2017 /
    CA1873 at error severity; `SerilogAnalyzer` auto-attach when Serilog
    detected; `semgrep-logging` hook for sensitive-data leakage +
    `Console.Write*` ban.

### OUT — deferred to later specs

- NuGet publishing of `dotnet-quality` as a public tool (v2).
- `dotnet new` template package (v2).
- Phase 10 E2E (Playwright .NET) — opt-in extension, docs only in v1.
- Auto-baseline / ratchet mode.
- Full EF Core migration drift hook polish (code path present, enablement only
  when EF Core detected).
- Azure DevOps / GitLab CI templates.
- Visual Studio / Rider config injection (`.vsconfig`, `.idea/`) — `.editorconfig`
  covers both.
- `AnalysisMode=All` (pedantic CA rules like CA1303 localization).
- Telemetry.

### Out-of-scope by design (not deferred)

- Opinionated app architecture (DDD, vertical slices, etc.).
- Logging/observability defaults beyond `ILogger` rules.
- Authentication scaffolding.
- Anything prescribing business code shape.

## References

- Source template (TS): `myprojectclone-typescript/.pre-commit-config.yaml`
- Related AVM workflow: `terraform-azurerm-avm-res-compute-virtualmachine/avm`
- Roslyn AnalysisMode values: Microsoft Docs — "Overview of .NET source code analysis"
- NetArchTest: <https://github.com/BenMorris/NetArchTest>
- Meziantou.Analyzer rules: <https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules.md>
- SerilogAnalyzer rules: <https://github.com/Suchiman/SerilogAnalyzer>
- Pre-commit framework: <https://pre-commit.com>
