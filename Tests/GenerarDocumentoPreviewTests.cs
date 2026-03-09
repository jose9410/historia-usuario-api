using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace HistoriaUsuario.Tests
{
    /// <summary>
    /// Genera un archivo Word de vista previa con datos de muestra y lo abre automáticamente
    /// en el programa predeterminado (Word / LibreOffice) para revisión visual del formato.
    ///
    /// Uso: ejecutar este test individualmente desde el explorador de tests del IDE
    ///       o con: dotnet test --filter "FullyQualifiedName~GenerarDocumentoPreviewTests"
    ///
    /// El archivo se guarda en: backend/HistoriaUsuario.Tests/Output/koncilia_preview.docx
    /// </summary>
    public class GenerarDocumentoPreviewTests
    {
        private readonly ITestOutputHelper _output;

        // Carpeta de salida fija junto al proyecto de tests
        private static readonly string CarpetaSalida = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "Output");

        private static readonly string RutaArchivo = Path.Combine(CarpetaSalida, "koncilia_preview.docx");

        public GenerarDocumentoPreviewTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(DisplayName = "PREVIEW - Genera el .docx de muestra y lo abre en Word")]
        public void GenerarYAbrirDocumentoPreview()
        {
            // ---- Datos de muestra realistas ----
            var requerimientos = new List<Requerimiento>
            {
                new Requerimiento
                {
                    NombreProceso    = "Conciliación Bancaria Automática",
                    Asistentes       = "Juan Pérez (CFO), María García (Líder Técnico), Carlos Ruiz (BA)",
                    Objetivo         = "Permitir al usuario cargar extractos bancarios en formato CSV/XLSX y compararlos automáticamente contra el libro mayor del ERP, identificando diferencias y generando un reporte de conciliación en menos de 30 segundos para archivos de hasta 50.000 filas.",
                    Justificacion    = "Actualmente el proceso de conciliación es 100% manual, requiere 3 días de trabajo mensual y genera errores en aproximadamente el 15% de los registros. La automatización eliminará esos errores y reducirá el tiempo de cierre contable de 5 días a 1 día.",
                    Alcance          = "1. Módulo de carga de archivos (CSV, XLSX, OFX).\n2. Motor de conciliación con reglas configurables por cuenta.\n3. Dashboard de diferencias con clasificación automática por tipo (timing, error, duplicado).\n4. Exportación del reporte en PDF y Excel.\n5. Historial de conciliaciones por período.",
                    Dependencias     = "- API REST del ERP SAP (módulo FI) para obtener el libro mayor.\n- Servicio de almacenamiento S3 para persistir los archivos cargados.\n- Active Directory para autenticación de usuarios.",
                    CriteriosBrutos  = "1. El sistema debe procesar archivos de hasta 50.000 filas en menos de 30 segundos.\n2. Las diferencias deben clasificarse automáticamente (timing, error de captura, duplicado).\n3. Solo usuarios con rol 'Contador' o superior pueden ejecutar la conciliación.\n4. Cada ejecución debe quedar auditada con usuario, fecha y resultado.\n5. El sistema debe manejar diferencias de centavos con tolerancia configurable.",
                    ResumenEjecutivo = "Se construirá un módulo de conciliación automática integrado con SAP FI que reducirá el tiempo de cierre mensual de 5 días a 1 día y eliminará los errores manuales actuales. El módulo permitirá a los contadores cargar extractos bancarios y obtener en tiempo real un reporte detallado de diferencias clasificadas por tipo, con capacidad de drill-down hasta el registro individual.",
                    FlujoFuncional   = "1. Contador carga archivo bancario (CSV/XLSX/OFX).\n2. Sistema valida formato y estructura del archivo.\n3. Sistema consulta libro mayor SAP vía API para el período correspondiente.\n4. Motor de conciliación compara movimientos por monto, fecha y referencia.\n5. Sistema clasifica diferencias automáticamente.\n6. Sistema genera reporte de conciliación.\n7. Contador revisa y aprueba/rechaza diferencias.\n8. Sistema registra resultado en historial.",
                    PlantUml         = ""
                },
                new Requerimiento
                {
                    NombreProceso    = "Gestión de Maestro de Proveedores",
                    Asistentes       = "Ana Martínez (Compras), Luis Fernández (IT), Sandra Torres (Gerente)",
                    Objetivo         = "Centralizar el maestro de proveedores con un flujo de aprobación de doble nivel, garantizando que todos los proveedores activos cumplan con los requisitos legales y comerciales de la empresa antes de poder emitirles órdenes de compra.",
                    Justificacion    = "Los proveedores están dispersos en 7 hojas de cálculo distintas mantenidas por diferentes departamentos, lo que genera inconsistencias, proveedores duplicados y pagos a proveedores no validados. Se han detectado 3 incidentes de fraude en los últimos 2 años relacionados con proveedores fantasma.",
                    Alcance          = "1. CRUD completo de proveedores con campos obligatorios validados (RUC, razón social, IBAN, contacto).\n2. Flujo de aprobación: Compras crea → Jefe Compras aprueba nivel 1 → Gerente Finanzas aprueba nivel 2.\n3. Validación automática de RUC contra API de SUNAT.\n4. Módulo de documentos adjuntos (RUC, contrato, certificados).\n5. Auditoría completa de cambios.",
                    Dependencias     = "- API de SUNAT para validación de RUC.\n- Active Directory (LDAP) para roles y aprobadores.\n- Sistema de correo SMTP para notificaciones de workflow.",
                    CriteriosBrutos  = "1. Solo usuarios con rol 'Compras' pueden crear/editar proveedores.\n2. Un proveedor no puede recibir OC hasta ser aprobado en ambos niveles.\n3. Cada cambio debe registrar usuario, fecha y campo modificado.\n4. El RUC debe validarse contra SUNAT en tiempo real al ingresarlo.\n5. Los aprobadores deben recibir notificación por email al llegar solicitudes pendientes.\n6. El sistema debe bloquear automáticamente proveedores con más de 12 meses sin transacciones.",
                    ResumenEjecutivo = "Se implementará un módulo centralizado de gestión de proveedores que reemplazará las 7 hojas de cálculo actuales, incorporando validación automática de identidad fiscal vía SUNAT y un flujo de aprobación de doble nivel para garantizar que solo proveedores verificados puedan recibir órdenes de compra.",
                    FlujoFuncional   = "1. Compras ingresa datos del proveedor (RUC se valida en tiempo real contra SUNAT).\n2. Sistema notifica a Jefe de Compras para aprobación nivel 1.\n3. Jefe de Compras revisa y aprueba o rechaza con comentarios.\n4. Si aprobado: Sistema notifica a Gerente de Finanzas para aprobación nivel 2.\n5. Gerente aprueba o rechaza con comentarios.\n6. Si aprobado en ambos niveles: Proveedor queda activo en el sistema.\n7. Sistema notifica al creador el resultado del proceso.",
                    PlantUml         = ""
                }
            };

            // ---- Generar el documento ----
            Directory.CreateDirectory(CarpetaSalida);

            var service = new HistoriaUsuarioWordService(NullLogger<HistoriaUsuarioWordService>.Instance);
            service.GenerateDocument(requerimientos, RutaArchivo);

            _output.WriteLine($"✅ Documento generado en:");
            _output.WriteLine($"   {RutaArchivo}");

            Assert.True(File.Exists(RutaArchivo),
                $"No se encontró el archivo generado en: {RutaArchivo}");

            // ---- Abrir en Word / programa predeterminado ----
            try
            {
                Process.Start(new ProcessStartInfo(RutaArchivo) { UseShellExecute = true });
                _output.WriteLine("📄 Archivo abierto en el programa predeterminado.");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️  No se pudo abrir automáticamente: {ex.Message}");
                _output.WriteLine("    Abrilo manualmente en la ruta indicada arriba.");
            }
        }
    }
}
