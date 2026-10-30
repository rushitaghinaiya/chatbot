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

    public class UserDetailsExcel
    {
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LoginEmail { get; set; }
        public string Phone { get; set; }
        public string Courses { get; set; }
        public string Queries { get; set; }
        public bool IsMembership {  get; set; }
    }

}
