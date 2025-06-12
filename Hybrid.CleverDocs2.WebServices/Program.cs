using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Auth;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Document;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Collection;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Conversation;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Prompt;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Search;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Ingestion;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Graph;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Tools;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Maintenance;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Orchestration;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.LocalLLM;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Validation;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.McpTuning;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.WebDev;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database Configuration (temporarily disabled for demo)
// builder.Services.AddDbContext<AuthDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// JWT Configuration (simplified for demo)
var jwtKey = "CleverDocs2-Super-Secret-JWT-Key-For-Authentication-2024-Very-Long-And-Secure";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Polly Policies for R2R clients
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Register R2R HTTP Clients
void RegisterR2RClient<TInterface, TImplementation>(IServiceCollection services)
    where TInterface : class
    where TImplementation : class, TInterface
{
    services.AddHttpClient<TInterface, TImplementation>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:7272");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(retryPolicy);
}

// Register all R2R clients
RegisterR2RClient<IAuthClient, AuthClient>(builder.Services);
RegisterR2RClient<IDocumentClient, DocumentClient>(builder.Services);
RegisterR2RClient<ICollectionClient, CollectionClient>(builder.Services);
RegisterR2RClient<IConversationClient, ConversationClient>(builder.Services);
RegisterR2RClient<IPromptClient, PromptClient>(builder.Services);
RegisterR2RClient<ISearchClient, SearchClient>(builder.Services);
RegisterR2RClient<IIngestionClient, IngestionClient>(builder.Services);
RegisterR2RClient<IGraphClient, GraphClient>(builder.Services);
RegisterR2RClient<IToolsClient, ToolsClient>(builder.Services);
RegisterR2RClient<IMaintenanceClient, MaintenanceClient>(builder.Services);
RegisterR2RClient<IOrchestrationClient, OrchestrationClient>(builder.Services);
RegisterR2RClient<ILocalLLMClient, LocalLLMClient>(builder.Services);
RegisterR2RClient<IValidationClient, ValidationClient>(builder.Services);
RegisterR2RClient<IMcpTuningClient, McpTuningClient>(builder.Services);
RegisterR2RClient<IWebDevClient, WebDevClient>(builder.Services);

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Demo endpoint
app.MapGet("/api/demo", () => Results.Ok(new { 
    message = "CleverDocs2 WebServices Demo", 
    version = "1.0.0",
    features = new[] { "R2R Integration", "JWT Auth", "REST API" }
}));

app.Run();