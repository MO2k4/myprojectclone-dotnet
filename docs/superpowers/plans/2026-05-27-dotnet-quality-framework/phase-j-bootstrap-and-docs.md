# Phase J — Bootstrap + docs + handoff — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase J — Bootstrap + docs + handoff

### Task 28: `install.sh` + `install.ps1`

**Files:**
- Create: `install.sh`
- Create: `install.ps1`

- [ ] **Step 1: `install.sh`** (~20 lines, spec §"Bootstrap flow").

```bash
#!/usr/bin/env bash
set -euo pipefail

required_sdk=$(jq -r '.sdk.version' global.json)
if ! command -v dotnet >/dev/null || [ "$(dotnet --version)" != "$required_sdk" ]; then
  echo "Installing .NET SDK $required_sdk via dotnet-install..."
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version "$required_sdk"
  export PATH="$HOME/.dotnet:$PATH"
fi

if [ ! -f .config/dotnet-tools.json ]; then
  dotnet new tool-manifest --force
fi

dotnet tool install --local dotnet-quality --add-source ./artifacts 2>/dev/null || true
dotnet tool restore
dotnet quality install --into "$(pwd)"
```

- [ ] **Step 2: `install.ps1`** — equivalent logic.

```powershell
$ErrorActionPreference = 'Stop'

$required = (Get-Content global.json | ConvertFrom-Json).sdk.version
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -or (dotnet --version) -ne $required) {
    Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
    .\dotnet-install.ps1 -Version $required
    $env:PATH = "$HOME\.dotnet;$env:PATH"
}

if (-not (Test-Path .config/dotnet-tools.json)) {
    dotnet new tool-manifest --force
}

dotnet tool install --local dotnet-quality --add-source .\artifacts 2>$null
dotnet tool restore
dotnet quality install --into (Get-Location)
```

- [ ] **Step 3: Make `install.sh` executable.**

```bash
chmod +x install.sh
```

- [ ] **Step 4: Commit.**

```bash
git add install.sh install.ps1
git commit -m "feat: add cross-platform bootstrap scripts (~20 lines each)"
```

### Task 29: `docs/practices.md` + `docs/opt-out-guide.md`

**Files:**
- Create: `docs/practices.md`
- Create: `docs/opt-out-guide.md`

- [ ] **Step 1: `docs/practices.md`** — one entry per check. Use this template per entry:

```markdown
## <check-id>

**Phase:** <N>
**What it catches:** <one sentence>
**Why:** <reason it exists>
**Enabled by default:** yes
**Where it lives:** <.editorconfig | tools/Quality.Cli | semgrep | external repo>
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#<check-id>)
```

Populate one section for every checked item:
`CA2254`, `CA1727`, `CA1848`, `CA2017`, `CA1873`, `MA0026`, `max-lines`, `bypass-directive-check`, `env-exhaustiveness`, `unused-nuget-packages`, `ef-migrations-drift`, `lockfile-integrity`, `dotnet-list-vulnerable`, `osv-scanner`, `trivy`, `semgrep-arch`, `semgrep-security`, `semgrep-logging`, `jscpd`, `dotnet-build-locked`, `dotnet-test`.

- [ ] **Step 2: `docs/opt-out-guide.md`** — three sections (one per opt-out layer from spec lines 254–296). For each layer, show the exact syntax + the required `Justification:` shape + a worked example with `ISSUE-1234, owner @alice, review 2026-07-01`.

- [ ] **Step 3: Commit.**

```bash
git add docs/practices.md docs/opt-out-guide.md
git commit -m "docs: add practices catalog + opt-out guide"
```

### Task 30: `AGENTS.md` / `CLAUDE.md` + README update

**Files:**
- Create: `AGENTS.md`
- Create: `CLAUDE.md` (or symlink to `AGENTS.md` — confirm preference; ship both for now)
- Modify: `README.md`

- [ ] **Step 1: `AGENTS.md`** — mirror the TS template's stance + quick-reference commands.

```markdown
# AGENTS.md — myprojectclone-dotnet

Strict-by-default .NET quality framework template.

## Hard rules
- `git commit --no-verify` is never used unless the user explicitly confirms it in the conversation. When used, the commit message must state hooks were bypassed and why.
- **Lowering quality rules is not allowed.** Thresholds and severities must never be relaxed to make a check pass. Fix the code, not the rule.

## Commands
- `dotnet quality doctor` — diagnose toolchain.
- `dotnet quality fmt` — verify formatting.
- `dotnet quality check <id|all>` — run one or every check.
- `dotnet quality pr-check` — fmt + check + build + test (mirrors CI).
- `dotnet quality status` — table of every check + reason if disabled.
- `pre-commit run --all-files` — full hook pipeline.

## Structure
See [`docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md`](docs/superpowers/specs/2026-05-27-dotnet-quality-framework-design.md) for the design and [`docs/practices.md`](docs/practices.md) for per-check rationale.
```

- [ ] **Step 2: `CLAUDE.md`** — copy of `AGENTS.md` until preference is confirmed.

```bash
cp AGENTS.md CLAUDE.md
```

- [ ] **Step 3: Update `README.md`** — replace the "Status: pre-implementation" stanza with the quick-start.

```markdown
## Quick start (greenfield)
Click **Use this template** on GitHub, then:
```bash
./install.sh           # or .\install.ps1 on Windows
dotnet quality doctor
dotnet quality pr-check
```

## Quick start (retrofit existing repo)
```bash
curl -sSL https://raw.githubusercontent.com/<owner>/myprojectclone-dotnet/main/install.sh | bash
```

## Status
v1 implementation complete. See [`docs/practices.md`](docs/practices.md) for the full check catalog.
```

- [ ] **Step 4: Commit.**

```bash
git add AGENTS.md CLAUDE.md README.md
git commit -m "docs: add AGENTS/CLAUDE guidance + README quick-start"
```

### Task 31: Final smoke test + version tag

- [ ] **Step 1: Full local verification.**

```bash
dotnet tool restore
dotnet build --no-restore -warnaserror
dotnet test
pre-commit run --all-files
dotnet quality pr-check
```

Expected: every command exits 0.

- [ ] **Step 2: Tag the release.**

```bash
git tag -a v0.1.0 -m "v0.1.0: initial .NET quality framework"
```

- [ ] **Step 3: (Manual)** Push to remote and confirm CI green:

```bash
git push origin main --tags
```

Expected: `quality-native` and `quality-docker` GitHub Actions jobs both succeed.
