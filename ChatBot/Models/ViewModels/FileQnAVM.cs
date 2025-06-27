namespace ChatBot.Models.ViewModels
{
    public class FileQnAVM
    {
        public bool Success { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
        public double Confidence { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
