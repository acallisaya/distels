using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Image;
using iText.Layout.Borders;
using System.Globalization;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using distels.Models;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using Npgsql.Internal;

namespace distels.Services
{
    public interface IPdfService
    {
        byte[] GenerarPdfTarjetas(List<Tarjeta> tarjetas);
        byte[] GenerarPdfTarjetasConImagenEnTodas(List<Tarjeta> tarjetas, string imagenPath);
    }

    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        private readonly IWebHostEnvironment _env;

        public PdfService(ILogger<PdfService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public byte[] GenerarPdfTarjetas(List<Tarjeta> tarjetas)
        {
            return GenerarPdfTarjetasConImagenEnTodas(tarjetas, null);
        }

        public byte[] GenerarPdfTarjetasConImagenEnTodas(List<Tarjeta> tarjetas, string imagenPath)
        {
            using var memoryStream = new MemoryStream();

            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);

            // Página CARTA (215.9mm x 279.4mm)
            var document = new Document(pdf, PageSize.LETTER);

            // **MÁRGENES MÍNIMOS**
            document.SetMargins(3, 3, 3, 3); // Solo 3 puntos de margen

            // Encabezado MÍNIMO
            var primeraTarjeta = tarjetas.First();
            var servicio = primeraTarjeta.Plan?.Servicio;

            var header = new Paragraph($"LOTE: {primeraTarjeta.Lote} - {tarjetas.Count} TARJETAS")
                .SetFont(PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD))
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(1);

            document.Add(header);

            if (!string.IsNullOrEmpty(servicio?.Nombre))
            {
                document.Add(new Paragraph(servicio.Nombre.ToUpper())
                    .SetFontSize(6)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(2));
            }

            document.Add(new LineSeparator(new SolidLine(0.3f)).SetMarginBottom(3));

            // Cargar imagen
            byte[] imagenBytes = null;
            if (!string.IsNullOrEmpty(imagenPath) && File.Exists(imagenPath))
            {
                try
                {
                    imagenBytes = File.ReadAllBytes(imagenPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo cargar la imagen");
                }
            }

            // **CALCULAR DIMENSIONES EXACTAS para que quepan 4x6 en una página**
            float anchoCelda = 151.75165f;   // 5.1cm
            float altoCelda = 226.8f;    // 8.0cm

            var table = new Table(new float[] { anchoCelda, anchoCelda, anchoCelda, anchoCelda })
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                .SetMarginTop(0)
                .SetMarginBottom(0);

            // Agregar numeración a cada tarjeta
            for (int i = 0; i < tarjetas.Count; i++)
            {
                var tarjeta = tarjetas[i];
                var numeroTarjeta = i + 1; // Numeración empezando desde 1

                var cell = CrearCeldaTarjetaConImagenYNumeracion(tarjeta, numeroTarjeta, tarjetas.Count, imagenBytes, anchoCelda, altoCelda);
                table.AddCell(cell);
            }

            // Completar filas si no hay múltiplo de 4
            int resto = tarjetas.Count % 4;
            if (resto > 0)
            {
                for (int i = resto; i < 4; i++)
                {
                    table.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .SetMinHeight(altoCelda));
                }
            }

            document.Add(table);

            // Pie de página mínimo
            var footer = new Paragraph($"Pág. {pdf.GetNumberOfPages()}")
                .SetFontSize(5)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(2);
            document.Add(footer);

