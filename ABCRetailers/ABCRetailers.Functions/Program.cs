using ABCRetailers.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Isolated worker HTTP pipeline
builder.ConfigureFunctionsWebApplication();

// Application Insights (optional)
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Our services
builder.Services.AddSingleton<TableService>();
builder.Services.AddSingleton<UploadService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<CounterService>();

builder.Build().Run();
