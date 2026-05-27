# Phase H — Architecture tests + sample fleshing — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase H — Architecture tests + sample fleshing

### Task 24: `ArchitectureTests` project (NetArchTest)

**Files:**
- Create: `tests/ArchitectureTests/ArchitectureTests.csproj`
- Create: `tests/ArchitectureTests/LayeringTests.cs`
- Modify: `MyProjectClone.Dotnet.sln`

- [ ] **Step 1: Project file.**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Sample.Lib\Sample.Lib.csproj" />
    <ProjectReference Include="..\..\src\Sample.Api\Sample.Api.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write two illustrative architecture tests.** NetArchTest does not ship a cycle check, so we ship only rules it expresses cleanly. Cycle detection is documented as a v2 follow-up in `docs/practices.md`.

```csharp
using NetArchTest.Rules;

namespace ArchitectureTests;

public class LayeringTests
{
    [Fact]
    public void Domain_must_not_reference_Infrastructure()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOn("Infrastructure")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Public_types_in_Sample_Lib_are_sealed_or_abstract()
    {
        var result = Types.InAssembly(typeof(Sample.Lib.Greeter).Assembly)
            .That().ArePublic()
            .Should().BeSealed().Or().BeAbstract()
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
}
```

- [ ] **Step 3: Run.** `dotnet test tests/ArchitectureTests`. Expected: PASS.

- [ ] **Step 4: Commit.**

```bash
git add tests/ArchitectureTests MyProjectClone.Dotnet.sln
git commit -m "test: add architecture tests via NetArchTest"
```

### Task 25: Sample API gets an `IOptions<T>` + .env.example to exercise env-exhaustiveness end-to-end

**Files:**
- Modify: `src/Sample.Api/Program.cs`
- Create: `src/Sample.Api/appsettings.json`
- Create: `src/Sample.Api/.env.example`
- Create: `src/Sample.Api/SampleOptions.cs`

- [ ] **Step 1: `appsettings.json`.**

```json
{ "Sample": { "Greeting": "Hello" } }
```

- [ ] **Step 2: `.env.example`.**

```
SAMPLE__GREETING=Hello
```

- [ ] **Step 3: `SampleOptions.cs`.**

```csharp
namespace Sample.Api;

public sealed class SampleOptions { public string Greeting { get; init; } = ""; }
```

- [ ] **Step 4: `Program.cs` updated to bind options.**

```csharp
using Microsoft.Extensions.Options;
using Sample.Api;
using Sample.Lib;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SampleOptions>(builder.Configuration.GetSection("Sample"));

var app = builder.Build();
app.MapGet("/", (IOptions<SampleOptions> opts) => Greeter.Greet(opts.Value.Greeting));
app.Run();
```

- [ ] **Step 5: Re-run env-exhaustiveness against the sample.**

Run: `dotnet quality check env-exhaustiveness`
Expected: pass (both keys present in `.env.example`).

- [ ] **Step 6: Commit.**

```bash
git add src/Sample.Api
git commit -m "feat(sample): demonstrate IOptions + appsettings + .env.example wiring"
```

