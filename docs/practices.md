# Practices — check catalog

One entry per check enforced by this template. Each entry states what the check
catches, why it exists, where it runs from, and where to find the opt-out
syntax if a legitimate disable is needed.

The opt-out path is intentionally narrow: every disable carries a justification
(see [`opt-out-guide.md`](opt-out-guide.md)). Lowering quality rules is not a
shortcut for failing checks — fix the code, not the rule.

---

## CA2254

**Phase:** Roslyn (build-time)
**What it catches:** non-literal logger message templates (`logger.LogInformation($"...")` or string concatenation in the template slot).
**Why:** prevents template-cardinality blowups in structured log sinks and keeps templates greppable.
**Enabled by default:** yes
**Where it lives:** `.editorconfig`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ca2254)

## CA1727

**Phase:** Roslyn (build-time)
**What it catches:** logger placeholder names that are not PascalCase (e.g. `{userId}` instead of `{UserId}`).
**Why:** standardises placeholder names so dashboards and queries don't fork between casing styles.
**Enabled by default:** yes
**Where it lives:** `.editorconfig`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ca1727)

## CA1848

**Phase:** Roslyn (build-time)
**What it catches:** logger callsites that don't use `LoggerMessage` source generators.
**Why:** `LoggerMessage`-generated delegates avoid boxing and template parsing on the hot path.
**Enabled by default:** yes
**Where it lives:** `.editorconfig`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ca1848)

## CA2017

**Phase:** Roslyn (build-time)
**What it catches:** mismatch between the number of placeholders in the template and the number of arguments passed.
**Why:** silent arg/placeholder mismatch hides production logs at the worst time.
**Enabled by default:** yes
**Where it lives:** `.editorconfig`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ca2017)

## CA1873

**Phase:** Roslyn (build-time)
**What it catches:** eager `.ToString()` arguments to logger calls that may be filtered by level.
**Why:** prevents expensive string materialisation at log-levels that get dropped.
**Enabled by default:** yes
**Where it lives:** `.editorconfig`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ca1873)

## MA0026

**Phase:** Roslyn (build-time)
**What it catches:** `// TODO` / `// FIXME` / `// HACK` comments in `.cs` files.
**Why:** TODOs accumulate in code where they're invisible to triage; track them as issues instead.
**Enabled by default:** yes
**Where it lives:** `.editorconfig` (Meziantou.Analyzer)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ma0026)

## gitleaks

