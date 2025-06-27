namespace ChatBot.Models.ViewModels
{
    public class StoreFileVM
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FileUploadResult> UploadedFiles { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
    public class FileUploadResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool ProcessingSuccess { get; set; }
        public string? ProcessingError { get; set; }
    }
}
