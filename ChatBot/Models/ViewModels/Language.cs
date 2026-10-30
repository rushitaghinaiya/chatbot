namespace ChatBot.Models.ViewModels
{
    public class Language
    {
        public int Id { get; set; }
        public string LanguageName { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public string icon { get; set; }
        public string Language_code { get; set; }
        public int IsActive { get; set; }
        
        public int updatedBy { get; set; }
        public DateTime updatedOn { get; set; }
    }
}
