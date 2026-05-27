# Phase C — Config layer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase C — Config layer

### Task 10: `.quality.toml` POCO + Tomlyn reader

**Files:**
- Create: `tools/Quality.Cli/Config/QualityConfig.cs`
- Create: `tools/Quality.Cli/Config/ConfigReader.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Config/ConfigReaderTests.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/quality/full.toml`
- Create: `tests/UnitTests/Quality.Cli.Tests/_fixtures/quality/disabled-without-reason.toml`

- [ ] **Step 1: Write the fixture `full.toml`** — covers a typical strict config.

```toml
[phase4]
trivy_fs       = { enabled = true,  severity = "MEDIUM" }
license_check  = { enabled = true,  denylist = ["GPL-3.0", "AGPL-3.0"] }

[phase5]
max_lines       = { enabled = true,  threshold = 400 }
semgrep_logging = { enabled = true }

[phase9]
coverage = { enabled = true, line = 90, branch = 90 }
```

- [ ] **Step 2: Write the second fixture `disabled-without-reason.toml`** — legitimate target for the validator test in Task 11.

```toml
[phase4]
trivy_fs = { enabled = false }
```

- [ ] **Step 3: Write failing tests.**

```csharp
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Config;

public class ConfigReaderTests
{
    private static string Fixture(string name)
        => Path.Combine(AppContext.BaseDirectory, "_fixtures", "quality", name);

    [Fact]
    public void Reads_enabled_checks_with_defaults()
    {
        var cfg = ConfigReader.Read(Fixture("full.toml"));
        Assert.True(cfg.Phase5.MaxLines.Enabled);
        Assert.Equal(400, cfg.Phase5.MaxLines.Threshold);
        Assert.Equal(90, cfg.Phase9.Coverage.Line);
    }

    [Fact]
    public void Disabled_without_reason_round_trips_with_empty_reason()
    {
        var cfg = ConfigReader.Read(Fixture("disabled-without-reason.toml"));
        Assert.False(cfg.Phase4.TrivyFs.Enabled);
        Assert.Equal(string.Empty, cfg.Phase4.TrivyFs.Reason);
    }
}
```

- [ ] **Step 4: Run** — expect compile failure (no `Quality.Cli.Config`). Fixture-copy item group already exists in `Quality.Cli.Tests.csproj` from Task 8.

- [ ] **Step 5: Implement the POCO + reader.**

`tools/Quality.Cli/Config/QualityConfig.cs`:

```csharp
namespace Quality.Cli.Config;

public sealed class QualityConfig
{
    public Phase4 Phase4 { get; init; } = new();
    public Phase5 Phase5 { get; init; } = new();
    public Phase9 Phase9 { get; init; } = new();
}

public sealed class Phase4
{
    public CheckEntry TrivyFs      { get; init; } = new();
    public LicenseCheckEntry LicenseCheck { get; init; } = new();
}

public sealed class Phase5
{
    public MaxLinesEntry  MaxLines       { get; init; } = new();
    public CheckEntry     SemgrepLogging { get; init; } = new();
}

public sealed class Phase9
{
    public CoverageEntry Coverage { get; init; } = new();
}

public class CheckEntry
{
    public bool   Enabled  { get; init; } = true;
    public string Reason   { get; init; } = string.Empty;
    public string? Severity { get; init; }
}

public sealed class MaxLinesEntry : CheckEntry { public int Threshold { get; init; } = 400; }

public sealed class CoverageEntry : CheckEntry
{
    public int Line   { get; init; } = 90;
    public int Branch { get; init; } = 90;
}

public sealed class LicenseCheckEntry : CheckEntry
{
    public IReadOnlyList<string> Denylist { get; init; } = Array.Empty<string>();
}
```

`tools/Quality.Cli/Config/ConfigReader.cs`:

```csharp
using Tomlyn;

namespace Quality.Cli.Config;

public static class ConfigReader
{
    public static QualityConfig Read(string path)
    {
        var text = File.ReadAllText(path);
        return Toml.ToModel<QualityConfig>(text, options: new TomlModelOptions
        {
            ConvertPropertyName = name => ToSnakeCase(name)
        });
    }

    private static string ToSnakeCase(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length + 4);
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0 && char.IsUpper(c)) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 6: Re-run tests.**

Run: `dotnet test tests/UnitTests/Quality.Cli.Tests`
Expected: PASS.

- [ ] **Step 7: Commit.**

```bash
git add tools/Quality.Cli/Config tests/UnitTests/Quality.Cli.Tests/Config tests/UnitTests/Quality.Cli.Tests/_fixtures
git commit -m "feat(cli): add .quality.toml reader (Tomlyn-based)"
```

### Task 11: Config validator + `status` command

**Files:**
- Create: `tools/Quality.Cli/Config/ConfigValidator.cs`
- Create: `tools/Quality.Cli/Commands/StatusCommand.cs`
- Modify: `tools/Quality.Cli/Program.cs`
- Create: `tests/UnitTests/Quality.Cli.Tests/Config/ConfigValidatorTests.cs`
- Create: `.quality.toml`

- [ ] **Step 1: Write failing validator tests.**

```csharp
using Quality.Cli.Config;

