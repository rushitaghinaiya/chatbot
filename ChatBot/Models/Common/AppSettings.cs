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
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        public int ExpirationInMinutes { get; set; } = 60; // Default 1 hour

        public int RefreshTokenExpirationInDays { get; set; } = 7; // Default 7 days

        public string UserFilePath { get; set; }
        public string HtmlFolderPath { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string DomainEmailAddress { get; set; } = string.Empty;

        #region Email sender settings

        public bool IsMailGun { get; set; }
        public string MailGunApiKey { get; set; } = string.Empty;
        public string MailGunDomain { get; set; } = string.Empty;

        public string SMTPFromAddress { get; set; } = string.Empty;
        public string SMTPSubject { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;

        public string SMTPServer { get; set; } = string.Empty;
        public string SMTPPort { get; set; } = string.Empty;
        public string SMTPUsername { get; set; } = string.Empty;
        public string SMTPPassword { get; set; } = string.Empty;

        public string SMTPUseSSL { get; set; } = string.Empty;




        #endregion

        #region Social Media Links
        public string SendOtpEmailSubject { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        public string FacebookImgPath { get; set; } = string.Empty;
        public string LinkedInImgPath { get; set; } = string.Empty;

        public string WordPressUrl { get; set; } = string.Empty;
        public string WordPressImgPath { get; set; } = string.Empty;

        #endregion

    }
}
