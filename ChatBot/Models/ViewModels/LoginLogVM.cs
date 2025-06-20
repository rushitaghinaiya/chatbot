namespace ChatBot.Models.ViewModels
{
    public class LoginLogVM
    {
        public int LogId { get; set; }         
        public int UserId { get; set; }        
        public DateTime LoginTime { get; set; } = DateTime.Now;
        public string Status { get; set; } = string.Empty;     
        public string? FailureReason { get; set; }            
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
