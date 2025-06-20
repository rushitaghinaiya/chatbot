using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IUser
    {
        List<Users> GetUserList();
    }
}
