namespace Quality.Cli.Config;

internal sealed class CheckEntry
{
    public bool Enabled { get; init; } = true;

    public string Reason { get; init; } = string.Empty;

    // Per-check optional fields. Each check reads only the one(s) it cares about
    // and falls back to its hardcoded default when null. Kept on the same type to
    // keep Tomlyn deserialization trivial (one type per dictionary value).
    public int? Threshold { get; init; }

    public List<string>? Denylist { get; init; }
}
