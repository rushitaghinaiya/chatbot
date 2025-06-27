using System.ComponentModel.DataAnnotations;

namespace ChatBot.Models.Entities
{
    public class StoreFileRequest
    {
        public string? YtUrl { get; set; }

        [Required]
        public string KbName { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        public string? DocumentCategory { get; set; }

        [Required]
        public string DbType { get; set; } = string.Empty;

        public List<IFormFile> Files { get; set; } = new();
    }
}
