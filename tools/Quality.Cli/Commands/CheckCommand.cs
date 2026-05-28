namespace Quality.Cli.Commands;

using System.Globalization;
using Quality.Cli.Checks;
using Quality.Cli.Config;
using Quality.Cli.Output;

internal static class CheckCommand
{
    public static int Run(string id, string configPath, IConsoleOutput? sink = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        sink ??= new SpectreConsoleOutput();

        if (!TryLoadConfig(configPath, sink, out var cfg))
        {
            return 2;
        }

        var toRun = ResolveChecks(id);
        if (toRun.Count == 0)
        {
            sink.Error(string.Create(CultureInfo.InvariantCulture, $"unknown check '{id}'"));
            return 2;
        }

        return ExecuteChecks(toRun, cfg, sink);
    }

    private static bool TryLoadConfig(string configPath, IConsoleOutput sink, out QualityConfig cfg)
    {
        try
        {
            cfg = ConfigReader.Read(configPath);
        }
        catch (ConfigReadException ex)
        {
            sink.Error(ex.Message);
            cfg = new QualityConfig();
            return false;
        }

        var configErrors = ConfigValidator.Validate(cfg).ToList();
        if (configErrors.Count > 0)
        {
            foreach (var e in configErrors)
            {
                sink.Error(e);
            }

            return false;
        }

        return true;
    }

    private static IReadOnlyList<ICheck> ResolveChecks(string id) =>
        string.Equals(id, "all", StringComparison.Ordinal)
            ? CheckRegistry.All
            : CheckRegistry.All.Where(c => string.Equals(c.Id, id, StringComparison.Ordinal)).ToArray();

    private static int ExecuteChecks(IReadOnlyList<ICheck> toRun, QualityConfig cfg, IConsoleOutput sink)
    {
        var ctx = new CheckContext(Directory.GetCurrentDirectory(), cfg);
        var failed = 0;

        foreach (var c in toRun)
        {
            sink.Heading(c.Id);

            if (cfg.Checks.TryGetValue(c.Id, out var entry) && !entry.Enabled)
            {
                sink.Info(string.Create(CultureInfo.InvariantCulture, $"skipped (disabled: {entry.Reason})"));
                continue;
            }

            var r = c.Run(ctx);
            if (r.Ok)
            {
                sink.Info("ok");
            }
            else
            {
                foreach (var f in r.Findings)
                {
                    sink.Error(f);
                }

                failed++;
            }
        }

        return failed == 0 ? 0 : 1;
    }
}
