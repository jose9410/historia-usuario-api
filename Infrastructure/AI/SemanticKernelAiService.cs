using Automatizacion.AgentesKoncilia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automatizacion.AgentesKoncilia.Infrastructure.AI
{
    /// <summary>
    /// Servicio genérico de IA basado en Semantic Kernel.
    /// No conoce ningún prompt ni modelo de negocio específico.
    /// Cada módulo le envía su propio prompt y recibe su propio tipo de respuesta.
    /// </summary>
    public class SemanticKernelAiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<SemanticKernelAiService> _logger;

        public SemanticKernelAiService(Kernel kernel, IChatCompletionService chatCompletionService, ILogger<SemanticKernelAiService> logger)
        {
            _kernel = kernel;
            _chatCompletionService = chatCompletionService;
            _logger = logger;
        }

        public async Task<T?> AnalizarAsync<T>(string transcripcion, string systemPrompt) where T : class
        {
            _logger.LogInformation("Iniciando análisis con IA...");

            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(transcripcion);

            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                Temperature = 0.3
            };

            try
            {
                var resultado = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory: history,
                    executionSettings: settings,
                    kernel: _kernel
                );

                string jsonResultado = resultado.Content ?? string.Empty;
                _logger.LogInformation("Respuesta recibida de IA.");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                return JsonSerializer.Deserialize<T>(jsonResultado, jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al comunicarse con la IA o deserializar la respuesta.");
                throw;
            }
        }
    }
}