**Phase:** 1 (safety + syntax)
**What it catches:** secrets committed to the repo — API keys, tokens, private keys.
**Why:** removing secrets from history is costly; refusing them client-side is free.
**Enabled by default:** yes
**Where it lives:** `.pre-commit-config.yaml` (external — `github.com/gitleaks/gitleaks`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#gitleaks)

## max-lines

**Phase:** 5 (residual static analysis)
**What it catches:** `.cs` files over the configured LOC threshold (default 400).
**Why:** long files signal missing extraction points and slow code review.
**Enabled by default:** yes (threshold 400)
**Where it lives:** `tools/Quality.Cli` → `Checks/MaxLinesCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#max-lines)

## bypass-directive-check

**Phase:** 5 (residual static analysis)
**What it catches:** `#pragma warning disable` directives and `[SuppressMessage]` attributes without an adjacent `// Justification:` line or non-empty `Justification = "…"` argument.
**Why:** bypasses without justification rot — six months later nobody knows why the rule was disabled.
**Enabled by default:** yes
**Where it lives:** `tools/Quality.Cli` → `Checks/BypassDirectiveCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#bypass-directive-check)

## env-exhaustiveness

**Phase:** 5 (residual static analysis)
**What it catches:** drift between `appsettings.json` keys, `IOptions<T>` POCO properties, and `.env.example`. Any of the three missing a key the others have fails.
**Why:** missing config keys surface as `NullReferenceException` in prod instead of at startup.
**Enabled by default:** yes
**Where it lives:** `tools/Quality.Cli` → `Checks/EnvExhaustivenessCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#env-exhaustiveness)

## unused-nuget-packages

**Phase:** 3 (dead code)
**What it catches:** `<PackageReference>` entries with no callsite in the project that declares them.
**Why:** unused packages bloat the dependency graph and the supply-chain attack surface.
**Enabled by default:** yes
**Where it lives:** `tools/Quality.Cli` → `Checks/UnusedNuGetPackagesCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#unused-nuget-packages)

## ef-migrations-drift

**Phase:** 2 (formatting + schema)
**What it catches:** model classes that no longer match the latest migration. Conditional on the solution referencing `Microsoft.EntityFrameworkCore.Design`.
**Why:** undetected drift ships a broken migration to the next environment.
**Enabled by default:** yes (skipped automatically when EF Design is not referenced)
**Where it lives:** `tools/Quality.Cli` → `Checks/EfMigrationsDriftCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#ef-migrations-drift)

## lockfile-integrity

**Phase:** 4 (dependency + supply chain)
**What it catches:** `packages.lock.json` drift — `dotnet restore --locked-mode` fails when the lockfile and `.csproj` graph diverge.
**Why:** lockfile drift means the build is non-reproducible across machines and CI.
**Enabled by default:** yes
**Where it lives:** `tools/Quality.Cli` → `Checks/LockfileIntegrityCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#lockfile-integrity)

## dotnet-list-vulnerable

**Phase:** 4 (dependency + supply chain)
**What it catches:** vulnerable transitive NuGet dependencies reported by `dotnet list package --vulnerable --include-transitive`.
**Why:** known-CVE deps in the graph are the cheapest exploit vector to close.
**Enabled by default:** yes
**Where it lives:** `.pre-commit-config.yaml` (local hook)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#dotnet-list-vulnerable)

## license-check

**Phase:** 4 (dependency + supply chain)
**What it catches:** NuGet dependencies licensed under the configured denylist (default GPL-3.0, AGPL-3.0).
**Why:** copyleft licences in the graph create downstream redistribution obligations the consumer may not accept.
**Enabled by default:** yes
**Where it lives:** `tools/Quality.Cli` → `Checks/LicenseCheck.cs`
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#license-check)

## osv-scanner

**Phase:** 4 (dependency + supply chain)
**What it catches:** vulnerabilities in `packages.lock.json` matched against the OSV database.
**Why:** complements `dotnet list --vulnerable` with the cross-ecosystem OSV feed.
**Enabled by default:** yes
**Where it lives:** `.pre-commit-config.yaml` (external — `github.com/google/osv-scanner`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#osv-scanner)

## trivy

**Phase:** 4 (dependency + supply chain)
**What it catches:** filesystem-level vulnerabilities (CVEs, misconfigs) at MEDIUM severity and above.
**Why:** second opinion on the dependency graph plus catches secret/IaC issues `dotnet list` doesn't see.
**Enabled by default:** yes (MEDIUM+)
**Where it lives:** `.pre-commit-config.yaml` (local hook invoking `trivy fs`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#trivy)

## semgrep-arch

**Phase:** 5 (residual static analysis)
**What it catches:** Domain → Infrastructure references and other layering violations Roslyn can't express.
**Why:** keeps the dependency direction one-way so business logic stays testable in isolation.
**Enabled by default:** yes
**Where it lives:** `.semgrep/arch.yml` (invoked via `.pre-commit-config.yaml`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#semgrep-arch)

## semgrep-security

**Phase:** 5 (residual static analysis)
**What it catches:** unsafe deserialization, reflection abuse, and `Process.Start` with user-controlled input.
**Why:** patterns Roslyn doesn't flag but that map directly to RCE / injection bugs in past incidents.
**Enabled by default:** yes
**Where it lives:** `.semgrep/security.yml` (invoked via `.pre-commit-config.yaml`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#semgrep-security)

## semgrep-logging

**Phase:** 5 (residual static analysis)
**What it catches:** sensitive-data leaks in log calls and `Console.Write*` usage outside `Program.cs`.
**Why:** PII in logs is a recurring compliance failure; `Console.Write*` bypasses the structured-logging pipeline.
**Enabled by default:** yes
**Where it lives:** `.semgrep/logging.yml` (invoked via `.pre-commit-config.yaml`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#semgrep-logging)

## jscpd

**Phase:** 7 (module architecture)
**What it catches:** copy-paste duplication across `.cs` files in `src/`.
**Why:** duplicated logic drifts independently and multiplies the fix surface for any bug.
**Enabled by default:** yes (threshold 0)
**Where it lives:** `.pre-commit-config.yaml` (local hook invoking `jscpd`)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#jscpd)

## dotnet-build-locked

**Phase:** 8 (build)
**What it catches:** any compile error or analyzer warning — `--locked-mode` restore + `-warnaserror`.
**Why:** warnings-as-errors keeps the analyzer feedback loop tight; locked-mode ensures the build matches the lockfile.
**Enabled by default:** yes
**Where it lives:** `.pre-commit-config.yaml` (local hook)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#dotnet-build-locked)

## dotnet-test

**Phase:** 9 (tests + coverage)
**What it catches:** test failures and coverage below the configured line/branch threshold (default 90% each).
**Why:** untested code is undefined behaviour with a smile painted on.
**Enabled by default:** yes (line 90, branch 90)
**Where it lives:** `.pre-commit-config.yaml` (local hook) + `Directory.Build.targets` (threshold target)
**How to legitimately disable:** see [opt-out-guide.md](opt-out-guide.md#dotnet-test)
