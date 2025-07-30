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
        Task UpdateSessionAsync(int? userId, string sessionKey, string ip, string agent);
        List<UserSession> GetActiveSessions();
        UserStatsDto GetUserStats();
        ResponseTimeStatsDto GetAverageResponseTime();
        List<UserChatbotStatsDto> GetUserChatbotStats();
        Task<List<QueryTopicDistributionDto>> GetQueryTopicDistributionAsync();
        Task<List<QueryStatusDistribution>> GetQueryStatusDistributionAsync();
        Task<List<UserTypeDistribution>> GetUserTypeDistributionAsync();
        Task<AverageMetricsDto> GetAverageMetricsAsync();
        Task<List<AdminLoginLog>> GetAdminLogsAndStatusAsync();
    }
}
