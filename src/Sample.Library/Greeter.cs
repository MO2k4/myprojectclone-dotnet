namespace Sample.Library;

/// <summary>Returns greeting strings for the sample.</summary>
public static class Greeter
{
    /// <summary>Returns a greeting addressed to <paramref name="name"/>.</summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting in the form <c>Hello, {name}!</c>.</returns>
    public static string Greet(string name) => $"Hello, {name}!";
}
