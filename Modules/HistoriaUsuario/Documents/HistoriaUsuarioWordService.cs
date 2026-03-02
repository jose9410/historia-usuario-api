using Automatizacion.Agentes.Modules.HistoriaUsuario.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Automatizacion.Agentes.Modules.HistoriaUsuario.Documents
{
    public class HistoriaUsuarioWordService
    {
        private readonly ILogger<HistoriaUsuarioWordService> _logger;

        public HistoriaUsuarioWordService(ILogger<HistoriaUsuarioWordService> logger)
        {
            _logger = logger;
        }

        public void GenerateDocument(List<Requerimiento> requerimientos, string rutaArchivo, Dictionary<int, string>? imagenesDiagramas = null)
        {
            _logger.LogInformation("Generando documento Word en: {rutaArchivo}", rutaArchivo);

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(rutaArchivo, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    // ===== HEADER CON IMAGEN EN TODAS LAS PÁGINAS =====
                    string headerPartId = AgregarHeaderConImagen(mainPart);

                    // ===== FOOTER CON IMAGEN EN TODAS LAS PÁGINAS =====
                    string footerPartId = AgregarFooterConImagen(mainPart);

                    // ===== PORTADA =====
                    AgregarTitulo(body, "KONCILIA - HISTORIA DE USUARIO", 28, true, true);
                    if (requerimientos.Count > 0)
                    {
                        AgregarTablaInfo(body, requerimientos[0]);
                    }
                    AgregarSaltoPage(body);

                    // ===== CADA REQUERIMIENTO =====
                    for (int i = 0; i < requerimientos.Count; i++)
                    {
                        var req = requerimientos[i];

                        AgregarTitulo(body, $"{i + 1}. {req.NombreProceso}", 18, true, false);

                        AgregarTablaContenido(body, "OBJETIVO ESPECÍFICO", req.Objetivo);
                        AgregarTablaContenido(body, "JUSTIFICACIÓN DE NEGOCIO", req.Justificacion);
                        AgregarTablaContenido(body, "ALCANCE FUNCIONAL CLAVE", req.Alcance);
                        AgregarTablaContenido(body, "DEPENDENCIAS Y FUENTES", req.Dependencias);
                        AgregarTablaContenido(body, "CRITERIOS DE ACEPTACIÓN", req.CriteriosBrutos);
                        AgregarTablaContenido(body, "RESUMEN EJECUTIVO", req.ResumenEjecutivo);
                        AgregarTablaContenido(body, "FLUJO FUNCIONAL ALTO NIVEL", req.FlujoFuncional);

                        if (imagenesDiagramas != null && imagenesDiagramas.TryGetValue(i, out string? pathImagen) && File.Exists(pathImagen))
                        {
                            AgregarTitulo(body, "DIAGRAMA C4 DE CONTENEDORES", 14, true, false);
                            InsertarImagen(mainPart, body, pathImagen);
                        }

                        if (i < requerimientos.Count - 1)
                        {
                            AgregarSaltoPage(body);
                        }
                    }

                    // ===== SECCIÓN CON REFERENCIA AL HEADER =====
                    var sectionProps = body.Elements<SectionProperties>().FirstOrDefault();
                    if (sectionProps == null)
                    {
                        sectionProps = new SectionProperties();
                        body.AppendChild(sectionProps);
                    }

                    // Forzar tamaño Carta (21.59cm x 27.94cm)
                    sectionProps.RemoveAllChildren<PageSize>();
                    sectionProps.AppendChild(new PageSize() { Width = (UInt32Value)12240U, Height = (UInt32Value)15840U });
                    
                    // Márgenes estándar (2.54cm = 1440 Twips)
                    sectionProps.RemoveAllChildren<PageMargin>();
                    sectionProps.AppendChild(new PageMargin() 
                    { 
                        Top = 1440, Right = (UInt32Value)1440U, Bottom = 1440, Left = (UInt32Value)1440U, 
                        Header = (UInt32Value)720U, Footer = (UInt32Value)720U, 
                        Gutter = (UInt32Value)0U 
                    });

                    sectionProps.PrependChild(new HeaderReference()
                    {
                        Type = HeaderFooterValues.Default,
                        Id = headerPartId
                    });

                    sectionProps.PrependChild(new FooterReference()
                    {
                        Type = HeaderFooterValues.Default,
                        Id = footerPartId
                    });
                }
                _logger.LogInformation("Documento generado exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el documento Word.");
                throw;
            }
        }

        private void AgregarTablaContenido(Body body, string titulo, string contenido)
        {
            var table = body.AppendChild(new Table());

            var tblPr = table.AppendChild(new TableProperties());
            tblPr.AppendChild(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

            tblPr.AppendChild(new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.Single, Size = 12, Color = "E2E2E2" }
            });

            var rowTitulo = table.AppendChild(new TableRow());
            var cellTitulo = rowTitulo.AppendChild(new TableCell());
            cellTitulo.TableCellProperties = new TableCellProperties
            {
                Shading = new Shading { Fill = "D3D3D3" },
                GridSpan = new GridSpan { Val = 2 },
                TableCellWidth = new TableCellWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
            };

            var paraTitulo = cellTitulo.AppendChild(new Paragraph());
            paraTitulo.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Left }
            };
            var runTitulo = paraTitulo.AppendChild(new Run());
            runTitulo.AppendChild(new Text(titulo));
            runTitulo.RunProperties = new RunProperties
            {
                Bold = new Bold(),
                FontSize = new FontSize { Val = "20" },
                Color = new Color { Val = "000000" }
            };

            var rowContenido = table.AppendChild(new TableRow());
            var cellContenido = rowContenido.AppendChild(new TableCell());
            cellContenido.TableCellProperties = new TableCellProperties
            {
                Shading = new Shading { Fill = "FFFFFF" },
                TableCellWidth = new TableCellWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
            };

            var paraContenido = cellContenido.AppendChild(new Paragraph());
            var paraPr = new ParagraphProperties();
            paraPr.AppendChild(new SpacingBetweenLines { Line = "240", LineRule = LineSpacingRuleValues.AtLeast });
            paraContenido.ParagraphProperties = paraPr;

            var runContenido = paraContenido.AppendChild(new Run());
            runContenido.AppendChild(new Text(contenido ?? ""));
            runContenido.RunProperties = new RunProperties
            {
                FontSize = new FontSize { Val = "18" }
            };

            AgregarSeparador(body, 1);
        }

        private void AgregarTitulo(Body body, string texto, int fontSize, bool negrita, bool centrado)
        {
            var para = body.AppendChild(new Paragraph());
            if (centrado)
            {
                para.ParagraphProperties = new ParagraphProperties
                {
                    Justification = new Justification { Val = JustificationValues.Center }
                };
            }

            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(texto));
            run.RunProperties = new RunProperties
            {
                Bold = negrita ? new Bold() : null,
                FontSize = new FontSize { Val = fontSize.ToString() }
            };
        }

        private void AgregarSeparador(Body body, int lineas = 1)
        {
            for (int i = 0; i < lineas; i++)
            {
                body.AppendChild(new Paragraph());
            }
        }

        private void AgregarTablaInfo(Body body, Requerimiento req)
        {
            var table = body.AppendChild(new Table());

            var tblPr = table.AppendChild(new TableProperties());
            tblPr.AppendChild(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

            tblPr.AppendChild(new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" },
                BottomBorder = new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" },
                LeftBorder = new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" },
                RightBorder = new RightBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.Single, Size = 12, Color = "4472C4" }
            });

            void AddRow(string label, string value)
            {
                var row = table.AppendChild(new TableRow());

                var cellLabel = row.AppendChild(new TableCell());
                cellLabel.TableCellProperties = new TableCellProperties
                {
                    Shading = new Shading { Fill = "D3D3D3" },
                    TableCellWidth = new TableCellWidth { Width = "1000", Type = TableWidthUnitValues.Pct }
                };
                cellLabel.Append(new Paragraph(new Run(new Text(label)))
                {
                    ParagraphProperties = new ParagraphProperties
                    {
                        Justification = new Justification { Val = JustificationValues.Left }
                    }
                });

                var cellValue = row.AppendChild(new TableCell());
                cellValue.TableCellProperties = new TableCellProperties
                {
                    Shading = new Shading { Fill = "FFFFFF" },
                    TableCellWidth = new TableCellWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
                };
                cellValue.Append(new Paragraph(new Run(new Text(value ?? "")))
                {
                    ParagraphProperties = new ParagraphProperties
                    {
                        Justification = new Justification { Val = JustificationValues.Left }
                    }
                });
            }

            AddRow("Nombre del Cliente", "{INSERTAR NOMBRE DEL CLIENTE}");
            AddRow("Personas Asistentes", req.Asistentes);
            AddRow("Objetivo Específico", "Describir de forma simple y centrada en el usuario qué funcionalidad necesita, por qué la necesita y qué valor le aporta, para guiar al equipo de desarrollo en la creación de soluciones que resuelvan necesidades reales del negocio");

            AgregarSeparador(body, 2);
        }

        private string AgregarHeaderConImagen(MainDocumentPart mainPart)
        {
            // Crear el HeaderPart
            HeaderPart headerPart = mainPart.AddNewPart<HeaderPart>();
            string headerPartId = mainPart.GetIdOfPart(headerPart);

            // Cargar la imagen desde el recurso embebido
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "HistoriaUsuario.Api.Assets.koncilia_header.png";
            using (Stream? resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    _logger.LogWarning("No se encontró el recurso embebido del header: {resourceName}", resourceName);
                    // Crear header vacío si no se encuentra la imagen
                    headerPart.Header = new Header(new Paragraph(new Run(new Text("KONCILIA"))));
                    headerPart.Header.Save();
                    return headerPartId;
                }

                ImagePart imagePart = headerPart.AddImagePart(ImagePartType.Png);
                imagePart.FeedData(resourceStream);
                string imageRelId = headerPart.GetIdOfPart(imagePart);

                // Ancho completo de página Letter: 21.59cm = 215900 EMUs * 100 = 7772400 EMUs
                long widthEmus = 7772400L;
                // Proporción de la imagen del header (aprox 1024x200 → ratio ~5.12:1)
                long heightEmus = (long)(widthEmus / 5.12);

                var drawing = new Drawing(
                    new DW.Anchor(
                        new DW.SimplePosition() { X = 0L, Y = 0L },
                        new DW.HorizontalPosition(
                            new DW.PositionOffset("0")
                        )
                        { RelativeFrom = DW.HorizontalRelativePositionValues.Page },
                        new DW.VerticalPosition(
                            new DW.PositionOffset("0")
                        )
                        { RelativeFrom = DW.VerticalRelativePositionValues.Page },
                        new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                        new DW.EffectExtent()
                        {
                            LeftEdge = 0L,
                            TopEdge = 0L,
                            RightEdge = 0L,
                            BottomEdge = 0L
                        },
                        new DW.WrapNone(),
                        new DW.DocProperties()
                        {
                            Id = (UInt32Value)100U,
                            Name = "Header HistoriaUsuario"
                        },
                        new DW.NonVisualGraphicFrameDrawingProperties(
                            new A.GraphicFrameLocks() { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties()
                                        {
                                            Id = (UInt32Value)101U,
                                            Name = "koncilia_header.png"
                                        },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new A.Blip()
                                        {
                                            Embed = imageRelId,
                                            CompressionState = A.BlipCompressionValues.Print
                                        },
                                        new A.Stretch(
                                            new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(
                                            new A.Offset() { X = 0L, Y = 0L },
                                            new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                        new A.PresetGeometry(
                                            new A.AdjustValueList()
                                        )
                                        { Preset = A.ShapeTypeValues.Rectangle }))
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                    )
                    {
                        DistanceFromTop = (UInt32Value)0U,
                        DistanceFromBottom = (UInt32Value)0U,
                        DistanceFromLeft = (UInt32Value)0U,
                        DistanceFromRight = (UInt32Value)0U,
                        SimplePos = false,
                        RelativeHeight = (UInt32Value)251658240U,
                        BehindDoc = true,
                        Locked = false,
                        LayoutInCell = true,
                        AllowOverlap = true
                    });

                headerPart.Header = new Header(
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center }
                        ),
                        new Run(drawing)
                    )
                );
                headerPart.Header.Save();
            }

            return headerPartId;
        }

        private string AgregarFooterConImagen(MainDocumentPart mainPart)
        {
            // Crear el FooterPart
            FooterPart footerPart = mainPart.AddNewPart<FooterPart>();
            string footerPartId = mainPart.GetIdOfPart(footerPart);

            // Cargar la imagen desde el recurso embebido
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "HistoriaUsuario.Api.Assets.koncilia_footer.png";
            using (Stream? resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    _logger.LogWarning("No se encontró el recurso embebido del footer: {resourceName}", resourceName);
                    footerPart.Footer = new Footer(new Paragraph(new Run(new Text("KONCILIA"))));
                    footerPart.Footer.Save();
                    return footerPartId;
                }

                ImagePart imagePart = footerPart.AddImagePart(ImagePartType.Png);
                imagePart.FeedData(resourceStream);
                string imageRelId = footerPart.GetIdOfPart(imagePart);

                // Mismo ancho que el header para consistencia (21.59 cm)
                long widthEmus = 7772400L;
                // Proporción del footer (aprox 1024x250 → ratio ~4.1:1)
                long heightEmus = (long)(widthEmus / 4.1);

                var drawing = new Drawing(
                    new DW.Anchor(
                        new DW.SimplePosition() { X = 0L, Y = 0L },
                        new DW.HorizontalPosition(
                            new DW.PositionOffset("0")
                        )
                        { RelativeFrom = DW.HorizontalRelativePositionValues.Page },
                        new DW.VerticalPosition(
                            new DW.PositionOffset((10058400L - heightEmus).ToString())
                        )
                        { RelativeFrom = DW.VerticalRelativePositionValues.Page },
                        new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                        new DW.EffectExtent()
                        {
                            LeftEdge = 0L,
                            TopEdge = 0L,
                            RightEdge = 0L,
                            BottomEdge = 0L
                        },
                        new DW.WrapNone(),
                        new DW.DocProperties()
                        {
                            Id = (UInt32Value)200U,
                            Name = "Footer HistoriaUsuario"
                        },
                        new DW.NonVisualGraphicFrameDrawingProperties(
                            new A.GraphicFrameLocks() { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties()
                                        {
                                            Id = (UInt32Value)201U,
                                            Name = "koncilia_footer.png"
                                        },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new A.Blip()
                                        {
                                            Embed = imageRelId,
                                            CompressionState = A.BlipCompressionValues.Print
                                        },
                                        new A.Stretch(
                                            new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(
                                            new A.Offset() { X = 0L, Y = 0L },
                                            new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                        new A.PresetGeometry(
                                            new A.AdjustValueList()
                                        )
                                        { Preset = A.ShapeTypeValues.Rectangle }))
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                    )
                    {
                        DistanceFromTop = (UInt32Value)0U,
                        DistanceFromBottom = (UInt32Value)0U,
                        DistanceFromLeft = (UInt32Value)0U,
                        DistanceFromRight = (UInt32Value)0U,
                        SimplePos = false,
                        RelativeHeight = (UInt32Value)251658240U,
                        BehindDoc = true,
                        Locked = false,
                        LayoutInCell = true,
                        AllowOverlap = true
                    });

                footerPart.Footer = new Footer(
                    new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center }
                        ),
                        new Run(drawing)
                    )
                );
                footerPart.Footer.Save();
            }

            return footerPartId;
        }

        private void AgregarSaltoPage(Body body)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Break { Type = BreakValues.Page });
        }

        private void InsertarImagen(MainDocumentPart mainPart, Body body, string pathImagen)
        {
            string extension = Path.GetExtension(pathImagen).ToLower();
            var partType = extension == ".svg" ? ImagePartType.Svg : ImagePartType.Png;

            ImagePart imagePart = mainPart.AddImagePart(partType);
            using (FileStream stream = new FileStream(pathImagen, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }

            string relationshipId = mainPart.GetIdOfPart(imagePart);

            long widthEmus = 15 * 360000;
            long heightEmus = 10 * 360000;

            var element =
                 new Drawing(
                     new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                         new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = widthEmus, Cy = heightEmus },
                         new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent()
                         {
                             LeftEdge = 0L,
                             TopEdge = 0L,
                             RightEdge = 0L,
                             BottomEdge = 0L
                         },
                         new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                         {
                             Id = (UInt32Value)1U,
                             Name = "Diagrama C4"
                         },
                         new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                             new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                         new DocumentFormat.OpenXml.Drawing.Graphic(
                             new DocumentFormat.OpenXml.Drawing.GraphicData(
                                 new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                     new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                         new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                         {
                                             Id = (UInt32Value)2U,
                                             Name = "Diagrama.png"
                                         },
                                         new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                     new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                         new DocumentFormat.OpenXml.Drawing.Blip()
                                         {
                                             Embed = relationshipId,
                                             CompressionState =
                                             DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                                         },
                                         new DocumentFormat.OpenXml.Drawing.Stretch(
                                             new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                     new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                         new DocumentFormat.OpenXml.Drawing.Transform2D(
                                             new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                             new DocumentFormat.OpenXml.Drawing.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                         new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                             new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                         )
                                         { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }))
                             )
                             { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                     )
                     {
                         DistanceFromTop = (UInt32Value)0U,
                         DistanceFromBottom = (UInt32Value)0U,
                         DistanceFromLeft = (UInt32Value)0U,
                         DistanceFromRight = (UInt32Value)0U,
                         EditId = "50D07946"
                     });

            body.AppendChild(new Paragraph(new Run(element)));
            AgregarSeparador(body, 2);
        }
    }
}
