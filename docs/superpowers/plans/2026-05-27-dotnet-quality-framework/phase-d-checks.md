# Phase D — Individual checks (each TDD'd) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase D — Individual checks (each TDD’d, each independently invocable)

Common pattern in this phase:
1. Define `ICheck` once (Task 12).
2. For every check thereafter: failing test → implement → passing test → wire under `dotnet quality check <id>` → commit.

### Task 12: `ICheck` abstraction + `MaxLinesCheck`

**Files:**
- Create: `tools/Quality.Cli/Checks/ICheck.cs`
- Create: `tools/Quality.Cli/Checks/MaxLinesCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/MaxLinesCheckTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/max-lines/ok.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/max-lines/too_long.cs`

- [ ] **Step 1: Define `ICheck`.**

```csharp
namespace Quality.Cli.Checks;

public sealed record CheckResult(string Id, bool Ok, IReadOnlyList<string> Findings);

public interface ICheck
{
    string Id { get; }
    CheckResult Run(CheckContext ctx);
}

public sealed record CheckContext(string RepoRoot, Config.QualityConfig Config);
```

- [ ] **Step 2: Create fixtures.** `ok.cs` is a short class (10 lines). `too_long.cs` is generated below 400 lines + 1.

```bash
printf 'namespace Fixtures; public class Ok { public int X; }\n' \
  > tests/UnitTests/Quality.Cli.Tests/_fixtures/max-lines/ok.cs

{ echo 'namespace Fixtures; public class TooLong {';
  for i in $(seq 1 401); do echo "    public int F$i;"; done;
  echo '}'; } > tests/UnitTests/Quality.Cli.Tests/_fixtures/max-lines/too_long.cs
```

- [ ] **Step 3: Write failing test.**

```csharp
using Quality.Cli.Checks;
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Checks;

public class MaxLinesCheckTests
{
    private static string FixDir() =>
        Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines");

    private static CheckContext Ctx() => new(FixDir(), new QualityConfig
    {
        Phase5 = new Phase5 { MaxLines = new MaxLinesEntry { Enabled = true, Threshold = 400 } }
    });

    [Fact]
    public void Flags_files_over_threshold()
    {
        var result = new MaxLinesCheck().Run(Ctx());
        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("too_long.cs"));
        Assert.DoesNotContain(result.Findings, f => f.Contains("ok.cs"));
    }
}
```

- [ ] **Step 4: Run** — expect compile failure.

- [ ] **Step 5: Implement `MaxLinesCheck.cs`.**

```csharp
namespace Quality.Cli.Checks;

public sealed class MaxLinesCheck : ICheck
{
    public string Id => "max-lines";

    public CheckResult Run(CheckContext ctx)
    {
        var threshold = ctx.Config.Phase5.MaxLines.Threshold;
        var findings = new List<string>();
        foreach (var path in Directory.EnumerateFiles(ctx.RepoRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("/bin/") || path.Contains("/obj/")) continue;
            var lineCount = File.ReadLines(path).Count();
            if (lineCount > threshold)
                findings.Add($"{path}: {lineCount} lines (max {threshold})");
        }
        return new CheckResult(Id, findings.Count == 0, findings);
    }
}
```

- [ ] **Step 6: Re-run tests.** Expect PASS.

- [ ] **Step 7: Commit.**

```bash
git add tools/Quality.Cli/Checks tests/UnitTests/Quality.Cli.Tests/Checks/MaxLinesCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/max-lines
git commit -m "feat(cli): add ICheck abstraction + MaxLinesCheck"
```

### Task 13: `BypassDirectiveCheck`

**Files:**
- Create: `tools/Quality.Cli/Checks/BypassDirectiveCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/BypassDirectiveCheckTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/bypass/justified.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/bypass/unjustified.cs`

- [ ] **Step 1: Create fixtures.**

`justified.cs`:
```csharp
namespace Fixtures;

public class Justified
{
    // Justification: external API requires unused parameter.
    #pragma warning disable CA1822
    public void Method(int unused) { }
    #pragma warning restore CA1822
}
```

`unjustified.cs`:
```csharp
namespace Fixtures;

public class Unjustified
{
    #pragma warning disable CA1822
    public void Method(int unused) { }
    #pragma warning restore CA1822
}
```

- [ ] **Step 2: Write failing test.**

