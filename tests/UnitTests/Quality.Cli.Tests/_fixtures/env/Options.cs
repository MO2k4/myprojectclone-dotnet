namespace Fixtures;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class AuthOptions
{
    public string Issuer { get; set; } = string.Empty;
}
