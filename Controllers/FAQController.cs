using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Proj_Frame.ViewModels;

namespace Proj323.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly string _connectionString;

        public FAQController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlServerConnection");
        }

        // GET: api/FAQ
        [HttpGet("executeGetFAQs")]
        public async Task<ActionResult<List<FAQViewModel>>> ExecuteGetFAQs()
        {
            return await Task.FromResult(GetFAQs(string.Empty));
        }

        // GET: api/FAQ
        [HttpGet]
        public ActionResult<List<FAQViewModel>> GetFAQs([FromQuery] string searchQuery = "")
        {
            var faqs = new List<FAQViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT QUEST_ID, Question_Desc, Answer FROM dbo.FAQ WHERE 1=1";

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += " AND (Question_Desc LIKE @SearchQuery OR Answer LIKE @SearchQuery)";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            faqs.Add(new FAQViewModel
                            {
                                Question_Desc = reader["Question_Desc"].ToString(),
                                Answer = reader["Answer"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(faqs);
        }

        // POST: api/FAQ
        [HttpPost]
        public async Task<IActionResult> CreateFAQ([FromForm] FAQViewModel faq)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get the highest QUEST_ID and increment it
                var getMaxIdQuery = "SELECT ISNULL(MAX(QUEST_ID), 0) + 1 FROM dbo.FAQ";
                int newId;
                using (var command = new SqlCommand(getMaxIdQuery, connection))
                {
                    newId = (int)await command.ExecuteScalarAsync();
                }

                var query = "INSERT INTO dbo.FAQ (QUEST_ID, Question_Desc, Answer) VALUES (@QUEST_ID, @Question_Desc, @Answer)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@QUEST_ID", newId);
                    command.Parameters.AddWithValue("@Question_Desc", faq.Question_Desc);
                    command.Parameters.AddWithValue("@Answer", faq.Answer);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { message = "FAQ created successfully", faq });
        }

        // PUT: api/FAQ/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFAQ(int id, [FromForm] FAQViewModel faq)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "UPDATE dbo.FAQ SET Question_Desc = @Question_Desc, Answer = @Answer WHERE QUEST_ID = @QUEST_ID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Question_Desc", faq.Question_Desc);
                    command.Parameters.AddWithValue("@Answer", faq.Answer);
                    command.Parameters.AddWithValue("@QUEST_ID", id);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = "FAQ not found." });
                    }
                }
            }

            return Ok(new { message = "FAQ updated successfully" });
        }

        // DELETE: api/FAQ/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "DELETE FROM dbo.FAQ WHERE QUEST_ID = @QUEST_ID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@QUEST_ID", id);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound(new { message = "FAQ not found." });
                    }
                }
            }

            return Ok(new { message = "FAQ deleted successfully" });
        }
    }
}