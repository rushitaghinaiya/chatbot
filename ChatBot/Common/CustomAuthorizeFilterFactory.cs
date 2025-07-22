using ChatBot.Middleware;
using ChatBot.Models.Common;
using ChatBot.Models.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ChatBot.Common
{
    public class CustomAuthorizeFilterFactory : IFilterFactory
    {
        public IUser _userService;
        public IConfiguration _configuration;
        private IOptions<AppSettings> _appSetting;
        private IHttpContextAccessor _contextAccessor;

        public CustomAuthorizeFilterFactory(IUser userService, IHttpContextAccessor contextAccessor, IOptions<AppSettings> appSettings, IConfiguration configuration)
        {
            _appSetting = appSettings;
            _contextAccessor = contextAccessor;
            _userService = userService;
            _configuration = configuration;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new AuthorizeAttribute(_userService, _contextAccessor, _appSetting, _configuration);
        }

        public bool IsReusable => false;
    }

}
