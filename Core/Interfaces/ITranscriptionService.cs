using System.Threading.Tasks;

namespace Automatizacion.Agentes.Core.Interfaces
{
    public interface ITranscriptionService
    {
        Task<string> GetTranscriptionAsync();
    }
}
