using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;


namespace CMPG323_resetPassword.Pages
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

        public resetPassword RP { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }

        public string Contact { get; set; }

        public string SecurityQ { get; set; }

        public string NewPassword { get; set; }


        public void OnGet()
        {

        }

        public IActionResult OnPost()
        {
            // Security cheack
            conn.Open();

            comm = new SqlCommand("select * from USERS where Username'" + RP.Username + "'", conn);
            SqlDataReader dr = comm.ExecuteReader();

            if (dr.Read())
            {
                //Meassage of sesesfull read
            }
            else
            {
                //Error message to show unsecsesfull read
            }

            dr.Close();
            conn.Close();
            // Security cheack

            // Reset 

            if (RP.Password == RP.NewPassword)
            {

                conn.Open();

                comm = new SqlCommand("update USERS set password '" + RP.Password + "'where Usermane'" + RP.Username + "'`", conn);
                comm.ExecuteNonQuery();

                conn.Close();

                // Changed succsesfully message
            }
            else
            {
                // Error message passwords do not match
            }

            //reset
        }


    }
}
