using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface ISetting
    {
         Task<List<Language>> GetLanguage();

        SecuritySettings? GetSecuritySettings();
        bool UpdateSecuritySettings(SecuritySettings model);
        VoiceAccessibilitySettings? GetVoiceSettings();
        bool UpdateVoiceSettings(VoiceAccessibilitySettings model);
        SystemLimits? GetSystemLimits();
        bool UpdateSystemLimits(SystemLimits model);
    }
}
