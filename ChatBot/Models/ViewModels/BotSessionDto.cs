namespace ChatBot.Models.ViewModels
{
    public class BotSessionDto
    {
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalTimeSpent { get; set; }
    }
}
