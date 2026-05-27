namespace Quality.Cli.Config;

using System.Text;
using Tomlyn;

internal static class ConfigReader
{
    public static QualityConfig Read(string path)
    {
        var text = File.ReadAllText(path);
        return Toml.ToModel<QualityConfig>(text, options: new TomlModelOptions
        {
            ConvertPropertyName = ToSnakeCase,
        });
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
