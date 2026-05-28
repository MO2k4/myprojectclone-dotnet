# Phase E — Top-level commands — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase E — Top-level commands

### Task 18: `fmt` + `check` + `pr-check` + `doctor`

**Files:**
- Create: `tools/Quality.Cli/Commands/FmtCommand.cs`
- Create: `tools/Quality.Cli/Commands/CheckCommand.cs`
- Create: `tools/Quality.Cli/Commands/PrCheckCommand.cs`
- Create: `tools/Quality.Cli/Commands/DoctorCommand.cs`
- Modify: `tools/Quality.Cli/Program.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Commands/CheckCommandTests.cs`

- [ ] **Step 1: `FmtCommand.cs`** — thin wrapper.

```csharp
using System.Diagnostics;

namespace Quality.Cli.Commands;

public static class FmtCommand
{
    public static int Run()
    {
        var psi = new ProcessStartInfo("dotnet", "format --verify-no-changes --severity error");
        psi.RedirectStandardOutput = false;
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }
}
```

- [ ] **Step 2: `CheckCommand.cs`** — dispatch by id; `all` runs every registered check.

```csharp
using Quality.Cli.Checks;
using Quality.Cli.Config;
using Quality.Cli.Output;

namespace Quality.Cli.Commands;

public static class CheckCommand
{
    public static int Run(string id, string configPath, IConsoleOutput? sink = null)
    {
        sink ??= new SpectreConsoleOutput();
        var cfg = ConfigReader.Read(configPath);

        var configErrors = ConfigValidator.Validate(cfg).ToList();
        if (configErrors.Count > 0)
        {
            foreach (var e in configErrors) sink.Error(e);
            return 2;
        }

        var ctx = new CheckContext(Directory.GetCurrentDirectory(), cfg);
        var checks = AllChecks();
        var toRun = id == "all" ? checks : checks.Where(c => c.Id == id).ToArray();
        if (toRun.Length == 0) { sink.Error($"unknown check '{id}'"); return 2; }

        var failed = 0;
        foreach (var c in toRun)
        {
            sink.Heading(c.Id);
            var r = c.Run(ctx);
            if (r.Ok) sink.Info("ok");
            else { foreach (var f in r.Findings) sink.Error(f); failed++; }
        }
        return failed == 0 ? 0 : 1;
    }

    private static ICheck[] AllChecks() =>
    [
        new MaxLinesCheck(),
        new BypassDirectiveCheck(),
        new EnvExhaustivenessCheck(),
        new UnusedNuGetPackagesCheck(),
        new EfMigrationsDriftCheck(),
        new LockfileIntegrityCheck(),
        new LicenseCheck(),  // Task 17b appends this entry
    ];
}
```

- [ ] **Step 3: Failing test for `CheckCommand.Run("all", …)`.** Use `RecordingConsoleOutput` to assert behavior.

```csharp
[Fact]
public void Reports_failure_when_a_check_fails()
{
    var cfg = Path.GetTempFileName();
    File.WriteAllText(cfg, "[phase5]\nmax_lines = { enabled = true, threshold = 1 }\n");
    // Run against the fixtures dir so max-lines fires.
    Directory.SetCurrentDirectory(Path.Combine(AppContext.BaseDirectory, "_fixtures", "max-lines"));
    var sink = new RecordingConsoleOutput();
    var code = CheckCommand.Run("max-lines", cfg, sink);
    Assert.NotEqual(0, code);
    Assert.True(sink.HasErrors);
}
```

- [ ] **Step 4: Run test.** Expect PASS.

- [ ] **Step 5: `PrCheckCommand.cs`** = `CheckCommand.Run("all", …)` + `FmtCommand.Run()` + `dotnet build` + `dotnet test`.

```csharp
using System.Diagnostics;

namespace Quality.Cli.Commands;

public static class PrCheckCommand
{
    public static int Run()
    {
        var steps = new (string Name, Func<int> Fn)[]
        {
            ("fmt",   FmtCommand.Run),
            ("check", () => CheckCommand.Run("all", ".quality.toml")),
            ("build", () => Shell("dotnet build --no-restore -warnaserror")),
            ("test",  () => Shell("dotnet test --no-build")),
        };
        foreach (var (name, fn) in steps)
        {
            Console.WriteLine($"── {name} ──");
            var rc = fn();
            if (rc != 0) return rc;
        }
        return 0;
    }

    private static int Shell(string cmdline)
    {
        var parts = cmdline.Split(' ', 2);
        var psi = new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "");
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }
}
```

- [ ] **Step 6: `DoctorCommand.cs`** — checks SDK, restored tools, `pre-commit --version`, `docker --version`.

```csharp
using System.Diagnostics;

namespace Quality.Cli.Commands;

public static class DoctorCommand
{
    public static int Run()
    {
        var ok = true;
        ok &= Probe("dotnet", "--version");
        ok &= Probe("dotnet", "tool list --local");
        ok &= Probe("pre-commit", "--version");
        ok &= Probe("docker", "--version");
        return ok ? 0 : 1;
    }

    private static bool Probe(string exe, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(exe, args) { RedirectStandardOutput = true });
            p!.WaitForExit();
            Console.WriteLine($"✓ {exe} {args}");
            return p.ExitCode == 0;
        }
        catch { Console.WriteLine($"✗ {exe} not found"); return false; }
    }
}
```

- [ ] **Step 7: Wire commands into `Program.cs`.**

