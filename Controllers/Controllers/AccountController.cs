using Microsoft.AspNetCore.Mvc;
using Proj_Frame.ViewModels;
using System.Data;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using System;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Proj323.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _blobServiceClient;

        // Inject connection string and BlobServiceClient
        public AccountController(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost]
        [Route("create")]
        public IActionResult CreateAccount([FromBody] AccountViewModel accountCreateViewModel)
        {
            // Validate input
            if (accountCreateViewModel == null ||
                string.IsNullOrEmpty(accountCreateViewModel.UserName) ||
                string.IsNullOrEmpty(accountCreateViewModel.Password)||
                string.IsNullOrEmpty(accountCreateViewModel.SecurityQuestion)||
                string.IsNullOrEmpty(accountCreateViewModel.SecurityAnswer))

            {
                return BadRequest("Invalid account creation request.");
            }

           

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Check if the username already exists
                string checkQuery = "SELECT COUNT(*) FROM [USER] WHERE Username = @UserName";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@UserName", accountCreateViewModel.UserName);

                    connection.Open();
                    int userCount = (int)checkCommand.ExecuteScalar();

                    // If the username already exists, return a conflict response
                    if (userCount > 0)
                    {
                        return Conflict("Username already in use.");
                    }
                }

                // If username is available, proceed to create the account
                string insertQuery = "INSERT INTO [USER] (Username, Password, Sec_Quest, Sec_Ans, Role_ID) " +
                                     "VALUES (@UserName, @Password, @SecurityQuestion, @SecurityAwnser, @Role_ID)";

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.Add(new SqlParameter("@UserName", accountCreateViewModel.UserName));
                    insertCommand.Parameters.Add(new SqlParameter("@Password", accountCreateViewModel.Password));
                    insertCommand.Parameters.Add(new SqlParameter("@SecurityQuestion", accountCreateViewModel.SecurityQuestion));
                    insertCommand.Parameters.Add(new SqlParameter("@SecurityAwnser", accountCreateViewModel.SecurityAnswer));
                    insertCommand.Parameters.Add(new SqlParameter("@Role_ID", accountCreateViewModel.Role_ID));

                    int rowsAffected = insertCommand.ExecuteNonQuery();

                    // Check if the insert was successful
                    if (rowsAffected > 0)
                    {
                        return Ok("Account created successfully.");
                    }
                    else
                    {
                        return StatusCode(500, "An error occurred while creating the account.");
                    }
                }
            }
        }

       

        [HttpDelete]
        [Route("delete")]
        public IActionResult DeleteAccount(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Invalid account deletion request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [USER] WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", username));

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Account deleted successfully.");
                    }
                    else
                    {
                        return NotFound("Account not found.");
                    }
                }
            }
        }

        [HttpPost]
        [Route("update")]
        public IActionResult UpdateAccount([FromBody] AccountViewModel accountUpdateViewModel)
        {
            Console.WriteLine("Received UpdateAccount request:");
            Console.WriteLine($"UserName: {accountUpdateViewModel?.UserName}");
            Console.WriteLine($"Password: {accountUpdateViewModel?.Password}");
            Console.WriteLine($"SecurityQuestion: {accountUpdateViewModel?.SecurityQuestion}");
            Console.WriteLine($"SecurityAnswer: {accountUpdateViewModel?.SecurityAnswer}");
            Console.WriteLine($"Role_ID: {accountUpdateViewModel?.Role_ID}");

            if (!ModelState.IsValid)
            {
                // Log specific validation errors and return only if UserName is missing
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("ModelState validation errors: " + string.Join(", ", errors));

                // Return validation error only if UserName is missing or invalid
                if (string.IsNullOrEmpty(accountUpdateViewModel.UserName))
                {
                    return BadRequest(new { message = "UserName is required.", errors });
                }
            }

            if (string.IsNullOrEmpty(accountUpdateViewModel.UserName))
            {
                return BadRequest(new { message = "Invalid account update request", reason = "UserName is missing" });
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var updateFields = new List<string>();

                if (!string.IsNullOrEmpty(accountUpdateViewModel.Password))
                    updateFields.Add("Password = @Password");
                if (!string.IsNullOrEmpty(accountUpdateViewModel.SecurityQuestion))
                    updateFields.Add("Sec_Quest = @SecurityQuestion");
                if (!string.IsNullOrEmpty(accountUpdateViewModel.SecurityAnswer))
                    updateFields.Add("Sec_Ans = @SecurityAnswer");
                if (accountUpdateViewModel.Role_ID.HasValue)
                    updateFields.Add("Role_ID = @Role_ID");

                if (updateFields.Count == 0)
                    return BadRequest(new { message = "No valid fields to update", reason = "No fields provided in request" });

                string query = $"UPDATE [USER] SET {string.Join(", ", updateFields)} WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", accountUpdateViewModel.UserName);

                    if (!string.IsNullOrEmpty(accountUpdateViewModel.Password))
                        command.Parameters.AddWithValue("@Password", accountUpdateViewModel.Password);
                    if (!string.IsNullOrEmpty(accountUpdateViewModel.SecurityQuestion))
                        command.Parameters.AddWithValue("@SecurityQuestion", accountUpdateViewModel.SecurityQuestion);
                    if (!string.IsNullOrEmpty(accountUpdateViewModel.SecurityAnswer))
                        command.Parameters.AddWithValue("@SecurityAnswer", accountUpdateViewModel.SecurityAnswer);
                    if (accountUpdateViewModel.Role_ID.HasValue)
                        command.Parameters.AddWithValue("@Role_ID", accountUpdateViewModel.Role_ID.Value);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Account updated successfully.");
                    }
                    else
                    {
                        return NotFound(new { message = "Account not found", reason = "No matching Username in database" });
                    }
                }
            }
        }

        [HttpPost]
        [Route("reset-password")]
        public IActionResult ResetPassword([FromForm] AccountViewModel resetPasswordViewModel)
        {
            if (resetPasswordViewModel == null ||
                string.IsNullOrEmpty(resetPasswordViewModel.UserName) ||
                string.IsNullOrEmpty(resetPasswordViewModel.Password))
            {
                return BadRequest("Invalid password reset request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE [USER] SET Password = @Password WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", resetPasswordViewModel.UserName));
                    command.Parameters.Add(new SqlParameter("@Password", resetPasswordViewModel.Password));
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Password reset successfully.");
                    }
                    else
                    {
                        return NotFound("Account not found.");
                    }
                }
            }
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login([FromQuery]string UserName,[FromQuery]string Password)
        {
            // Validate input
           /* if (loginViewModel == null ||
                string.IsNullOrEmpty(loginViewModel.UserName) ||
                string.IsNullOrEmpty(loginViewModel.Password))
            {
                return BadRequest("Invalid login request.");
            }*/

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Fetch username, password, and role from the database
                string query = "SELECT Password, Role_ID FROM [USER] WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", /*loginViewModel.*/UserName));

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    // Check if the user exists and fetch the password and role
                    if (!reader.Read())
                    {
                        return Unauthorized("Invalid username or password.");
                    }

                    var storedPassword = reader["Password"].ToString();
                    int roleId = (int)reader["Role_ID"];

                    // Check if the provided password matches the stored password
                    if (/*loginViewModel.*/Password == storedPassword)
                    {
                        // Login successful, generate JWT token
                        var token = GenerateJwtToken(/*loginViewModel.*/UserName, roleId);
                        return Ok(new { Token = token });
                    }
                    else
                    {
                        return Unauthorized("Invalid username or password.");
                    }
                }
            }
        }

        private string GenerateJwtToken(string username, int roleId)
        {
            // Make sure to replace this with a key that is at least 256 bits
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHereThatIsAtLeast32CharactersLong"));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, username),
        new Claim("role", roleId.ToString())
    };

            var token = new JwtSecurityToken(
                issuer: "YourIssuer",
                audience: "YourAudience",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("search")]
        public IActionResult SearchAccounts(string username)
        {
            List<AccountViewModel> accounts = new List<AccountViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Username, Password, Sec_Quest, Sec_Ans, User_ID, Role_ID FROM [USER] WHERE Username LIKE @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", $"%{username}%");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Log each field to see the exact values being retrieved
                            Console.WriteLine("Database Values:");
                            Console.WriteLine("Username: " + (reader["Username"]?.ToString() ?? "NULL"));
                            Console.WriteLine("Password: " + (reader["Password"]?.ToString() ?? "NULL"));
                            Console.WriteLine("Sec_Quest: " + (reader["Sec_Quest"]?.ToString() ?? "NULL"));
                            Console.WriteLine("Sec_Ans: " + (reader["Sec_Ans"]?.ToString() ?? "NULL"));
                            Console.WriteLine("User_ID: " + (reader["User_ID"] != DBNull.Value ? reader["User_ID"].ToString() : "NULL"));
                            Console.WriteLine("Role_ID: " + (reader["Role_ID"] != DBNull.Value ? reader["Role_ID"].ToString() : "NULL"));

                            // Add the account data to the list
                            accounts.Add(new AccountViewModel
                            {
                                UserName = reader["Username"]?.ToString() ?? "N/A",
                                Password = reader["Password"]?.ToString() ?? "N/A",
                                SecurityQuestion = reader["Sec_Quest"]?.ToString() ?? "N/A",
                                SecurityAnswer = reader["Sec_Ans"]?.ToString() ?? "N/A",
                                User_ID = reader["User_ID"] != DBNull.Value ? (int?)reader["User_ID"] : null,
                                Role_ID = reader["Role_ID"] != DBNull.Value ? (int?)reader["Role_ID"] : null
                            });
                        }
                    }
                }
            }

            return Ok(accounts);
        }
        [HttpGet("searchAll")]
        public IActionResult SearchAllAccounts(string username)
        {
            List<AccountViewModel> accounts = new List<AccountViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Username, Password, Sec_Quest, Sec_Ans, User_ID, Role_ID FROM [USER]";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", $"%{username}%");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            accounts.Add(new AccountViewModel
                            {
                                UserName = reader["Username"].ToString(),
                                Password = reader["Password"].ToString(),
                                SecurityQuestion = reader["Sec_Quest"].ToString(),
                                SecurityAnswer = reader["Sec_Ans"].ToString(),
                                Role_ID = (int)reader["Role_ID"]
                            });
                        }
                    }
                }
            }

            return Ok(accounts);
        }

    }

}
