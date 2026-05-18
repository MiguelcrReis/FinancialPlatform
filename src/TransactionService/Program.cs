using BuildingBlocks.Correlation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using RabbitMQ.Client;
using MongoDB.Driver;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Infrastructure.Settings;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Services;
using TransactionService.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Environment.ApplicationName;
var environmentName = builder.Environment.EnvironmentName;
var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

// --------------------
// Logging (Serilog)
// --------------------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", serviceName)
    .Enrich.WithProperty("Environment", environmentName)
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpEndpoint;
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = serviceName,
            ["deployment.environment"] = environmentName,
            ["service.namespace"] = "FinancialPlatform"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

// --------------------
// Services
// --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

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
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(MongoDbSettings.SectionName));

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

builder.Services.AddSingleton<IMongoClient>(_ =>
{
    var settings = builder.Configuration
        .GetSection(MongoDbSettings.SectionName)
        .Get<MongoDbSettings>() ?? new MongoDbSettings();

    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<ITransactionRepository, MongoTransactionRepository>();

// Query service used by Controllers
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();

// Processor service is scoped (created per message handling scope)
builder.Services.AddScoped<ITransactionProcessorService, TransactionProcessorService>();

// RabbitMQ connection
builder.Services.AddSingleton(sp =>
{
    var settings = builder.Configuration
        .GetSection(RabbitMqSettings.SectionName)
        .Get<RabbitMqSettings>() ?? new RabbitMqSettings();

    var factory = new ConnectionFactory
    {
        HostName = settings.HostName,
        DispatchConsumersAsync = true
    };

    if (!string.IsNullOrEmpty(settings.UserName)) factory.UserName = settings.UserName;
    if (!string.IsNullOrEmpty(settings.Password)) factory.Password = settings.Password;

    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

    return CreateRabbitMqConnectionWithRetry(
        factory,
        logger,
        settings.ConnectionRetryCount,
        TimeSpan.FromSeconds(settings.ConnectionRetryDelaySeconds));
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

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseHttpMetrics();

app.UseAuthorization();

app.MapControllers();

// Prometheus
app.MapMetrics();

app.Run();

static IConnection CreateRabbitMqConnectionWithRetry(
    ConnectionFactory factory,
    Microsoft.Extensions.Logging.ILogger logger,
    int maxAttempts,
    TimeSpan retryDelay)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            return factory.CreateConnection();
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "RabbitMQ connection failed. Attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                retryDelay.TotalSeconds);

            Thread.Sleep(retryDelay);
        }
    }

    return factory.CreateConnection();
}
