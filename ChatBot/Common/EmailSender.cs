using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Net.Mail;
using System.Text;
using VRMDBCommon2023;

namespace API.Common
{
    public class EmailSender
    {
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;
        private readonly SmtpClient _smtpClient;
        //private readonly IMailParserservice _mailParserservice;

        public EmailSender(IOptions<AppSettings> appSettings
            //IMailParserservice mailParserservice
            )
        {
            _appSettings = appSettings.Value;
          //  _mailParserservice = mailParserservice;
        }

       

      
        public async Task SendOtpEmail(UserDetailsExcel userDetailVM, string otpNumber)
        {

            string htmlTemplate = Path.Combine(_appSettings.HtmlFolderPath, "SendOtp.html");
            string plainTextContent = string.Empty;
            string htmlContent = string.Empty;
            try
            {
                string ProductName = string.IsNullOrEmpty(_appSettings.ProductName.ReturnString()) ? "Icare Life" : _appSettings.ProductName.ReturnString();
                StringBuilder sb = new StringBuilder();
                StreamReader sr = new StreamReader(htmlTemplate);
                sb.Append(sr.ReadToEnd()
                 .Replace("@PatientName", string.IsNullOrEmpty(userDetailVM.FirstName)?"Admin": userDetailVM.FirstName)
                 .Replace("@ProductName", ProductName)
                 .Replace("@OtpNumber", otpNumber));
                sr.Close();
                sb = ReplaceProductInfo(sb);
                if (_appSettings.IsMailGun)
                {
                    var res = SendEmailViaMailgun(userDetailVM.LoginEmail, (string.IsNullOrEmpty(_appSettings.SendOtpEmailSubject.ReturnString()) ? "Email OTP Verification" : _appSettings.SendOtpEmailSubject.ReturnString()), sb.ToString());
                }
                else
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(_appSettings.SMTPFromAddress.ReturnString(), _appSettings.SenderName.ReturnString());
                    message.To.Add(new MailAddress(userDetailVM.LoginEmail));
                    message.Subject = string.IsNullOrEmpty(_appSettings.SendOtpEmailSubject.ReturnString()) ? "Email OTP Verification" : _appSettings.SendOtpEmailSubject.ReturnString();
                    message.IsBodyHtml = true;
                    message.Body = sb.ReturnString();
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(sb.ReturnString());
                    htmlView.ContentType = new System.Net.Mime.ContentType("text/html");
                    message.AlternateViews.Add(htmlView);
                    _smtpClient.Send(message);
                   // LogAutoEmail(message, "Send Email OTP", userDetailVM);
                }

                //Dispose();
            }
            catch (OperationCanceledException ex)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }


        #region Utility methods

        private StringBuilder ReplaceProductInfo(StringBuilder sb)
        {
            sb.Replace("@FacebookUrl", _appSettings.FacebookUrl.ReturnString())
             .Replace("@LinkedInUrl", _appSettings.LinkedInUrl.ReturnString())
             .Replace("@WordPressUrl", _appSettings.WordPressUrl.ReturnString())
            .Replace("@ProductName", _appSettings.ProductName.ReturnString());
            return sb;
        }

        private RestResponse SendEmailViaMailgun(string to, string subject, string body, string filename = "", string filepath = "", string cc = "", string bcc = "")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var clientOptions = new RestClientOptions("https://api.mailgun.net/v3")
            {
                Authenticator = new HttpBasicAuthenticator("api", _appSettings.MailGunApiKey.ReturnString())
            };

            var client = new RestSharp.RestClient(clientOptions);

            var domain = _appSettings.MailGunDomain.ReturnString();
            var request = new RestRequest($"{domain}/messages", RestSharp.Method.Post);

            request.AddParameter("from", $"{_appSettings.SenderName.ReturnString()} <{_appSettings.SMTPFromAddress.ReturnString()}>");
            request.AddParameter("to", to);

            if (!string.IsNullOrEmpty(cc))
                request.AddParameter("cc", cc);

            if (!string.IsNullOrEmpty(bcc))
                request.AddParameter("bcc", bcc);

            if (!string.IsNullOrEmpty(subject))
                request.AddParameter("subject", subject);

            if (!string.IsNullOrEmpty(body))
            {
                request.AddParameter("text", body);
                request.AddParameter("html", body);
            }

            if (!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(filepath))
            {
                request.AddFile("attachment", Path.Combine(filepath, filename));
            }

            request.AddParameter("o:require-tls", true);
            request.AddParameter("o:skip-verification", true);

            return client.Execute(request);
        }

       
        #endregion
    }
}
