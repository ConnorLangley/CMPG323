namespace Proj_Frame.ViewModels
{
    public class AccountViewModel
    {

        public string UserName { get; set; }
        public string? Password { get; set; }
        public string? SecurityAnswer { get; set; }
        public string? SecurityQuestion { get; set; }
        public int? User_ID { get; set; }  
        public int? Role_ID { get; set; }  
    }
}
