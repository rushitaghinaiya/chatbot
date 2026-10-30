using System.ComponentModel.DataAnnotations;

namespace ChatBot.Models.Configuration
{
    public class GoogleTranslateOptions
    {
        public const string SectionName = "GoogleTranslate";

        [Required]
        public string ApiKey { get; set; } = string.Empty;
    }
}
