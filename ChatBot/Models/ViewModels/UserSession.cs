namespace ChatBot.Models.ViewModels
{
    public class UserSession
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime LastActiveAt { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
