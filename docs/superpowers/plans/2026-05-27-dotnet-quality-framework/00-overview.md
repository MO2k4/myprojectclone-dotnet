# .NET Quality Framework Template — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a strict-by-default .NET quality framework template — Roslyn-native build-time enforcement plus ~15 pre-commit hooks plus a `dotnet quality` CLI — that any ASP.NET Core solution can adopt via GitHub-template clone or `install.sh` / `install.ps1` retrofit.

**Architecture:** Three layers stacked. (1) Build-time Roslyn layer: `Directory.Build.props` + `Directory.Packages.props` + `.editorconfig` + 9 analyzer NuGets covers ~70% of the rules and fires in the IDE and at `dotnet build`. (2) `dotnet quality` CLI — a C# console app packed as a local dotnet tool — implements every custom check in one place. (3) `.pre-commit-config.yaml` invokes the CLI plus upstream scanners (gitleaks, OSV, trivy, semgrep). CI re-runs the identical hook set so `--no-verify` only delays failure to PR time.

**Tech Stack:** .NET 9 SDK, xUnit + coverlet, System.CommandLine, Spectre.Console, Tomlyn, LibGit2Sharp, Microsoft.Build.Locator + Microsoft.Build, NetArchTest, pre-commit framework, gitleaks / OSV / trivy / semgrep / jscpd, GitHub Actions, Docker.

**Spec:** [`docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md`](../../specs/2026-05-27-dotnet-quality-framework-design.md). Plan phases mirror spec sections; deviations from the spec are called out where they occur.

**Suggested incremental shipping points (32 tasks total: 1–17, 17b, 18–31):**
- After Task 6 — strict build infra works on the sample solution (Phase A done).
- After Task 19 — `dotnet quality` CLI is feature-complete and unit-tested (Phases B–E done).
- After Task 25 — pre-commit pipeline runs end-to-end locally plus sample + arch tests (Phases F–H done).
- After Task 31 — template is ready to clone or retrofit; CI green; tagged v0.1.0 (Phases I–J done).


---

## Phase index

| File | Phase | Tasks | Outcome |
|------|-------|-------|---------|
| [`phase-a-foundations.md`](phase-a-foundations.md)               | A | 1–6     | Strict-building sample (SDK pin, props, packages, editorconfig, lockfiles) |
| [`phase-b-cli-scaffolding.md`](phase-b-cli-scaffolding.md)       | B | 7–9     | `dotnet quality` tool project + test project + Spectre wrapper |
| [`phase-c-config-layer.md`](phase-c-config-layer.md)             | C | 10–11   | `.quality.toml` POCO + Tomlyn reader + validator + `status` command |
| [`phase-d-checks.md`](phase-d-checks.md)                         | D | 12–17b  | `ICheck` abstraction + seven CLI checks (max-lines, bypass, env, unused-nuget, ef-drift, lockfile, license) |
| [`phase-e-top-level-commands.md`](phase-e-top-level-commands.md) | E | 18–19   | `fmt` / `check` / `pr-check` / `doctor` / `install` commands |
| [`phase-f-external-scanners.md`](phase-f-external-scanners.md)   | F | 20      | `.gitleaks.toml` + `.semgrep/{arch,security,logging}.yml` |
| [`phase-g-precommit-pipeline.md`](phase-g-precommit-pipeline.md) | G | 21–23   | `.pre-commit-config.yaml` — Phases 1–9, install resource extension |
| [`phase-h-arch-and-sample.md`](phase-h-arch-and-sample.md)       | H | 24–25   | `tests/ArchitectureTests` + sample API IOptions/appsettings/.env wiring |
| [`phase-i-ci.md`](phase-i-ci.md)                                 | I | 26–27   | `docker/Dockerfile.quality` + `.github/workflows/quality.yml` |
| [`phase-j-bootstrap-and-docs.md`](phase-j-bootstrap-and-docs.md) | J | 28–31   | `install.sh` / `install.ps1` + `docs/practices.md` + `docs/opt-out-guide.md` + AGENTS/CLAUDE/README + v0.1.0 tag |

Total: 32 tasks (1–17, 17b, 18–31).

---

## File Structure

Every file the plan creates, with one-line responsibility statements. Built-up incrementally — order matches the task sequence.

### Repo root
- `global.json` — pins SDK to 9.0.x.
- `.editorconfig` — style rules, analyzer severities (default `error`), structured-logging rules pinned.
- `Directory.Build.props` — global MSBuild defaults: nullable on, warnings-as-errors, `AnalysisMode=AllEnabledByDefault`, locked-mode restore in CI.
- `Directory.Packages.props` — central package management; all 9 analyzer NuGets declared once.
- `Directory.Build.targets` — coverage threshold target + lockfile-required target.
- `MyProjectClone.Dotnet.sln` — solution file wiring `src/`, `tests/`, `tools/`.
- `.config/dotnet-tools.json` — local tool manifest: pinned `dotnet-quality` (plus, eventually, `dotnet-outdated-tool` if used).
- `.pre-commit-config.yaml` — ~15 hooks across 10 phases.
- `.gitleaks.toml` — secret scanning config.
- `.quality.toml` — per-check opt-out matrix; `enabled = false` requires `reason`.
- `install.sh` / `install.ps1` — bootstrap (~20 lines each).
- `README.md` — updated with quick-start + status.
- `AGENTS.md` / `CLAUDE.md` — agent guidance, mirrors TS template's stance.

