# Phase B — Quality.Cli scaffolding — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase B — Quality.Cli scaffolding

### Task 7: Create the `Quality.Cli` tool project

**Files:**
- Create: `tools/Quality.Cli/Quality.Cli.csproj`
- Create: `tools/Quality.Cli/Program.cs`
- Create: `.config/dotnet-tools.json`
- Modify: `MyProjectClone.Dotnet.sln`

- [ ] **Step 1: Create `tools/Quality.Cli/Quality.Cli.csproj`** packed as a local tool (spec §"Project shape").

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>quality</ToolCommandName>
    <PackageId>dotnet-quality</PackageId>
    <Version>0.1.0</Version>
    <RootNamespace>Quality.Cli</RootNamespace>
    <!-- The tool itself must NOT generate XML docs; suppress noisy CS1591s. -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" />
    <PackageReference Include="Spectre.Console" />
    <PackageReference Include="Tomlyn" />
    <PackageReference Include="LibGit2Sharp" />
    <PackageReference Include="Microsoft.Build.Locator" />
    <PackageReference Include="Microsoft.Build" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create `tools/Quality.Cli/Program.cs`** with MSBuildLocator init + a root command that prints the version.

```csharp
using Microsoft.Build.Locator;
using System.CommandLine;

namespace Quality.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        var root = new RootCommand("dotnet quality — strict-by-default quality framework for .NET");
        root.SetHandler(() => Console.WriteLine("quality 0.1.0"));
        return await root.InvokeAsync(args);
    }
}
```

- [ ] **Step 3: Create the local tool manifest.**

```bash
mkdir -p .config
dotnet new tool-manifest --force
```

This writes `.config/dotnet-tools.json`. Leave the `tools` map empty for now (Task 8 will register Quality.Cli locally via `dotnet pack` + `dotnet tool install --add-source`).

- [ ] **Step 4: Add to solution and verify build.**

```bash
dotnet sln add tools/Quality.Cli/Quality.Cli.csproj
dotnet build tools/Quality.Cli/Quality.Cli.csproj
```

Expected: builds clean.

- [ ] **Step 5: Run the tool directly to verify the root command.**

Run: `dotnet run --project tools/Quality.Cli -- --help`
Expected: prints `quality` help text including the description above.

- [ ] **Step 6: Commit.**

```bash
git add tools/ .config/dotnet-tools.json MyProjectClone.Dotnet.sln
git commit -m "feat: scaffold Quality.Cli tool project + root command"
```

### Task 8: Unit-test project for Quality.Cli

**Files:**
- Create: `tests/UnitTests/Quality.Cli.Tests/Quality.Cli.Tests.csproj`
- Create: `tests/UnitTests/Quality.Cli.Tests/SmokeTests.cs`
- Modify: `MyProjectClone.Dotnet.sln`

- [ ] **Step 1: Create the test project.**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="coverlet.msbuild" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\tools\Quality.Cli\Quality.Cli.csproj" />
  </ItemGroup>

  <!-- Fixture .cs files are test inputs, not test code: exclude from compile and
       publish as content so tests can read them at runtime. -->
  <ItemGroup>
    <Compile Remove="_fixtures/**/*.cs" />
    <None Include="_fixtures/**/*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write a failing smoke test.**

```csharp
namespace Quality.Cli.Tests;

public class SmokeTests
{
    [Fact]
    public void Program_type_is_resolvable_from_test_project()
    {
        var t = typeof(Quality.Cli.Program);
        Assert.Equal("Quality.Cli", t.Namespace);
    }
}
```

- [ ] **Step 3: Run.**

```bash
dotnet sln add tests/UnitTests/Quality.Cli.Tests/Quality.Cli.Tests.csproj
dotnet test tests/UnitTests/Quality.Cli.Tests
```

Expected: 1 test fails — `Program` is `internal`.

- [ ] **Step 4: Add `InternalsVisibleTo` in `Quality.Cli.csproj`.**

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="Quality.Cli.Tests" />
  </ItemGroup>
```

- [ ] **Step 5: Re-run tests.**

Run: `dotnet test tests/UnitTests/Quality.Cli.Tests`
Expected: 1 test passes; coverage report generated (Phase-A threshold currently 90%, but acceptable here since the project is one-class).

- [ ] **Step 6: Commit.**

```bash
git add tests/ tools/Quality.Cli/Quality.Cli.csproj MyProjectClone.Dotnet.sln
git commit -m "test: bootstrap Quality.Cli unit-test project"
```

### Task 9: Spectre console wrapper

**Files:**
- Create: `tools/Quality.Cli/Output/Console.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Output/ConsoleTests.cs`

- [ ] **Step 1: Write failing test for an `IConsoleOutput` indirection** (lets tests assert messages without grabbing stdout).

```csharp
using Quality.Cli.Output;

namespace Quality.Cli.Tests.Output;

public class ConsoleTests
{
    [Fact]
    public void Error_appends_red_marker_and_returns_nonzero_intent()
    {
        var sink = new RecordingConsoleOutput();
        sink.Error("boom");
        Assert.Contains("boom", sink.Captured);
        Assert.True(sink.HasErrors);
    }
}
```

- [ ] **Step 2: Run** — expect compile failure.

Run: `dotnet test tests/UnitTests/Quality.Cli.Tests`
Expected: FAIL — `Quality.Cli.Output` namespace does not exist.

- [ ] **Step 3: Implement `Output/Console.cs`.**

```csharp
using Spectre.Console;

namespace Quality.Cli.Output;

public interface IConsoleOutput
{
    void Heading(string text);
    void Info(string text);
    void Error(string text);
    bool HasErrors { get; }
}

public sealed class SpectreConsoleOutput : IConsoleOutput
{
    public bool HasErrors { get; private set; }

    public void Heading(string text) => AnsiConsole.MarkupLine($"[bold cyan]── {text} ──[/]");
    public void Info(string text)    => AnsiConsole.MarkupLine(text);
    public void Error(string text)
    {
        AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(text)}[/]");
        HasErrors = true;
    }
}

public sealed class RecordingConsoleOutput : IConsoleOutput
{
    private readonly System.Text.StringBuilder _buf = new();
    public string Captured => _buf.ToString();
    public bool HasErrors { get; private set; }

    public void Heading(string text) => _buf.AppendLine($"## {text}");
    public void Info(string text)    => _buf.AppendLine(text);
    public void Error(string text)   { _buf.AppendLine($"ERR {text}"); HasErrors = true; }
}
```

- [ ] **Step 4: Re-run.**

Run: `dotnet test tests/UnitTests/Quality.Cli.Tests`
Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add tools/Quality.Cli/Output tests/UnitTests/Quality.Cli.Tests/Output
git commit -m "feat(cli): add Spectre console wrapper with testable recorder"
```

