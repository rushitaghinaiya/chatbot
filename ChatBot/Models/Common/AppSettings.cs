using System.ComponentModel.DataAnnotations;

namespace ChatBot.Models.Common
{
    public class AppSettings
    {
        public string TwoFactorApiKey {  get; set; }
        public string SmsFrom {  get; set; }
        public int MobileOtpVerificationTime {  get; set; }

        //jwt token
        public const string SectionName = "Jwt";

        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        public int ExpirationInMinutes { get; set; } = 60; // Default 1 hour

        public int RefreshTokenExpirationInDays { get; set; } = 7; // Default 7 days

    }
}
