using Sample.Lib;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Greeter.Greet("world"));

app.Run();
