using Blazored.LocalStorage;
using Hybrid.CleverDocs.WebUI.Components;
using Hybrid.CleverDocs.WebUI.Services.Api;
using Hybrid.CleverDocs.WebUI.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/webui-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add HttpClient
builder.Services.AddHttpClient<IApiClient, ApiClient>();

// Add Authentication services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthorizationCore();

// Add API services
builder.Services.AddScoped<IApiClient, ApiClient>();

// Add configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    Log.Information("Starting Hybrid.CleverDocs2.WebUI");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
