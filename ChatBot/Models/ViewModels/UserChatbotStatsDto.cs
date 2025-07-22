namespace ChatBot.Models.ViewModels
{
    public class UserChatbotStatsDto
    {
        public string UserId { get; set; }
        public string Mobile { get; set; }
        public string Type { get; set; }         // Free / Paid
        public string Queries { get; set; }      // e.g., 8/10 or 45/∞
        public int Family { get; set; } = 0;     // Static for now
        public int TimeInMin { get; set; }       // e.g., 24
    }

}
