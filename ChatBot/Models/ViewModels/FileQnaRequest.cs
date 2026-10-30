namespace ChatBot.Models.ViewModels
{
    public class FileQnaRequest
    {
        public string Question { get; set; }
        public string KbName { get; set; }
        public string Language { get; set; }
        public string? DbType { get; set; }
    }

}
