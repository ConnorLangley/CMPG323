using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure;
using System;
using Proj_Frame.ViewModels;

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
            if (accountCreateViewModel == null ||
                string.IsNullOrEmpty(accountCreateViewModel.UserName) ||
                string.IsNullOrEmpty(accountCreateViewModel.Password))
            {
                return BadRequest("Invalid account creation request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "Insert INTO [USER] (Username, Password, Sec_Quest, Sec_Ans, Role_ID) " +
                               "VALUES (@UserName, @Password, @ContactInformation, @SecurityQuestion, @Role_ID)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", accountCreateViewModel.UserName));
                    command.Parameters.Add(new SqlParameter("@Password", accountCreateViewModel.Password));
                    command.Parameters.Add(new SqlParameter("@ContactInformation", accountCreateViewModel.ContactInformation));
                    command.Parameters.Add(new SqlParameter("@SecurityQuestion", accountCreateViewModel.SecurityQuestion));
                    command.Parameters.Add(new SqlParameter("@Role_ID", accountCreateViewModel.Role_ID));

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

     
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
                string query = "UPDATE [USER] SET Password = @Password, Sec_Quest = @ContactInformation, " +
                               "Sec_Ans = @SecurityQuestion, Role_ID = @Role_ID WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", accountUpdateViewModel.UserName));
                    command.Parameters.Add(new SqlParameter("@Password", accountUpdateViewModel.Password)); // Consider hashing the password again
                    command.Parameters.Add(new SqlParameter("@ContactInformation", accountUpdateViewModel.ContactInformation));
                    command.Parameters.Add(new SqlParameter("@SecurityQuestion", accountUpdateViewModel.SecurityQuestion));
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
        public IActionResult Login([FromForm] LoginViewModel loginViewModel)
        {
            if (loginViewModel == null ||
                string.IsNullOrEmpty(loginViewModel.UserName) ||
                string.IsNullOrEmpty(loginViewModel.Password))
            {
                return BadRequest("Invalid login request.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Password FROM [USER] WHERE Username = @UserName";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserName", loginViewModel.UserName));

                    connection.Open();
                    var storedPassword = command.ExecuteScalar() as string;

                    if (storedPassword == null)
                    {
                        return Unauthorized("Invalid username or password.");
                    }

                    // Check if the provided password matches the stored password
                    if (loginViewModel.Password == storedPassword)
                    {
                        // Login successful
                        return Ok("Login successful.");
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