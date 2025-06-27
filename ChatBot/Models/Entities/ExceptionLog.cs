namespace ChatBot.Models.Entities
{
    public class ExceptionLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string ExceptionType { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string User { get; set; }
    }
}
