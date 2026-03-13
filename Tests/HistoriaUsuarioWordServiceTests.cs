using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Documents;
using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HistoriaUsuario.Tests
{
    /// <summary>
    /// Valida la estructura y el formato del documento Word generado por HistoriaUsuarioWordService.
    /// El archivo .docx se genera en una ruta temporal y se elimina al finalizar cada test.
    /// </summary>
    public class HistoriaUsuarioWordServiceTests : IDisposable
    {
        private readonly string _rutaArchivo;
        private readonly List<Requerimiento> _requerimientos;
        private readonly HistoriaUsuarioWordService _service;

        public HistoriaUsuarioWordServiceTests()
        {
            _rutaArchivo = Path.Combine(Path.GetTempPath(), $"test_koncilia_{Guid.NewGuid()}.docx");

            _requerimientos = new List<Requerimiento>
            {
                new Requerimiento
                {
                    NombreProceso          = "Proceso de Conciliación Bancaria",
                    Asistentes             = "Juan Pérez, María García",
                    QueSeQuiereHacer       = "Permitir al usuario cargar extractos bancarios y compararlos automáticamente contra el libro mayor.",
                    ParaQueSirve           = "Actualmente el proceso es manual y genera errores en las conciliaciones mensuales.",
                    ComoDeberiaFuncionar   = "Usuario carga archivo → Sistema valida formato → Motor concilia → Genera reporte de diferencias.",
                    QueSeNecesita          = "Archivos CSV/XLSX, API de ERP SAP, servicio de almacenamiento S3.",
                    CriteriosAceptacion    = "1. El sistema debe procesar archivos de hasta 50.000 filas en menos de 30 segundos.\n2. Las diferencias deben clasificarse automáticamente por tipo.",
                    PlantUml               = ""
                },
                new Requerimiento
                {
                    NombreProceso          = "Gestión de Proveedores",
                    Asistentes             = "Carlos López",
                    QueSeQuiereHacer       = "Centralizar el maestro de proveedores con aprobación por niveles.",
                    ParaQueSirve           = "Los proveedores están dispersos en múltiples hojas de cálculo.",
                    ComoDeberiaFuncionar   = "Compras crea proveedor → Jefe aprueba nivel 1 → Gerente aprueba nivel 2 → Activo en sistema.",
                    QueSeNecesita          = "Directorio Activo (LDAP) para roles.",
                    CriteriosAceptacion    = "1. Solo usuarios con rol 'Compras' pueden crear proveedores.\n2. Los cambios deben quedar auditados.",
                    PlantUml               = ""
                }
            };

            _service = new HistoriaUsuarioWordService(NullLogger<HistoriaUsuarioWordService>.Instance);
            _service.GenerateDocument(_requerimientos, _rutaArchivo);
        }

        public void Dispose()
        {
            if (File.Exists(_rutaArchivo))
                File.Delete(_rutaArchivo);
        }

        // ------------------------------------------------------------------ //
        //  TEST 1: El archivo se genera correctamente                         //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T01 - El archivo .docx es creado en disco")]
        public void T01_ArchivoDocxExiste()
        {
            Assert.True(File.Exists(_rutaArchivo),
                $"No se encontró el archivo generado en: {_rutaArchivo}");
        }

        // ------------------------------------------------------------------ //
        //  TEST 2: Tamaño de página Carta (12240 × 15840 twips)              //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T02 - Página tamaño Carta (12240 × 15840 twips)")]
        public void T02_TamanoPaginaCarta()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;
            var pageSize = body.Elements<SectionProperties>().First().Elements<PageSize>().First();

            Assert.Equal((uint)12240, pageSize.Width!.Value);
            Assert.Equal((uint)15840, pageSize.Height!.Value);
        }

        // ------------------------------------------------------------------ //
        //  TEST 3: Márgenes estándar 1440 twips (≈ 2.54 cm)                  //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T03 - Márgenes de página = 1440 twips (2.54 cm)")]
        public void T03_MargenesEstandar()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;
            var margin = body.Elements<SectionProperties>().First().Elements<PageMargin>().First();

            Assert.Equal(1440, margin.Top!.Value);
            Assert.Equal((uint)1440, margin.Left!.Value);
            Assert.Equal((uint)1440, margin.Right!.Value);
            Assert.Equal(1440, margin.Bottom!.Value);
        }

        // ------------------------------------------------------------------ //
        //  TEST 4: HeaderReference presente en SectionProperties             //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T04 - SectionProperties contiene HeaderReference")]
        public void T04_HeaderReferencePresente()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;
            var section = body.Elements<SectionProperties>().First();
            var headerRef = section.Elements<HeaderReference>().FirstOrDefault();

            Assert.NotNull(headerRef);
            Assert.Equal(HeaderFooterValues.Default, headerRef.Type!.Value);
        }

        // ------------------------------------------------------------------ //
        //  TEST 5: FooterReference presente en SectionProperties             //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T05 - SectionProperties contiene FooterReference")]
        public void T05_FooterReferencePresente()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;
            var section = body.Elements<SectionProperties>().First();
            var footerRef = section.Elements<FooterReference>().FirstOrDefault();

            Assert.NotNull(footerRef);
            Assert.Equal(HeaderFooterValues.Default, footerRef.Type!.Value);
        }

        // ------------------------------------------------------------------ //
        //  TEST 6: Cantidad mínima de tablas en el documento                 //
        //  1 tabla de portada + 7 sections × N requerimientos                //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T06 - Número mínimo de tablas generadas")]
        public void T06_CantidadMinimaTablas()
        {
            // 1 tabla de portada + 5 secciones × N requerimientos
            int expectedMin = 1 + (5 * _requerimientos.Count);

            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;
            int totalTablas = body.Elements<Table>().Count();

            Assert.True(totalTablas >= expectedMin,
                $"Se esperaban al menos {expectedMin} tablas pero se encontraron {totalTablas}.");
        }

        // ------------------------------------------------------------------ //
        //  TEST 7: Título principal del documento                            //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T07 - El primer párrafo contiene el título principal")]
        public void T07_TituloPrincipal()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            // El primer párrafo real del body debe tener el título
            var textoTitulo = body.Elements<Paragraph>()
                .SelectMany(p => p.Elements<Run>())
                .SelectMany(r => r.Elements<Text>())
                .Select(t => t.Text)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

            Assert.Equal("KONCILIA - HISTORIA DE USUARIO", textoTitulo);
        }

        // ------------------------------------------------------------------ //
        //  TEST 8: Formato del título (negrita, centrado)                    //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T08 - Título principal está centrado y en negrita")]
        public void T08_TituloNegritaCentrado()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            var paraTitulo = body.Elements<Paragraph>()
                .First(p => p.InnerText.Contains("KONCILIA - HISTORIA DE USUARIO"));

            // Centrado
            var justif = paraTitulo.ParagraphProperties?.Justification?.Val?.Value;
            Assert.Equal(JustificationValues.Center, justif);

            // Negrita
            var bold = paraTitulo.Elements<Run>().First().RunProperties?.Bold;
            Assert.NotNull(bold);
        }

        // ------------------------------------------------------------------ //
        //  TEST 9: Encabezados de tablas de contenido con fondo D3D3D3       //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T09 - Las filas de título de tabla tienen fondo D3D3D3")]
        public void T09_FondoEncabezadoTablas()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            // Buscamos celdas con Shading Fill = D3D3D3
            var celdasConFondoGris = body
                .Descendants<TableCell>()
                .Where(c => c.TableCellProperties?.Shading?.Fill?.Value == "D3D3D3")
                .ToList();

            // Debe haber al menos las filas de encabezado: 5 secciones × N reqs + 3 filas de portada
            int expectedMin = (5 * _requerimientos.Count) + 3; // +3 filas de portada con D3D3D3
            Assert.True(celdasConFondoGris.Count >= expectedMin,
                $"Se esperaban al menos {expectedMin} celdas con fondo D3D3D3 pero se encontraron {celdasConFondoGris.Count}.");
        }

        // ------------------------------------------------------------------ //
        //  TEST 10: Saltos de página entre requerimientos                    //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T10 - Existen saltos de página entre requerimientos")]
        public void T10_SaltosDePagina()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            // Buscar todos los Break con Type = Page
            var saltosDePagina = body
                .Descendants<Break>()
                .Where(b => b.Type?.Value == BreakValues.Page)
                .ToList();

            // Con 2 reqs: 1 salto tras portada + 1 entre reqs = al menos 2
            // (el último req NO tiene salto → Count reqs - 1 entre sections + 1 portada = reqs)
            int expectedSaltos = _requerimientos.Count; // 2 en este caso
            Assert.True(saltosDePagina.Count >= expectedSaltos,
                $"Se esperaban al menos {expectedSaltos} saltos de página pero se encontraron {saltosDePagina.Count}.");
        }

        // ------------------------------------------------------------------ //
        //  TEST 11: Secciones de contenido llevan el nombre del proceso      //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T11 - Los títulos de requerimientos aparecen en el documento")]
        public void T11_TitulosRequerimientosPresentes()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            var todosLosTextos = body
                .Descendants<Text>()
                .Select(t => t.Text)
                .ToList();

            for (int i = 0; i < _requerimientos.Count; i++)
            {
                string tituloEsperado = $"{i + 1}. {_requerimientos[i].NombreProceso}";
                Assert.True(
                    todosLosTextos.Any(t => t.Contains(_requerimientos[i].NombreProceso)),
                    $"No se encontró el título del requerimiento: '{tituloEsperado}'");
            }
        }

        // ------------------------------------------------------------------ //
        //  TEST 12: Bordes de tablas de contenido en color E2E2E2            //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T12 - Tablas de contenido tienen bordes E2E2E2")]
        public void T12_BordesTablaContenido()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            // Las tablas de contenido (AgregarTablaContenido) usan color E2E2E2
            var tablasConBordeGris = body
                .Elements<Table>()
                .Where(t =>
                {
                    var tblPr = t.Elements<TableProperties>().FirstOrDefault();
                    var borders = tblPr?.Elements<TableBorders>().FirstOrDefault();
                    return borders?.TopBorder?.Color?.Value == "E2E2E2";
                })
                .ToList();

            // 5 secciones × N requerimientos = tablas con borde E2E2E2
            int expectedMin = 5 * _requerimientos.Count;
            Assert.True(tablasConBordeGris.Count >= expectedMin,
                $"Se esperaban al menos {expectedMin} tablas con bordes E2E2E2 pero se encontraron {tablasConBordeGris.Count}.");
        }

        // ------------------------------------------------------------------ //
        //  TEST 13: Tamaño de fuente del contenido = 18 (9pt en half-points) //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T13 - El texto de contenido usa FontSize = 18 (9pt)")]
        public void T13_TamanoFuenteContenido()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            // Buscamos runs con FontSize = "18"
            var runsConFuente18 = body
                .Descendants<Run>()
                .Where(r => r.RunProperties?.FontSize?.Val?.Value == "18")
                .ToList();

            // Al menos 5 secciones × N requerimientos runs de contenido
            int expectedMin = 5 * _requerimientos.Count;
            Assert.True(runsConFuente18.Count >= expectedMin,
                $"Se esperaban al menos {expectedMin} runs con FontSize=18 pero se encontraron {runsConFuente18.Count}.");
        }

        // ------------------------------------------------------------------ //
        //  TEST 14: Tamaño de fuente de encabezados de tabla = 20 (10pt)    //
        // ------------------------------------------------------------------ //
        [Fact(DisplayName = "T14 - Los encabezados de sección usan FontSize = 20 (10pt)")]
        public void T14_TamanoFuenteEncabezados()
        {
            using var doc = WordprocessingDocument.Open(_rutaArchivo, false);
            var body = doc.MainDocumentPart!.Document.Body!;

            var runsConFuente20 = body
                .Descendants<Run>()
                .Where(r => r.RunProperties?.FontSize?.Val?.Value == "20")
                .ToList();

            Assert.True(runsConFuente20.Count > 0,
                "No se encontraron runs con FontSize = 20 para los encabezados de sección.");
        }
    }
}
