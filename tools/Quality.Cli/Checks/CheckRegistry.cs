namespace Quality.Cli.Checks;

internal static class CheckRegistry
{
    public static IReadOnlyList<ICheck> All { get; } =
    [
        new MaxLinesCheck(),
        new BypassDirectiveCheck(),
        new EnvExhaustivenessCheck(),
        new UnusedNuGetPackagesCheck(),
        new EfMigrationsDriftCheck(),
        new LockfileIntegrityCheck(),
        new LicenseCheck(),
    ];

    public static HashSet<string> Ids { get; } =
        new(All.Select(c => c.Id), StringComparer.Ordinal);
}