```csharp
using Quality.Cli.Checks;
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Checks;

public class BypassDirectiveCheckTests
{
    private static CheckContext Ctx() => new(
        Path.Combine(AppContext.BaseDirectory, "_fixtures", "bypass"),
        new QualityConfig());

    [Fact]
    public void Flags_unjustified_pragma_and_passes_justified_one()
    {
        var result = new BypassDirectiveCheck().Run(Ctx());
        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("unjustified.cs"));
        Assert.DoesNotContain(result.Findings, f => f.Contains("justified.cs"));
    }
}
```

- [ ] **Step 3: Implement.** Allow either `// Justification:` on the prior line OR `Justification = "..."` inside a `[SuppressMessage]` attribute.

```csharp
using System.Text.RegularExpressions;

namespace Quality.Cli.Checks;

public sealed class BypassDirectiveCheck : ICheck
{
    public string Id => "bypass-directive-check";

    private static readonly Regex SuppressMessageWithJustification =
        new(@"\[SuppressMessage\([^)]*Justification\s*=\s*""[^""]+""", RegexOptions.Compiled);

    public CheckResult Run(CheckContext ctx)
    {
        var findings = new List<string>();
        foreach (var path in Directory.EnumerateFiles(ctx.RepoRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains("/bin/") || path.Contains("/obj/")) continue;
            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("#pragma warning disable"))
                {
                    var prev = i > 0 ? lines[i - 1].Trim() : "";
                    if (!prev.StartsWith("// Justification:", StringComparison.Ordinal))
                        findings.Add($"{path}:{i + 1}: #pragma without preceding `// Justification:`");
                }
                if (line.Contains("[SuppressMessage") && !SuppressMessageWithJustification.IsMatch(line))
                    findings.Add($"{path}:{i + 1}: [SuppressMessage] without non-empty Justification");
            }
        }
        return new CheckResult(Id, findings.Count == 0, findings);
    }
}
```

- [ ] **Step 4: Re-run.** Expect PASS.

- [ ] **Step 5: Commit.**

```bash
git add tools/Quality.Cli/Checks/BypassDirectiveCheck.cs tests/UnitTests/Quality.Cli.Tests/Checks/BypassDirectiveCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/bypass
git commit -m "feat(cli): add BypassDirectiveCheck"
```

### Task 14: `EnvExhaustivenessCheck`

**Files:**
- Create: `tools/Quality.Cli/Checks/EnvExhaustivenessCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/EnvExhaustivenessCheckTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/env/{appsettings.json,.env.example,Options.cs}`

- [ ] **Step 1: Write fixtures.** Three files that together represent: a key present in `appsettings.json` and `IOptions<T>` but missing from `.env.example`.

`appsettings.json`:
```json
{ "Database": { "ConnectionString": "x" }, "Auth": { "Issuer": "y" } }
```

`.env.example`:
```
DATABASE__CONNECTIONSTRING=
```

`Options.cs`:
```csharp
namespace Fixtures;

public sealed class DatabaseOptions { public string ConnectionString { get; set; } = ""; }
public sealed class AuthOptions     { public string Issuer { get; set; } = ""; }
```

- [ ] **Step 2: Write failing test.**

```csharp
using Quality.Cli.Checks;
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Checks;

public class EnvExhaustivenessCheckTests
{
    [Fact]
    public void Flags_keys_missing_from_dotenv_example()
    {
        var ctx = new CheckContext(
            Path.Combine(AppContext.BaseDirectory, "_fixtures", "env"),
            new QualityConfig());
        var result = new EnvExhaustivenessCheck().Run(ctx);
        Assert.False(result.Ok);
        Assert.Contains(result.Findings, f => f.Contains("AUTH__ISSUER"));
    }
}
```

- [ ] **Step 3: Implement.** Use System.Text.Json for `appsettings.json`, simple line-parse for `.env.example`; map keys to `SECTION__SUBKEY` env-var form.

```csharp
using System.Text.Json;

namespace Quality.Cli.Checks;

public sealed class EnvExhaustivenessCheck : ICheck
{
    public string Id => "env-exhaustiveness";

