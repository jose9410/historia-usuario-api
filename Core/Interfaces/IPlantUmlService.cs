using System.Threading.Tasks;

namespace Automatizacion.AgentesKoncilia.Core.Interfaces
{
    public interface IPlantUmlService
    {
        Task<string?> RenderToImageAsync(string plantUmlCode, string outputId, string outputDirectory);
    }
}
