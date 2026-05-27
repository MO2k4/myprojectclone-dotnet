using Newtonsoft.Json;

namespace Fixtures;

public static class Used
{
    public static string J() => JsonConvert.SerializeObject(new { });
}
