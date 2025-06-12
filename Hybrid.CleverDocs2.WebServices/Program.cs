using Microsoft.EntityFrameworkCore;
using MassTransit;
using Polly;
using Polly.Extensions.Http;
using RabbitMQ.Client;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Consumers;
using Hybrid.CleverDocs2.WebServices.Workers;
using Hybrid.CleverDocs2.WebServices.Services.Clients;

var builder = WebApplication.CreateBuilder(args);

// DbContext / PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(opts => {
    opts.Configuration = builder.Configuration["Redis:Configuration"];
});

// MassTransit / RabbitMQ
builder.Services.AddMassTransit(x => {
    x.AddConsumer<IngestionChunkConsumer>();
    x.UsingRabbitMq((context, cfg) => {
        var rmq = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rmq["Host"], rmq["VirtualHost"], h => {
            h.Username(rmq["Username"]);
            h.Password(rmq["Password"]);
        });
        cfg.ReceiveEndpoint("ingestion-chunk-queue", e => {
            e.ConfigureConsumer<IngestionChunkConsumer>(context);
        });
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres")
    .AddRedis(builder.Configuration["Redis:Configuration"]!, name: "redis");
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

// Background Workers
builder.Services.AddHostedService<IngestionWorker>();

// CORS
builder.Services.AddCors(options => 
    options.AddDefaultPolicy(p => 
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

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
app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

app.Run();