using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUser
    {
        List<Users> GetUserList();
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
