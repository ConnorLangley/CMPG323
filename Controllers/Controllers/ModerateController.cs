using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Proj_Frame.ViewModels;

namespace Proj323.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class ModerateController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;

        public ModerateController(IConfiguration configuration, BlobServiceClient blobServiceClient)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _blobServiceClient = blobServiceClient;
        }

        [HttpGet]
        public ActionResult<List<ModerateViewModel>> GetFilesToModerate()
        {
            try
            {
                var files = new List<ModerateViewModel>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT File_ID, Storage_Loc, Keywords, Subject, Date_Created, Sum_Reviews, Review_Amount, Grade, Reports FROM FILES WHERE Reports > 5";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return NotFound("No files meet the moderation criteria.");
                            }

                            while (reader.Read())
                            {
                                var fileName = reader["Storage_Loc"].ToString();
                                fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

                                files.Add(new ModerateViewModel
                                {
                                    File_ID = reader.GetInt32(reader.GetOrdinal("File_ID")),
                                    FileName = fileName,
                                    Keywords = reader["Keywords"].ToString(),
                                    Subject = reader["Subject"].ToString(),
                                    DateCreated = reader.GetDateTime(reader.GetOrdinal("Date_Created")),
                                    Sum_Reviews = reader.GetInt32(reader.GetOrdinal("Sum_Reviews")),
                                    Review_Amount = reader.GetInt32(reader.GetOrdinal("Review_Amount")),
                                    Rating = reader.GetInt32(reader.GetOrdinal("Review_Amount")) > 0
                                        ? (double)reader.GetInt32(reader.GetOrdinal("Sum_Reviews")) / reader.GetInt32(reader.GetOrdinal("Review_Amount"))
                                        : 0.0,
                                    Grade = reader.GetInt32(reader.GetOrdinal("Grade")),
                                    Reports = reader.GetInt32(reader.GetOrdinal("Reports"))
                                });
                            }
                        }
                    }
                }

                return Ok(files);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, $"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFile([FromQuery] int fileId)
        {
            string fileName = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var selectCommand = new SqlCommand("SELECT Storage_Loc FROM FILES WHERE File_ID = @FileId", connection);
                selectCommand.Parameters.AddWithValue("@FileId", fileId);

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        fileName = reader["Storage_Loc"].ToString();
                    }
                    else
                    {
                        return NotFound("File not found.");
                    }
                }

                var deleteCommand = new SqlCommand("DELETE FROM FILES WHERE File_ID = @FileId", connection);
                deleteCommand.Parameters.AddWithValue("@FileId", fileId);

                int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound("File not found.");
                }
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient("uploads");
                var blobClient = blobContainerClient.GetBlobClient(fileName.Substring(fileName.LastIndexOf("/") + 1));

                try
                {
                    await blobClient.DeleteIfExistsAsync();
                }
                catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
                {
                    // Blob already not found, continue with delete process.
                }
            }

            return Ok("File deleted successfully.");
        }

        [HttpPost("allow")]
        public async Task<IActionResult> AllowFile([FromQuery] int fileId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE FILES SET Reports = 0 WHERE File_ID = @FileId", connection);
                command.Parameters.AddWithValue("@FileId", fileId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound("File not found.");
                }
            }

            return Ok("File allowed successfully.");
        }

        
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient("uploads");
            var blobClient = blobContainerClient.GetBlobClient(fileName + ".pdf");

            try
            {
                var blobProperties = await blobClient.GetPropertiesAsync();
                var originalFileName = blobProperties.Value.Metadata.ContainsKey("OriginalFileName")
                    ? blobProperties.Value.Metadata["OriginalFileName"]
                    : fileName;

                // Ensure the file is downloaded as a PDF
                if (!originalFileName.EndsWith(".pdf"))
                {
                    originalFileName += ".pdf";
                }

                var blobDownloadInfo = await blobClient.DownloadAsync();
                return File(blobDownloadInfo.Value.Content, blobDownloadInfo.Value.ContentType, originalFileName);
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
            {
                return NotFound("File not found.");
            }
        }
    }
}
