using Automatizacion.Agentes.Core.Interfaces;
using Automatizacion.Agentes.Infrastructure.AI;
using Automatizacion.Agentes.Infrastructure.Diagrams;
using Automatizacion.Agentes.Infrastructure.Transcription;
using Automatizacion.Agentes.Modules.HistoriaUsuario;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = WebApplication.CreateBuilder(args); // <--- Línea corregida


#pragma warning disable SKEXP0070

// === Registro de Servicios ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Semantic Kernel (Traído de tu Program.cs original)
builder.Services.AddSingleton<Kernel>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();
    
    string provider = cfg["AiSettings:Provider"] ?? "AzureOpenAI";

    if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
    {
        string apiKey = cfg["Anthropic:ApiKey"] ?? throw new Exception("Falta Anthropic:ApiKey");
        string modelId = cfg["Anthropic:ModelId"] ?? "claude-3-5-sonnet-20240620";
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1200) };
        kernelBuilder.AddAnthropicChatCompletion(apiKey, modelId, options: null, httpClient: httpClient);
    }
    else
    {
        string deploymentName = cfg["AzureOpenAI:DeploymentName"] ?? throw new Exception("Falta AzureOpenAI:DeploymentName");
        string endpoint = cfg["AzureOpenAI:Endpoint"] ?? throw new Exception("Falta AzureOpenAI:Endpoint");
        string apiKey = cfg["AzureOpenAI:ApiKey"] ?? throw new Exception("Falta AzureOpenAI:ApiKey");
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1200) };
        kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey, httpClient: httpClient);
    }

    return kernelBuilder.Build();
});

builder.Services.AddTransient<IChatCompletionService>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    return kernel.GetRequiredService<IChatCompletionService>();
});

// Registrar tus otros servicios
builder.Services.AddTransient<IAiService, SemanticKernelAiService>();
builder.Services.AddTransient<ITranscriptionService, FileTranscriptionService>();
builder.Services.AddTransient<IPlantUmlService, PlantUmlService>();
builder.Services.AddTransient<HistoriaUsuarioWordService>();
builder.Services.AddTransient<HistoriaUsuarioAgent>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy"));
app.Run();
