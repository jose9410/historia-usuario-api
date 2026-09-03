using Microsoft.AspNetCore.Mvc;
using Automatizacion.Agentes.Modules.HistoriaUsuario;
using Automatizacion.Agentes.Services;
using Automatizacion.Agentes.Services.DataService;

[ApiController]
[Route("api/[controller]")]
public class HistoriaUsuarioController : ControllerBase
{
    private readonly HistoriaUsuarioAgent _agent;
    private readonly IConfiguration _configuration;
    // Typed HttpClient — created via IHttpClientFactory so OTel HttpClient
    // instrumentation automatically injects the W3C traceparent header on
    // every outbound call, linking historia-api → qaautomation-api in one trace.
    private readonly QAAutomationHealthClient _qaClient;
    private readonly DataServiceClient? _dataClient;

    public HistoriaUsuarioController(
        HistoriaUsuarioAgent agent,
        IConfiguration configuration,
        QAAutomationHealthClient qaClient,
        DataServiceClient? dataClient = null)
    {
        _agent         = agent;
        _configuration = configuration;
        _qaClient      = qaClient;
        _dataClient    = dataClient;
    }

    [HttpPost("upload-vtt")]
    public async Task<IActionResult> UploadVtt(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            string inputDir = Path.Combine(Directory.GetCurrentDirectory(), "Inputs");
            
            if (!Directory.Exists(inputDir))
                Directory.CreateDirectory(inputDir);

            string filePath = Path.Combine(inputDir, "transcripcion.vtt");
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Ejecutar el agente
            await _agent.RunAsync();

            return Ok(new { message = "Archivo VTT recibido y procesado correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Error al procesar el archivo VTT", 
                detail = ex.Message,
                inner = ex.InnerException?.Message,
                stack = ex.StackTrace 
            });
        }
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
    {
        // Step 1: Run the AI agent (Azure OpenAI call)
        await _agent.RunAsync();

        // Step 2: Call qaautomation-api — OTel HttpClient instrumentation injects
        // the W3C traceparent header automatically, propagating the same traceId
        // to the downstream service so both appear in one distributed trace.
        var qaStatus = await _qaClient.CheckHealthAsync(HttpContext.RequestAborted);

        // Step 3: Persistir en data-service (PostgreSQL) para asegurar la traza
        // distribuida completa hacia data-service y la base de datos RDS.
        StoryResponse? savedStory = null;
        if (_dataClient != null)
        {
            var req = new CreateStoryRequest(
                $"HU-{DateTime.UtcNow:yyyyMMdd-HHmmss}: Conciliación Automática",
                "Historia de usuario generada en el flujo distribuido de observabilidad.",
                StoryStatus.Approved);
            savedStory = await _dataClient.SaveStoryAsync(req, HttpContext.RequestAborted);
        }

        return Ok(new
        {
            message         = "Proceso de Historia de Usuario completado con éxito.",
            qa_api_status   = qaStatus,
            persisted_story = savedStory
        });
    }

    // Lista los archivos generados en el directorio de salida
    [HttpGet("outputs")]
    public IActionResult ListOutputs()
    {
        try
        {
            string outputDir = _configuration["TranscriptionSettings:OutputDirectory"] ?? "Outputs";
            string fullPath = Path.IsPathRooted(outputDir)
                ? outputDir
                : Path.Combine(Directory.GetCurrentDirectory(), outputDir);

            if (!Directory.Exists(fullPath))
                return Ok(Array.Empty<object>());

            var files = Directory.GetFiles(fullPath)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new
                {
                    name = f.Name,
                    size = f.Length,
                    date = f.CreationTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }

    // Descarga un archivo específico del directorio de salida
    [HttpGet("outputs/{fileName}")]
    public IActionResult DownloadOutput(string fileName)
    {
        string outputDir = _configuration["TranscriptionSettings:OutputDirectory"] ?? "Outputs";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), outputDir, fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("Archivo no encontrado.");

        string contentType = fileName.EndsWith(".docx")
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : fileName.EndsWith(".png")
            ? "image/png"
            : "application/octet-stream";

        return PhysicalFile(fullPath, contentType, fileName);
    }
}

