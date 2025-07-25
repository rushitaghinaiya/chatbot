namespace ChatBot.Models.ViewModels
{
    public class SaveQueryHistoryRequest
    {
        public int UserId { get; set; }
        public List<Message> QueryHistory { get; set; }
    }
}
