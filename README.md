# HistoriaUsuario.Api

## Descripción

HistoriaUsuario.Api es una API RESTful desarrollada en ASP.NET Core (.NET 9.0) diseñada para automatizar la extracción y documentación de historias de usuario (requerimientos funcionales) a partir de transcripciones de reuniones. Utiliza inteligencia artificial (IA) para analizar transcripciones en formato VTT, extraer información estructurada y generar documentos profesionales en formato Word, incluyendo diagramas C4 generados con PlantUML.

El proyecto sigue una arquitectura limpia (Clean Architecture) con separación de responsabilidades en capas: Core (interfaces), Infrastructure (implementaciones) y Modules (lógica de negocio).

## Características Principales

### Funcionalidades de la API
- **Carga de Transcripciones**: Endpoint para subir archivos VTT de transcripciones de reuniones.
- **Análisis Automático con IA**: Procesamiento de transcripciones usando modelos de IA (Azure OpenAI o Anthropic) para extraer requerimientos estructurados.
- **Generación de Diagramas**: Creación automática de diagramas C4 de contenedores usando PlantUML.
- **Documentación Profesional**: Generación de documentos Word (.docx) con toda la información extraída, incluyendo diagramas incrustados.
- **Arquitectura Modular**: Diseño extensible para agregar nuevos módulos de análisis.

### Endpoints de la API
- `POST /api/HistoriaUsuario/upload-vtt`: Sube un archivo VTT, lo guarda y ejecuta el proceso de análisis.
- `POST /api/HistoriaUsuario/generate`: Ejecuta el proceso de análisis usando la transcripción existente.
- `GET /health`: Endpoint de salud para verificar el estado de la aplicación.

### Servicios Integrados
- **IA (Semantic Kernel)**: Soporte para Azure OpenAI y Anthropic Claude.
- **Transcripción**: Lectura de archivos VTT desde el directorio `Inputs`.
- **Diagramas**: Renderizado de PlantUML a imágenes PNG.
- **Documentos**: Generación de Word con OpenXML, incluyendo headers/footers personalizados.

## Arquitectura del Proyecto

### Estructura de Directorios
```
HistoriaUsuario.Api/
├── Controllers/                 # Controladores de la API
│   └── HistoriaUsuarioController.cs
├── Core/                        # Interfaces y contratos
│   ├── Converters/
│   │   └── StringOrArrayConverter.cs
│   └── Interfaces/
│       ├── IAiService.cs
│       ├── IPlantUmlService.cs
│       └── ITranscriptionService.cs
├── Infrastructure/              # Implementaciones concretas
│   ├── AI/
│   │   └── SemanticKernelAiService.cs
│   ├── Diagrams/
│   │   └── PlantUmlService.cs
│   └── Transcription/
│       └── FileTranscriptionService.cs
├── Modules/                     # Lógica de negocio modular
│   └── HistoriaUsuario/
│       ├── Documents/
│       │   └── HistoriaUsuarioWordService.cs
│       ├── Models/
│       │   └── HistoriaUsuarioModels.cs
│       ├── Prompts/
│       │   └── HistoriaUsuarioPrompt.cs
│       └── HistoriaUsuarioAgent.cs
├── Assets/                      # Recursos embebidos (headers/footers)
├── Properties/
│   └── launchSettings.json
├── appsettings.Development.json
├── Dockerfile
├── HistoriaUsuario.Api.csproj
├── Koncilia.HistoriaUsuario.Api.http
└── Program.cs
```

### Modelos de Datos
- **Requerimiento**: Representa un requerimiento funcional extraído, con campos como:
  - Nombre del Proceso
  - Personas Asistentes
  - Objetivo Específico
  - Justificación de Negocio
  - Alcance Funcional Clave
  - Dependencias y Fuentes
  - Criterios de Aceptación
  - Resumen Ejecutivo
  - Flujo Funcional Alto Nivel
  - Diagrama C4 (código PlantUML)

### Servicios
- **IAiService**: Interfaz para análisis con IA, implementada con Semantic Kernel.
- **IPlantUmlService**: Renderiza diagramas PlantUML a imágenes.
- **ITranscriptionService**: Lee transcripciones desde archivos.

## Requisitos del Sistema

- .NET 9.0 SDK
- Java 17+ (para PlantUML)
- Acceso a servicios de IA (Azure OpenAI o Anthropic)

## Configuración

### Variables de Entorno / appsettings.json
```json
{
  "AiSettings": {
    "Provider": "AzureOpenAI" // o "Anthropic"
  },
  "AzureOpenAI": {
    "DeploymentName": "your-deployment",
    "Endpoint": "https://your-endpoint.openai.azure.com/",
    "ApiKey": "your-api-key"
  },
  "Anthropic": {
    "ApiKey": "your-api-key",
    "ModelId": "claude-3-5-sonnet-20240620"
  },
  "TranscriptionSettings": {
    "OutputDirectory": "Outputs"
  }
}
```

## Instalación y Ejecución

### Desarrollo Local
1. Clona el repositorio.
2. Restaura dependencias: `dotnet restore`
3. Configura las variables de entorno en `appsettings.Development.json`.
4. Ejecuta la aplicación: `dotnet run`
5. Accede a Swagger UI en `https://localhost:5001/swagger`

### Con Docker
1. Construye la imagen: `docker build -t historia-usuario-api .`
2. Ejecuta el contenedor: `docker run -p 8080:80 historia-usuario-api`

## Uso de la API

### Subir Transcripción VTT
```bash
curl -X POST "https://localhost:5001/api/HistoriaUsuario/upload-vtt" \
     -F "file=@transcripcion.vtt"
```

### Generar Análisis
```bash
curl -X POST "https://localhost:5001/api/HistoriaUsuario/generate"
```

Los documentos generados se guardan en el directorio `Outputs` con timestamps.

## Dependencias Principales

- **Microsoft.SemanticKernel**: Framework para integración con IA.
- **Anthropic.SDK**: Cliente para Anthropic Claude.
- **DocumentFormat.OpenXml**: Manipulación de documentos Word.
- **Swashbuckle.AspNetCore**: Generación de documentación Swagger.

## Contribución

1. Fork el proyecto.
2. Crea una rama para tu feature: `git checkout -b feature/nueva-funcionalidad`
3. Commit tus cambios: `git commit -am 'Agrega nueva funcionalidad'`
4. Push a la rama: `git push origin feature/nueva-funcionalidad`
5. Abre un Pull Request.
