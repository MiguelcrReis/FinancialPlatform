using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using RabbitMQ.Client;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Services;
using TransactionService.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Logging (Serilog)
// --------------------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// --------------------
// Services
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------
// OpenTelemetry
// --------------------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation();
    });

// --------------------
// Application DI
// --------------------
// Repository should be a singleton for in-memory store and thread-safety
builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();

// Query service used by Controllers
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();

// Processor service is scoped (created per message handling scope)
builder.Services.AddScoped<ITransactionProcessorService, TransactionProcessorService>();

// RabbitMQ connection
builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration.GetSection("RabbitMq");
    var host = cfg.GetValue<string>("HostName") ?? "localhost";
    var user = cfg.GetValue<string>("UserName");
    var pass = cfg.GetValue<string>("Password");

    var factory = new ConnectionFactory
    {
        HostName = host,
        DispatchConsumersAsync = true
    };

    if (!string.IsNullOrEmpty(user)) factory.UserName = user;
    if (!string.IsNullOrEmpty(pass)) factory.Password = pass;

    return factory.CreateConnection();
});

// Background consumer (hosted service) - it will create scopes per message
builder.Services.AddHostedService<TransactionConsumer>();

var app = builder.Build();

// --------------------
// Middleware
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseHttpMetrics();

app.UseAuthorization();

app.MapControllers();

// Prometheus
app.MapMetrics();

app.Run();
