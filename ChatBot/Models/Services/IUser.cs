using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUser
    {
        List<Users> GetUserList();
        List<RefreshToken> GetRefreshTokenByUserId(int userId);
        int SaveRefreshToken(RefreshToken refreshToken);
        bool UpdateRefreshToken(RefreshToken refreshToken);
        Users GetUserByRefreshToken(string token);
        RefreshToken GetRefreshByRefreshToken(string token);
    }
}
