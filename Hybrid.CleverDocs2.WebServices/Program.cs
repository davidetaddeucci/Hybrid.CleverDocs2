using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Consumers;
using Hybrid.CleverDocs2.WebServices.Workers;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Services.Logging;
using Hybrid.CleverDocs2.WebServices.Services.Queue;
using Hybrid.CleverDocs2.WebServices.Services.Cache;
using Hybrid.CleverDocs2.WebServices.Services.Collections;
using Hybrid.CleverDocs2.WebServices.Services.Documents;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Middleware;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL Database - Optimized Configuration (192.168.1.4:5433)
builder.Services.AddDbContext<ApplicationDbContext>(opts => {
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("PostgreSQL connection string 'Postgres' not found in configuration");

    opts.UseNpgsql(connectionString, npgsqlOpts => {
        npgsqlOpts.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue<int>("Database:RetryPolicy:MaxRetryAttempts", 3),
            maxRetryDelay: TimeSpan.FromMilliseconds(builder.Configuration.GetValue<int>("Database:RetryPolicy:MaxDelayMs", 5000)),
            errorCodesToAdd: null);
        npgsqlOpts.CommandTimeout(builder.Configuration.GetValue<int>("Database:CommandTimeout", 30));
    });

    // Development optimizations
    if (builder.Environment.IsDevelopment())
    {
        opts.EnableSensitiveDataLogging();
        opts.EnableDetailedErrors();
        opts.LogTo(Console.WriteLine, LogLevel.Information);
    }

    // Production optimizations
    if (builder.Environment.IsProduction())
    {
        opts.EnableServiceProviderCaching();
        opts.EnableSensitiveDataLogging(false);
    }
});

// Memory Cache - Required for L1 cache
builder.Services.AddMemoryCache();

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

// Redis Connection Multiplexer - Required for L2 cache
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    var connectionString = redisConfig["Configuration"]
        ?? throw new InvalidOperationException("Redis configuration not found");
    return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
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

// PostgreSQL Health Check - Enhanced Configuration
var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrEmpty(postgresConnection))
{
    var dbHealthCheckTimeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("Database:HealthCheck:TimeoutSeconds", 10));

    healthChecksBuilder.AddNpgSql(
        connectionString: postgresConnection,
        healthQuery: "SELECT 1;",
        name: "postgresql-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "postgresql", "external" },
        timeout: dbHealthCheckTimeout);
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

// Correlation and Logging Services
builder.Services.AddCorrelationServices();

// Rate Limiting Services
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();

// RabbitMQ Services (placeholder implementation)
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));
// builder.Services.AddScoped<IRabbitMQService, RabbitMQService>(); // Commented out until implementation is complete

// Multi-Level Cache Services
builder.Services.Configure<MultiLevelCacheOptions>(builder.Configuration.GetSection("MultiLevelCache"));
builder.Services.Configure<L1CacheOptions>(builder.Configuration.GetSection("L1Cache"));
builder.Services.Configure<L2CacheOptions>(builder.Configuration.GetSection("L2Cache"));
builder.Services.Configure<L3CacheOptions>(builder.Configuration.GetSection("L3Cache"));
builder.Services.Configure<CacheInvalidationOptions>(builder.Configuration.GetSection("CacheInvalidation"));
builder.Services.Configure<CacheWarmingOptions>(builder.Configuration.GetSection("CacheWarming"));

// Cache Service Implementations
builder.Services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
builder.Services.AddSingleton<IL1MemoryCache, L1MemoryCache>();
builder.Services.AddSingleton<IL2RedisCache, L2RedisCache>();
builder.Services.AddSingleton<IL3PersistentCache, L3PersistentCache>();
builder.Services.AddScoped<IMultiLevelCacheService, MultiLevelCacheService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<ICacheWarmingService, CacheWarmingService>();

// R2R-Specific Cache Service
builder.Services.Configure<R2RCacheOptions>(builder.Configuration.GetSection("R2RCache"));
builder.Services.AddScoped<IR2RCacheService, R2RCacheService>();

// Collection Services
builder.Services.Configure<UserCollectionOptions>(builder.Configuration.GetSection("UserCollections"));
builder.Services.Configure<CollectionSyncOptions>(builder.Configuration.GetSection("CollectionSync"));
builder.Services.Configure<CollectionSuggestionOptions>(builder.Configuration.GetSection("CollectionSuggestions"));
builder.Services.Configure<CollectionAnalyticsOptions>(builder.Configuration.GetSection("CollectionAnalytics"));

builder.Services.AddScoped<IUserCollectionService, UserCollectionService>();
builder.Services.AddScoped<ICollectionSyncService, CollectionSyncService>();
builder.Services.AddScoped<ICollectionSuggestionService, CollectionSuggestionService>();
builder.Services.AddScoped<ICollectionAnalyticsService, CollectionAnalyticsService>();

// Document Upload Services
builder.Services.Configure<DocumentUploadOptions>(builder.Configuration.GetSection("DocumentUpload"));
builder.Services.Configure<ChunkedUploadOptions>(builder.Configuration.GetSection("ChunkedUpload"));
builder.Services.Configure<DocumentProcessingOptions>(builder.Configuration.GetSection("DocumentProcessing"));

builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
builder.Services.AddScoped<IChunkedUploadService, ChunkedUploadService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

// Upload support services (placeholder implementations)
builder.Services.AddScoped<IUploadProgressService, UploadProgressService>();
builder.Services.AddScoped<IUploadValidationService, UploadValidationService>();
builder.Services.AddScoped<IUploadStorageService, UploadStorageService>();
builder.Services.AddScoped<IUploadMetricsService, UploadMetricsService>();

// Document Management Services
builder.Services.AddScoped<IUserDocumentService, UserDocumentService>();

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

// SignalR for real-time updates
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Add custom middleware (order is important)
app.UseCorrelation(); // First: Set up correlation context
app.UseGlobalExceptionHandling(); // Second: Handle exceptions globally
app.UseMiddleware<JwtMiddleware>(); // Third: JWT authentication
app.UseMiddleware<TenantResolutionMiddleware>(); // Fourth: Tenant resolution

app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// SignalR Hubs
app.MapHub<CollectionHub>("/hubs/collection");
app.MapHub<DocumentUploadHub>("/hubs/upload");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();