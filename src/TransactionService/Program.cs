using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using TransactionService.Consumers;
using TransactionService.Services;

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

builder.Services.AddSingleton<ITransactionProcessorService, TransactionProcessorService>();
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
