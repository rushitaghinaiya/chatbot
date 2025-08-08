using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IQuestion
    {
        List<Question> GetQuestionGroup();
        List<Question> GetQuestionsById(int id);
        List<Question> GetQuestionsList();
    }
}
