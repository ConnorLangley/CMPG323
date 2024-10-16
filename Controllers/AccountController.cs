using Microsoft.AspNetCore.Mvc;
using Proj323.ViewModels;
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
    [Route("api/[controller]")]
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
        public IActionResult CreateAccount([FromForm] AccountViewModel accountCreateViewModel)
        {
            // Validate input
            if (accountCreateViewModel == null ||
                string.IsNullOrEmpty(accountCreateViewModel.UserName) ||
                string.IsNullOrEmpty(accountCreateViewModel.Password))
            {
                return BadRequest("Invalid account creation request.");
            }

            // Validate Role_ID to ensure it's either 0, 1, 2, or 3
            if (accountCreateViewModel.Role_ID < 0 || accountCreateViewModel.Role_ID > 3)
            {
                return BadRequest("Role_ID must be either 0 (Open User), 1 (Admin), 2 (Moderator), or 3 (Educator).");
            }

            if (!IsValidPassword(accountCreateViewModel.Password))
            {
                return BadRequest("Password must contain at least one number and one special character.");
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
                    insertCommand.Parameters.Add(new SqlParameter("@SecurityAwnser", accountCreateViewModel.SecurityAwnser));
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

        private bool IsValidPassword(string password)
        {
            // Password must contain at least one number and one special character
            string pattern = @"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':\\|,.<>\/?]).+$";
            return Regex.IsMatch(password, pattern);
        }

        [HttpDelete]
        [Route("delete/{username}")]
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

        [HttpPut]
        [Route("update")]
        public IActionResult UpdateAccount([FromForm] AccountViewModel accountUpdateViewModel)
        {
            if (accountUpdateViewModel == null ||
                string.IsNullOrEmpty(accountUpdateViewModel.UserName) ||
                string.IsNullOrEmpty(accountUpdateViewModel.Password))
            {
                return BadRequest("Invalid account update request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "UPDATE [USER] SET Password = @Password, Sec_Quest = @SecurityQuestion, " +
                               "Sec_Ans = @SecurityAwnser, Role_ID = @Role_ID WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", accountUpdateViewModel.UserName));
                    command.Parameters.Add(new SqlParameter("@Password", accountUpdateViewModel.Password)); // Consider hashing the password again
                    command.Parameters.Add(new SqlParameter("@SecurityQuestion", accountUpdateViewModel.SecurityQuestion));
                    command.Parameters.Add(new SqlParameter("@SecurityAwnser", accountUpdateViewModel.SecurityAwnser));
                    command.Parameters.Add(new SqlParameter("@Role_ID", accountUpdateViewModel.Role_ID));

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok("Account updated successfully.");
                    }
                    else
                    {
                        return NotFound("Account not found.");
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

        [HttpPost]
        [Route("login")]
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
                // Fetch username, password, and role from the database
                string query = "SELECT Password, Role_ID FROM [USER] WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", loginViewModel.UserName));

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
                    if (loginViewModel.Password == storedPassword)
                    {
                        // Login successful, generate JWT token
                        var token = GenerateJwtToken(loginViewModel.UserName, roleId);
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

        [HttpGet("search/{username}")]
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
                            accounts.Add(new AccountViewModel
                            {
                                UserName = reader["Username"].ToString(),
                                Password = reader["Password"].ToString(),
                                SecurityQuestion = reader["Sec_Quest"].ToString(),
                                SecurityAwnser = reader["Sec_Ans"].ToString(),
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