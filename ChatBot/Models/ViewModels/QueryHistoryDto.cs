namespace ChatBot.Models.ViewModels
{
    public class QueryHistoryDto
    {
        public string SessionId { get; set; }
        public string EmailId { get; set; }
        public string QueryText { get; set; }
        public string ResponseText { get; set; }
        public string ChatJson { get; set; }
        public double? ResponseTime { get; set; }
        public string Topic { get; set; }
        public string Status { get; set; }
    }
}
