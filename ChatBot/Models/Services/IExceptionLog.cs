using ChatBot.Models.Entities;

namespace ChatBot.Models.Services
{
    public interface IExceptionLog
    {
       void Log(ExceptionLog log);
    }
}
