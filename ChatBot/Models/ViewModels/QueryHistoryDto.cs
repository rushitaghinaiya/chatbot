namespace ChatBot.Models.ViewModels
{
    public class QueryHistoryDto
    {
        public int UserId { get; set; }
        public string QueryText { get; set; }
        public string ResponseText { get; set; }
        public double? ResponseTime { get; set; }
        public string Topic { get; set; }
        public string Status { get; set; }
    }
}
