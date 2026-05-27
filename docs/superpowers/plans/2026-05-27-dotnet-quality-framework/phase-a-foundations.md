# Phase A — Foundations: strict-building sample — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase A — Foundations: strict-building sample

Goal of this phase: end with a solution that fails `dotnet build` when an analyzer rule is intentionally violated.

### Task 1: SDK pin + solution skeleton + sample projects

**Files:**
- Create: `global.json`
- Create: `MyProjectClone.Dotnet.sln`
- Create: `src/Sample.Library/Sample.Library.csproj`
- Create: `src/Sample.Library/Greeter.cs`
- Create: `src/Sample.Api/Sample.Api.csproj`
- Create: `src/Sample.Api/Program.cs`

- [ ] **Step 1: Create `global.json`** pinning the SDK so every contributor uses the same .NET version (spec line 70).

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- [ ] **Step 2: Create a placeholder `src/Sample.Library/Sample.Library.csproj`** — properties left empty since `Directory.Build.props` (Task 2) will inject them. PackageReferences will move to central management in Task 3.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup />
</Project>
```

- [ ] **Step 3: Add `src/Sample.Library/Greeter.cs`** — a deliberately tiny class that respects nullable and avoids analyzer noise. XML doc comments are required because `Directory.Build.props` (Task 2) sets `GenerateDocumentationFile=true` and `TreatWarningsAsErrors=true`, which together promote CS1591 to error.

```csharp
namespace Sample.Library;

/// <summary>Returns greeting strings for the sample.</summary>
public static class Greeter
{
    /// <summary>Returns a greeting addressed to <paramref name="name"/>.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting in the form <c>Hello, {name}!</c>.</returns>
    public static string Greet(string name) => $"Hello, {name}!";
}
```

- [ ] **Step 4: Create `src/Sample.Api/Sample.Api.csproj`** — minimal ASP.NET Core API.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Sample.Library\Sample.Library.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Add `src/Sample.Api/Program.cs`** — minimal API surface that uses `Greeter`. `await app.RunAsync().ConfigureAwait(false)` rather than `app.Run()` because Sonar's S6966 (prefer RunAsync) plus CA2007 / MA0004 (ConfigureAwait) both fire under the strict ruleset.

```csharp
using Sample.Library;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Greeter.Greet("world"));

await app.RunAsync().ConfigureAwait(false);
```

- [ ] **Step 6: Create the solution file** wiring the two projects.

```bash
dotnet new sln -n MyProjectClone.Dotnet
dotnet sln add src/Sample.Library/Sample.Library.csproj src/Sample.Api/Sample.Api.csproj
```

- [ ] **Step 7: Skip the build check.**

The placeholder `<PropertyGroup />` in `Sample.Library.csproj` has no `<TargetFramework>`, so `dotnet build` will fail with "Invalid framework identifier ''" until `Directory.Build.props` injects it in Task 2. The build is exercised at the end of Task 2 instead.

- [ ] **Step 8: Commit.**

```bash
git add global.json MyProjectClone.Dotnet.sln src/
git commit -m "chore: add SDK pin + sample solution skeleton"
```

### Task 2: `Directory.Build.props` — strict global defaults

**Files:**
- Create: `Directory.Build.props`

- [ ] **Step 1: Create `Directory.Build.props`** mirroring spec lines 103–121. This is the single point that turns every project strict.

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

- [ ] **Step 2: Confirm strictness by introducing a deliberate violation** — add an unused private field to `Greeter.cs`:

```csharp
namespace Sample.Library;

/// <summary>Returns greeting strings for the sample.</summary>
public static class Greeter
{
    private static readonly string _unused = "bug bait";

    /// <summary>Returns a greeting addressed to <paramref name="name"/>.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting in the form <c>Hello, {name}!</c>.</returns>
    public static string Greet(string name) => $"Hello, {name}!";
}
```

Run: `dotnet build`
Expected: build FAILS with CA1823 (or equivalent unused-private-field rule) treated as error. The strict ruleset is wide — expect CA1823 alongside CS0414, CA1802, and possibly other correctness rules. Any of these confirm the strict layer works.

- [ ] **Step 3: Remove the bug bait.**

```csharp
namespace Sample.Library;

