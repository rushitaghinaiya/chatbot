using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUserMgmtService
    {
        Task<FreeUsersOverviewDto> GetFreeUsersOverviewAsync();
        Task<List<FreeUserQueryTypeDto>> GetFreeUserQueryTypesAsync();
        Task<List<FreeUserDetail>> GetFreeUserDetailsAsync();
        Task<List<CommunicationSetting>> GetAllSettingsAsync();
        Task<int> UpdateSettingAsync(CommunicationSetting setting);
    }
}
