using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class SqlDbService
{
    private readonly string _connectionString;

    public SqlDbService(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Method to check database connection
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
            // Log the exception (consider using a logging framework)
            Console.WriteLine($"Database connection error: {ex.Message}");
            return false; // Connection failed
        }
    }

    // Method to insert file metadata into SQL Server
    public async Task<bool> InsertFileMetadataAsync(int userId, string uploadDate, string subject, int year, string keywords, string storageLoc)
    {
        int fileId = await GetNextFileIdAsync();

        string query = "INSERT INTO FILES (User_ID, Date_Created, Subject, Grade, Keywords, Storage_Loc, File_ID) " +
                       "VALUES (@userId, @uploadDate, @subject, @year, @keywords, @storageLoc, @fileId)";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@uploadDate", DateTime.Parse(uploadDate));
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@year", year);
                command.Parameters.AddWithValue("@keywords", keywords);
                command.Parameters.AddWithValue("@storageLoc", storageLoc);
                command.Parameters.AddWithValue("@fileId", fileId);

                try
                {
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0; // Return true if data was inserted
                }
                catch (Exception ex)
                {
                    // Log the exception (consider using a logging framework)
                    Console.WriteLine($"Error inserting file metadata: {ex.Message}");
                    throw; // Re-throw the exception after logging
                }
            }
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
                try
                {
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
                catch (Exception ex)
                {
                    // Log the exception (consider using a logging framework)
                    Console.WriteLine($"Error getting next file ID: {ex.Message}");
                    throw; // Re-throw the exception after logging
                }
            }
        }
    }
}