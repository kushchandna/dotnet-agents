using DotnetAgents.Api.Endpoints;
using DotnetAgents.Core.Configuration;
using DotnetAgents.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Config path resolution: --config CLI > DOTNET_AGENTS_CONFIG env > appsettings AgentsConfigPath > ./config.json
string? cliConfigPath = null;
for (int i = 0; i < args.Length - 1; i++)
    if (args[i] == "--config") { cliConfigPath = args[i + 1]; break; }

var configPath = cliConfigPath
    ?? Environment.GetEnvironmentVariable("DOTNET_AGENTS_CONFIG")
    ?? builder.Configuration["AgentsConfigPath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "config.json");

var config = await ConfigLoader.LoadAsync(configPath);
builder.Services.AddDotnetAgents(config);
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

if (!builder.Environment.IsEnvironment("Testing"))
    builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();
app.UseCors();
app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

UsersEndpoints.Map(app);
AgentsEndpoints.Map(app);
SessionsEndpoints.Map(app);
MessagesEndpoints.Map(app);

app.Run();

public partial class Program { } // for WebApplicationFactory
