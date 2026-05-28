namespace Quality.Cli.Config;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Minor Code Smell", "S3871:Exception types should be \"public\"", Justification = "Typed signal between Quality.Cli layers; the whole assembly's surface is internal, no consumer should catch this type.")]
internal sealed class ConfigReadException : Exception
{
    public ConfigReadException()
    {
    }

    public ConfigReadException(string message)
        : base(message)
    {
    }

    public ConfigReadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
