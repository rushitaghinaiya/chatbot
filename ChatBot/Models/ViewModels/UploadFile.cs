namespace ChatBot.Models.ViewModels
{
    public class UploadFile
    {
        public int Id { get; set; }
        public int UploadedBy { get; set; }
        public string FileName { get; set; } = null!;
        public string? FileType { get; set; }
        public int? FileSize { get; set; }
        public string? Status { get; set; } = "Processing";
        public int Queries { get; set; } = 0;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EditedAt { get; set; } = DateTime.Now;
    }
}
