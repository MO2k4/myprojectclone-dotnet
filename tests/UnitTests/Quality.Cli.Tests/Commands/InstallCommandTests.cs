namespace Quality.Cli.Tests.Commands;

using Quality.Cli.Commands;
using Xunit;

public class InstallCommandTests
{
    [Fact]
    public void Install_writes_canonical_files_to_target_dir()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var code = InstallCommand.Run(tmp);
            Assert.Equal(0, code);
            Assert.True(File.Exists(Path.Combine(tmp, "Directory.Build.props")));
            Assert.True(File.Exists(Path.Combine(tmp, "Directory.Packages.props")));
            Assert.True(File.Exists(Path.Combine(tmp, ".editorconfig")));
            Assert.True(File.Exists(Path.Combine(tmp, ".quality.toml")));
            Assert.True(File.Exists(Path.Combine(tmp, ".pre-commit-config.yaml")));
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Install_attaches_SerilogAnalyzer_when_Serilog_referenced()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var projDir = Path.Combine(tmp, "Sample");
            Directory.CreateDirectory(projDir);
            var csproj = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" Version="3.1.1" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(Path.Combine(projDir, "Sample.csproj"), csproj);

            var code = InstallCommand.Run(tmp);
            Assert.Equal(0, code);

            var packagesProps = File.ReadAllText(Path.Combine(tmp, "Directory.Packages.props"));
            Assert.Contains("SerilogAnalyzer", packagesProps, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Install_is_idempotent_when_SerilogAnalyzer_already_attached()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var projDir = Path.Combine(tmp, "Sample");
            Directory.CreateDirectory(projDir);
            var csproj = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" Version="3.1.1" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(Path.Combine(projDir, "Sample.csproj"), csproj);

            Assert.Equal(0, InstallCommand.Run(tmp));
            Assert.Equal(0, InstallCommand.Run(tmp));

            var packagesProps = File.ReadAllText(Path.Combine(tmp, "Directory.Packages.props"));
            var occurrences = packagesProps.Split("SerilogAnalyzer", StringSplitOptions.None).Length - 1;
            Assert.Equal(1, occurrences);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Install_does_not_attach_SerilogAnalyzer_without_Serilog()
    {
        var tmp = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var projDir = Path.Combine(tmp, "Sample");
            Directory.CreateDirectory(projDir);
            var csproj = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                  </ItemGroup>
                </Project>
                """;
            File.WriteAllText(Path.Combine(projDir, "Sample.csproj"), csproj);

            var code = InstallCommand.Run(tmp);
            Assert.Equal(0, code);

            var packagesProps = File.ReadAllText(Path.Combine(tmp, "Directory.Packages.props"));
            Assert.DoesNotContain("SerilogAnalyzer", packagesProps, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}
