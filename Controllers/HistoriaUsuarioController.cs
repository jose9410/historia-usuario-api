using Microsoft.AspNetCore.Mvc;
using Automatizacion.AgentesKoncilia.Modules.HistoriaUsuario;

[ApiController]
[Route("api/[controller]")]
public class HistoriaUsuarioController : ControllerBase
{
    private readonly HistoriaUsuarioAgent _agent;

    public HistoriaUsuarioController(HistoriaUsuarioAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("upload-vtt")]
public async Task<IActionResult> UploadVtt(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("No se ha seleccionado ningún archivo.");

    // 1. Obtener la ruta de la carpeta de Entradas (puedes hardcodearla por ahora para probar)
    string inputDir = Path.Combine(Directory.GetCurrentDirectory(), "Inputs");
    
    if (!Directory.Exists(inputDir))
        Directory.CreateDirectory(inputDir);

    // 2. Guardar el archivo con un nombre único o sobrescribir el último
    string filePath = Path.Combine(inputDir, "transcripcion.vtt");
    
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    // 3. Ejecutar el agente para que procese el nuevo archivo
    await _agent.RunAsync();

    return Ok(new { message = "Archivo VTT recibido y procesado correctamente." });
}


    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
    {
        await _agent.RunAsync();
        return Ok(new { message = "Proceso de Historia de Usuario completado con éxito." });
    }
}
