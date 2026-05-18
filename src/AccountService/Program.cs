using AccountService.Services;
using BuildingBlocks.Correlation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

builder.Services.AddScoped<IAccountService, AccountService.Services.AccountService>();

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
