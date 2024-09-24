using System.Data.SqlClient;
using System.Threading.Tasks;

public class SqlDbService
{
    private readonly string _connectionString;

    public SqlDbService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> InsertFileMetadataAsync(string subject, int year, string description, string fileName, string uploadDate, string storageLoc)
    {
        string query = @"INSERT INTO FILES (Subject, Grade, Keywords, Date_Created, Storage_Loc, File_ID) 
                         VALUES (@subject, @year, @description, @uploadDate, @storageLoc, (SELECT ISNULL(MAX(File_ID), 0) + 1 FROM FILES))";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@year", year);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@fileName", fileName);
                command.Parameters.AddWithValue("@uploadDate", uploadDate);
                command.Parameters.AddWithValue("@storageLoc", storageLoc);

                return await command.ExecuteNonQueryAsync();
            }
        }
    }
}
