# Phase F — External scanner config — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase F — External scanner config

### Task 20: `.gitleaks.toml` + semgrep rule files

**Files:**
- Create: `.gitleaks.toml`
- Create: `.semgrep/arch.yml`
- Create: `.semgrep/security.yml`
- Create: `.semgrep/logging.yml`

- [ ] **Step 1: `.gitleaks.toml`** — extend the default config and allow `.env.example` placeholders.

```toml
title = "myprojectclone-dotnet gitleaks config"

[extend]
useDefault = true

[allowlist]
description = "non-secret placeholders"
paths = ['''\.env\.example$''', '''docs/.*''']
regexes = ['''EXAMPLE_TOKEN_PLACEHOLDER''']
```

- [ ] **Step 2: `.semgrep/arch.yml`** — Domain → Infrastructure boundary ban (spec §"Phase 5").

```yaml
rules:
  - id: domain-must-not-reference-infrastructure
    languages: [csharp]
    message: Domain layer must not depend on Infrastructure namespaces.
    severity: ERROR
    paths:
      include:
        - "src/**/Domain/**"
    pattern: |
      using $X.Infrastructure.$Y;
```

- [ ] **Step 3: `.semgrep/security.yml`** — three high-value patterns Roslyn does not consistently catch.

```yaml
rules:
  - id: deserialize-untrusted
    languages: [csharp]
    message: BinaryFormatter / SoapFormatter are insecure; use System.Text.Json.
    severity: ERROR
    pattern-either:
      - pattern: new BinaryFormatter()
      - pattern: new SoapFormatter()

  - id: process-start-user-input
    languages: [csharp]
    message: Process.Start with user-controlled input is a command injection risk.
    severity: ERROR
    pattern: Process.Start($USER_INPUT)
    pattern-not: Process.Start("...")

  - id: reflection-from-name
    languages: [csharp]
    message: Type.GetType with user input enables type confusion.
    severity: ERROR
    pattern: Type.GetType($USER_INPUT)
```

- [ ] **Step 4: `.semgrep/logging.yml`** — sensitive-data leak + Console.Write ban (spec lines 210–214).

```yaml
rules:
  - id: sensitive-data-in-log
    languages: [csharp]
    message: |
      Log call references a sensitive identifier (password/token/etc.).
      Redact, hash, or omit before logging.
    severity: ERROR
    pattern-regex: |
      \.Log(Trace|Debug|Information|Warning|Error|Critical)\([^)]*\b(?i:password|secret|token|apikey|api_key|bearer|authorization|credit.?card|ssn|pii)\b

  - id: console-write-outside-allowed
    languages: [csharp]
    message: Console.Write* is banned outside Program.cs and tools/Quality.Cli.
    severity: ERROR
    pattern-either:
      - pattern: Console.WriteLine(...)
      - pattern: Console.Error.WriteLine(...)
      - pattern: Console.Write(...)
    paths:
      exclude:
        - "**/Program.cs"
        - "tools/Quality.Cli/**"
```

- [ ] **Step 5: Smoke-test each.**

```bash
gitleaks detect --config .gitleaks.toml
semgrep --config .semgrep/arch.yml --config .semgrep/security.yml --config .semgrep/logging.yml src/
```

Expected: no findings against the current clean sample.

- [ ] **Step 6: Commit.**

```bash
git add .gitleaks.toml .semgrep
git commit -m "feat: add gitleaks + semgrep rules (arch, security, logging)"
```
