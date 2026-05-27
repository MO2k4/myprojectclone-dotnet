namespace Quality.Cli;

using System.CommandLine;
using Microsoft.Build.Locator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var root = new RootCommand("dotnet quality — strict-by-default quality framework for .NET");
        root.SetHandler(() => Console.WriteLine("quality 0.1.0"));
        return await root.InvokeAsync(args).ConfigureAwait(false);
    }
}
