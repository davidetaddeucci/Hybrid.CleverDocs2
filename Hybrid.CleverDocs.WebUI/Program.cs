using Blazored.LocalStorage;
using Hybrid.CleverDocs.WebUI.Services.Api;
using Hybrid.CleverDocs.WebUI.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add HTTP Client
builder.Services.AddHttpClient();

// Add API Client
builder.Services.AddScoped<IApiClient, ApiClient>();

// Add Authentication Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Add Authorization
builder.Services.AddAuthorizationCore();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();