    public CheckResult Run(CheckContext ctx)
    {
        var settingsPath = Path.Combine(ctx.RepoRoot, "appsettings.json");
        var envExample   = Path.Combine(ctx.RepoRoot, ".env.example");
        if (!File.Exists(settingsPath) || !File.Exists(envExample))
            return new CheckResult(Id, true, Array.Empty<string>());

        var settingsKeys = FlattenJson(JsonDocument.Parse(File.ReadAllText(settingsPath)).RootElement, "")
            .Select(k => k.Replace(":", "__").ToUpperInvariant())
            .ToHashSet();

        var envKeys = File.ReadAllLines(envExample)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
            .Select(l => l.Split('=', 2)[0].Trim().ToUpperInvariant())
            .ToHashSet();

        var missing = settingsKeys.Except(envKeys)
            .Select(k => $".env.example missing key for `{k}`")
            .ToList();
        return new CheckResult(Id, missing.Count == 0, missing);
    }

    private static IEnumerable<string> FlattenJson(JsonElement el, string prefix)
    {
        if (el.ValueKind != JsonValueKind.Object) { yield return prefix; yield break; }
        foreach (var p in el.EnumerateObject())
            foreach (var k in FlattenJson(p.Value, prefix.Length == 0 ? p.Name : $"{prefix}:{p.Name}"))
                yield return k;
    }
}
```

> Note: this is the keys-vs-dotenv side. The IOptions<T> side adds value when keys exist in `.env.example` but no `IOptions<T>` consumes them; defer that scope to a follow-up if pressed for time.

- [ ] **Step 4: Re-run + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add tools/Quality.Cli/Checks/EnvExhaustivenessCheck.cs tests/UnitTests/Quality.Cli.Tests/Checks/EnvExhaustivenessCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/env
git commit -m "feat(cli): add EnvExhaustivenessCheck (appsettings ↔ .env.example)"
```

### Task 15: `UnusedNuGetPackagesCheck`

**Files:**
- Create: `tools/Quality.Cli/Msbuild/ProjectInspector.cs`
- Create: `tools/Quality.Cli/Checks/UnusedNuGetPackagesCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/UnusedNuGetPackagesCheckTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/unused-nuget/Sample.csproj`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/unused-nuget/Used.cs`

- [ ] **Step 1: Create fixtures.** A `.csproj` referencing two packages where only one is used in code.

`Sample.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog" Version="4.1.0" />
  </ItemGroup>
</Project>
```

`Used.cs`:
```csharp
using Newtonsoft.Json;

namespace Fixtures;

public static class Used { public static string J() => JsonConvert.SerializeObject(new { }); }
```

- [ ] **Step 2: Write failing test.**

```csharp
[Fact]
public void Flags_PackageReference_with_no_using_in_any_file()
{
    var ctx = new CheckContext(
        Path.Combine(AppContext.BaseDirectory, "_fixtures", "unused-nuget"),
        new QualityConfig());
    var result = new UnusedNuGetPackagesCheck().Run(ctx);
    Assert.False(result.Ok);
    Assert.Contains(result.Findings, f => f.Contains("Serilog"));
}
```

- [ ] **Step 3: Implement `ProjectInspector` + `UnusedNuGetPackagesCheck`.** Use Microsoft.Build to read `PackageReference` items; fall back to scanning `using <Namespace>;` lines for each package's root namespace (approximation acceptable in v1).

`ProjectInspector.cs`:
```csharp
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

namespace Quality.Cli.Msbuild;

public static class ProjectInspector
{
    public static IEnumerable<string> PackageReferences(string csprojPath)
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
        var p = new Project(csprojPath);
        try { return p.GetItems("PackageReference").Select(i => i.EvaluatedInclude).ToArray(); }
        finally { ProjectCollection.GlobalProjectCollection.UnloadProject(p); }
    }
}
```

`UnusedNuGetPackagesCheck.cs`:
```csharp
using Quality.Cli.Msbuild;

namespace Quality.Cli.Checks;

public sealed class UnusedNuGetPackagesCheck : ICheck
{
    public string Id => "unused-nuget-packages";

