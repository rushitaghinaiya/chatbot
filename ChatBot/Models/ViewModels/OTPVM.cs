using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.ViewModels
{
    public class OTPVM
    {
        public int OTPId { get; set; }
        public int UserId { get; set; }
        public DateTime OtpTime { get; set; }
        //public string UserName { get; set; } = string.Empty;
        public string OtpNumber { get; set; } = string.Empty;
        public string OtpType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

    }

    public class ResendOtpVM { 
        public int UserId { get; set; }
        public string CreatedAt { get; set; }
    }
}