```csharp
var fmt   = new Command("fmt", "dotnet format --verify-no-changes");
fmt.SetHandler(() => Environment.Exit(Commands.FmtCommand.Run()));
root.AddCommand(fmt);

var check = new Command("check", "Run one named check or 'all'");
var idArg = new Argument<string>("id");
check.AddArgument(idArg);
check.SetHandler((string id) =>
    Environment.Exit(Commands.CheckCommand.Run(id, ".quality.toml")), idArg);
root.AddCommand(check);

var pr = new Command("pr-check", "Run every phase (fmt, check, build, test)");
pr.SetHandler(() => Environment.Exit(Commands.PrCheckCommand.Run()));
root.AddCommand(pr);

var doctor = new Command("doctor", "Diagnose toolchain");
doctor.SetHandler(() => Environment.Exit(Commands.DoctorCommand.Run()));
root.AddCommand(doctor);
```

- [ ] **Step 8: Smoke + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
dotnet run --project tools/Quality.Cli -- doctor
git add tools/Quality.Cli tests/UnitTests/Quality.Cli.Tests/Commands
git commit -m "feat(cli): add fmt, check, pr-check, doctor commands"
```

### Task 19: `install` command

**Files:**
- Create: `tools/Quality.Cli/Commands/InstallCommand.cs`
- Create: `tools/Quality.Cli/Resources/` (copies of canonical templates)
- Modify: `tools/Quality.Cli/Quality.Cli.csproj` (embed resources)
- Create: `tests/UnitTests/Quality.Cli.Tests/Commands/InstallCommandTests.cs`

- [ ] **Step 1: Move the canonical templates into the tool as embedded resources.** Copy these files from the repo root into `tools/Quality.Cli/Resources/` (keep originals at root — they are how the template repo is used; the resources are how the `install` command writes them into a NEW repo).

Files to copy:
- `Directory.Build.props` → `Resources/Directory.Build.props`
- `Directory.Packages.props` → `Resources/Directory.Packages.props`
- `.editorconfig` → `Resources/.editorconfig`
- `.quality.toml` → `Resources/.quality.toml`
- *(`.pre-commit-config.yaml` is appended to the resource set in Task 23 — does not exist yet at Task 19 time.)*

In `Quality.Cli.csproj`:

```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources/**/*.*" />
  </ItemGroup>
```

- [ ] **Step 2: Write failing test** — install into a temp dir; assert files appear.

```csharp
[Fact]
public void Install_writes_canonical_files_to_target_dir()
{
    var tmp = Directory.CreateTempSubdirectory().FullName;
    var code = InstallCommand.Run(tmp);
    Assert.Equal(0, code);
    Assert.True(File.Exists(Path.Combine(tmp, "Directory.Build.props")));
    Assert.True(File.Exists(Path.Combine(tmp, ".editorconfig")));
    Assert.True(File.Exists(Path.Combine(tmp, ".quality.toml")));
}
```

- [ ] **Step 3: Implement `InstallCommand.cs`.**

```csharp
using System.Reflection;
using Quality.Cli.Msbuild;

namespace Quality.Cli.Commands;

public static class InstallCommand
{
    private static readonly (string Resource, string TargetRelativePath)[] Files =
    [
        ("Quality.Cli.Resources.Directory_Build_props",    "Directory.Build.props"),
        ("Quality.Cli.Resources.Directory_Packages_props", "Directory.Packages.props"),
        ("Quality.Cli.Resources._editorconfig",            ".editorconfig"),
        ("Quality.Cli.Resources._quality_toml",            ".quality.toml"),
        // .pre-commit-config.yaml entry appended in Task 23.
    ];

    public static int Run(string targetRoot)
    {
        var asm = typeof(InstallCommand).Assembly;
        foreach (var (res, rel) in Files)
        {
            var target = Path.Combine(targetRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var s = asm.GetManifestResourceStream(res)
                          ?? throw new InvalidOperationException($"missing embedded resource: {res}");
            using var fs = File.Create(target);
            s.CopyTo(fs);
        }
        AutoAttachSerilogAnalyzer(targetRoot);
        return 0;
    }

    private static void AutoAttachSerilogAnalyzer(string root)
    {
        foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var pkgs = ProjectInspector.PackageReferences(csproj);
            if (pkgs.Any(p => p.StartsWith("Serilog", StringComparison.Ordinal)))
            {
                AppendPackageVersionToPackagesProps(root, "SerilogAnalyzer", "0.15.0");
                return;
            }
        }
    }

    private static void AppendPackageVersionToPackagesProps(string root, string id, string version)
    {
        var path = Path.Combine(root, "Directory.Packages.props");
        if (!File.Exists(path)) return;
        var text = File.ReadAllText(path);
        if (text.Contains($"Include=\"{id}\"")) return;
        text = text.Replace("</Project>",
            $"  <ItemGroup>\n    <GlobalPackageReference Include=\"{id}\" Version=\"{version}\" />\n  </ItemGroup>\n</Project>");
        File.WriteAllText(path, text);
    }
}
```

> Note: the MSBuild resource-name mangling replaces `.` with `_` in path segments — verify the resource names with `Quality.Cli.dll`’s `GetManifestResourceNames()` if the test fails; adjust the constant list.

- [ ] **Step 4: Wire `install` in `Program.cs`.**

```csharp
var install = new Command("install", "Bootstrap a repo with the quality framework");
var targetOpt = new Option<string>("--into", () => Directory.GetCurrentDirectory());
install.AddOption(targetOpt);
install.SetHandler((string into) => Environment.Exit(Commands.InstallCommand.Run(into)), targetOpt);
root.AddCommand(install);
```

- [ ] **Step 5: Re-run tests + commit.**

```bash
dotnet test tests/UnitTests/Quality.Cli.Tests
git add tools/Quality.Cli tests/UnitTests/Quality.Cli.Tests/Commands/InstallCommandTests.cs
git commit -m "feat(cli): add install command + SerilogAnalyzer auto-attach"
```
