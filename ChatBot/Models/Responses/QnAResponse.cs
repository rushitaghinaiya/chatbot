namespace ChatBot.Models.Responses
{
    public class QnAResponse
    {
        public string Answer { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Sources { get; set; } = new();
        public string ResponseId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string KbName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string DbType { get; set; } = string.Empty;
    }
}
