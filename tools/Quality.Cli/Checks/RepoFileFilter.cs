namespace Quality.Cli.Checks;

internal static class RepoFileFilter
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static bool IsExcludedSegment(string root, string path)
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

    public static IEnumerable<string> EnumerateFiles(string root, string pattern)
    {
        foreach (var path in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            if (!IsExcludedSegment(root, path))
            {
                yield return path;
            }
        }
    }
}
