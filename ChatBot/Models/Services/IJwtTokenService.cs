using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IJwtTokenService
    {
        AuthenticationModel Authenticate(Users users);
        string GenerateAccessToken(Users user);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
        int? GetUserIdFromToken(string token);
        DateTime GetTokenExpiration(string token);
    }
}
