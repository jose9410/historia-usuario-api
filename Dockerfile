# 1. Etapa de Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia los archivos del proyecto y restaura dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia el resto del código y compila
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Etapa de Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# --- NUEVA SECCIÓN: Instalar Java para PlantUML ---
RUN apt-get update && apt-get install -y openjdk-17-jre-headless && rm -rf /var/lib/apt/lists/*

# Copia los binarios compilados
COPY --from=build /app/out .

# Copia las herramientas (PlantUML)
COPY Tools ./Tools

# --- NUEVA SECCIÓN: Crear carpetas de trabajo para el Agente ---
RUN mkdir -p /app/Inputs /app/Outputs && chmod -R 777 /app/Inputs /app/Outputs

# Exponer el puerto
EXPOSE 8080

ENTRYPOINT ["dotnet", "Koncilia.HistoriaUsuario.Api.dll"]