namespace Quality.Cli.Tests.Config;

public class ConfigValidatorTests
{
    [Fact]
    public void Disabled_check_without_reason_is_an_error()
    {
        var cfg = new QualityConfig
        {
            Phase4 = new Phase4 { TrivyFs = new CheckEntry { Enabled = false, Reason = "" } }
        };
        var errors = ConfigValidator.Validate(cfg).ToList();
        Assert.Single(errors);
        Assert.Contains("trivy_fs", errors[0]);
    }

    [Fact]
    public void Disabled_check_with_reason_passes()
    {
        var cfg = new QualityConfig
        {
            Phase4 = new Phase4 { TrivyFs = new CheckEntry { Enabled = false, Reason = "tracked in #123" } }
        };
        Assert.Empty(ConfigValidator.Validate(cfg));
    }
}
```

- [ ] **Step 2: Run** — expect compile failure.

- [ ] **Step 3: Implement `ConfigValidator.cs`.**

```csharp
namespace Quality.Cli.Config;

public static class ConfigValidator
{
    public static IEnumerable<string> Validate(QualityConfig cfg)
    {
        foreach (var (id, entry) in Enumerate(cfg))
        {
            if (!entry.Enabled && string.IsNullOrWhiteSpace(entry.Reason))
                yield return $"check '{id}' is disabled but has no `reason`. " +
                             "Add a non-empty reason in .quality.toml.";
        }
    }

    private static IEnumerable<(string Id, CheckEntry Entry)> Enumerate(QualityConfig c) =>
    [
        ("phase4.trivy_fs",       c.Phase4.TrivyFs),
        ("phase4.license_check",  c.Phase4.LicenseCheck),
        ("phase5.max_lines",      c.Phase5.MaxLines),
        ("phase5.semgrep_logging",c.Phase5.SemgrepLogging),
        ("phase9.coverage",       c.Phase9.Coverage),
    ];
}
```

- [ ] **Step 4: Re-run tests.** Expect PASS.

- [ ] **Step 5: Add `StatusCommand.cs`** rendering a Spectre table.

```csharp
using Quality.Cli.Config;
using Spectre.Console;

namespace Quality.Cli.Commands;

public static class StatusCommand
{
    public static int Run(string configPath)
    {
        var cfg = ConfigReader.Read(configPath);
        var table = new Table().AddColumns("Check", "Enabled", "Reason");
        foreach (var (id, entry) in ConfigValidator.AllEntries(cfg))
            table.AddRow(id, entry.Enabled ? "yes" : "no", entry.Reason);
        AnsiConsole.Write(table);
        return 0;
    }
}
```

- [ ] **Step 6: Expose `AllEntries` from the validator** for reuse:

```csharp
// In ConfigValidator.cs — promote Enumerate to public.
public static IEnumerable<(string Id, CheckEntry Entry)> AllEntries(QualityConfig c) =>
    Enumerate(c);
```

- [ ] **Step 7: Wire the command in `Program.cs`.**

```csharp
var statusCmd = new Command("status", "Print the table of every check + enabled/reason");
statusCmd.SetHandler(() => Commands.StatusCommand.Run(".quality.toml"));
root.AddCommand(statusCmd);
```

- [ ] **Step 8: Create `.quality.toml`** at the repo root using the spec defaults (spec lines 270–285).

```toml
[phase4]
trivy_fs       = { enabled = true, severity = "MEDIUM" }
license_check  = { enabled = true, denylist = ["GPL-3.0", "AGPL-3.0"] }

[phase5]
max_lines       = { enabled = true, threshold = 400 }
semgrep_logging = { enabled = true }

[phase9]
coverage = { enabled = true, line = 90, branch = 90 }
```

- [ ] **Step 9: Smoke-test.**

Run: `dotnet run --project tools/Quality.Cli -- status`
Expected: prints a Spectre table with every check marked enabled.

- [ ] **Step 10: Commit.**

```bash
git add tools/ tests/ .quality.toml
git commit -m "feat(cli): add config validator + status command"
```

