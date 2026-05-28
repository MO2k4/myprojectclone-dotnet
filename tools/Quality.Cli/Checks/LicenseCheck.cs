namespace Quality.Cli.Checks;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;

internal sealed class LicenseCheck : ICheck
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string Id => "license-check";

    public static IReadOnlyList<string> Evaluate(string json, IReadOnlyList<string> denylist)
    {
        ArgumentNullException.ThrowIfNull(denylist);

        var packages = JsonSerializer.Deserialize<List<PackageLicense>>(json, JsonOptions) ?? [];
        var denied = new HashSet<string>(denylist, StringComparer.OrdinalIgnoreCase);

        return packages
            .Where(p => p.LicenseType is not null && denied.Contains(p.LicenseType))
            .Select(p => string.Create(
                CultureInfo.InvariantCulture,
                $"{p.PackageName} {p.PackageVersion}: {p.LicenseType} is on the denylist"))
            .ToList();
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet tool run dotnet-project-licenses`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var jsonPath = Path.Combine(Path.GetTempPath(), string.Create(CultureInfo.InvariantCulture, $"licenses-{Guid.NewGuid():N}.json"));
        var (exitCode, stderr, timedOut) = RunLicenseScan(ctx.RepoRoot, jsonPath);

        if (timedOut)
        {
            return new CheckResult(this.Id, false, ["dotnet-project-licenses timed out after 5 minutes"]);
        }

        if (exitCode != 0)
        {
            return new CheckResult(this.Id, false, ["dotnet-project-licenses failed", stderr]);
        }

        try
        {
            var findings = Evaluate(File.ReadAllText(jsonPath), ctx.Config.Phase4.LicenseCheck.Denylist);
            return new CheckResult(this.Id, findings.Count == 0, findings);
        }
        finally
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet tool run dotnet-project-licenses`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    private static (int ExitCode, string Stderr, bool TimedOut) RunLicenseScan(string repoRoot, string jsonPath)
    {
        var args = string.Create(
            CultureInfo.InvariantCulture,
            $"tool run dotnet-project-licenses -- --input \"{repoRoot}\" --json --outfile \"{jsonPath}\" --include-transitive");
        var psi = new ProcessStartInfo("dotnet", args)
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Without these env vars, child MSBuild worker nodes inherit the parent's
        // stdout/stderr handles and keep them open past process exit, deadlocking
        // WaitForExit() on stream drain.
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";

        var stderr = new StringBuilder();

        using var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (_, _) => { };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stderr)
                {
                    stderr.AppendLine(e.Data);
                }
            }
        };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds))
        {
            TryKill(proc);
            return (-1, stderr.ToString(), true);
        }

        return (proc.ExitCode, stderr.ToString(), false);
    }

    [ExcludeFromCodeCoverage(Justification = "Defensive kill on timeout path; not branch-asserted.")]
    private static void TryKill(Process proc)
    {
        try
        {
            proc.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the timeout check and the kill — nothing to do.
        }
        catch (Win32Exception)
        {
            // Insufficient privileges or transient OS error — best-effort cleanup, swallow.
        }
    }
}
