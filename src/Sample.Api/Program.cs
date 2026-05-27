using Sample.Library;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Greeter.Greet("world"));

await app.RunAsync().ConfigureAwait(false);
