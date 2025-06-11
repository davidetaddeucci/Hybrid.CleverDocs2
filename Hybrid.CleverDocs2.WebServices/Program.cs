using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Consumers;
using Hybrid.CleverDocs2.WebServices.Workers;
using MassTransit;
using System;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
// Configure R2R options
builder.Services.Configure<R2ROptions>(builder.Configuration.GetSection("R2R"));
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
        cfg.Host(rmq["Host"], h => {
        h.VirtualHost(rmq["VirtualHost"]);
            h.Username(rmq["Username"]);
            h.Password(rmq["Password"]);
        });
        cfg.ReceiveEndpoint("ingestion-chunk-queue", e => {
            e.ConfigureConsumer<IngestionChunkConsumer>(context);
        });
    });
});

// HealthChecks

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres"), name: "postgres")
    .AddRedis(builder.Configuration["Redis:Configuration"], name: "redis")
    .AddRabbitMQ(sp => {
        var configSection = sp.GetRequiredService<IConfiguration>().GetSection("RabbitMQ");
        var uri = new Uri($"amqp://{configSection["Username"]}:{configSection["Password"]}@{configSection["Host"]}/{configSection["VirtualHost"]}");
        var factory = new ConnectionFactory { Uri = uri };
        return factory.CreateConnection();
    }, name: "rabbitmq");
// CORS
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));


// Register AuthClient with resilience policies
builder.Services.AddHttpClient<IAuthClient, AuthClient>(client =>
{
    var cfg = builder.Configuration.GetSection("R2R");
    var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
// Register WebDevClient with resilience policies
builder.Services.AddHttpClient<IWebDevClient, WebDevClient>(client =>
{
    var cfg = builder.Configuration.GetSection("R2R");
    var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(builder.Configuration.GetSection("R2R").GetValue<int>("MaxRetries"), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));


// Add services to the container.

// Register WebDevClient with resilience policies
builder.Services.AddHttpClient<IWebDevClient, WebDevClient>(client =>
{
    var cfg = builder.Configuration.GetSection("R2R");
    var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(builder.Configuration.GetSection("R2R").GetValue<int>("MaxRetries"), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
// Register DocumentClient with resilience policies
builder.Services.AddHttpClient<IDocumentClient, DocumentClient>(client =>
{
    var cfg = builder.Configuration.GetSection("R2R");
    var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(builder.Configuration.GetSection("R2R").GetValue<int>("MaxRetries"), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
// Register WebDevClient with resilience policies
builder.Services.AddHttpClient<IWebDevClient, WebDevClient>(client =>
{
    var cfg = builder.Configuration.GetSection("R2R");
    var url = cfg.GetValue<string>("ApiUrl") ?? throw new InvalidOperationException("R2R:ApiUrl not set");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue<int>("DefaultTimeout"));
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(builder.Configuration.GetSection("R2R").GetValue<int>("MaxRetries"), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
// Run background ingestion worker
builder.Services.AddHostedService<IngestionWorker>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");


app.UseAuthorization();

app.MapControllers();

app.Run();
