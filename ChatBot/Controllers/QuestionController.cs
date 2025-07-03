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
        // This action returns all question groups with their sub-questions.
        // It uses the injected IQuestion service to fetch data.
        // Logging is performed at the start and end of the method.
        [HttpGet]
        public IActionResult GetQuestionGroup()
        {
            Console.WriteLine("GetQuestionGroup action started.");
            var questions = _question.GetQuestionGroup();
            foreach (var questionsItem in questions)
            {
                var questionList = _question.GetQuestionsById(questionsItem.Id);
                questionsItem.SubQuestion = questionList.Select(a => a.Text).ToList();
            }
            Console.WriteLine("GetQuestionGroup action completed.");
            return Ok(new { responseData = questions, status = "Success", isSuccess = true });
        }

        /// <summary>
        /// Retrieves questions by group id.
        /// </summary>
        /// <param name="id">The group id to filter questions.</param>
        /// <returns>A list of questions for the specified group id.</returns>
        // This action returns questions for a specific group id.
        // It uses the injected IQuestion service to fetch data.
        // Logging is performed at the start and end of the method.
        [HttpGet]
        public IActionResult GetQuestions(int id)
        {
            Console.WriteLine($"GetQuestions action started for group id: {id}.");
            var questions = _question.GetQuestionsById(id);
            Console.WriteLine("GetQuestions action completed.");
            return Ok(questions);
        }

    }
}
