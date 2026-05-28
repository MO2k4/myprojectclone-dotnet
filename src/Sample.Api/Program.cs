using Microsoft.Extensions.Options;
using Sample.Api;
using Sample.Library;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SampleOptions>(builder.Configuration.GetSection("Sample"));

var app = builder.Build();
app.MapGet("/", (IOptions<SampleOptions> opts) => Greeter.Greet(opts.Value.Greeting));

await app.RunAsync().ConfigureAwait(false);
