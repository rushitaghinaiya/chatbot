namespace ChatBot.Models.Request
{
    public class QnARequest
    {
        public string Question { get; set; } = string.Empty;
        public string? KbName { get; set; }
        public string? Language { get; set; }
        public string? DocumentCategory { get; set; }
        public string? DbType { get; set; }
    }
}
