using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUser
    {
        List<Users> GetUserList();
        Users GetUserById(int userId);
        Task UpdateSessionAsync(int? userId, string sessionKey, string ip, string agent);
        List<UserSession> GetActiveSessions();
        UserStatsDto GetUserStats();
        List<UserChatbotStatsDto> GetUserChatbotStats();
        Task<(int todayCount, int lastMonthCount, double percentageChange)> GetTodayQueryStatsAsync();
        Task<(double avgResponseTime, double lastMonthAvg, double percentageChange)> GetAverageResponseTimeAsync();
        Task<List<QueryTopicDistributionDto>> GetQueryTopicDistribution();
        Task<QueryStatusDistribution> GetQueryStatusDistributionAsync();
        Task<List<UserTypeDistribution>> GetUserTypeDistributionAsync();
        Task<AverageMetricsDto> GetAverageMetricsAsync();
        Task<SessionStatsDto> GetSessionStats();
        Task<List<AdminLoginLog>> GetAdminLogsAndStatusAsync();
        Task<bool> SaveUserSession(BotSession botSession);
        Task<bool> SaveQueryHistory(QueryHistoryDto dto);
        List<RefreshToken> GetRefreshTokenByUserId(int userId);
        int SaveRefreshToken(RefreshToken refreshToken);
        bool UpdateRefreshToken(RefreshToken refreshToken);
        Users GetUserByRefreshToken(string token);
        RefreshToken GetRefreshByRefreshToken(string token);
    
        //ResponseTimeStatsDto GetAverageResponseTime();
        
        //Task<List<QueryTopicDistributionDto>> GetQueryTopicDistributionAsync();
       
        
    }
}
