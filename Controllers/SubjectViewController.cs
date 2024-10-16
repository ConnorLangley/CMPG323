using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using System;
using Proj_Frame.ViewModels;

namespace Proj323.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectViewController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;

        // Inject connection string and BlobServiceClient
        public SubjectViewController(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _blobServiceClient = blobServiceClient;
        }

        // Fetch distinct subjects from the database
        [HttpGet]
        [Route("subjects")]
        public async Task<ActionResult<List<string>>> GetSubjects()
        {
            List<string> subjects = new List<string>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT DISTINCT Subject FROM FILES";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            subjects.Add(reader.GetString(reader.GetOrdinal("Subject")));
                        }
                    }
                }
            }

            return Ok(subjects);
        }

        // Fetch all files with the selected subject from the database and blob metadata from Azure
        [HttpGet]
        [Route("subject-details")]
        public async Task<ActionResult<List<FileRecordViewModel>>> GetSubjectDetails([FromQuery] string subject)
        {
            List<FileRecordViewModel> fileRecords = new List<FileRecordViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT File_ID, Storage_Loc, Keywords, Subject, Date_Created, Sum_Reviews, Review_Amount, Grade, Reports FROM FILES WHERE Subject = @Subject";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Subject", subject);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var fileRecord = new FileRecordViewModel
                            {
                                File_ID = reader.GetInt32(reader.GetOrdinal("File_ID")),
                                FileName = reader["Storage_Loc"].ToString().Substring(reader["Storage_Loc"].ToString().LastIndexOf('/') + 1),
                                Keywords = reader.GetString(reader.GetOrdinal("Keywords")),
                                Subject = reader.GetString(reader.GetOrdinal("Subject")),
                                DateCreated = reader.GetDateTime(reader.GetOrdinal("Date_Created")),
                                Sum_Reviews = reader.GetInt32(reader.GetOrdinal("Sum_Reviews")),
                                Review_Amount = reader.GetInt32(reader.GetOrdinal("Review_Amount")),
                                Rating = reader.GetInt32(reader.GetOrdinal("Review_Amount")) > 0
                                    ? (double)reader.GetInt32(reader.GetOrdinal("Sum_Reviews")) / reader.GetInt32(reader.GetOrdinal("Review_Amount"))
                                    : 0.0,
                                Grade = reader.GetInt32(reader.GetOrdinal("Grade")),
                                Reports = reader.GetInt32(reader.GetOrdinal("Reports"))
                            };

                            fileRecords.Add(fileRecord);
                        }
                    }
                }
            }

            return Ok(fileRecords);
        }

        [HttpPost("rate")]
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


        [HttpPost("report")]
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
    }
}
