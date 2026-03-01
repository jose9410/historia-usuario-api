using System.Threading.Tasks;

namespace Automatizacion.AgentesKoncilia.Core.Interfaces
{
    public interface ITranscriptionService
    {
        Task<string> GetTranscriptionAsync();
    }
}
