using ChatBot.Controllers;

namespace ChatBot.Models.Entities
{
    public class MedlinePlusResponse
    {
        public List<MedlinePlusDocument>? Documents { get; set; }
        public int TotalResults { get; set; }
        public string Term { get; set; } = string.Empty;
        public int RetStart { get; set; }
        public int RetMax { get; set; }
        public DateTime Timestamp { get; set; }
        public string DataSource { get; set; } = string.Empty;  
    }
}
