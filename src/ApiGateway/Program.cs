using ApiGateway.Messaging.Publishers;
using ApiGateway.Services;
using BuildingBlocks.Correlation;
using BuildingBlocks.Messaging.Interfaces;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Prometheus;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

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
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

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

builder.Services.AddHttpClient("TransactionService", client =>
{
    var baseUrl = builder.Configuration["Services:TransactionService:BaseUrl"]
        ?? "https://localhost:7057";

    client.BaseAddress = new Uri(baseUrl);
});


builder.Services.AddHttpClient("AccountService", client =>
{
    var baseUrl = builder.Configuration["Services:AccountService:BaseUrl"]
        ?? "http://localhost:5092";

    client.BaseAddress = new Uri(baseUrl);
})
.AddTransientHttpErrorPolicy(policy => policy
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt)
    )
)
.AddTransientHttpErrorPolicy(policy => policy
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 2,
        durationOfBreak: TimeSpan.FromSeconds(10)
    )
)
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(10),
    Polly.Timeout.TimeoutStrategy.Pessimistic))
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddScoped<TransactionServiceClient>();
builder.Services.AddScoped<AccountServiceClient>();

builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

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
