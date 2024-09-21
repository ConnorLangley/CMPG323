using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly string _storageConnectionString;
    private readonly string _containerName;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
        _storageConnectionString = _configuration["AzureStorage:ConnectionString"];
        _containerName = _configuration["AzureStorage:ContainerName"];
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

        // Validate file extension
        var fileExtension = Path.GetExtension(UploadedFile.FileName).ToLower();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" };

        if (!allowedExtensions.Contains(fileExtension))
        {
            ModelState.AddModelError(string.Empty, "Invalid file type. Only PDF, Word, and PowerPoint files are allowed.");
            return Page();
        }

        // Create the full file name
        var fullFileName = $"{FileName}{fileExtension}";

        // Add metadata including subject, year, description, and upload date
        var metadata = new Dictionary<string, string>
        {
            { "Subject", Subject },
            { "Year", Year.ToString() },
            { "Description", Description },
            { "UploadDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
        };

        // Upload file to Azure Blob Storage
        var blobServiceClient = new BlobServiceClient(_storageConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(fullFileName);

        using (var stream = UploadedFile.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, true);
        }

        // Set metadata on the uploaded file
        await blobClient.SetMetadataAsync(metadata);

        UploadSuccess = true;
        return Page();
    }
}
