namespace Quality.Cli.Config;

using System.Globalization;
using System.Text;
using Tomlyn;

internal static class ConfigReader
{
    public static QualityConfig Read(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (FileNotFoundException ex)
        {
            throw new ConfigReadException(string.Create(CultureInfo.InvariantCulture, $"config file not found: {path}"), ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new ConfigReadException(string.Create(CultureInfo.InvariantCulture, $"config directory not found: {path}"), ex);
        }
        catch (IOException ex)
        {
            throw new ConfigReadException(string.Create(CultureInfo.InvariantCulture, $"could not read config file: {path}: {ex.Message}"), ex);
        }

        try
        {
            return Toml.ToModel<QualityConfig>(text, options: new TomlModelOptions
            {
                ConvertPropertyName = ToSnakeCase,
            });
        }
        catch (TomlException ex)
        {
            throw new ConfigReadException(string.Create(CultureInfo.InvariantCulture, $"could not parse config file: {path}: {ex.Message}"), ex);
        }
    }

    private static string ToSnakeCase(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0 && char.IsUpper(c))
            {
                sb.Append('_');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