### Build infra extras
- `.semgrep/arch.yml` — Domain → Infrastructure boundary patterns.
- `.semgrep/security.yml` — deserialization, reflection, `Process.Start` with user input.
- `.semgrep/logging.yml` — sensitive-data leak in log calls + `Console.Write*` ban.

### `tools/Quality.Cli/` — the CLI
- `Quality.Cli.csproj` — `PackAsTool=true`, ToolCommandName `quality`, PackageId `dotnet-quality`.
- `Program.cs` — entry point: MSBuildLocator init, root command wiring.
- `Commands/InstallCommand.cs` — writes Build infra + pre-commit config + auto-attaches SerilogAnalyzer when Serilog is referenced.
- `Commands/FmtCommand.cs` — `dotnet format --verify-no-changes` thin wrapper.
- `Commands/CheckCommand.cs` — dispatches to one or all phase/check IDs.
- `Commands/StatusCommand.cs` — Spectre table: check / enabled / reason / days-since-disabled.
- `Commands/PrCheckCommand.cs` — runs every phase.
- `Commands/DoctorCommand.cs` — diagnoses SDK version, tool restore, pre-commit install, Docker presence.
- `Checks/ICheck.cs` — common check abstraction (returns `CheckResult`).
- `Checks/MaxLinesCheck.cs` — flags `.cs` files over the configured LOC threshold.
- `Checks/BypassDirectiveCheck.cs` — bans `#pragma warning disable` / `[SuppressMessage]` without `// Justification:`.
- `Checks/EnvExhaustivenessCheck.cs` — diffs `appsettings.json` keys vs `IOptions<T>` properties vs `.env.example`.
- `Checks/UnusedNuGetPackagesCheck.cs` — detects `<PackageReference>` entries with no callsite.
- `Checks/EfMigrationsDriftCheck.cs` — conditional on `Microsoft.EntityFrameworkCore.Design`; runs `dotnet ef migrations has-pending-model-changes`.
- `Checks/LockfileIntegrityCheck.cs` — runs `dotnet restore --locked-mode` and fails on drift.
- `Config/QualityConfig.cs` — POCO mapping `.quality.toml`.
- `Config/ConfigReader.cs` — Tomlyn deserialization + defaults.
- `Config/ConfigValidator.cs` — enforces `enabled = false` ⇒ non-empty `reason`.
- `Git/StagedFiles.cs` — LibGit2Sharp helper returning the staged-file list.
- `Msbuild/ProjectInspector.cs` — Microsoft.Build wrapper used by install + several checks.
- `Output/Console.cs` — Spectre.Console helpers (tables, headings, error formatting).

### `tests/UnitTests/Quality.Cli.Tests/`
- `Quality.Cli.Tests.csproj` — xUnit + coverlet + reference to Quality.Cli.
- `Checks/MaxLinesCheckTests.cs`, `Checks/BypassDirectiveCheckTests.cs`, etc. — one per check.
- `Config/ConfigReaderTests.cs`, `Config/ConfigValidatorTests.cs`.
- `_fixtures/` — input files used by tests (e.g. `max-lines/too_long.cs`).

### `tests/ArchitectureTests/`
- `ArchitectureTests.csproj` — xUnit + NetArchTest reference.
- `LayeringTests.cs` — Domain must not reference Infrastructure, no cyclic deps, etc.

### Sample solution
- `src/Sample.Lib/Sample.Lib.csproj` + `Class1.cs` — illustrative class library.
- `src/Sample.Api/Sample.Api.csproj` + `Program.cs` — minimal ASP.NET Core API.

### CI
- `docker/Dockerfile.quality` — pinned image with SDK + restored tool manifest + external scanners.
- `.github/workflows/quality.yml` — native and Docker jobs.

### Docs
- `docs/practices.md` — one entry per check explaining the rule + rationale.
- `docs/opt-out-guide.md` — how to legitimately disable a check + the justification template.



## Out-of-scope checklist (spec §"OUT — deferred")

Confirmed deferred to v2, NOT included in this plan:
- NuGet publication of `dotnet-quality`.
- `dotnet new` template package.
- Phase 10 Playwright E2E hook.
- Ratchet/baseline mode.
- Azure DevOps / GitLab CI templates.
- Visual Studio / Rider config injection.
- `AnalysisMode=All` (pedantic CA rules).
- Telemetry.

If any of these surface during execution, do not implement — document the request in a follow-up issue and continue with the plan.
