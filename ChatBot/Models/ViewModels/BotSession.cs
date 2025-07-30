namespace ChatBot.Models.ViewModels
{
    public class BotSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalTimeSpent {  get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
