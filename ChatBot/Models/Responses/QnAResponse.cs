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

    public class QnaResponse
    {
        public string Question { get; set; }
        public List<Answer> Answer { get; set; }
    }

    public class Answer
    {
        public string Category { get; set; }
        public string Response { get; set; }
        public List<Source> Source { get; set; }
    }

    public class Source
    {
        public string Filename { get; set; }
        public List<string> Timestamps { get; set; }
    }

}
