using Microsoft.Extensions.Configuration;

namespace Fixtures;

// Only Microsoft.Extensions.Configuration is genuinely used. The Logging package
// must be flagged as unused. Under the OLD probe (`using Microsoft`), this file
// would have falsely satisfied both packages.
public static class Used
{
    public static IConfigurationRoot? Build() => null;
}
