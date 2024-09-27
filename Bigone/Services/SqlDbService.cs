using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Bigone;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class SqlDbService
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;

    public SqlDbService(string connectionString, IConfiguration configuration, BlobServiceClient blobServiceClient)
    {
        _connectionString = connectionString;
        _configuration = configuration;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return true; // Connection successful
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database connection error: {ex.Message}");
            return false; // Connection failed
        }
    }

    public async Task<int> GetNextFileIdAsync()
    {
        string query = "SELECT ISNULL(MAX(File_ID), 0) + 1 FROM FILES";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(query, connection))
            {
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }
    }

    public async Task<bool> InsertFileMetadataAsync(int userId, string uploadDate, string subject, int grade, string keywords, string storageLoc)
    {
        int fileId = await GetNextFileIdAsync();

        string query = "INSERT INTO FILES (User_ID, Date_Created, Subject, Grade, Keywords, Storage_Loc, File_ID) " +
                       "VALUES (@userId, @uploadDate, @subject, @grade, @keywords, @storageLoc, @fileId)";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@uploadDate", DateTime.Parse(uploadDate));
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@grade", grade);
                command.Parameters.AddWithValue("@keywords", keywords);
                command.Parameters.AddWithValue("@storageLoc", storageLoc);
                command.Parameters.AddWithValue("@fileId", fileId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
    }

    public async Task<bool> DeleteFileMetadataAsync(int fileId)
    {
        string query = "DELETE FROM FILES WHERE File_ID = @fileId";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@fileId", fileId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0; // Return true if at least one row was deleted
            }
        }
    }

}