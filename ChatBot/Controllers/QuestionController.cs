using ChatBot.Models.Common;
using ChatBot.Models.Services;
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
        [HttpGet]
        public IActionResult GetQuestionGroup()
        {
            var questions = _question.GetQuestionGroup();
            return Ok(questions);
        }

        [HttpGet]
        public IActionResult GetQuestions(int id)
        {
            var questions = _question.GetQuestionsById(id);
            return Ok(questions);
        }

    }
}
