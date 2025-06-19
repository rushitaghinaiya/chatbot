using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUserSignUp
    {
        int SaveUser(Users users);
    }
}
