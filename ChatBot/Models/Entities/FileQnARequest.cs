namespace ChatBot.Models.Entities
{
    public class FileQnARequest
    {
        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string KbName { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        public string? DocumentCategory { get; set; }

        [Required]
        public string DbType { get; set; } = string.Empty;
    }
}
