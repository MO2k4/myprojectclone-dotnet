# myprojectclone-dotnet

Strict-by-default quality framework template for ASP.NET Core solutions —
.NET counterpart to [`myprojectclone-typescript`](https://github.com/MO2k4/myprojectclone-typescript).

## Status

**Pre-implementation.** Design approved; implementation plan pending.

See [`docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md`](docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md)
for the full design.

## What this will ship

- Build-time enforcement via `Directory.Build.props` + `.editorconfig` +
  Roslyn analyzer NuGets (nullable, warnings-as-errors, `AnalysisMode=AllEnabledByDefault`).
- A `dotnet quality` CLI tool (cross-platform C#) replacing bash hook scripts.
- ~15 pre-commit hooks across 10 phases — only the checks Roslyn can't see
  (secrets, supply-chain, duplication, architecture, sensitive-data logging,
  env exhaustiveness, license compliance).
- Hybrid delivery: GitHub template for greenfield + `install.sh`/`install.ps1`
  for retrofit into existing solutions.
- Strict defaults with documented per-check opt-out — every disable carries a
  required justification.
- CI: GitHub Actions with native + Docker hermetic jobs.

## Quality stance

Same as the TypeScript template:

> **`git commit --no-verify` is never used** unless explicitly confirmed.
> **Lowering quality rules is not allowed.** Thresholds must never be relaxed
> to make a check pass. Fix the code, not the rule.
