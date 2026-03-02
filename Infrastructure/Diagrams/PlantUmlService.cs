using Automatizacion.Agentes.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Automatizacion.Agentes.Infrastructure.Diagrams
{
    public class PlantUmlService : IPlantUmlService
    {
        private readonly ILogger<PlantUmlService> _logger;
        private const string ToolsDir = "Tools";
        private const string JarName = "plantuml.jar";

        public PlantUmlService(ILogger<PlantUmlService> logger)
        {
            _logger = logger;
        }

        public async Task<string?> RenderToImageAsync(string plantUmlCode, string outputId, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(plantUmlCode)) return null;

            string jarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ToolsDir, JarName);
            if (!File.Exists(jarPath))
            {
                 jarPath = Path.Combine(Directory.GetCurrentDirectory(), ToolsDir, JarName);
            }

            if (!File.Exists(jarPath))
            {
                _logger.LogError("No se encontró plantuml.jar en {Path}", jarPath);
                return null;
            }

            string absoluteOutputDir = Path.GetFullPath(outputDirectory);
            string pumlPath = Path.Combine(absoluteOutputDir, $"{outputId}.puml");
            string svgPath = Path.Combine(absoluteOutputDir, $"{outputId}.svg");

            try
            {
                await File.WriteAllTextAsync(pumlPath, plantUmlCode);
                _logger.LogInformation("Archivo fuente puml guardado en: {Path}", pumlPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{jarPath}\" -tsvg \"{pumlPath}\" -o \"{absoluteOutputDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return null;

                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(svgPath))
                {
                    _logger.LogInformation("Diagrama SVG renderizado exitosamente: {Path}", svgPath);
                    return svgPath;
                }
                else
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("Error al renderizar PlantUML a SVG: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción durante el renderizado de PlantUML.");
            }

            return null;
        }
    }
}
