namespace ChatBot.Models.ViewModels
{
    public class Question
    {
       
            public int Id { get; set; }
            public int GroupId { get; set; }
            public string Text { get; set; } = null!;
            public string Category { get; set; } = null!;
            public bool IsActive { get; set; } = true;
            public DateTime? CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public List<string> SubQuestion { get; set; }
        

    }
}
