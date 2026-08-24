// ═══════════════════════════════════════════════════════════════════════════════
// historia-api  —  Program.cs
// OpenTelemetry SDK wiring: Traces, Metrics, Logs
// .NET 9
// ═══════════════════════════════════════════════════════════════════════════════

using Automatizacion.Agentes.Core.Interfaces;
using Automatizacion.Agentes.Infrastructure.AI;
using Automatizacion.Agentes.Infrastructure.Diagrams;
using Automatizacion.Agentes.Infrastructure.Transcription;
using Automatizacion.Agentes.Modules.HistoriaUsuario;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Observability;
using Automatizacion.Agentes.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

#pragma warning disable SKEXP0070

var builder = WebApplication.CreateBuilder(args);
var cfg     = builder.Configuration;

// ─────────────────────────────────────────────────────────────────────────────
// 1.  OpenTelemetry — shared resource
// ─────────────────────────────────────────────────────────────────────────────
var otlpEndpoint = cfg["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(
        serviceName:    Telemetry.ServiceName,
        serviceVersion: Telemetry.ServiceVersion)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = cfg["ASPNETCORE_ENVIRONMENT"] ?? "production",
        ["host.name"]              = Environment.MachineName,
    });

// ─────────────────────────────────────────────────────────────────────────────
// 2.  Logging — structured JSON + OTel log bridge
//     The OTel log provider injects trace_id / span_id automatically on every
//     ILogger call when a span is active (no Serilog needed).
// ─────────────────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
    options.FormatterName = "json");     // structured JSON to stdout
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(resourceBuilder);
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes           = true;
    // Sends every ILogger record (with trace_id/span_id) to the collector
    logging.AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri(otlpEndpoint);
        o.Protocol = OtlpExportProtocol.Grpc;
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// 3.  OpenTelemetry SDK — Traces + Metrics
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    // ── Traces ──────────────────────────────────────────────────────────────
    .WithTracing(traces =>
    {
        traces
            .SetResourceBuilder(resourceBuilder)
            .SetSampler(new AlwaysOnSampler())
            // ASP.NET Core: captures every inbound HTTP request as a root span
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                // Exclude noisy health/metrics probes from traces
                o.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/health") &&
                    !ctx.Request.Path.StartsWithSegments("/metrics");
            })
            // HttpClient: propagates W3C traceparent to qaautomation-api
            .AddHttpClientInstrumentation(o =>
            {
                o.RecordException = true;
                // Suppress spans for Semantic Kernel AI calls (very chatty)
                o.FilterHttpRequestMessage = req =>
                    req.RequestUri?.Host?.Contains("openai") == false &&
                    req.RequestUri?.Host?.Contains("anthropic") == false;
            })
            // SqlClient: captures raw SQL queries as child spans (rubric DB requirement)
            .AddSqlClientInstrumentation(o =>
            {
                o.SetDbStatementForText    = true;
                o.RecordException          = true;
                o.EnableConnectionLevelAttributes = true;
            })
            // Custom spans from Telemetry.Source
            .AddSource(Telemetry.ServiceName)
            // OTLP → Collector sidecar (gRPC)
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otlpEndpoint);
                o.Protocol = OtlpExportProtocol.Grpc;
            });
    })
    // ── Metrics ─────────────────────────────────────────────────────────────
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(resourceBuilder)
            // Standard ASP.NET Core metrics: request rate, duration, active requests
            .AddAspNetCoreInstrumentation()
            // HttpClient metrics: outbound request duration, count
            .AddHttpClientInstrumentation()
            // .NET runtime metrics: GC, thread pool, heap — satisfies CPU SLI panel
            .AddRuntimeInstrumentation()
            // Process metrics: CPU time, memory, handle count
            .AddProcessInstrumentation()
            // Exposes /metrics for Prometheus scraping (replaces prometheus-net)
            .AddPrometheusExporter()
            // Also forward metrics to collector via OTLP
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otlpEndpoint);
                o.Protocol = OtlpExportProtocol.Grpc;
            });
    });

// ─────────────────────────────────────────────────────────────────────────────
// 4.  HttpClient — typed client for cross-service call (service-a → service-b)
// ─────────────────────────────────────────────────────────────────────────────
var qaBaseUrl = cfg["Services:QAAutomationApi"] ?? "http://localhost:8081";
builder.Services.AddHttpClient<QAAutomationHealthClient>(client =>
{
    client.BaseAddress = new Uri(qaBaseUrl);
    client.Timeout     = TimeSpan.FromSeconds(10);
});

// ─────────────────────────────────────────────────────────────────────────────
// 5.  Application services (unchanged from original)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    string provider = config["AiSettings:Provider"] ?? "AzureOpenAI";

    if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
    {
        string apiKey  = config["Anthropic:ApiKey"]  ?? throw new Exception("Falta Anthropic:ApiKey");
        string modelId = config["Anthropic:ModelId"] ?? "claude-3-5-sonnet-20240620";
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1200) };
        kernelBuilder.AddAnthropicChatCompletion(apiKey, modelId, options: null, httpClient: httpClient);
    }
    else
    {
        string deploymentName = config["AzureOpenAI:DeploymentName"] ?? throw new Exception("Falta AzureOpenAI:DeploymentName");
        string endpoint       = config["AzureOpenAI:Endpoint"]       ?? throw new Exception("Falta AzureOpenAI:Endpoint");
        string apiKey         = config["AzureOpenAI:ApiKey"]         ?? throw new Exception("Falta AzureOpenAI:ApiKey");
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1200) };
        kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey, httpClient: httpClient);
    }

    return kernelBuilder.Build();
});

builder.Services.AddTransient<IChatCompletionService>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    return kernel.GetRequiredService<IChatCompletionService>();
});

builder.Services.AddTransient<IAiService, SemanticKernelAiService>();
builder.Services.AddTransient<ITranscriptionService, FileTranscriptionService>();
builder.Services.AddTransient<IPlantUmlService, PlantUmlService>();
builder.Services.AddTransient<HistoriaUsuarioWordService>();
builder.Services.AddTransient<HistoriaUsuarioAgent>();

// ─────────────────────────────────────────────────────────────────────────────
// 6.  Middleware pipeline
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Prometheus scrape endpoint — replaces prometheus-net's app.MapMetrics()
app.MapPrometheusScrapingEndpoint("/metrics");

app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status  = "healthy",
    service = Telemetry.ServiceName,
    version = Telemetry.ServiceVersion
}));

app.Run();
