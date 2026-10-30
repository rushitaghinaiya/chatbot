using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Net.Http.Headers;
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
        // Shared HttpClient for MSG91 calls (EmailSender is created per controller, so avoid a new HttpClient per email)
        private static readonly HttpClient _msg91HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private const string Msg91EmailSendUrl = "https://control.msg91.com/api/v5/email/send";
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
                 .Replace("{{name}}", string.IsNullOrEmpty(userDetailVM.FirstName)?"Admin": userDetailVM.FirstName)
                 .Replace("{{otp}}", otpNumber));
                sr.Close();
                sb = ReplaceProductInfo(sb);
                if (_appSettings.IsMsg91)
                {
                    // MSG91 renders its own stored template (Msg91OtpTemplateId); we only pass the variables
                    var variables = new Dictionary<string, string>
                    {
                        { "name", string.IsNullOrEmpty(userDetailVM.FirstName) ? "Admin" : userDetailVM.FirstName },
                        { "otp", otpNumber },
                        { "product_name", ProductName }
                    };
                    await SendEmailViaMsg91(userDetailVM.LoginEmail, userDetailVM.FirstName, _appSettings.Msg91OtpTemplateId.ReturnString(), variables);
                }
                else if (_appSettings.IsMailGun)
                {
                    var res = SendEmailViaMailgun(userDetailVM.LoginEmail, (string.IsNullOrEmpty(_appSettings.SendOtpEmailSubject.ReturnString()) ? "Email OTP Verification" : _appSettings.SendOtpEmailSubject.ReturnString()), sb.ToString());
                    // RestSharp does not throw on HTTP errors, so check the result explicitly
                    if (!res.IsSuccessful)
                        throw new InvalidOperationException($"Mailgun send failed ({(int)res.StatusCode}): {res.ErrorMessage ?? res.Content}");
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
            sb.Replace("{{product_name}}", _appSettings.ProductName.ReturnString());
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

        /// <summary>
        /// Sends an email through MSG91 (https://control.msg91.com/api/v5/email/send) using a template stored in MSG91.
        /// The template's {{variable}} placeholders are filled from <paramref name="variables"/>.
        /// </summary>
        /// <returns>The MSG91 response body.</returns>
        private async Task<string> SendEmailViaMsg91(string toEmail, string? toName, string templateId, Dictionary<string, string> variables,
            string cc = "", string bcc = "")
        {
            var authKey = _appSettings.Msg91AuthKey.ReturnString();
            if (string.IsNullOrEmpty(authKey))
                throw new InvalidOperationException("MSG91 auth key is not configured (ApplicationSettings:Msg91AuthKey).");
            if (string.IsNullOrEmpty(templateId))
                throw new InvalidOperationException("MSG91 template id is not configured.");

            var recipient = new Dictionary<string, object>
            {
                { "to", new[] { new { name = string.IsNullOrEmpty(toName) ? toEmail : toName, email = toEmail } } },
                { "variables", variables }
            };
            if (!string.IsNullOrEmpty(cc))
                recipient["cc"] = new[] { new { name = cc, email = cc } };
            if (!string.IsNullOrEmpty(bcc))
                recipient["bcc"] = new[] { new { name = bcc, email = bcc } };

            var payload = new
            {
                recipients = new[] { recipient },
                from = new
                {
                    name = string.IsNullOrEmpty(_appSettings.Msg91FromName.ReturnString()) ? _appSettings.SenderName.ReturnString() : _appSettings.Msg91FromName.ReturnString(),
                    email = string.IsNullOrEmpty(_appSettings.Msg91FromEmail.ReturnString()) ? _appSettings.SMTPFromAddress.ReturnString() : _appSettings.Msg91FromEmail.ReturnString()
                },
                domain = _appSettings.Msg91Domain.ReturnString(),
                template_id = templateId
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, Msg91EmailSendUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("authkey", authKey);

            using var response = await _msg91HttpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"MSG91 email send failed ({(int)response.StatusCode}): {body}");

            return body;
        }



       
        #endregion
    }
}
