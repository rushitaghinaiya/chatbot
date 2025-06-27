namespace ChatBot.Models.Responses
{
    public class StoreFileResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string KbName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string DocumentCategory { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
    }
}