            document.Close();
            return memoryStream.ToArray();
        }

        private Cell CrearCeldaTarjetaConImagenYNumeracion(Tarjeta tarjeta, int numeroTarjeta, int totalTarjetas, byte[] imagenBytes, float anchoPuntos, float altoPuntos)
        {
            var cell = new Cell()
                .SetWidth(anchoPuntos)
                .SetHeight(altoPuntos)
                .SetBorder(new SolidBorder(ColorConstants.BLACK, 0.5f))
                .SetPadding(0);

            try
            {
                if (imagenBytes != null && imagenBytes.Length > 0)
                {
                    float factor = 3f; // 3x resolución para alta nitidez
                    int anchoBitmap = (int)(anchoPuntos * factor);
                    int altoBitmap = (int)(altoPuntos * factor);

                    using (var ms = new MemoryStream(imagenBytes))
                    using (var originalImage = System.Drawing.Image.FromStream(ms))
                    using (var bitmap = new System.Drawing.Bitmap(anchoBitmap, altoBitmap))
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        // 1. CONFIGURACIÓN ALTA CALIDAD
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                        // Escalar todo al tamaño normal
                        graphics.ScaleTransform(factor, factor);

                        // 2. DIBUJAR IMAGEN DE FONDO
                        graphics.DrawImage(originalImage, 0, 0, (int)anchoPuntos, (int)altoPuntos);

                        // 3. FUENTES
                        using (var fontCodigo = new System.Drawing.Font("Arial", 6, System.Drawing.FontStyle.Bold))
                        using (var fontNumeracion = new System.Drawing.Font("Arial", 6, System.Drawing.FontStyle.Bold))
                        using (var fontSerieLote = new System.Drawing.Font("Arial", 4, System.Drawing.FontStyle.Bold))
                        {
                            // Numeración y código
                            string numeracionTexto = $"  {numeroTarjeta}  ";
                            string codigoTexto = "CODE: " + (tarjeta.Codigo ?? "000000000000000");

                            float posY = altoPuntos - 23; // desde abajo
                            float numeracionX = 0.5f;
                            float paddingVertical = 1f; // antes tenías ~2
                            // Numeración fondo blanco
                            var numeracionSize = graphics.MeasureString(numeracionTexto, fontNumeracion);
                            graphics.FillRectangle(System.Drawing.Brushes.White,
                                numeracionX - 2, posY ,
                                numeracionSize.Width + 4, numeracionSize.Height + paddingVertical);

                            graphics.DrawString(numeracionTexto, fontNumeracion, Brushes.Black, numeracionX, posY);

                            // Código a la derecha de numeración
                            float codigoX = numeracionX + numeracionSize.Width + 5;
                            float margenDerecho = 3f;
                            float fondoCodigoAncho = anchoPuntos - codigoX - margenDerecho;
                            var codigoSize = graphics.MeasureString(codigoTexto, fontCodigo);
                            graphics.FillRectangle(System.Drawing.Brushes.White,  codigoX - 2, posY,   fondoCodigoAncho + 2, codigoSize.Height + paddingVertical);

                            graphics.DrawString(codigoTexto, fontCodigo, Brushes.Black, codigoX, posY);

                            // Serie y Lote izquierda, texto blanco, ancho mitad de celda
                            string serieLoteTexto = $"LOTE: {tarjeta.Lote} | SERIE: {tarjeta.Serie}";
                            float serieLoteY = posY + Math.Max(numeracionSize.Height, graphics.MeasureString(codigoTexto, fontCodigo).Height) + 1;
                            float serieLoteX = 3f;
                            float anchoMaximo = anchoPuntos / 2f;

                            string textoAUsar = serieLoteTexto;
                            while (graphics.MeasureString(textoAUsar, fontSerieLote).Width > anchoMaximo)
                                textoAUsar = textoAUsar.Substring(0, textoAUsar.Length - 1);

                            graphics.DrawString(textoAUsar, fontSerieLote, Brushes.White, serieLoteX, serieLoteY);

                            // "Uso un dispositivo" centrado debajo del bloque Serie/Lote
                            string textoUso = "Uso en 1 dispositivo";
                            var usoSize = graphics.MeasureString(textoUso, fontSerieLote);
                            float usoX = serieLoteX + (Math.Min(graphics.MeasureString(textoAUsar, fontSerieLote).Width, anchoMaximo) - usoSize.Width) / 2f;
                            float usoY = serieLoteY + graphics.MeasureString(textoAUsar, fontSerieLote).Height - 2;

                            graphics.DrawString(textoUso, fontSerieLote, Brushes.White, usoX, usoY);
                        }

                        // 4. GUARDAR bitmap a iText
                        using (var outputMs = new MemoryStream())
                        {
                            bitmap.Save(outputMs, System.Drawing.Imaging.ImageFormat.Png);
                            var imageData = ImageDataFactory.Create(outputMs.ToArray());
                            var imagenFinal = new iText.Layout.Element.Image(imageData)
                                .SetWidth(anchoPuntos)
                                .SetHeight(altoPuntos);

                            cell.Add(imagenFinal);
                        }
                    }
                }
                else
                {
                    cell = CrearCeldaTarjetaNormalConNumeracion(tarjeta, numeroTarjeta, totalTarjetas, anchoPuntos, altoPuntos);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando tarjeta con numeración");
                cell = CrearCeldaTarjetaNormalConNumeracion(tarjeta, numeroTarjeta, totalTarjetas, anchoPuntos, altoPuntos);
            }

            return cell;
        }

        private Cell CrearCeldaTarjetaNormalConNumeracion(Tarjeta tarjeta, int numeroTarjeta, int totalTarjetas, float anchoPuntos, float altoPuntos)
        {
            var cell = new Cell()
                .SetWidth(anchoPuntos)
                .SetHeight(altoPuntos)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(1);

            var contentDiv = new Div()
                .SetWidth(anchoPuntos - 2)
                .SetTextAlignment(TextAlignment.RIGHT);

            // Numeración y código juntos
            string numeracion = $"   {numeroTarjeta.ToString()}  ";
            string codigo = "CODE: " + (tarjeta.Codigo ?? "000000000000000");

            // Contenedor para numeración y código
            var numeracionCodigoDiv = new Div()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(2)
                .SetMarginTop(altoPuntos - 35) // Posición cerca del fondo
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);

            // Agregar numeración (más grande y en negrita)
            numeracionCodigoDiv.Add(new Paragraph(numeracion)
                .SetFont(PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD))
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginBottom(2));

            // Agregar código (más pequeño)
            numeracionCodigoDiv.Add(new Paragraph(codigo)
                .SetFont(PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD))
                .SetFontSize(7)
                .SetTextAlignment(TextAlignment.RIGHT));

            contentDiv.Add(numeracionCodigoDiv);

            // SERIE Y LOTE debajo
            contentDiv.Add(new Paragraph($"S: {tarjeta.Serie} | L: {tarjeta.Lote}")
                .SetFontSize(5)
                .SetMarginTop(2)
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetPadding(1)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(Border.NO_BORDER));

            cell.Add(contentDiv);
            return cell;
        }


    }
}