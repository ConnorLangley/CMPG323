using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class IndexModel : PageModel
{
    private readonly SqlDbService _dbService;
    private readonly BlobServiceClient _blobServiceClient;

    public IndexModel(SqlDbService dbService, BlobServiceClient blobServiceClient)
    {
        _dbService = dbService;
        _blobServiceClient = blobServiceClient;
    }

    [BindProperty]
    public string Subject { get; set; }

    [BindProperty]
    public int Year { get; set; }

    [BindProperty]
    public string Description { get; set; }

    [BindProperty]
    public string FileName { get; set; }

    [BindProperty]
    public IFormFile UploadedFile { get; set; }

    public bool UploadSuccess { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0 || string.IsNullOrEmpty(FileName))
        {
            ModelState.AddModelError(string.Empty, "Please select a file and provide a file name.");
            return Page();
        }

        // Extract file extension and validate
        var fileExtension = Path.GetExtension(UploadedFile.FileName).ToLower();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" };

        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            ModelState.AddModelError(string.Empty, "Invalid file type. Only PDF, Word, and PowerPoint files are allowed.");
            return Page();
        }

        var fullFileName = $"{FileName}{fileExtension}";
        var uploadDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Step 1: Upload the file to Azure Blob Storage
        var containerClient = _blobServiceClient.GetBlobContainerClient("your-container-name");
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(fullFileName);

        using (var stream = UploadedFile.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, true);
        }

        // Step 2: Retrieve the Blob URI (Storage Location)
        var blobUri = blobClient.Uri.ToString();

        // Step 3: Store file metadata in SQL Server Database
        var metadata = new Dictionary<string, string>
        {
            { "Subject", Subject },
            { "Year", Year.ToString() },
            { "Description", Description },
            { "FileName", fullFileName },
            { "UploadDate", uploadDate },
            { "StorageLoc", blobUri } // Store the blob's URI
        };

        int result = await _dbService.InsertFileMetadataAsync(
            metadata["Subject"],
            int.Parse(metadata["Year"]),
            metadata["Description"],
            metadata["FileName"],
            metadata["UploadDate"],
            metadata["StorageLoc"]
        );

        if (result > 0)
        {
            UploadSuccess = true;
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Error saving metadata to database.");
        }

        return Page();
    }
}
