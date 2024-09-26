using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO;

namespace Bigone.Services
{
    public class PdfWatermarkService
    {
        public byte[] AddWatermarkToPdf(Stream inputPdfStream, string watermarkText)
        {
            // Load the PDF from the stream
            PdfDocument document = PdfReader.Open(inputPdfStream, PdfDocumentOpenMode.Modify);

            // Set up font and brush for the watermark
            XFont font = new XFont("Arial", 60, XFontStyleEx.Bold); // Increased font size for visibility
            XBrush brush = new XSolidBrush(XColor.FromArgb(128, 255, 0, 0)); // Semi-transparent red color

            // Iterate through each page and add the watermark
            foreach (PdfPage page in document.Pages)
            {
                // Create a graphics object for the page
                XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                // Get the page size for watermark placement
                double pageWidth = page.Width.Point;
                double pageHeight = page.Height.Point;

                // Calculate the position to center the watermark text
                XSize textSize = gfx.MeasureString(watermarkText, font);
                double x = (pageWidth - textSize.Width) / 2;  // Center horizontally
                double y = (pageHeight - textSize.Height) / 2; // Center vertically

                // Rotate the text to make it diagonal across the page
                gfx.RotateAtTransform(-45, new XPoint(pageWidth / 2, pageHeight / 2));

                // Draw the watermark on the page
                gfx.DrawString(watermarkText, font, brush, new XPoint(x, y));
            }

            // Save the modified PDF to a memory stream
            using (MemoryStream outputStream = new MemoryStream())
            {
                document.Save(outputStream, false);
                return outputStream.ToArray(); // Return the watermarked PDF as a byte array
            }
        }
    }
}