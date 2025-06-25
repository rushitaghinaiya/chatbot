using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRMDBCommon2023.UserAgent
{
    public interface IUserAgentService
    {
        string GetClientInfo(string userAgent);
    }
}
