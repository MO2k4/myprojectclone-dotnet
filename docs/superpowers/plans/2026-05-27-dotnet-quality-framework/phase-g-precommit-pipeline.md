# Phase G — Pre-commit pipeline — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase G — Pre-commit pipeline

### Task 21: Phase 1 hooks (safety + syntax)

**Files:**
- Create: `.pre-commit-config.yaml`

- [ ] **Step 1: Create `.pre-commit-config.yaml`** seeded with Phase 1 only (spec §"Phase 1 — Safety & syntax").

```yaml
fail_fast: true

repos:
  # ─── Phase 1: Safety & syntax ─────────────────────────────────────
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v5.0.0
    hooks:
      - id: trailing-whitespace
      - id: end-of-file-fixer
      - id: check-json
      - id: check-yaml
      - id: check-toml
      - id: check-xml
      - id: check-merge-conflict
      - id: check-case-conflict
      - id: no-commit-to-branch
        args: [--branch, main]

  - repo: https://github.com/gitleaks/gitleaks
    rev: v8.21.0
    hooks:
      - id: gitleaks

  - repo: local
    hooks:
      - id: todo-ban-non-cs
        name: phase1 — TODO/FIXME ban (non-.cs files)
        entry: bash -c 'rg -n "TODO|FIXME|HACK|XXX" --type-add "doc:*.{md,sh,yml,yaml,json,toml}" -t doc && exit 1 || exit 0'
        language: system
        pass_filenames: false
        always_run: true
```

- [ ] **Step 2: Install pre-commit and dry-run.**

```bash
pre-commit install
pre-commit run --all-files
```

Expected: all hooks pass.

- [ ] **Step 3: Commit.**

```bash
git add .pre-commit-config.yaml
git commit -m "feat(hooks): add Phase 1 (safety + syntax + gitleaks)"
```

### Task 22: Phases 2–4 hooks (format / dead code / supply chain)

**Files:**
- Modify: `.pre-commit-config.yaml`

- [ ] **Step 1: Append Phase 2 hooks (formatting + schema).**

```yaml
  # ─── Phase 2: Formatting & schema ────────────────────────────────
  - repo: local
    hooks:
      - id: dotnet-format-verify
        name: phase2 — dotnet format (verify-no-changes)
        entry: dotnet quality fmt
        language: system
        pass_filenames: false
        always_run: true

      - id: ef-migrations-drift
        name: phase2 — EF Core migration drift
        entry: dotnet quality check ef-migrations-drift
        language: system
        pass_filenames: false
        always_run: true

  - repo: https://github.com/editorconfig-checker/editorconfig-checker.python
    rev: 3.0.3
    hooks:
      - id: editorconfig-checker
        name: phase2 — editorconfig-checker (non-cs files)
        args: [-exclude, '(bin|obj|node_modules|packages.lock.json)']
```

- [ ] **Step 2: Append Phase 3 hook.**

```yaml
  # ─── Phase 3: Dead code ──────────────────────────────────────────
  - repo: local
    hooks:
      - id: unused-nuget-packages
        name: phase3 — unused NuGet packages
        entry: dotnet quality check unused-nuget-packages
        language: system
        pass_filenames: false
        always_run: true
```

- [ ] **Step 3: Append Phase 4 hooks.**

```yaml
  # ─── Phase 4: Dependency & supply-chain ──────────────────────────
  - repo: local
    hooks:
      - id: dotnet-list-vulnerable
        name: phase4 — vulnerable transitive deps
        entry: bash -c 'dotnet list package --vulnerable --include-transitive | tee /tmp/vuln.txt; ! grep -E "> [A-Za-z]" /tmp/vuln.txt'
        language: system
        pass_filenames: false
        always_run: true

      - id: lockfile-integrity
        name: phase4 — lockfile integrity (locked-mode restore)
        entry: dotnet quality check lockfile-integrity
        language: system
        pass_filenames: false
        always_run: true

      - id: license-check
        name: phase4 — NuGet license denylist (no GPL/AGPL)
        entry: dotnet quality check license-check
        language: system
        pass_filenames: false
        always_run: true

  - repo: https://github.com/google/osv-scanner
    rev: v1.9.0
    hooks:
      - id: osv-scanner
        args: ["--lockfile=src/Sample.Library/packages.lock.json"]

  - repo: https://github.com/aquasecurity/trivy
    rev: v0.56.2
    hooks:
      - id: trivy
        args: ["fs", "--severity", "MEDIUM,HIGH,CRITICAL", "."]
```

