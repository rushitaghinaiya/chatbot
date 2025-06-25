using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace VRMDBCommon2023
{
    public class SendSms
    {
        private string _apiKey = "";
        private string _toSms = "";
        private string _from = "";
        private string _templateName = "";
        private string _var1 = "";
        private string _var2 = "";
        private string _var3 = "";
        private string _var4 = "";
        private string _var5 = "";
        public string Apikey
        {
            get
            {
                return _apiKey;
            }

            set
            {
                _apiKey = value;
            }
        }
        public string ToSms
        {
            get
            {
                return _toSms;
            }

            set
            {
                _toSms = value;
            }
        }
        public string From
        {
            get
            {
                return _from;
            }

            set
            {
                _from = value;
            }
        }
        public string TemplateName
        {
            get
            {
                return _templateName;
            }

            set
            {
                _templateName = value;
            }
        }
        public string Var1
        {
            get
            {
                return _var1;
            }

            set
            {
                _var1 = value;
            }
        }
        public string Var2
        {
            get
            {
                return _var2;
            }

            set
            {
                _var2 = value;
            }
        }
        public string Var3
        {
            get
            {
                return _var3;
            }

            set
            {
                _var3 = value;
            }
        }
        public string Var4
        {
            get
            {
                return _var4;
            }

            set
            {
                _var4 = value;
            }
        }
        public string Var5
        {
            get
            {
                return _var5;
            }

            set
            {
                _var5 = value;
            }
        }

        #region Methods

        public string SendMessage()
        {
            try
            {

                //https://2factor.in/API/R1/?module=TRANS_SMS&apikey=2296c400-bd4f-11ea-9fa5-0200cd936042&to=7276473961&from=MediBK&templatename=MobileNoVerification&var1=VAR1_VALUE&var2=VAR2_VALUE
                String url = "https://2factor.in/API/R1/?";
                HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
                ASCIIEncoding encoding = new ASCIIEncoding();

                string template_name = HttpUtility.UrlEncode(_templateName);
                string var1 = null;
                string var2 = null;
                string var3 = null;
                string var4 = null;
                string var5 = null;
                if (!string.IsNullOrEmpty(_var1))
                {
                    var1 = HttpUtility.UrlEncode(_var1);
                }

                if (!string.IsNullOrEmpty(_var2))
                {
                    var2 = HttpUtility.UrlEncode(_var2);
                }

                if (!string.IsNullOrEmpty(_var3))
                {
                    var3 = HttpUtility.UrlEncode(_var3);
                }
                if (!string.IsNullOrEmpty(_var4))
                {
                    var4 = HttpUtility.UrlEncode(_var4);
                }
                if (!string.IsNullOrEmpty(_var5))
                {
                    var5 = HttpUtility.UrlEncode(_var5);
                }

                string postData = "module=TRANS_SMS";
                postData += "&apikey=" + _apiKey;
                postData += "&to=" + _toSms;
                postData += "&from=" + _from;
                postData += "&templatename=" + template_name;
                postData += "&var1=" + var1;
                postData += "&var2=" + var2;
                if (!string.IsNullOrEmpty(var3))
                {
                    postData += "&var3=" + var3;
                }
                if (!string.IsNullOrEmpty(var4))
                {
                    postData += "&var4=" + var4;
                }
                if (!string.IsNullOrEmpty(var5))
                {
                    postData += "&var5=" + var5;
                }

                byte[] data = encoding.GetBytes(postData);

                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/x-www-form-urlencoded";
                httpWReq.ContentLength = data.Length;
                using (Stream stream = httpWReq.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();


                return (new StreamReader(response.GetResponseStream()).ReadToEnd());
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
