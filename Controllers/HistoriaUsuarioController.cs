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

    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
    {
        await _agent.RunAsync();
        return Ok(new { message = "Proceso de Historia de Usuario completado con éxito." });
    }
}
