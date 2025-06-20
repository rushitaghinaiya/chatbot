namespace ChatBot.Models.Common
{
    public class AppSettings
    {
        public string TwoFactorApiKey {  get; set; }
        public string SmsFrom {  get; set; }
        public int MobileOtpVerificationTime {  get; set; }

    }
}
