namespace ChatBot.Models.ViewModels
{
    public class AdminLoginLog
    {
        public int Id { get; set; }             
        public int AdminId { get; set; }         
        public string Email { get; set; } = null!; 
        public DateTime? LoginTime { get; set; }  
        public string? Actions { get; set; }     
    }

}
