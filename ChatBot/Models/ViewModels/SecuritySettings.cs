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

}
