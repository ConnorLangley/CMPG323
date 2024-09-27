using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Xml.Serialization;



namespace test.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;


        string connectionString = "Data Source=cmpg323.database.windows.net;Initial Catalog=CMPG323;User ID=41223268@mynwu.ac.za;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Authentication=ActiveDirectoryDefault;Application Intent=ReadWrite;Multi Subnet Failover=False";
        SqlConnection conn;
        SqlCommand com;
        SqlDataAdapter dataAdapter;
        SqlDataReader reader;
        int reviewAmount = 0;
        int fileID = -1;



        public int SelectedIndex { get; set; }


        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

        }

        [BindProperty]

        public int Rating { get; set; }





        public void OnGetAsync()
        {
            
           

        }

        protected void DropDownChange(Object sender, EventArgs e)
        {
           // reviewAmount = rating.SelectedIndex;
        }

        protected void ReviewClick(Object sender, EventArgs e)
        {
            int rating = Int32.Parse( Request.Form["Rating"]);
            conn.Open();
            string sqlcom = "SELECT Sum_Reviews FROM FILES WHERE File_ID = " + fileID;
            dataAdapter = new SqlDataAdapter();
            com = new SqlCommand(sqlcom,conn);
            dataAdapter.SelectCommand = com;
            reader = com.ExecuteReader();

            int sum = Int32.Parse( reader.GetValue(0).ToString());
            sqlcom = "SELECT review_amount FROM FILES WHERE File_ID=" + fileID;
            dataAdapter = new SqlDataAdapter();
            com = new SqlCommand(sqlcom,conn);
            dataAdapter.SelectCommand = com;
            reader = com.ExecuteReader();
            int total = Int32.Parse( reader.GetValue(0).ToString() );
            sum = sum + rating;
            int newVal = sum / total;
            sqlcom = "UPDATE FILES SET Reviews ="+newVal;

            sqlcom = "UPDATE User_Analytics SET Files_Rated = Files_Rated +1 WHERE User_ID =  " /*Session["ID"]*/;
            dataAdapter = new SqlDataAdapter();
            com = new SqlCommand(sqlcom, conn);
            dataAdapter.UpdateCommand = com;
            dataAdapter.UpdateCommand.ExecuteNonQuery();
            conn.Close();


        }

        protected void reportClick()
        {
            conn.Open();
            string sqlcom = "UPDATE FILES SET Reports = Reports+1";
            com = new SqlCommand(sqlcom,conn);
            dataAdapter = new SqlDataAdapter();
            dataAdapter.UpdateCommand = com;
            dataAdapter.UpdateCommand.ExecuteNonQuery();
            

            sqlcom = "UPDATE User_Analytics SET Files_Reported = Files_Reported+1 WHERE User_ID = " /*Session["ID"]*/;
            com = new SqlCommand(sqlcom,conn);
            dataAdapter=new SqlDataAdapter();
            dataAdapter.UpdateCommand = com;
            dataAdapter.UpdateCommand.ExecuteNonQuery();

            conn.Close();


        }
    }
}
