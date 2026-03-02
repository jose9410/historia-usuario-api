using Automatizacion.Agentes.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Automatizacion.Agentes.Infrastructure.Transcription
{
    public class FileTranscriptionService : ITranscriptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileTranscriptionService> _logger;

        public FileTranscriptionService(IConfiguration configuration, ILogger<FileTranscriptionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetTranscriptionAsync()
        {
            string inputDir = _configuration["TranscriptionSettings:InputDirectory"] ?? "Inputs";
            
            if (!Directory.Exists(inputDir))
            {
                _logger.LogWarning("Directorio de entrada no encontrado: {Dir}. Creándolo...", inputDir);
                Directory.CreateDirectory(inputDir);
                return string.Empty;
            }

            // Buscar el archivo .vtt más reciente
            var vttFile = new DirectoryInfo(inputDir)
                .GetFiles("*.vtt")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (vttFile == null)
            {
                _logger.LogWarning("No se encontraron archivos .vtt en {Dir}", inputDir);
                return string.Empty;
            }

            _logger.LogInformation("Leyendo transcripción desde: {File}", vttFile.FullName);

            string content = await File.ReadAllTextAsync(vttFile.FullName);
            return CleanVttContent(content);
        }

        private string CleanVttContent(string vttContent)
        {
            if (string.IsNullOrWhiteSpace(vttContent)) return string.Empty;

            var lines = vttContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();

            var timestampRegex = new Regex(@"\d{2}:\d{2}:\d{2}\.\d{3}\s-->\s\d{2}:\d{2}:\d{2}\.\d{3}", RegexOptions.Compiled);

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;
                if (int.TryParse(trimmedLine, out _)) continue;
                if (timestampRegex.IsMatch(trimmedLine)) continue;

                sb.AppendLine(trimmedLine);
            }

            return sb.ToString();
        }
    }
}
