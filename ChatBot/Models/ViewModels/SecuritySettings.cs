namespace ChatBot.Models.ViewModels
{
    public class SecuritySettings
    {
        public int Id { get; set; }
        public bool TwoFactorAuthEnabled { get; set; }
        public bool AutoLogoutEnabled { get; set; }
        public int AutoLogoutDurationMinutes { get; set; }
        public bool EnableAuditLogging { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class VoiceAccessibilitySettings
    {
        public int Id { get; set; }
        public bool EnableVoiceCommands { get; set; }
        public bool EnableTextToSpeech { get; set; }
        public bool EnableVoiceToText { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class SystemLimits
    {
        public int Id { get; set; }
        public int FreeUserQueryLimit { get; set; }
        public int CharacterLimitPerQuery { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }   // You can change to string if duration is in text format like "3 months"
        public decimal Price { get; set; }
        public string VideoRuntime { get; set; }   // Keeping as string (e.g. "02:30:00"), can use TimeSpan if always HH:MM:SS
        public int LanguageId { get; set; }
        public string CourseDetails { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }


}
