using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Proj_Frame.ViewModels;

namespace Proj323.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;

        public SearchController(IConfiguration configuration, BlobServiceClient blobServiceClient)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _blobServiceClient = blobServiceClient;
        }

        [HttpGet("executeGetFiles")]
        public async Task<ActionResult<List<SearchViewModel>>> ExecuteGetFiles()
        {
            return await Task.FromResult(GetFiles(string.Empty, null, null));
        }

        [HttpGet]
        public ActionResult<List<SearchViewModel>> GetFiles([FromQuery] string searchQuery = "", [FromQuery] int? grade = null, [FromQuery] double? minRating = null)
        {
            var files = new List<SearchViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT File_ID, Storage_Loc, Keywords, Subject, Date_Created, Sum_Reviews, Review_Amount, Grade, Reports FROM FILES WHERE 1=1";

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += " AND (Storage_Loc LIKE @SearchQuery OR Subject LIKE @SearchQuery OR Keywords LIKE @SearchQuery)";
                }
                if (grade.HasValue)
                {
                    query += " AND Grade = @Grade";
                }
                if (minRating.HasValue)
                {
                    query += " AND (CASE WHEN Review_Amount > 0 THEN CAST(Sum_Reviews AS FLOAT) / Review_Amount ELSE 0 END) >= @MinRating";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    }
                    if (grade.HasValue)
                    {
                        command.Parameters.AddWithValue("@Grade", grade.Value);
                    }
                    if (minRating.HasValue)
                    {
                        command.Parameters.AddWithValue("@MinRating", minRating.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fileName = reader["Storage_Loc"].ToString();
                            fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

                            files.Add(new SearchViewModel
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

            // Always return all files when page loads
            return Ok(files);
        }

        [HttpPost]
        public async Task<IActionResult> RateFile([FromQuery] int fileId, [FromQuery] int rating)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest("Rating must be between 1 and 5.");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE FILES SET Sum_Reviews = Sum_Reviews + @Rating, Review_Amount = Review_Amount + 1 WHERE File_ID = @FileId", connection);
                command.Parameters.AddWithValue("@Rating", rating);
                command.Parameters.AddWithValue("@FileId", fileId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound("File not found.");
                }
            }

            return Ok("Rating submitted successfully.");
        }

        [HttpPost]
        public async Task<IActionResult> ReportFile([FromQuery] int fileId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE FILES SET Reports = Reports + 1 WHERE File_ID = @FileId", connection);
                command.Parameters.AddWithValue("@FileId", fileId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound("File not found.");
                }
            }

            return Ok("File reported successfully.");
        }
        [HttpGet]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName)
        {
            try
            {
                // Extract the correct filename (since filename might already include extension)
                string blobFileName = fileName.Contains("/") ? fileName.Substring(fileName.LastIndexOf("/") + 1) : fileName;

                // Get a reference to the Blob container
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient("uploads");
                var blobClient = blobContainerClient.GetBlobClient(blobFileName);

                // Check if the blob exists
                if (!await blobClient.ExistsAsync())
                {
                    return NotFound("File not found in storage.");
                }

                // Download the blob
                var blobDownloadInfo = await blobClient.DownloadAsync();

                // Return the file to the user as a downloadable file
                return File(blobDownloadInfo.Value.Content, blobDownloadInfo.Value.ContentType, blobFileName);
            }
            catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
            {
                return NotFound("File not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}