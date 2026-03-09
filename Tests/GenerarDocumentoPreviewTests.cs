using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Automatizacion.Agentes.Infrastructure.Diagrams;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace HistoriaUsuario.Tests
{
    /// <summary>
    /// Genera un archivo Word de vista previa con datos de muestra (incluyendo diagrama PlantUML)
    /// y lo abre automáticamente en Word para revisión visual del formato.
    ///
    /// Uso:
    ///   dotnet test --filter "FullyQualifiedName~GenerarDocumentoPreviewTests"
    ///
    /// El archivo se guarda en: HistoriaUsuario.Api/Tests/Output/koncilia_preview.docx
    /// </summary>
    public class GenerarDocumentoPreviewTests
    {
        private readonly ITestOutputHelper _output;

        private static readonly string CarpetaSalida = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "Output");

        private static readonly string RutaArchivo = Path.Combine(CarpetaSalida, "koncilia_preview.docx");

        public GenerarDocumentoPreviewTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(DisplayName = "PREVIEW - Genera el .docx con diagrama PlantUML y lo abre en Word")]
        public async Task GenerarYAbrirDocumentoPreview()
        {
            // ---- Datos de muestra ----
            var requerimientos = new List<Requerimiento>
            {
                new Requerimiento
                {
                    NombreProceso    = "Conciliación Bancaria Automática",
                    Asistentes       = "Juan Pérez (CFO), María García (Líder Técnico), Carlos Ruiz (BA)",
                    Objetivo         = "Permitir al usuario cargar extractos bancarios en formato CSV/XLSX y compararlos automáticamente contra el libro mayor del ERP, identificando diferencias y generando un reporte de conciliación en menos de 30 segundos para archivos de hasta 50.000 filas.",
                    Justificacion    = "Actualmente el proceso de conciliación es 100% manual, requiere 3 días de trabajo mensual y genera errores en aproximadamente el 15% de los registros.",
                    Alcance          = "1. Módulo de carga de archivos (CSV, XLSX, OFX).\n2. Motor de conciliación con reglas configurables por cuenta.\n3. Dashboard de diferencias.\n4. Exportación del reporte en PDF y Excel.",
                    Dependencias     = "- API REST del ERP SAP (módulo FI).\n- Almacenamiento S3.\n- Active Directory.",
                    CriteriosBrutos  = "1. Procesar hasta 50.000 filas en menos de 30 segundos.\n2. Clasificar diferencias automáticamente.\n3. Solo rol 'Contador' puede ejecutar conciliación.",
                    ResumenEjecutivo = "Módulo de conciliación automática integrado con SAP FI que reducirá el cierre mensual de 5 días a 1 día.",
                    FlujoFuncional   = "1. Contador carga archivo.\n2. Sistema valida formato.\n3. Sistema consulta libro mayor SAP.\n4. Motor concilia.\n5. Genera reporte.",
                    PlantUml         = @"@startuml
!pragma layout smetana
skinparam backgroundColor #FAFAFA
skinparam componentStyle rectangle

actor Contador
package ""Módulo Koncilia"" {
    component ""Frontend Web"" as web
    component ""API Backend"" as api
    database ""PostgreSQL"" as db
}
cloud ""SAP FI"" as sap

Contador --> web : Carga extracto
web --> api : REST/JSON
api --> sap : Consulta libro mayor
api --> db : Guarda resultado
@enduml"
                },
                new Requerimiento
                {
                    NombreProceso    = "Gestión de Maestro de Proveedores",
                    Asistentes       = "Ana Martínez (Compras), Luis Fernández (IT), Sandra Torres (Gerente)",
                    Objetivo         = "Centralizar el maestro de proveedores con un flujo de aprobación de doble nivel.",
                    Justificacion    = "Los proveedores están dispersos en 7 hojas de cálculo. Se han detectado 3 incidentes de fraude en 2 años.",
                    Alcance          = "1. CRUD de proveedores.\n2. Flujo aprobación 2 niveles.\n3. Validación RUC vs SUNAT.\n4. Auditoría completa.",
                    Dependencias     = "- API SUNAT.\n- Active Directory (LDAP).\n- SMTP.",
                    CriteriosBrutos  = "1. Solo rol 'Compras' crea proveedores.\n2. No puede emitir OC sin aprobación.\n3. Todos los cambios quedan auditados.",
                    ResumenEjecutivo = "Módulo centralizado de proveedores que reemplaza 7 hojas de cálculo, con validación SUNAT y aprobación de doble nivel.",
                    FlujoFuncional   = "1. Compras crea proveedor.\n2. Jefe aprueba nivel 1.\n3. Gerente aprueba nivel 2.\n4. Proveedor queda activo.",
                    PlantUml         = "" // Sin diagrama en este requerimiento
                }
            };

            // ---- Preparar carpeta de salida ----
            Directory.CreateDirectory(CarpetaSalida);

            // ---- Renderizar diagramas PlantUML (igual que el agente real) ----
            var plantUmlService = new PlantUmlService(NullLogger<PlantUmlService>.Instance);
            var imagenesDiagramas = new Dictionary<int, string>();

            for (int i = 0; i < requerimientos.Count; i++)
            {
                var req = requerimientos[i];
                if (!string.IsNullOrWhiteSpace(req.PlantUml))
                {
                    string outputId = $"diagrama_{i}_preview";
                    string? pathImagen = await plantUmlService.RenderToImageAsync(
                        req.PlantUml, outputId, Path.GetFullPath(CarpetaSalida));

                    if (pathImagen != null)
                    {
                        imagenesDiagramas[i] = pathImagen;
                        _output.WriteLine($"🖼️  Diagrama {i} renderizado en: {pathImagen}");
                    }
                    else
                    {
                        _output.WriteLine($"⚠️  No se pudo renderizar el diagrama {i}.");
                        _output.WriteLine($"    Verificá que Java esté instalado y que plantuml.jar esté en Tools/");
                    }
                }
            }

            // Pequeña espera para asegurar que Java liberó los archivos SVG
            await Task.Delay(500);

            // ---- Generar el documento Word ----
            var wordService = new HistoriaUsuarioWordService(NullLogger<HistoriaUsuarioWordService>.Instance);
            wordService.GenerateDocument(requerimientos, RutaArchivo,
                imagenesDiagramas.Count > 0 ? imagenesDiagramas : null);

            string rutaAbsoluta = Path.GetFullPath(RutaArchivo);
            _output.WriteLine($"✅ Documento generado en:");
            _output.WriteLine($"   {rutaAbsoluta}");

            Assert.True(File.Exists(rutaAbsoluta), $"No se encontró el archivo generado en: {rutaAbsoluta}");

            // ---- Abrir en Word ----
            try
            {
                Process.Start(new ProcessStartInfo(rutaAbsoluta) { UseShellExecute = true });
                _output.WriteLine("📄 Archivo abierto en Word.");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️  No se pudo abrir automáticamente: {ex.Message}");
            }
        }
    }
}
