namespace Quality.Cli.Checks;

using System.Globalization;

internal sealed class MaxLinesCheck : ICheck
{
    public string Id => "max-lines";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var threshold = ctx.Config.Checks.TryGetValue(this.Id, out var entry)
            ? entry.Threshold ?? 400
            : 400;

        var findings = new List<string>();
        foreach (var path in RepoFileFilter.EnumerateFiles(ctx.RepoRoot, "*.cs"))
        {
            var lineCount = File.ReadLines(path).Count();
            if (lineCount > threshold)
            {
                findings.Add(string.Create(CultureInfo.InvariantCulture, $"{path}: {lineCount} lines (max {threshold})"));
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }
}
