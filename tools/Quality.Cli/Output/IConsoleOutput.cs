namespace Quality.Cli.Output;

internal interface IConsoleOutput
{
    bool HasErrors { get; }

    void Heading(string text);

    void Info(string text);

    void Error(string text);
}