- [ ] **Step 4: Dry-run.** `pre-commit run --all-files`. Expected: all pass on the clean sample.

- [ ] **Step 5: Commit.**

```bash
git add .pre-commit-config.yaml
git commit -m "feat(hooks): add Phases 2–4 (format, dead code, supply chain)"
```

### Task 23: Phases 5, 7–9 hooks (static analysis residual / arch / build / test)

**Files:**
- Modify: `.pre-commit-config.yaml`

- [ ] **Step 1: Append Phase 5 hooks.**

```yaml
  # ─── Phase 5: Static analysis (residual) ─────────────────────────
  - repo: local
    hooks:
      - id: max-lines
        name: phase5 — max lines per file
        entry: dotnet quality check max-lines
        language: system
        pass_filenames: false
        always_run: true

      - id: bypass-directive-check
        name: phase5 — bypass directives require justification
        entry: dotnet quality check bypass-directive-check
        language: system
        pass_filenames: false
        always_run: true

      - id: env-exhaustiveness
        name: phase5 — env-key exhaustiveness
        entry: dotnet quality check env-exhaustiveness
        language: system
        pass_filenames: false
        always_run: true

  - repo: https://github.com/returntocorp/semgrep
    rev: v1.92.0
    hooks:
      - id: semgrep
        name: phase5 — semgrep (arch + security + logging)
        args:
          - --config=.semgrep/arch.yml
          - --config=.semgrep/security.yml
          - --config=.semgrep/logging.yml
          - --error
```

- [ ] **Step 2: Append Phase 7 hook (duplication).**

```yaml
  # ─── Phase 7: Module architecture ────────────────────────────────
  - repo: https://github.com/kucherenko/jscpd
    rev: v4.0.4
    hooks:
      - id: jscpd
        name: phase7 — duplication (jscpd, C#)
        args: ["--threshold", "0", "--reporters", "console", "--pattern", "src/**/*.cs", "."]
```

- [ ] **Step 3: Append Phase 8 + 9 hooks.**

```yaml
  # ─── Phase 8: Build ──────────────────────────────────────────────
  - repo: local
    hooks:
      - id: dotnet-build-locked
        name: phase8 — dotnet build (locked, WaE)
        entry: bash -c 'dotnet restore --locked-mode && dotnet build --no-restore -warnaserror'
        language: system
        pass_filenames: false
        always_run: true

  # ─── Phase 9: Tests + coverage ───────────────────────────────────
      - id: dotnet-test
        name: phase9 — dotnet test + coverage ≥ 90%
        entry: bash -c 'dotnet test --no-build'
        language: system
        pass_filenames: false
        always_run: true
```

- [ ] **Step 4: Dry-run end-to-end.** `pre-commit run --all-files`. Expected: green.

- [ ] **Step 5: Copy the now-stable `.pre-commit-config.yaml` into the tool's resource set** so `dotnet quality install` writes it to retrofitted repos.

```bash
cp .pre-commit-config.yaml tools/Quality.Cli/Resources/.pre-commit-config.yaml
```

Then extend `tools/Quality.Cli/Commands/InstallCommand.cs`:

```csharp
    private static readonly (string Resource, string TargetRelativePath)[] Files =
    [
        ("Quality.Cli.Resources.Directory_Build_props",    "Directory.Build.props"),
        ("Quality.Cli.Resources.Directory_Packages_props", "Directory.Packages.props"),
        ("Quality.Cli.Resources._editorconfig",            ".editorconfig"),
        ("Quality.Cli.Resources._quality_toml",            ".quality.toml"),
        ("Quality.Cli.Resources._pre_commit_config_yaml",  ".pre-commit-config.yaml"),
    ];
```

Add an assertion to `InstallCommandTests` proving the new file lands:

```csharp
Assert.True(File.Exists(Path.Combine(tmp, ".pre-commit-config.yaml")));
```

- [ ] **Step 6: Re-run tests + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add .pre-commit-config.yaml tools/Quality.Cli/Resources/.pre-commit-config.yaml tools/Quality.Cli/Commands/InstallCommand.cs tests/UnitTests/Quality.Cli.Tests/Commands/InstallCommandTests.cs
git commit -m "feat(hooks): add Phases 5, 7–9 + extend install resource set"
```

