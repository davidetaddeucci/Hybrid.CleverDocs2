using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Consumers;
using Hybrid.CleverDocs2.WebServices.Workers;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Middleware;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL Database - Dynamically configured
builder.Services.AddDbContext<ApplicationDbContext>(opts => {
    var connectionString = builder.Configuration.GetConnectionString("Postgres") 
        ?? throw new InvalidOperationException("PostgreSQL connection string 'Postgres' not found in configuration");
    opts.UseNpgsql(connectionString);
});

// Redis Cache - Dynamically configured
builder.Services.AddStackExchangeRedisCache(opts => {
    var redisConfig = builder.Configuration.GetSection("Redis");
    opts.Configuration = redisConfig["Configuration"] 
        ?? throw new InvalidOperationException("Redis configuration not found");
    opts.InstanceName = redisConfig["InstanceName"] ?? "CleverDocs2";
    
    // Additional Redis options from configuration
    if (int.TryParse(redisConfig["ConnectTimeout"], out var connectTimeout))
        opts.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(opts.Configuration);
});

// MassTransit / RabbitMQ - Temporarily disabled for testing
/*
builder.Services.AddMassTransit(x => {
    x.AddConsumer<IngestionChunkConsumer>();
    x.UsingRabbitMq((context, cfg) => {
        var rmq = builder.Configuration.GetSection("RabbitMQ");
        var host = rmq["Host"] ?? throw new InvalidOperationException("RabbitMQ Host not configured");
        var virtualHost = rmq["VirtualHost"] ?? "/";
        var username = rmq["Username"] ?? throw new InvalidOperationException("RabbitMQ Username not configured");
        var password = rmq["Password"] ?? throw new InvalidOperationException("RabbitMQ Password not configured");
        
        cfg.Host(host, virtualHost, h => {
            h.Username(username);
            h.Password(password);
            
            // Additional RabbitMQ configuration from appsettings
            if (int.TryParse(rmq["Heartbeat"], out var heartbeat))
                h.Heartbeat((ushort)heartbeat);
            if (int.TryParse(rmq["RequestedConnectionTimeout"], out var timeout))
                h.RequestedConnectionTimeout(TimeSpan.FromMilliseconds(timeout));
        });
        
        cfg.ReceiveEndpoint("ingestion-chunk-queue", e => {
            e.ConfigureConsumer<IngestionChunkConsumer>(context);
        });
    });
});
*/

// Health Checks - Dynamically configured
var healthChecksBuilder = builder.Services.AddHealthChecks();

// PostgreSQL Health Check
var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrEmpty(postgresConnection))
{
    healthChecksBuilder.AddNpgSql(postgresConnection, name: "postgres");
}

// Redis Health Check
var redisConnection = builder.Configuration["Redis:Configuration"];
if (!string.IsNullOrEmpty(redisConnection))
{
    healthChecksBuilder.AddRedis(redisConnection, name: "redis");
}

// RabbitMQ Health Check (optional - commented out due to package compatibility)
// var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
// var enableRabbitMqHealthCheck = builder.Configuration.GetValue<bool>("HealthChecks:RabbitMQ:Enabled");
// if (enableRabbitMqHealthCheck && !string.IsNullOrEmpty(rabbitMqConfig["Host"]))
// {
//     // RabbitMQ health check can be added when compatible package is available
// }
    // Note: RabbitMQ health check temporarily disabled due to package compatibility issues

// Polly Policies
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(builder.Configuration.GetSection("R2R").GetValue<int>("MaxRetries"), 
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Register all R2R HTTP Clients with resilience policies
void RegisterR2RClient<TInterface, TImplementation>(IServiceCollection services)
    where TInterface : class
    where TImplementation : class, TInterface
{
    services.AddHttpClient<TInterface, TImplementation>(client =>
    {
        var cfg = builder.Configuration.GetSection("R2R");
        var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
        client.BaseAddress = new Uri(url);
        client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
}

// Register all R2R clients
RegisterR2RClient<IAuthClient, AuthClient>(builder.Services);
RegisterR2RClient<IDocumentClient, DocumentClient>(builder.Services);
RegisterR2RClient<ICollectionClient, CollectionClient>(builder.Services);
RegisterR2RClient<IConversationClient, ConversationClient>(builder.Services);
RegisterR2RClient<IPromptClient, PromptClient>(builder.Services);
RegisterR2RClient<IIngestionClient, IngestionClient>(builder.Services);
RegisterR2RClient<IGraphClient, GraphClient>(builder.Services);
RegisterR2RClient<ISearchClient, SearchClient>(builder.Services);
RegisterR2RClient<IToolsClient, ToolsClient>(builder.Services);
RegisterR2RClient<IMaintenanceClient, MaintenanceClient>(builder.Services);
RegisterR2RClient<IOrchestrationClient, OrchestrationClient>(builder.Services);
RegisterR2RClient<ILocalLLMClient, LocalLLMClient>(builder.Services);
RegisterR2RClient<IValidationClient, ValidationClient>(builder.Services);
RegisterR2RClient<IMcpTuningClient, McpTuningClient>(builder.Services);
RegisterR2RClient<IWebDevClient, WebDevClient>(builder.Services);

// Authentication & Authorization
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CompanyOrAdmin", policy => policy.RequireRole("Admin", "Company"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// Auth Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Background Workers - Temporarily disabled for testing
// builder.Services.AddHostedService<IngestionWorker>();

// CORS - Enhanced configuration
var corsSection = builder.Configuration.GetSection("Cors");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
        var allowCredentials = corsSection.GetValue<bool>("AllowCredentials");

        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        if (allowedMethods.Contains("*"))
        {
            policy.AllowAnyMethod();
        }
        else
        {
            policy.WithMethods(allowedMethods);
        }

        if (allowedHeaders.Contains("*"))
        {
            policy.AllowAnyHeader();
        }
        else
        {
            policy.WithHeaders(allowedHeaders);
        }

        if (allowCredentials && !allowedOrigins.Contains("*"))
        {
            policy.AllowCredentials();
        }
    });
});

// Controllers and OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Add custom middleware
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

app.Run();