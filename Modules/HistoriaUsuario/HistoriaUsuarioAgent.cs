using Automatizacion.Agentes.Core.Interfaces;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Prompts;
using Automatizacion.Agentes.Services;
using Automatizacion.Agentes.Services.DataService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Automatizacion.Agentes.Modules.HistoriaUsuario
{
    /// <summary>
    /// Agente orquestador para el módulo de Historia de Usuario.
    /// Usa el servicio genérico de IA con su prompt especializado.
    /// Al finalizar la generación, persiste cada requerimiento en el
    /// data-service (PostgreSQL) mediante <see cref="DataServiceClient"/>.
    /// </summary>
    public class HistoriaUsuarioAgent
    {
        private readonly IAiService                     _aiService;
        private readonly HistoriaUsuarioWordService     _documentService;
        private readonly ITranscriptionService          _transcriptionService;
        private readonly IPlantUmlService               _plantUmlService;
        private readonly IConfiguration                 _configuration;
        private readonly ILogger<HistoriaUsuarioAgent>  _logger;
        // Cliente HTTP tipado hacia el data-service (puede ser null si no está
        // registrado, para mantener compatibilidad hacia atrás).
        private readonly DataServiceClient?             _dataClient;

        public HistoriaUsuarioAgent(
            IAiService                      aiService,
            HistoriaUsuarioWordService      documentService,
            ITranscriptionService           transcriptionService,
            IPlantUmlService                plantUmlService,
            IConfiguration                  configuration,
            ILogger<HistoriaUsuarioAgent>   logger,
            DataServiceClient?              dataClient = null)   // opcional para retrocompatibilidad
        {
            _aiService            = aiService;
            _documentService      = documentService;
            _transcriptionService = transcriptionService;
            _plantUmlService      = plantUmlService;
            _configuration        = configuration;
            _logger               = logger;
            _dataClient           = dataClient;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Inicializando Agente de Historia de Usuario...");

            try
            {
                // 1. Obtener Transcripción
                string transcripcion = await _transcriptionService.GetTranscriptionAsync();

                // 2. Analizar con IA usando el prompt de Historia de Usuario
                var respuesta = await _aiService.AnalizarAsync<RespuestaHistoriaUsuario>(
                    transcripcion,
                    HistoriaUsuarioPrompt.SystemMessage
                );

                if (respuesta?.Proyectos != null && respuesta.Proyectos.Count > 0)
                {
                    _logger.LogInformation("Se identificaron {Count} proyectos/requerimientos.", respuesta.Proyectos.Count);

                    var imagenesDiagramas = new System.Collections.Generic.Dictionary<int, string>();

                    for (int i = 0; i < respuesta.Proyectos.Count; i++)
                    {
                        var req = respuesta.Proyectos[i];
                        Console.WriteLine($"\n📋 Proceso: {req.NombreProceso}");
                        Console.WriteLine($"   ¿Qué se quiere hacer?: {req.QueSeQuiereHacer}");

                        if (!string.IsNullOrWhiteSpace(req.PlantUml) && req.PlantUml != "Informacion no discutida en el proceso")
                        {
                            string targetDir = _configuration["TranscriptionSettings:OutputDirectory"] ?? "Outputs";
                            if (!System.IO.Directory.Exists(targetDir))
                            {
                                System.IO.Directory.CreateDirectory(targetDir);
                            }

                            string outputId = $"diagrama_{i}_{DateTime.Now:HHmmss}";
                            string? pathImagen = await _plantUmlService.RenderToImageAsync(req.PlantUml, outputId, targetDir);
                            if (pathImagen != null)
                            {
                                imagenesDiagramas[i] = pathImagen;
                            }
                        }
                    }

                    // 3. Generar Documento con marca de tiempo
                    string outputDir = _configuration["TranscriptionSettings:OutputDirectory"] ?? "Outputs";
                    if (!System.IO.Directory.Exists(outputDir))
                    {
                        System.IO.Directory.CreateDirectory(outputDir);
                    }

                    string timestamp     = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string nombreArchivo = $"HistoriaUsuario_{timestamp}.docx";
                    string rutaDocumento = System.IO.Path.Combine(outputDir, nombreArchivo);

                    _documentService.GenerateDocument(respuesta.Proyectos, rutaDocumento, imagenesDiagramas);

                    _logger.LogInformation("✅ Proceso completado. Documento generado en: {Ruta}", rutaDocumento);

                    // ── 4. Persistir en data-service (PostgreSQL) ─────────────────────
                    // Se ejecuta DESPUÉS de que el .docx fue generado con éxito.
                    // Si el data-service no está disponible, el error se loguea pero
                    // NO interrumpe el flujo — el documento ya fue generado.
                    await PersistirHistoriasAsync(respuesta.Proyectos, timestamp, ct);
                }
                else
                {
                    _logger.LogWarning("⚠️ No se encontraron requerimientos en la respuesta de la IA.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ocurrió un error durante la ejecución del agente.");
            }
        }

        /// <summary>
        /// Persiste cada <see cref="Requerimiento"/> generado como una
        /// <c>UserStory</c> en el data-service. Los errores de red/BD son
        /// no-fatales: se loguean y el flujo continúa.
        /// </summary>
        private async Task PersistirHistoriasAsync(
            System.Collections.Generic.List<Requerimiento> requerimientos,
            string                                          sessionTimestamp,
            CancellationToken                               ct)
        {
            if (_dataClient is null)
            {
                _logger.LogDebug(
                    "DataServiceClient no registrado — las historias no se persistirán en BD.");
                return;
            }

            _logger.LogInformation(
                "Iniciando persistencia de {Count} historia(s) en data-service...",
                requerimientos.Count);

            int guardadas = 0;

            foreach (var req in requerimientos)
            {
                // ── Construir el contenido estructurado ───────────────────────
                // Mapea los campos del Requerimiento al Content de la UserStory
                // en formato legible y estructurado, listo para futuras búsquedas.
                var content = BuildStoryContent(req);

                // Título: NombreProceso (máx. 500 chars por contrato del data-service)
                var title = string.IsNullOrWhiteSpace(req.NombreProceso)
                    ? $"Historia generada el {sessionTimestamp}"
                    : req.NombreProceso.Length > 490
                        ? req.NombreProceso[..490] + "..."
                        : req.NombreProceso;

                var request = new CreateStoryRequest(
                    Title:   title,
                    Content: content,
                    Status:  StoryStatus.Draft  // Las historias generadas por IA empiezan en Draft
                );

                var saved = await _dataClient.SaveStoryAsync(request, ct);

                if (saved is not null)
                {
                    guardadas++;
                    _logger.LogInformation(
                        "  ✅ Historia persistida [{Index}/{Total}] Id={StoryId} Título='{Title}'",
                        guardadas, requerimientos.Count, saved.Id, saved.Title);
                }
                else
                {
                    _logger.LogWarning(
                        "  ⚠️ No se pudo persistir la historia '{Title}' — continuando.",
                        title);
                }
            }

            _logger.LogInformation(
                "Persistencia completada: {Guardadas}/{Total} historias guardadas en data-service.",
                guardadas, requerimientos.Count);
        }

        /// <summary>
        /// Construye un contenido estructurado a partir de los campos del
        /// <see cref="Requerimiento"/>, en formato legible para auditores y QA.
        /// </summary>
        private static string BuildStoryContent(Requerimiento req)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrWhiteSpace(req.QueSeQuiereHacer))
                sb.AppendLine($"## ¿Qué se quiere hacer?\n{req.QueSeQuiereHacer}\n");

            if (!string.IsNullOrWhiteSpace(req.ParaQueSirve))
                sb.AppendLine($"## ¿Para qué sirve?\n{req.ParaQueSirve}\n");

            if (!string.IsNullOrWhiteSpace(req.ComoDeberiaFuncionar))
                sb.AppendLine($"## ¿Cómo debería funcionar?\n{req.ComoDeberiaFuncionar}\n");

            if (!string.IsNullOrWhiteSpace(req.QueSeNecesita))
                sb.AppendLine($"## ¿Qué se necesita?\n{req.QueSeNecesita}\n");

            if (!string.IsNullOrWhiteSpace(req.CriteriosAceptacion))
                sb.AppendLine($"## Criterios de Aceptación\n{req.CriteriosAceptacion}\n");

            if (!string.IsNullOrWhiteSpace(req.Asistentes))
                sb.AppendLine($"## Asistentes\n{req.Asistentes}");

            return sb.Length > 0
                ? sb.ToString().Trim()
                : "(Contenido no disponible)";
        }
    }
}

