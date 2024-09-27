using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class FileInfo
{
    public BlobItem BlobItem { get; set; }
    public int FileId { get; set; }
}

public class HomeModel : PageModel
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly SqlDbService _sqlDbService;

    public HomeModel(BlobServiceClient blobServiceClient, SqlDbService sqlDbService)
    {
        _blobServiceClient = blobServiceClient;
        _sqlDbService = sqlDbService;
    }

    public List<FileInfo> FileList { get; set; } = new List<FileInfo>();
    public bool NoResultsFound { get; set; } = false;

    public async Task OnGetAsync(string searchTerm)
    {
        string containerName = "uploads";
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // Clear previous file list
        FileList.Clear();

        // Get a list of blobs in the container
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            BlobProperties properties = await blobClient.GetPropertiesAsync();

            // Check if metadata matches the search term
            if (properties.Metadata.TryGetValue("subject", out string subject) &&
                (string.IsNullOrEmpty(searchTerm) || subject.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            {
                if (properties.Metadata.TryGetValue("File_ID", out string fileIdStr) &&
                    int.TryParse(fileIdStr, out int fileId))
                {
                    FileList.Add(new FileInfo { BlobItem = blobItem, FileId = fileId });
                }
            }
        }

        // Set NoResultsFound to true if FileList is empty
        NoResultsFound = !FileList.Any();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            ModelState.AddModelError(string.Empty, "File name cannot be null or empty.");
            return Page();
        }

        string containerName = "uploads";
        var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(fileName);

        try
        {
            // Retrieve properties and metadata before deletion
            BlobProperties properties = await blobClient.GetPropertiesAsync();

            // Get File_ID from metadata
            if (properties.Metadata.TryGetValue("File_ID", out string fileIdStr) &&
                int.TryParse(fileIdStr, out int fileId))
            {
                // Delete from Azure Blob Storage
                await blobClient.DeleteIfExistsAsync();

                // Delete from SQL Database using File_ID
                bool deleteSuccess = await _sqlDbService.DeleteFileMetadataAsync(fileId);

                if (!deleteSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Failed to delete metadata from the database.");
                    return Page();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to retrieve File_ID from metadata.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred while deleting: {ex.Message}");
            return Page();
        }

        return RedirectToPage(); // Refresh the page to show updated list
    }

    public async Task<IActionResult> OnPostDownloadAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            ModelState.AddModelError(string.Empty, "File name cannot be null or empty.");
            return Page();
        }

        string containerName = "uploads";
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        try
        {
            // Create a memory stream to hold the downloaded blob
            using (var memoryStream = new MemoryStream())
            {
                // Download the blob's contents to the memory stream
                await blobClient.DownloadToAsync(memoryStream);

                // Reset stream position for reading
                memoryStream.Position = 0;

                // Return the file to the client for download
                return File(memoryStream.ToArray(), "application/octet-stream", fileName);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred while downloading the file: {ex.Message}");
            return Page();
        }
    }
}
