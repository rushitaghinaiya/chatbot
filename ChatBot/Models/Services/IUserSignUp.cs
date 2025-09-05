using ChatBot.Models.ViewModels;
using Model.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUserSignUp
    {
        int SaveUser(Users users);
        Users IsExistUser(string number);
        (bool exists, UserDetailsExcel user) VerifyEmail(string email);
        Task<int> SaveOTP(OTPVM otpVM);
        OTPVM GetOTP(OTPVM otpVM);
        int SaveLoginLog(LoginLogVM loginLog);
        int SaveAdminLoginLog(AdminLoginLog loginLog);
    }
}
