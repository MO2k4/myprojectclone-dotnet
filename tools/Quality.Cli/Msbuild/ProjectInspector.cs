namespace Quality.Cli.Msbuild;

using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

internal static class ProjectInspector
{
    public static IReadOnlyList<string> PackageReferences(string csprojPath)
    {
        EnsureMsBuildLocatorRegistered();
        return PackageReferencesCore(csprojPath);
    }

    private static void EnsureMsBuildLocatorRegistered()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    // Critical: this method MUST be in its own non-inlined frame so the JIT
    // does not resolve Microsoft.Build assemblies before MSBuildLocator runs.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string[] PackageReferencesCore(string csprojPath)
    {
        var project = new Microsoft.Build.Evaluation.Project(csprojPath);
        try
        {
            return project.GetItems("PackageReference")
                .Select(i => i.EvaluatedInclude)
                .ToArray();
        }
        finally
        {
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadProject(project);
        }
    }
}
