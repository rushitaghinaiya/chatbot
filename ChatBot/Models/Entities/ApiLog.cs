namespace ChatBot.Models.Entities
{
    public class ApiLog
    {
        public int Id { get; set; }
        public string ApiName { get; set; }
        public string UserId { get; set; }
        public DateTime RequestTime { get; set; }
        public string RequestMethod { get; set; }
        public string RequestHeaders { get; set; }
        public string RequestBody { get; set; }
        public string QueryString { get; set; }
    }
}
