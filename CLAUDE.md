# AGENTS.md — myprojectclone-dotnet

Strict-by-default .NET quality framework template.

## Hard rules
- `git commit --no-verify` is never used unless the user explicitly confirms it in the conversation. When used, the commit message must state hooks were bypassed and why.
- **Lowering quality rules is not allowed.** Thresholds and severities must never be relaxed to make a check pass. Fix the code, not the rule.

## Commands
- `mise install` — install pinned scanners (semgrep, pre-commit, jscpd, trivy) on a fresh clone.
- `dotnet quality doctor` — diagnose toolchain.
- `dotnet quality fmt` — verify formatting.
- `dotnet quality check <id|all>` — run one or every check.
- `dotnet quality pr-check` — fmt + check + build + test (mirrors CI).
- `dotnet quality status` — table of every check + reason if disabled.
- `pre-commit run --all-files` — full hook pipeline.

## Structure
See [`docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md`](docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md) for the design and [`docs/practices.md`](docs/practices.md) for per-check rationale.
