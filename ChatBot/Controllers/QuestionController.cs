using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("chatbot/v1/[controller]/[action]")]
    public class QuestionController : Controller
    {
        private AppSettings _appSetting;
        private readonly IQuestion _question;
        public QuestionController(IQuestion question, IOptions<AppSettings> appSettings)
        {
            _appSetting = appSettings.Value;
            _question = question;
        }

        /// <summary>
        /// Retrieves all question groups.
        /// </summary>
        /// <returns>A list of question groups.</returns>
        [HttpGet]
        public IActionResult GetQuestionGroup()
        {
            var questions = _question.GetQuestionGroup();
            foreach (var questionsItem in questions)
            {
                var questionList = _question.GetQuestionsById(questionsItem.Id);
                questionsItem.SubQuestion = questionList.Select(a => a.Text).ToList();
                
            }
            return Ok(new { responseData = questions, status = "Success", isSuccess = true });
        }

        /// <summary>
        /// Retrieves questions by group id.
        /// </summary>
        /// <param name="id">The group id to filter questions.</param>
        /// <returns>A list of questions for the specified group id.</returns>
        [HttpGet]
        public IActionResult GetQuestions(int id)
        {
            var questions = _question.GetQuestionsById(id);
            return Ok(questions);
        }

    }
}
