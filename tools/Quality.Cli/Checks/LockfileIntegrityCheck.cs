namespace Quality.Cli.Checks;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

internal sealed class LockfileIntegrityCheck : ICheck
{
    public string Id => "lockfile-integrity";

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet restore --locked-mode`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    public CheckResult Run(CheckContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var (exitCode, stdout, stderr, timedOut) = RunLockedRestore(ctx.RepoRoot);

        if (timedOut)
        {
            return new CheckResult(this.Id, false, ["dotnet restore --locked-mode timed out after 5 minutes"]);
        }

        if (exitCode == 0)
        {
            return new CheckResult(this.Id, true, Array.Empty<string>());
        }

        var findings = new List<string>();
        if (stdout.Length > 0)
        {
            findings.Add(stdout);
        }

        if (stderr.Length > 0)
        {
            findings.Add(stderr);
        }

        return new CheckResult(this.Id, false, findings);
    }

    [ExcludeFromCodeCoverage(Justification = "Shells out to `dotnet restore --locked-mode`; exercised by the integration smoke test against the real repo root, not branch-asserted here.")]
    private static (int ExitCode, string Stdout, string Stderr, bool TimedOut) RunLockedRestore(string repoRoot)
    {
        var psi = new ProcessStartInfo("dotnet", "restore --locked-mode")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Without these env vars, `dotnet restore` spawns MSBuild worker nodes with /nodeReuse:true
        // that inherit the parent's stdout/stderr handles and outlive the parent process,
        // causing WaitForExit() to block forever waiting for the streams to drain.
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        psi.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (stdout)
                {
                    stdout.AppendLine(e.Data);
                }
            }
        };
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
            return (-1, stdout.ToString(), stderr.ToString(), true);
        }

        return (proc.ExitCode, stdout.ToString(), stderr.ToString(), false);
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
