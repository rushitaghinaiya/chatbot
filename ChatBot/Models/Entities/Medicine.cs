namespace ChatBot.Models.Entities
{
    public class Medicine
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double? Price { get; set; }
        public bool? Is_discontinued { get; set; }
        public string? Manufacturer_name { get; set; }
        public string? Type { get; set; }
        public string? Pack_size_label { get; set; }
        public string? Short_composition1 { get; set; }
        public string? Short_composition2 { get; set; }
        public string? Salt_composition { get; set; }
        public string? Medicine_desc { get; set; }
        public string? Side_effects { get; set; }
        public string? Drug_interactions { get; set; }
    }
}
