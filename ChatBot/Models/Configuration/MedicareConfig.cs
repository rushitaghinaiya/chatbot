using System.ComponentModel.DataAnnotations;

namespace ChatBot.Models.Configuration
{
    public class MedicareConfig
    {
        public const string SectionName = "Medicare";

        [Required]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        public string KbName { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        [Required]
        public string DbType { get; set; } = string.Empty;

        public List<string> DocumentCategories { get; set; } = new();

        [Required]
        public string PythonApiBaseUrl { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 300; // 5 minutes default for file uploads
    }
}
