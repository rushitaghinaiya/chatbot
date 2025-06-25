using ChatBot.Models.Common;
using ChatBot.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatBot.Controllers
{
    public class UserController : Controller
    {
        private AppSettings _appSetting;
        private readonly IUser _user;
        public UserController(IUser user, IOptions<AppSettings> appSettings)
        {
            _appSetting = appSettings.Value;
            _user = user;
        }

        /// <summary>
        /// Retrieves the list of all users.
        /// </summary>
        /// <returns>
        /// An IActionResult containing the list of users.
        /// </returns>
        [HttpGet]
        public IActionResult GetUserList()
        {
            var userList = _user.GetUserList();
            return Ok(userList);
        }
    }
}
