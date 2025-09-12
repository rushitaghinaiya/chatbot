using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUserMgmtService
    {
        Task<FreeUsersOverviewDto> GetFreeUsersOverviewAsync();
        Task<PaidUsersOverviewDto> GetPaidUsersOverviewAsync();
        Task<List<FreeUserQueryTypeDto>> GetFreeUserQueryTypesAsync();
        Task<List<UserDetail>> GetFreeUserDetailsAsync();
        Task<List<UserDetail>> GetPaidUserDetailsAsync();
        Task<List<CommunicationSetting>> GetAllSettingsAsync();
        Task<int> UpdateSettingAsync(CommunicationSetting setting);
    }
}
