using Automatizacion.Agentes.Core.Interfaces;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Prompts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Automatizacion.Agentes.Modules.HistoriaUsuario
{
    /// <summary>
    /// Agente orquestador para el módulo de Historia de Usuario.
    /// Usa el servicio genérico de IA con su prompt especializado.
    /// </summary>
    public class HistoriaUsuarioAgent
    {
        private readonly IAiService _aiService;
        private readonly HistoriaUsuarioWordService _documentService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly IPlantUmlService _plantUmlService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HistoriaUsuarioAgent> _logger;

        public HistoriaUsuarioAgent(
            IAiService aiService,
            HistoriaUsuarioWordService documentService,
            ITranscriptionService transcriptionService,
            IPlantUmlService plantUmlService,
            IConfiguration configuration,
            ILogger<HistoriaUsuarioAgent> logger)
        {
            _aiService = aiService;
            _documentService = documentService;
            _transcriptionService = transcriptionService;
            _plantUmlService = plantUmlService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RunAsync()
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

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string nombreArchivo = $"HistoriaUsuario_{timestamp}.docx";
                    string rutaDocumento = System.IO.Path.Combine(outputDir, nombreArchivo);

                    _documentService.GenerateDocument(respuesta.Proyectos, rutaDocumento, imagenesDiagramas);

                    _logger.LogInformation("✅ Proceso completado. Documento generado en: {Ruta}", rutaDocumento);
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
    }
}
