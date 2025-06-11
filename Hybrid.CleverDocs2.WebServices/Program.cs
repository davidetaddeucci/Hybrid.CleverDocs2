using System;
using Hybrid.CleverDocs2.WebServices.Services.Clients;
using Hybrid.CleverDocs2.WebServices.Services.DTOs;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// Configure R2R options
builder.Services.Configure<R2ROptions>(builder.Configuration.GetSection("R2R"));

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
