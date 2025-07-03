using ChatBot.Models.Common;
using ChatBot.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatBot.Controllers
{
    public class UserController : Controller
    {
        private readonly AppSettings _appSetting;
        private readonly IUser _user;
        private readonly ILogger<UserController> _logger;

        public UserController(IUser user, IOptions<AppSettings> appSettings, ILogger<UserController> logger)
        {
            _appSetting = appSettings.Value;
            _user = user;
            _logger = logger;
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
            _logger.LogInformation("GetUserList called.");
            var userList = _user.GetUserList();
            _logger.LogInformation("GetUserList returned {Count} users.", userList.Count);
            return Ok(userList);
        }
    }
}
