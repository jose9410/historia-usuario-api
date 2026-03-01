using System.Threading.Tasks;

namespace Automatizacion.AgentesKoncilia.Core.Interfaces
{
    /// <summary>
    /// Contrato genérico para el servicio de IA.
    /// Cada módulo envía su propio prompt y recibe su propio modelo de respuesta.
    /// </summary>
    public interface IAiService
    {
        Task<T?> AnalizarAsync<T>(string transcripcion, string systemPrompt) where T : class;
    }
}
