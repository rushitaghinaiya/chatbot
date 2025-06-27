namespace ChatBot.Models.ViewModels
{
    public class MedicineSearchVM
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double? Price { get; set; }
        public bool? IsDiscontinued { get; set; }
        public string? ManufacturerName { get; set; }
        public string? Type { get; set; }
        public string? PackSizeLabel { get; set; }
        public string? ShortComposition1 { get; set; }
        public string? ShortComposition2 { get; set; }
        public string? SaltComposition { get; set; }
        public string? MedicineDescription { get; set; }
        public string? SideEffects { get; set; }
        public string? DrugInteractions { get; set; }
    }
    public class SearchResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
    }
}
