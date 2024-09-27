using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace test.Pages
{
    public class ModerationModel : PageModel
    {
        string connectionString = "Data Source=cmpg323.database.windows.net;Initial Catalog=CMPG323;User ID=41223268@mynwu.ac.za;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Authentication=ActiveDirectoryDefault;Application Intent=ReadWrite;Multi Subnet Failover=False";
        SqlConnection conn;
        SqlCommand com;
        SqlDataAdapter adapter;
        int FileID;
        

        public void OnGet()
        {

        }

        

        protected void AllowClick(object sender,EventArgs e)
        {
            conn.Open();
            string sqlCom = "UPDATE FILES SET Reports = 0 WHERE FILE_ID = "+FileID;
            com = new SqlCommand(sqlCom,conn);
            adapter = new SqlDataAdapter();
            adapter.UpdateCommand = com;
            adapter.UpdateCommand.ExecuteNonQuery();
            conn.Close();
        }

        protected void DeleteClick(Object sender, EventArgs e)
        {
            conn.Open();
            string sqlCom = "DELETE * FROM FILES WHERE FILE_ID = " + FileID;
            com = new SqlCommand(sqlCom, conn);
            adapter = new SqlDataAdapter();
            adapter.DeleteCommand = com;
            adapter.DeleteCommand.ExecuteNonQuery();
            conn.Close();

        }
    }
}
