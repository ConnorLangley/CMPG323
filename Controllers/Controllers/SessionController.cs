using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Proj_Frame.ViewModels;

namespace Proj323.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]

    public class SessionController : Controller
    {

        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;

        // Inject connection string and BlobServiceClient
        public SessionController(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _blobServiceClient = blobServiceClient;
        }


        // Method to set the username in the session
        public void SetUserSession(string username)
        {
            HttpContext.Session.SetString(SessionVarubles.SessionKeyUsername, username);
        }
        [HttpPost("SetSession")]
        public void SetRoleSession(string role)
        {
            HttpContext.Session.SetString(SessionVarubles.SessionKeyRole, role);
        }

        // Method to get session information
        [HttpGet("Session")]
        public IEnumerable<string> GetSessionInfo()
        {
            List<string> sessionInfo = new List<string>();

            // Retrieve the username from the session
            var userName = HttpContext.Session.GetString(SessionVarubles.SessionKeyUsername);
            var Role = HttpContext.Session.GetString(SessionVarubles.SessionKeyRole);
            if (string.IsNullOrEmpty(userName))
            {
                return new string[] { "No user is logged in." };
            }

            sessionInfo.Add(userName);
            sessionInfo.Add(Role);
            return sessionInfo;
        }

        // Example method to demonstrate login and set session
        [HttpGet("login")]
        public IActionResult Login([FromBody] LoginViewModel loginViewModel)
        {
            // Validate input
            if (loginViewModel == null ||
                string.IsNullOrEmpty(loginViewModel.UserName) ||
                string.IsNullOrEmpty(loginViewModel.Password))
            {
                return BadRequest("Invalid login request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Password, Role_ID FROM [USER] WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", loginViewModel.UserName));

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    // Check if the user exists
                    if (!reader.Read())
                    {
                        return Unauthorized("Invalid username or password.");
                    }

                    var storedPassword = reader["Password"].ToString();
                    int roleId = (int)reader["Role_ID"];

                    // Check if the provided password matches the stored password
                    if (loginViewModel.Password == storedPassword)
                    {
                        // Login successful, set the session
                        SetUserSession(loginViewModel.UserName);
                        return Ok(new { Message = "Login successful.", RoleId = roleId });
                    }
                    else
                    {
                        return Unauthorized("Invalid username or password.");
                    }
                }
            }
        }
    }
}
