namespace ChatBot.Models.Entities
{
    public class MedlinePlusDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public List<string> AltTitles { get; set; } = new();
        public List<string> GroupNames { get; set; } = new();
        public List<string> MeshTerms { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public string Source { get; set; } = string.Empty;
        public int Rank { get; set; }
    }
}
