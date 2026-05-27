namespace Quality.Cli.Checks;

internal interface ICheck
{
    string Id { get; }

    CheckResult Run(CheckContext ctx);
}