    public CheckResult Run(CheckContext ctx)
    {
        var findings = new List<string>();
        foreach (var csproj in Directory.EnumerateFiles(ctx.RepoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var packages = ProjectInspector.PackageReferences(csproj);
            var sources = Directory.EnumerateFiles(Path.GetDirectoryName(csproj)!, "*.cs", SearchOption.AllDirectories)
                                   .SelectMany(File.ReadAllLines)
                                   .ToArray();
            foreach (var pkg in packages)
            {
                var root = pkg.Split('.')[0];
                var used = sources.Any(l => l.StartsWith($"using {root}", StringComparison.Ordinal));
                if (!used) findings.Add($"{csproj}: PackageReference '{pkg}' has no `using {root}` in any source file");
            }
        }
        return new CheckResult(Id, findings.Count == 0, findings);
    }
}
```

- [ ] **Step 4: Re-run + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add tools/Quality.Cli/Msbuild tools/Quality.Cli/Checks/UnusedNuGetPackagesCheck.cs tests/UnitTests/Quality.Cli.Tests/Checks/UnusedNuGetPackagesCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/unused-nuget
git commit -m "feat(cli): add UnusedNuGetPackagesCheck via MSBuild project inspector"
```

### Task 16: `EfMigrationsDriftCheck` (conditional)

**Files:**
- Create: `tools/Quality.Cli/Checks/EfMigrationsDriftCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/EfMigrationsDriftCheckTests.cs`

- [ ] **Step 1: Write failing test** — when no project references EF Design, the check is a pass (skipped).

```csharp
[Fact]
public void Skips_when_no_project_references_ef_design()
{
    var ctx = new CheckContext(
        Path.Combine(AppContext.BaseDirectory, "_fixtures", "ef-drift", "no-ef"),
        new QualityConfig());
    var result = new EfMigrationsDriftCheck().Run(ctx);
    Assert.True(result.Ok);
    Assert.Empty(result.Findings);
}
```

Create the fixture: `tests/UnitTests/Quality.Cli.Tests/_fixtures/ef-drift/no-ef/Sample.csproj` (empty csproj).

- [ ] **Step 2: Implement.** Scan `.csproj` files for `Microsoft.EntityFrameworkCore.Design`; if found, shell out to `dotnet ef migrations has-pending-model-changes` for that project; otherwise return Ok.

```csharp
using System.Diagnostics;
using Quality.Cli.Msbuild;

namespace Quality.Cli.Checks;

public sealed class EfMigrationsDriftCheck : ICheck
{
    public string Id => "ef-migrations-drift";

    public CheckResult Run(CheckContext ctx)
    {
        var findings = new List<string>();
        foreach (var csproj in Directory.EnumerateFiles(ctx.RepoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var pkgs = ProjectInspector.PackageReferences(csproj);
            if (!pkgs.Contains("Microsoft.EntityFrameworkCore.Design")) continue;

            var psi = new ProcessStartInfo("dotnet",
                $"ef migrations has-pending-model-changes --project \"{csproj}\"")
            { RedirectStandardOutput = true, RedirectStandardError = true };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                findings.Add($"{csproj}: pending model changes (run `dotnet ef migrations add`)");
        }
        return new CheckResult(Id, findings.Count == 0, findings);
    }
}
```

- [ ] **Step 3: Re-run + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add tools/Quality.Cli/Checks/EfMigrationsDriftCheck.cs tests/UnitTests/Quality.Cli.Tests/Checks/EfMigrationsDriftCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/ef-drift
git commit -m "feat(cli): add EfMigrationsDriftCheck (conditional on EF.Design)"
```

### Task 17: `LockfileIntegrityCheck`

**Files:**
- Create: `tools/Quality.Cli/Checks/LockfileIntegrityCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/LockfileIntegrityCheckTests.cs`

- [ ] **Step 1: Write failing test** — run inside a copy of the repo where the lockfile is intact; expect Ok.

```csharp
[Fact]
public void Locked_restore_succeeds_on_clean_repo()
{
    // Use the real repo root so we exercise dotnet restore --locked-mode.
    var repoRoot = LocateRepoRoot();
    var result = new LockfileIntegrityCheck().Run(new CheckContext(repoRoot, new QualityConfig()));
    Assert.True(result.Ok, string.Join("\n", result.Findings));
}

private static string LocateRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MyProjectClone.Dotnet.sln")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
}
```

- [ ] **Step 2: Implement.**

```csharp
using System.Diagnostics;

namespace Quality.Cli.Checks;

public sealed class LockfileIntegrityCheck : ICheck
{
    public string Id => "lockfile-integrity";

