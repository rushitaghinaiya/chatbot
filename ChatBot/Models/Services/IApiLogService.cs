using ChatBot.Models.Entities;

namespace ChatBot.Models.Services
{
    public interface IApiLogService
    {
        Task LogAsync(ApiLog log);
    }
}
