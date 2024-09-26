
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

    public UploadPageModel(BlobServiceClient blobServiceClient, IConfiguration configuration, SqlDbService sqlDbService)
    {
        _blobServiceClient = blobServiceClient;
        _configuration = configuration;
        _sqlDbService = sqlDbService;
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

    // Property to indicate if connected to the database
    public bool IsConnectedToDatabase { get; private set; }

    public async Task OnGetAsync()
    {
        // Check if connected to the database when loading the page
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
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" };

        if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
        {
            ModelState.AddModelError(string.Empty, "Invalid file type. Only PDF, Word, and PowerPoint files are allowed.");
            return Page();
        }

        var fullFileName = $"{FileName}{fileExtension}";
        var uploadDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var containerName = _configuration["AzureStorage:containerName"];

        // Constructing the storage location path
        string storageLocation = $"{containerName}/{fullFileName}";

        byte[] fileBytes;

        try
        {
            using (var ms = new MemoryStream())
            {
                await UploadedFile.CopyToAsync(ms);
                fileBytes = ms.ToArray(); // Read the uploaded file into bytes
            }

            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new InvalidOperationException("No data was read from the uploaded file.");
            }

            using (var stream = new MemoryStream(fileBytes))
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(fullFileName);

                // Create a consistent metadata dictionary
                var metadata = new Dictionary<string, string>
            {
                { "Subject", Subject },
                { "Year", Year.ToString() },
                { "Description", Description },
                { "FileName", fullFileName },
                { "UploadDate", uploadDate },
                { "StorageLocation", storageLocation } // Save storage location in metadata
            };

                // Upload the file to Blob Storage
                await blobClient.UploadAsync(stream, true);
                await blobClient.SetMetadataAsync(metadata);

                // Insert metadata into SQL Server
                int userId = 123; // Hardcoded User ID
                bool insertSuccess = await _sqlDbService.InsertFileMetadataAsync(userId, uploadDate, Subject, Year, Description, storageLocation);

                if (!insertSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Failed to insert file metadata into the database.");
                    return Page();
                }
            }

            IsConnectedToDatabase = await _sqlDbService.CheckConnectionAsync();
            UploadSuccess = true;
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            return Page();
        }
    }
}
