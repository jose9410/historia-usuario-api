using Microsoft.AspNetCore.Mvc;
using Automatizacion.Agentes.Modules.HistoriaUsuario;

[ApiController]
[Route("api/[controller]")]
public class HistoriaUsuarioController : ControllerBase
{
    private readonly HistoriaUsuarioAgent _agent;
    private readonly IConfiguration _configuration;

    public HistoriaUsuarioController(HistoriaUsuarioAgent agent, IConfiguration configuration)
    {
        _agent = agent;
        _configuration = configuration;
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
        await _agent.RunAsync();
        return Ok(new { message = "Proceso de Historia de Usuario completado con éxito." });
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

