using System;
using System.Diagnostics;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Bigone.Services
{
    public class PdfWatermarkService
    {
        public void AddPageWatermark(string inputFilePath, string outputFilePath)
        {
            // Path to the watermark PDF
            string watermarkFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "watermarks", "watermark.pdf");

            try
            {
                // Create a new PDF document for output
                using (PdfDocument outputDocument = new PdfDocument())
                {
                    // Load the original document
                    PdfDocument inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import);

                    // Load the watermark document
                    PdfDocument watermarkDocument = PdfReader.Open(watermarkFilePath, PdfDocumentOpenMode.Import);
                    var watermarkPage = watermarkDocument.Pages[0];

                    // Add the watermark page as the first page
                    outputDocument.AddPage(watermarkPage);

                    // Copy all pages from the original document to the new document
                    foreach (var page in inputDocument.Pages)
                    {
                        outputDocument.AddPage(page);
                    }

                    // Save the output document
                    outputDocument.Save(outputFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; // Rethrow or handle as needed
            }
        }
    }
}