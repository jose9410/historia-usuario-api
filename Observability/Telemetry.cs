using System.Diagnostics;

namespace Automatizacion.Agentes.Observability;

/// <summary>
/// Centralized ActivitySource for historia-api.
/// All custom spans must use this instance so they are captured
/// by the OpenTelemetry SDK (which is registered with the same source name).
/// </summary>
public static class Telemetry
{
    public const string ServiceName    = "historia-api";
    public const string ServiceVersion = "1.0.0";

    /// <summary>The single ActivitySource for the service.</summary>
    public static readonly ActivitySource Source =
        new(ServiceName, ServiceVersion);
}
