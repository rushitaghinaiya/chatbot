using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRMDBCommon2023.UserAgent
{
    public class UserAgentParser : IUserAgentService
    {
        private string _userAgent;

        private ClientBrowser _browser;
        public ClientBrowser Browser
        {
            get
            {
                if (_browser == null)
                {
                    _browser = new ClientBrowser(_userAgent);
                }
                return _browser;
            }
        }

        private ClientOS _os;
        public ClientOS OS
        {
            get
            {
                if (_os == null)
                {
                    _os = new ClientOS(_userAgent);
                }
                return _os;
            }
        }

        public string ClientInfo
        {
            get
            {
                if (_browser == null)
                {
                    _browser = new ClientBrowser(_userAgent);
                }

                if (_os == null)
                {
                    _os = new ClientOS(_userAgent);
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("Browser: ");
                sb.Append(_browser.Name.ReturnString());
                sb.Append("-" + _browser.Version.ReturnString());
                sb.Append(" OS: " + _os.Name.ReturnString());
                sb.Append("-" + _os.Version.ReturnString());

                return sb.ReturnString();
            }
        }

        public UserAgentParser(string userAgent)
        {
            _userAgent = userAgent;
        }
        public UserAgentParser()
        {

        }

        public string GetClientInfo(string userAgent)
        {
            if (_browser == null)
            {
                _browser = new ClientBrowser(userAgent);
            }

            if (_os == null)
            {
                _os = new ClientOS(userAgent);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Browser: ");
            sb.Append(_browser.Name.ReturnString());
            sb.Append("-" + _browser.Version.ReturnString());
            sb.Append(" OS: " + _os.Name.ReturnString());
            sb.Append("-" + _os.Version.ReturnString());

            return sb.ReturnString();
        }
    }
}
