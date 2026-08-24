using Automatizacion.Agentes.Observability;
using System.Diagnostics;

namespace Automatizacion.Agentes.Services;

/// <summary>
/// Typed HttpClient that calls QAAutomation.Api's /health endpoint.
///
/// Purpose: demonstrates W3C TraceContext propagation between two
/// independent .NET processes (historia-api → qaautomation-api).
///
/// The OpenTelemetry HttpClient instrumentation automatically injects
/// the "traceparent" header on every outbound request, linking the
/// downstream span to the current trace without any manual work.
/// </summary>
public sealed class QAAutomationHealthClient
{
    private readonly HttpClient _http;
    private readonly ILogger<QAAutomationHealthClient> _logger;

    public QAAutomationHealthClient(HttpClient http,
        ILogger<QAAutomationHealthClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>
    /// Calls GET /health on qaautomation-api, wrapped in a custom span
    /// to make the cross-service hop clearly visible in Jaeger / X-Ray.
    /// </summary>
    public async Task<string> CheckHealthAsync(CancellationToken ct = default)
    {
        // Custom span — child of the incoming ASP.NET Core span.
        using var activity = Telemetry.Source.StartActivity(
            "qaautomation.health_check",
            ActivityKind.Client);

        try
        {
            activity?.SetTag("peer.service", "qaautomation-api");
            activity?.SetTag("http.url", _http.BaseAddress + "health");

            var response = await _http.GetAsync("health", ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "QAAutomation health check succeeded. Status: {StatusCode}",
                response.StatusCode);

            return body;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            // Record exception using OTel semantic conventions (no extension method needed)
            activity?.SetTag("exception.type",       ex.GetType().FullName);
            activity?.SetTag("exception.message",    ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            _logger.LogError(ex,
                "QAAutomation health check failed: {Message}", ex.Message);

            // Non-fatal: return degraded status so historia-api keeps working.
            return "degraded";
        }
    }
}
