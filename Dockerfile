# Usa la imagen oficial de SDK de .NET 9 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia los archivos del proyecto y restaura dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia el resto del código y compila
COPY . ./
RUN dotnet publish -c Release -o out

# Usa la imagen de tiempo de ejecución (más pequeña)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Exponer el puerto que usa la API
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Koncilia.HistoriaUsuario.Api.dll"]
