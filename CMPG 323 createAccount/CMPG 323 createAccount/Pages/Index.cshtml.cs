using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;


namespace CMPG_323_createAccount.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public SqlConnection conn;
        public SqlCommand comm;
        public DataSet ds;
        public SqlDataAdapter adap;
        public string constr = @"Server=tcp:cmpg323.database.windows.net,1433;Initial Catalog=CMPG323;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default"; //"Data Source=.;Initial Catalog=Projek;Integrated Security=True"; Data Sorce for the data base

        public createAccount AC { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Contact { get; set; }

        public string SecurityQ { get; set; }

        public string RePassword { get; set; }


        public void OnGet()
        {

        }

        public IActionResult OnPost()
        {
            if (AC.Password == AC.RePassword)
            {
                // Get Information ready and  insurt into database

                conn.Open();
                comm = new SqlCommand("insert into record USERS('" + AC.Username + "','" + AC.Password + "','" + AC.Contact + "','" + AC.SecurityQ + "')", conn);
                comm.ExecuteNonQuery();
                // Set message to display secsesfull data insertion
                conn.Close();

            }

        }

       
    }
}


