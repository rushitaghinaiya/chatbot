using Google.Protobuf.WellKnownTypes;

namespace ChatBot.Models.ViewModels
{
    public class Message
    {
        public string Type { get; set; } // "bot" or "user"
        public string Text { get; set; }
        public List<Option>? Options { get; set; }  // optional
        public string Timestamp { get; set; }
        public string SenderName { get; set; }
        public string Topic { get; set; }
        public double? ResponseTime { get; set; }  // optional
    }
    public class Option
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

}
