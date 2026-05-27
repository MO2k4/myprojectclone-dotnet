namespace Quality.Cli.Msbuild;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

// MSBuildLocator must run before any Microsoft.Build type is JIT-touched.
// With coverlet instrumentation in tests, the JIT eagerly loads referenced
// types, so this registration must happen at module load — not lazily on
// first ProjectInspector call.
[ExcludeFromCodeCoverage(Justification = "Module initializer; runs exactly once per AppDomain at assembly load and is implicitly exercised by every test that touches Quality.Cli.")]
internal static class MsbuildInit
{
    [ModuleInitializer]
    public static void Register()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