/// <summary>Returns greeting strings for the sample.</summary>
public static class Greeter
{
    /// <summary>Returns a greeting addressed to <paramref name="name"/>.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting in the form <c>Hello, {name}!</c>.</returns>
    public static string Greet(string name) => $"Hello, {name}!";
}
```

Run: `dotnet build`
Expected: build succeeds clean.

- [ ] **Step 4: Commit.**

```bash
git add Directory.Build.props src/Sample.Library/Greeter.cs
git commit -m "feat: enable strict global build defaults (nullable, WaE, AllEnabledByDefault)"
```

### Task 3: `Directory.Packages.props` + analyzer NuGets

**Files:**
- Create: `Directory.Packages.props`
- Modify: `src/Sample.Library/Sample.Library.csproj`
- Modify: `src/Sample.Api/Sample.Api.csproj`

- [ ] **Step 1: Create `Directory.Packages.props`** declaring central package management and pinning every analyzer from spec lines 129–141.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Analyzers — applied to every project">
    <GlobalPackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
    <GlobalPackageReference Include="SonarAnalyzer.CSharp" Version="10.4.0.108396" />
    <GlobalPackageReference Include="Roslynator.Analyzers" Version="4.12.9" />
    <GlobalPackageReference Include="Meziantou.Analyzer" Version="2.0.187" />
    <GlobalPackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7" />
    <GlobalPackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" Version="17.12.19" />
    <GlobalPackageReference Include="IDisposableAnalyzers" Version="4.0.8" />
    <GlobalPackageReference Include="ErrorProne.NET.CoreAnalyzers" Version="0.6.1-beta.1" />
  </ItemGroup>

  <ItemGroup Label="Test packages — version-only entries used by ItemGroups below">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.msbuild" Version="6.0.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
  </ItemGroup>

  <ItemGroup Label="Runtime packages">
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
    <PackageVersion Include="Tomlyn" Version="0.17.0" />
    <PackageVersion Include="LibGit2Sharp" Version="0.31.0" />
    <PackageVersion Include="Microsoft.Build.Locator" Version="1.7.8" />
    <PackageVersion Include="Microsoft.Build" Version="17.12.6" />
  </ItemGroup>
</Project>
```

> Note: pin to the highest stable available at implementation time; bump as needed.

- [ ] **Step 2: Restore the solution to verify analyzers attach.**

Run: `dotnet restore`
Expected: restore succeeds and all 8 analyzer packages appear in `obj/project.assets.json`. The build verification (Task 3 step 3) is deferred until Task 4 adds `.editorconfig`, because StyleCop's SA1633 (file header required) will fail every `.cs` file in the sample until opted out there.

- [ ] **Step 3: Commit.**

```bash
git add Directory.Packages.props
git commit -m "feat: centralize package versions + attach analyzer suite"
```

### Task 4: `.editorconfig` — severities + structured-logging pins

**Files:**
- Create: `.editorconfig`

- [ ] **Step 1: Create `.editorconfig`** with default-`error` severities plus the structured-logging rules from spec lines 150–157 explicitly pinned.

```ini
root = true

[*]
indent_style = space
end_of_line = lf
trim_trailing_whitespace = true
insert_final_newline = true
charset = utf-8

[*.{csproj,props,targets,sln}]
indent_size = 2

[*.cs]
indent_size = 4

# Every Roslyn rule defaults to error via AnalysisMode in Directory.Build.props.
# Below: explicit pins for high-value rules, plus a few documented opt-outs.

# --- Structured logging (spec §"Structured-logging enforcement") ---
dotnet_diagnostic.CA2254.severity = error   # template must be literal
dotnet_diagnostic.CA1727.severity = error   # placeholder PascalCase
dotnet_diagnostic.CA1848.severity = error   # prefer LoggerMessage source generators
dotnet_diagnostic.CA2017.severity = error   # template vs argument count mismatch
dotnet_diagnostic.CA1873.severity = error   # avoid eager .ToString() when level filtered

# --- Justification requirements ---
dotnet_diagnostic.MA0026.severity = error   # TODO ban (Meziantou)
dotnet_diagnostic.MA0091.severity = error   # SuppressMessage must have non-empty justification

# --- Documented opt-outs (spec §"Per-check opt-out: Layer A") ---
# CA1303 localization noise — not relevant for an internal template.
dotnet_diagnostic.CA1303.severity = suggestion
# SA1633 mandatory file header — internal template; copyright headers add churn without value.
dotnet_diagnostic.SA1633.severity = none
```

- [ ] **Step 2: Verify CA2254 actually fires** by adding (temporarily) a non-literal log call to `Program.cs`:

```csharp
using Sample.Library;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", (ILogger<Program> log) =>
{
    var who = "world";
    log.LogInformation($"hello {who}"); // CA2254
    return Greeter.Greet(who);
});

app.Run();
```

Run: `dotnet build`
Expected: build FAILS with CA2254.

- [ ] **Step 3: Revert the bug bait and re-build clean.**

- [ ] **Step 4: Commit.**

```bash
git add .editorconfig
git commit -m "feat: pin editorconfig severities incl. structured-logging rules"
```

### Task 5: Lock files + `Directory.Build.targets`

**Files:**
- Create: `Directory.Build.targets`
- Generate: `src/**/packages.lock.json` (via restore)

- [ ] **Step 1: Add `Directory.Build.targets`** with a coverage-threshold target that downstream test projects can opt into.

```xml
<Project>
  <PropertyGroup Condition="'$(IsTestProject)'=='true'">
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <Threshold>90</Threshold>
    <ThresholdType>line,branch</ThresholdType>
    <ThresholdStat>total</ThresholdStat>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Restore with lockfile generation.**

Run: `dotnet restore --use-lock-file`
Expected: `packages.lock.json` is generated next to each `.csproj`.

- [ ] **Step 3: Commit the lockfiles.**

```bash
git add Directory.Build.targets src/**/packages.lock.json
git commit -m "feat: add coverage threshold target + commit lock files"
```

### Task 6: Phase-A smoke test

**Files:**
- (none modified)

- [ ] **Step 1: Run the canary battery — build, format check, locked restore.**

Run:
```bash
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes --severity error
dotnet restore --locked-mode
```
Expected: all three succeed silently.

- [ ] **Step 2: No commit.** Phase A is a green baseline; further changes go to subsequent tasks.

