using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proj_Frame.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using DocumentFormat.OpenXml.Packaging;

namespace Proj323.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<UploadController> _logger;
        private readonly List<string> allowedKeywords = new List<string>
        {
            "test", "assignment", "quiz", "notes", "summary", "test memo", "assignment memo", "quiz memo"
        };

        public UploadController(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<UploadController> logger)
        {
            _connectionString = configuration.GetSection("ConnectionStrings")["SqlServerConnection"];
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadViewModel model)
        {
            try
            {
                _logger.LogInformation("Starting file upload process...");

                if (!allowedKeywords.Contains(model.Keywords.ToLower()))
                {
                    return BadRequest("Keyword validation failed. Invalid keyword. Use one of: " + string.Join(", ", allowedKeywords));
                }

                if (model.UploadedFile == null || model.UploadedFile.Length == 0)
                {
                    return BadRequest("File validation failed. The file is missing or invalid.");
                }

                string fileName = Path.ChangeExtension(model.FileName, ".pdf");
                MemoryStream pdfStream = new MemoryStream();

                _logger.LogInformation("Processing uploaded file...");
                if (!await ProcessFileAsync(model, pdfStream))
                {
                    return BadRequest("File processing failed. Unable to convert or read file content.");
                }
                _logger.LogInformation("File processed successfully.");

                _logger.LogInformation("Adding front page to the PDF...");
                if (!await AddFrontPageAsync(model, pdfStream, fileName))
                {
                    return BadRequest("Adding front page failed. Could not modify the document as expected.");
                }
                _logger.LogInformation("Front page added successfully.");

                _logger.LogInformation("Uploading file to Azure Blob Storage...");
                if (!await UploadToAzureAsync(model, fileName))
                {
                    return BadRequest("Azure upload failed. Unable to upload to Azure Blob Storage.");
                }
                _logger.LogInformation("File uploaded to Azure Blob Storage successfully.");

                _logger.LogInformation("Saving metadata to SQL Server...");
                if (!await SaveMetadataToSqlServerAsync(model, fileName))
                {
                    return BadRequest("SQL metadata saving failed. Could not save file metadata to the database.");
                }
                _logger.LogInformation("Metadata saved to SQL Server successfully.");

                return Ok("File uploaded successfully, metadata saved to Blob, and metadata stored in the database!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex.Message} | StackTrace: {ex.StackTrace}");
                // Simplified response for easier debugging
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }



        private async Task<bool> ProcessFileAsync(FileUploadViewModel model, MemoryStream pdfStream)
        {
            try
            {
                if (Path.GetExtension(model.UploadedFile.FileName).ToLower() == ".docx")
                {
                    using (var wordStream = new MemoryStream())
                    {
                        await model.UploadedFile.CopyToAsync(wordStream);
                        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordStream, false))
                        {
                            var mainPart = wordDoc.MainDocumentPart;
                            if (mainPart == null) throw new Exception("Invalid Word document.");

                            PdfDocument pdfDoc = new PdfDocument();
                            PdfPage page = pdfDoc.AddPage();
                            using (var xGraphics = PdfSharp.Drawing.XGraphics.FromPdfPage(page))
                            {
                                var paragraphs = mainPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
                                var yPoint = 20;
                                foreach (var para in paragraphs)
                                {
                                    var text = para.InnerText;
                                    xGraphics.DrawString(text, new PdfSharp.Drawing.XFont("Verdana", 12), PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint(20, yPoint));
                                    yPoint += 20;
                                }
                            }
                            pdfDoc.Save(pdfStream);
                        }
                    }
                }
                else if (Path.GetExtension(model.UploadedFile.FileName).ToLower() == ".pdf")
                {
                    await model.UploadedFile.CopyToAsync(pdfStream);
                }
                else
                {
                    return false; // Unsupported file type
                }

                pdfStream.Position = 0;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> AddFrontPageAsync(FileUploadViewModel model, MemoryStream pdfStream, string fileName)
        {
            try
            {
                using (PdfDocument outputDocument = new PdfDocument())
                {
                    string frontPagePdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resources", "frontpage.pdf");

                    if (!System.IO.File.Exists(frontPagePdfPath))
                    {
                        _logger.LogError($"Front page PDF not found at path: {frontPagePdfPath}");
                        return false;
                    }

                    using (PdfDocument frontPageDocument = PdfReader.Open(frontPagePdfPath, PdfDocumentOpenMode.Import))
                    {
                        outputDocument.AddPage(frontPageDocument.Pages[0]);
                    }

                    using (PdfDocument originalDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < originalDocument.PageCount; i++)
                        {
                            outputDocument.AddPage(originalDocument.Pages[i]);
                        }
                    }

                    MemoryStream outputPdfStream = new MemoryStream();
                    outputDocument.Save(outputPdfStream);
                    outputPdfStream.Position = 0;
                    model.UploadedFile = new FormFile(outputPdfStream, 0, outputPdfStream.Length, null, fileName);
                    model.FileName = Path.ChangeExtension(model.FileName, ".pdf");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding front page: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UploadToAzureAsync(FileUploadViewModel model, string fileName)
        {
            try
            {
                var blobContainerName = "uploads";
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(blobContainerName);
                var blobClient = blobContainerClient.GetBlobClient(fileName);

                using (var stream = model.UploadedFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                var metadata = new Dictionary<string, string>
                {
                    { "OriginalFileName", model.FileName },
                    { "UploadedBy", "User123" },
                    { "UploadedOn", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                };
                await blobClient.SetMetadataAsync(metadata);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading to Azure Blob Storage: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SaveMetadataToSqlServerAsync(FileUploadViewModel model, string fileName)
        {
            try
            {
                int nextFileId = await GetNextFileId();
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = "INSERT INTO FILES (File_ID, User_ID, Subject, Grade, Keywords, Storage_Loc, Date_Created) " +
                                "VALUES (@File_ID, @User_ID, @Subject, @Grade, @Keywords, @Storage_Loc, @Date_Created)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@File_ID", nextFileId);
                        cmd.Parameters.AddWithValue("@User_ID", GetSessionInfo());
                        cmd.Parameters.AddWithValue("@Subject", model.Subject);
                        cmd.Parameters.AddWithValue("@Grade", model.Year);
                        cmd.Parameters.AddWithValue("@Keywords", model.Keywords);
                        cmd.Parameters.AddWithValue("@Storage_Loc", $"uploads/{model.FileName}");
                        cmd.Parameters.AddWithValue("@Date_Created", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving metadata to SQL Server: {ex.Message}");
                return false;
            }
        }

        private async Task<int> GetNextFileId()
        {
            int nextFileId = 1;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = "SELECT ISNULL(MAX(File_ID), 0) + 1 FROM FILES";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            nextFileId = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching next File_ID: {ex.Message}");
            }
            return nextFileId;
        }
    }
}
