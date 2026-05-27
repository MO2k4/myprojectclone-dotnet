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
        var cfg = ConfigReader.Read(configPath);

        var configErrors = ConfigValidator.Validate(cfg).ToList();
        if (configErrors.Count > 0)
        {
            foreach (var e in configErrors)
            {
                sink.Error(e);
            }

            return 2;
        }

        var ctx = new CheckContext(Directory.GetCurrentDirectory(), cfg);
        var checks = AllChecks();
        var toRun = string.Equals(id, "all", StringComparison.Ordinal)
            ? checks
            : checks.Where(c => string.Equals(c.Id, id, StringComparison.Ordinal)).ToArray();
        if (toRun.Length == 0)
        {
            sink.Error(string.Create(CultureInfo.InvariantCulture, $"unknown check '{id}'"));
            return 2;
        }

        var failed = 0;
        foreach (var c in toRun)
        {
            sink.Heading(c.Id);
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

    private static ICheck[] AllChecks() =>
    [
        new MaxLinesCheck(),
        new BypassDirectiveCheck(),
        new EnvExhaustivenessCheck(),
        new UnusedNuGetPackagesCheck(),
        new EfMigrationsDriftCheck(),
        new LockfileIntegrityCheck(),
        new LicenseCheck(),
    ];
}
