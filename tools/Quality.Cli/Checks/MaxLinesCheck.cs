namespace Quality.Cli.Checks;

using System.Globalization;

internal sealed class MaxLinesCheck : ICheck
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public string Id => "max-lines";

    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var threshold = ctx.Config.Phase5.MaxLines.Threshold;
        var findings = new List<string>();

        foreach (var path in Directory.EnumerateFiles(ctx.RepoRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(ctx.RepoRoot, path))
            {
                continue;
            }

            var lineCount = File.ReadLines(path).Count();
            if (lineCount > threshold)
            {
                findings.Add(string.Create(CultureInfo.InvariantCulture, $"{path}: {lineCount} lines (max {threshold})"));
            }
        }

        return new CheckResult(this.Id, findings.Count == 0, findings);
    }

    private static bool IsBuildArtifact(string root, string path)
    {
        var rel = Path.GetRelativePath(root, path);
        foreach (var segment in rel.Split(PathSeparators))
        {
            if (string.Equals(segment, "bin", StringComparison.Ordinal)
                || string.Equals(segment, "obj", StringComparison.Ordinal)
                || string.Equals(segment, "_fixtures", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
