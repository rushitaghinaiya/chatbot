using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("chatbot/v1/[controller]/[action]")]
    public class UserSignUpController : Controller
    {
        private readonly IUserSignUp _userSignUp;
        public UserSignUpController(IUserSignUp userSignUp)
        {
            _userSignUp = userSignUp;
        }
        [HttpPost]

        public IActionResult SignUp(Users users)
        {
            _userSignUp.SaveUser(users);
            return Ok();
        }
    }
}
