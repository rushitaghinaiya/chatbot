namespace ChatBot.Models.ViewModels
{
    public class FreeUsersOverviewDto
    {
        public int TotalFreeUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int HighUsageUsers { get; set; } // >80% usage
    }

    public class FreeUserQueryTypeDto
    {
        public string QueryType { get; set; }
        public int Count { get; set; }
    }

    public class FreeUserDetail
    {
        public string UserId { get; set; }
        public string Mobile { get; set; }
        public string Status { get; set; }
        public int UsedQueries { get; set; }
        public int QueryLimit { get; set; }
        public string QueryUsage { get; set; } // Example: "8/10 (80%)"
        public string LastActivity { get; set; }
    }

    public class CommunicationSetting
    {
        public int SettingId { get; set; }
        public string UserType { get; set; }
        public bool EmailEnabled { get; set; }
        public bool SMSEnabled { get; set; }
        public bool WhatsAppEnabled { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
