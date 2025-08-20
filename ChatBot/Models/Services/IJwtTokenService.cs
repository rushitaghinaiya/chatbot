using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IJwtTokenService
    {
        AuthenticationModel Authenticate(iCareUser users);
        string GenerateAccessToken(iCareUser user);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
        int? GetUserIdFromToken(string token);
        DateTime GetTokenExpiration(string token);
    }
}
