namespace Quality.Cli.Checks;

internal static class RepoFileFilter
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static bool IsExcludedSegment(string root, string path)
    {
        var rel = Path.GetRelativePath(root, path);
        return rel.Split(PathSeparators).Any(segment => segment is "bin" or "obj" or "_fixtures");
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
