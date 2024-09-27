using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Spire.Doc; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bigone.Services; 

public class UploadPageModel : PageModel
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private readonly SqlDbService _sqlDbService;
    private readonly PdfWatermarkService _pdfWatermarkService;

    public UploadPageModel(BlobServiceClient blobServiceClient,IConfiguration configuration,SqlDbService sqlDbService,PdfWatermarkService pdfWatermarkService)
    {
        // Initialize the services
        _blobServiceClient = blobServiceClient;
        _configuration = configuration;
        _sqlDbService = sqlDbService;
        _pdfWatermarkService = pdfWatermarkService; 
    }

    [BindProperty]
    public string Subject { get; set; }

    [BindProperty]
    public int Grade { get; set; }

    [BindProperty]
    public string Keywords { get; set; }

    [BindProperty]
    public string FileName { get; set; }

    [BindProperty]
    public IFormFile UploadedFile { get; set; }

    public bool UploadSuccess { get; set; }

    public bool IsConnectedToDatabase { get; private set; }

    public async Task OnGetAsync()
    {
        IsConnectedToDatabase = await _sqlDbService.CheckConnectionAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0 || string.IsNullOrEmpty(FileName))
        {
            ModelState.AddModelError(string.Empty, "Please select a file and provide a file name.");
            return Page();
        }

        var fileExtension = Path.GetExtension(UploadedFile.FileName).ToLower();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            ModelState.AddModelError(string.Empty, "Invalid file type. Only PDF and Word files are allowed.");
            return Page();
        }

        var uploadDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var containerName = _configuration["AzureStorage:containerName"];
        string fullFileName = $"{FileName}.pdf";
        string storageLocation = $"{containerName}/{fullFileName}";

        string tempOriginalFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_original.pdf");
        string tempWatermarkedFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_watermarked.pdf");

        try
        {
            byte[] fileBytes;

            using (var memoryStream = new MemoryStream())
            {
                await UploadedFile.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new InvalidOperationException("No data was read from the uploaded file.");
            }

            MemoryStream outputStream = new MemoryStream();

            if (fileExtension == ".doc" || fileExtension == ".docx")
            {
                using (MemoryStream wordStream = new MemoryStream(fileBytes))
                {
                    Document document = new Document();
                    document.LoadFromStream(wordStream, FileFormat.Docx);
                    document.SaveToStream(outputStream, FileFormat.PDF);
                }
            }
            else if (fileExtension == ".pdf")
            {
                outputStream.Write(fileBytes, 0, fileBytes.Length);
            }

            outputStream.Position = 0;

            // Write to temporary original PDF
            using (var tempOriginalFileStream = new FileStream(tempOriginalFilePath, FileMode.Create, FileAccess.Write))
            {
                await outputStream.CopyToAsync(tempOriginalFileStream);
                tempOriginalFileStream.Flush();
            }

            _pdfWatermarkService.AddPageWatermark(tempOriginalFilePath, tempWatermarkedFilePath);

            // Reset stream position for the watermarked output stream
            using (var watermarkedStream = new FileStream(tempWatermarkedFilePath, FileMode.Open, FileAccess.Read))
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(fullFileName);

                await blobClient.UploadAsync(watermarkedStream, true);

                // Set metadata for the file
                var metadata = new Dictionary<string, string>()
            {
                {"Subject", Subject},
                {"Grade", Grade.ToString()},
                {"Keywords", Keywords},
                {"FileName", fullFileName},
                {"UploadDate", uploadDate},
                {"StorageLocation", storageLocation},
                {"File_ID", (await _sqlDbService.GetNextFileIdAsync()).ToString()}
            };

                await blobClient.SetMetadataAsync(metadata);

                // Insert metadata into SQL Server
                int userId = 123; 
                bool insertSuccess = await _sqlDbService.InsertFileMetadataAsync(userId, uploadDate, Subject, Grade, Keywords, storageLocation);

                if (!insertSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Failed to insert file metadata into the database.");
                    return Page();
                }

                IsConnectedToDatabase = await _sqlDbService.CheckConnectionAsync();
                UploadSuccess = true;

                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");

            if (ex is iText.Kernel.Exceptions.PdfException pdfEx)
            {
                Console.WriteLine($"PDF Exception: {pdfEx.Message}");
                ModelState.AddModelError(string.Empty, $"PDF processing error: {pdfEx.Message}");
            }

            return Page();
        }
        finally
        {
            // Clean up temporary files if necessary
            if (!string.IsNullOrEmpty(tempOriginalFilePath) && System.IO.File.Exists(tempOriginalFilePath))
            {
                try { System.IO.File.Delete(tempOriginalFilePath); } catch { /* Log or handle deletion error */ }
            }

            if (!string.IsNullOrEmpty(tempWatermarkedFilePath) && System.IO.File.Exists(tempWatermarkedFilePath))
            {
                try { System.IO.File.Delete(tempWatermarkedFilePath); } catch { /* Log or handle deletion error */ }
            }
        }
    }
}