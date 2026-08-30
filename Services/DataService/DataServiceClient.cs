// ═══════════════════════════════════════════════════════════════════════════════
// Services/DataService/DataServiceClient.cs
// Typed HttpClient para llamar al microservicio data-service.
//
// PATRÓN: Sigue exactamente el mismo diseño que QAAutomationHealthClient:
//   - Recibe HttpClient inyectado por IHttpClientFactory (registrado en Program.cs)
//   - Crea un custom OTel span por operación para visibilidad en Jaeger / X-Ray
//   - El OTel HttpClient instrumentation inyecta automáticamente el W3C traceparent
//     → la traza historia-api → data-service queda enlazada en una sola traza distribuida
//   - En caso de error: log + retorno null (degradación sin falla de la generación)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using System.Net.Http.Json;
using Automatizacion.Agentes.Observability;
using Automatizacion.Agentes.Services.DataService;

namespace Automatizacion.Agentes.Services;

/// <summary>
/// Cliente HTTP tipado para el microservicio data-service.
/// Persiste las historias de usuario generadas por la IA en PostgreSQL
/// a través del endpoint POST /api/data/stories del data-service.
/// </summary>
public sealed class DataServiceClient
{
    private readonly HttpClient                     _http;
    private readonly ILogger<DataServiceClient>     _logger;

    // El HttpClient es inyectado por IHttpClientFactory con el BaseAddress
    // ya configurado desde DATA_SERVICE_URL (ver Program.cs).
    public DataServiceClient(
        HttpClient                  http,
        ILogger<DataServiceClient>  logger)
    {
        _http   = http;
        _logger = logger;
    }

    /// <summary>
    /// Persiste una historia de usuario en el data-service vía POST JSON.
    /// </summary>
    /// <param name="request">Título, contenido y estado de la historia.</param>
    /// <param name="ct">CancellationToken del request entrante (propagado).</param>
    /// <returns>
    /// El <see cref="StoryResponse"/> creado por el data-service,
    /// o <c>null</c> si el servicio no está disponible (degradación elegante).
    /// </returns>
    public async Task<StoryResponse?> SaveStoryAsync(
        CreateStoryRequest  request,
        CancellationToken   ct = default)
    {
        // ── Custom span OTel ──────────────────────────────────────────────────
        // Hijo del span raíz del request HTTP entrante.
        // Aparece en Jaeger / X-Ray como "data-service.save_story" con los
        // atributos de negocio que enriquecen la traza.
        using var activity = Telemetry.Source.StartActivity(
            "data-service.save_story",
            ActivityKind.Client);

        activity?.SetTag("peer.service",       "data-service");
        activity?.SetTag("http.url",           $"{_http.BaseAddress}api/data/stories");
        activity?.SetTag("story.title_length", request.Title.Length);
        activity?.SetTag("story.status",       request.Status.ToString());

        try
        {
            _logger.LogInformation(
                "Persistiendo historia en data-service. Título: {Title} ({Chars} chars)",
                request.Title, request.Title.Length);

            // PostAsJsonAsync serializa el record a JSON y establece Content-Type:
            // application/json. El OTel HttpClient instrumentation inyecta
            // automáticamente el header 'traceparent' para propagación W3C.
            var response = await _http.PostAsJsonAsync(
                "api/data/stories",
                request,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "data-service devolvió {StatusCode} al intentar guardar la historia. Body: {Body}",
                    (int)response.StatusCode, body);

                activity?.SetTag("http.status_code", (int)response.StatusCode);
                activity?.SetStatus(ActivityStatusCode.Error,
                    $"HTTP {(int)response.StatusCode}");

                return null;  // Degradación elegante — la generación ya fue exitosa
            }

            var story = await response.Content.ReadFromJsonAsync<StoryResponse>(ct);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("story.id",          story?.Id.ToString() ?? "unknown");
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Historia persistida exitosamente en data-service. Id: {StoryId}",
                story?.Id);

            return story;
        }
        catch (HttpRequestException ex)
        {
            // data-service no disponible (timeout, DNS, conexión rechazada)
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type",       ex.GetType().FullName);
            activity?.SetTag("exception.message",    ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            _logger.LogError(ex,
                "No se pudo conectar al data-service para persistir la historia. " +
                "La historia generada NO fue guardada en BD. Error: {Message}",
                ex.Message);

            return null;  // La generación del .docx continúa sin persistencia
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type",       ex.GetType().FullName);
            activity?.SetTag("exception.message",    ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            _logger.LogError(ex,
                "Error inesperado al llamar al data-service: {Message}", ex.Message);

            return null;
        }
    }
}
