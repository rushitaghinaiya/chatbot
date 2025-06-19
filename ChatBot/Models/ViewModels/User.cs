namespace ChatBot.Models.ViewModels
{
    public class Users
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "user"; // Should be 'user', 'admin', or 'supervisor'

        public bool IsPremium { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
