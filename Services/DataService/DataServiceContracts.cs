// ═══════════════════════════════════════════════════════════════════════════════
// Services/DataService/DataServiceContracts.cs
// DTOs de contrato con el data-service.
//
// Se definen AQUÍ (en historia-usuario-api) en lugar de referenciar el proyecto
// data-service directamente, manteniendo el desacoplamiento entre microservicios.
// El contrato es idéntico al que expone data-service en sus Application/Contracts/.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text.Json.Serialization;

namespace Automatizacion.Agentes.Services.DataService;

// ── Enum de estado ────────────────────────────────────────────────────────────
// Debe mantenerse sincronizado con DataService.Domain.Entities.StoryStatus
public enum StoryStatus
{
    Draft    = 0,
    Approved = 1,
    Rejected = 2,
    Archived = 3
}

// ── Request (POST /api/data/stories) ─────────────────────────────────────────
public sealed record CreateStoryRequest(
    [property: JsonPropertyName("title")]   string      Title,
    [property: JsonPropertyName("content")] string      Content,
    [property: JsonPropertyName("status")]  StoryStatus Status = StoryStatus.Draft
);

// ── Response (201 Created / 200 OK) ───────────────────────────────────────────
public sealed record StoryResponse(
    [property: JsonPropertyName("id")]          Guid            Id,
    [property: JsonPropertyName("title")]        string          Title,
    [property: JsonPropertyName("content")]      string          Content,
    [property: JsonPropertyName("status")]       StoryStatus     Status,
    [property: JsonPropertyName("statusName")]   string          StatusName,
    [property: JsonPropertyName("createdAt")]    DateTimeOffset  CreatedAt
);
