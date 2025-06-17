using Hybrid.CleverDocs.WebUI.Services;
using Hybrid.CleverDocs.WebUI.Services.Documents;
using Hybrid.CleverDocs.WebUI.Services.Collections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddHttpContextAccessor();

// Add Memory Cache for fast local caching (Redis disabled for stability)
builder.Services.AddMemoryCache();

// Register caching services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add session support for redirect loop prevention
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure HttpClient for API calls
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7219");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Hybrid.CleverDocs.WebUI/1.0");
    client.Timeout = TimeSpan.FromSeconds(45); // Optimized timeout for API calls
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    handler.MaxConnectionsPerServer = 10;
    return handler;
});

builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7219");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Hybrid.CleverDocs.WebUI/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Increased timeout for authentication
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    // Optimize connection pooling
    handler.MaxConnectionsPerServer = 10;
    return handler;
});

// Add Document API Client
builder.Services.AddHttpClient<IDocumentApiClient, DocumentApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7219");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Hybrid.CleverDocs.WebUI/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    handler.MaxConnectionsPerServer = 10;
    return handler;
});

// Add Collections API Client with enterprise-grade configuration
builder.Services.AddHttpClient<ICollectionsApiClient, CollectionsApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7219");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Hybrid.CleverDocs.WebUI/1.0");
    client.Timeout = TimeSpan.FromSeconds(60); // Optimized for Collections operations
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    // Connection pooling optimization for Collections API
    handler.MaxConnectionsPerServer = 10;
    return handler;
});

// Cookie Authentication Configuration
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "CleverDocs.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Disabled for development
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Enable session support

// Cookie Authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Collections routes with GUID support
app.MapControllerRoute(
    name: "collection-details",
    pattern: "collections/{collectionId:guid}",
    defaults: new { controller = "Collections", action = "Details" });

app.MapControllerRoute(
    name: "collection-edit",
    pattern: "collections/{collectionId:guid}/edit",
    defaults: new { controller = "Collections", action = "Edit" });

app.MapControllerRoute(
    name: "collection-delete",
    pattern: "collections/{collectionId:guid}/delete",
    defaults: new { controller = "Collections", action = "Delete" });

app.MapControllerRoute(
    name: "collection-document-delete",
    pattern: "collections/{collectionId:guid}/documents/{documentId:guid}/delete",
    defaults: new { controller = "Collections", action = "DeleteDocument" });

// Document routes
app.MapControllerRoute(
    name: "document-upload",
    pattern: "documents/upload",
    defaults: new { controller = "Documents", action = "Upload" });

app.MapControllerRoute(
    name: "document-details",
    pattern: "documents/{documentId:guid}",
    defaults: new { controller = "Documents", action = "Details" });

app.MapControllerRoute(
    name: "document-edit",
    pattern: "documents/{documentId:guid}/edit",
    defaults: new { controller = "Documents", action = "Edit" });

app.MapControllerRoute(
    name: "document-delete",
    pattern: "documents/{documentId:guid}/delete",
    defaults: new { controller = "Documents", action = "Delete" });

app.MapControllerRoute(
    name: "document-download",
    pattern: "documents/{documentId:guid}/download",
    defaults: new { controller = "Documents", action = "Download" });

app.MapControllerRoute(
    name: "document-toggle-favorite",
    pattern: "documents/{documentId:guid}/toggle-favorite",
    defaults: new { controller = "Documents", action = "ToggleFavorite" });

app.MapControllerRoute(
    name: "documents-favorites",
    pattern: "documents/favorites",
    defaults: new { controller = "Documents", action = "Favorites" });

app.MapControllerRoute(
    name: "documents-r2r-test",
    pattern: "documents/r2r-test",
    defaults: new { controller = "Documents", action = "R2RTest" });

app.MapControllerRoute(
    name: "documents-search-suggestions",
    pattern: "documents/search-suggestions",
    defaults: new { controller = "Documents", action = "GetSearchSuggestions" });

app.MapControllerRoute(
    name: "documents-index",
    pattern: "documents",
    defaults: new { controller = "Documents", action = "Index" });

// Default route should go to login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