    public CheckResult Run(CheckContext ctx)
    {
        var psi = new ProcessStartInfo("dotnet", "restore --locked-mode")
        {
            WorkingDirectory = ctx.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return proc.ExitCode == 0
            ? new CheckResult(Id, true, Array.Empty<string>())
            : new CheckResult(Id, false, new[] { stdout, stderr }.Where(s => s.Length > 0).ToArray());
    }
}
```

- [ ] **Step 3: Re-run + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add tools/Quality.Cli/Checks/LockfileIntegrityCheck.cs tests/UnitTests/Quality.Cli.Tests/Checks/LockfileIntegrityCheckTests.cs
git commit -m "feat(cli): add LockfileIntegrityCheck (dotnet restore --locked-mode)"
```

### Task 17b: `LicenseCheck` (NuGet license denylist)

**Files:**
- Modify: `.config/dotnet-tools.json` (add `dotnet-project-licenses`)
- Create: `tools/Quality.Cli/Checks/LicenseCheck.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Checks/LicenseCheckTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/licenses/licenses.json`

- [ ] **Step 1: Install the license tool as a local tool.**

```bash
dotnet tool install --local dotnet-project-licenses --version 2.7.1
```

This updates `.config/dotnet-tools.json`.

- [ ] **Step 2: Fixture JSON** simulating `dotnet-project-licenses` output where one package has a denylisted license.

```json
[
  { "PackageName": "Newtonsoft.Json", "PackageVersion": "13.0.3", "LicenseType": "MIT" },
  { "PackageName": "Some.Copyleft",   "PackageVersion": "1.0.0",  "LicenseType": "GPL-3.0" }
]
```

- [ ] **Step 3: Write failing test** — load the fixture, run the check with the spec's default denylist, expect a finding mentioning `Some.Copyleft`.

```csharp
using Quality.Cli.Checks;
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Checks;

public class LicenseCheckTests
{
    [Fact]
    public void Flags_packages_with_denylisted_licenses()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "_fixtures", "licenses", "licenses.json");
        var cfg = new QualityConfig
        {
            Phase4 = new Phase4 { LicenseCheck = new LicenseCheckEntry { Enabled = true, Denylist = new[] { "GPL-3.0", "AGPL-3.0" } } }
        };
        var findings = LicenseCheck.Evaluate(File.ReadAllText(fixture), cfg.Phase4.LicenseCheck.Denylist);
        Assert.Contains(findings, f => f.Contains("Some.Copyleft") && f.Contains("GPL-3.0"));
    }
}
```

- [ ] **Step 4: Implement `LicenseCheck.cs`.** Two surfaces: the `Evaluate` pure function (testable) and the `Run` method that shells out to the local tool.

```csharp
using System.Diagnostics;
using System.Text.Json;

namespace Quality.Cli.Checks;

public sealed class LicenseCheck : ICheck
{
    public string Id => "license-check";

    public CheckResult Run(CheckContext ctx)
    {
        var jsonPath = Path.Combine(Path.GetTempPath(), $"licenses-{Guid.NewGuid():N}.json");
        var psi = new ProcessStartInfo("dotnet",
            $"tool run dotnet-project-licenses -- --input \"{ctx.RepoRoot}\" --json --output-file \"{jsonPath}\" --include-transitive")
        { WorkingDirectory = ctx.RepoRoot, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
            return new CheckResult(Id, false, new[] { "dotnet-project-licenses failed", p.StandardError.ReadToEnd() });

        var findings = Evaluate(File.ReadAllText(jsonPath), ctx.Config.Phase4.LicenseCheck.Denylist);
        File.Delete(jsonPath);
        return new CheckResult(Id, findings.Count == 0, findings);
    }

    public static IReadOnlyList<string> Evaluate(string json, IReadOnlyList<string> denylist)
    {
        var pkgs = JsonSerializer.Deserialize<List<PackageLicense>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        var denied = new HashSet<string>(denylist, StringComparer.OrdinalIgnoreCase);
        return pkgs
            .Where(p => p.LicenseType is not null && denied.Contains(p.LicenseType))
            .Select(p => $"{p.PackageName} {p.PackageVersion}: {p.LicenseType} is on the denylist")
            .ToList();
    }

    private sealed record PackageLicense(string PackageName, string PackageVersion, string? LicenseType);
}
```

- [ ] **Step 5: Wire into `CheckCommand.AllChecks()`** (add `new LicenseCheck()` to the array in `tools/Quality.Cli/Commands/CheckCommand.cs`).

- [ ] **Step 6: Re-run tests.** Expect PASS.

- [ ] **Step 7: Commit.**

```bash
git add .config/dotnet-tools.json tools/Quality.Cli/Checks/LicenseCheck.cs tools/Quality.Cli/Commands/CheckCommand.cs tests/UnitTests/Quality.Cli.Tests/Checks/LicenseCheckTests.cs tests/UnitTests/Quality.Cli.Tests/_fixtures/licenses
git commit -m "feat(cli): add LicenseCheck (NuGet license denylist)"
```
