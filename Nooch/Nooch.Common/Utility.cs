using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Nooch.Common.Entities;
using System.ServiceModel;
using System.ServiceModel.Web;


namespace Nooch.Common
{
    public static class Utility
    {

        public static IFormatProvider GetFormatProvider
        {
            get
            {
                const IFormatProvider iFormatProvider = null;
                return iFormatProvider;
            }
        }
        public static string GetValueFromConfig(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static bool IsInteger(string theValue)
        {
            var _isNumber = new Regex(@"^\d+$");
            Match m = _isNumber.Match(theValue);
            return m.Success;
        }
        public static CultureInfo GetCultureInfo
        {
            get
            {
                CultureInfo cultureInfo = CultureInfo.CurrentCulture;
                return cultureInfo;
            }
        }
        public static Object GetPropValue(String name, Object currentItem)
        {
            var parts = name.Split('.');
            for (int index = 0; index < parts.Length; index++)
            {
                if (currentItem == null)
                {
                    return null;
                }

                Type type = currentItem.GetType();
                PropertyInfo info = type.GetProperty(parts[index]);
                if (info == null)
                {
                    return null;
                }
                if (type == typeof(string))
                {
                    return currentItem;
                }

                currentItem = GetCurrentItem(name, index, parts, currentItem, type, info);
            }
            return currentItem;
        }
        private static object GetCurrentItem(string name, int index, string[] parts, object currentItem, Type type, PropertyInfo info)
        {
            currentItem = info.GetValue(currentItem, null);

            //Code to remove Time in DateTime column
            if (type.GetProperty(parts[index]).PropertyType == typeof(DateTime?))
            {
                currentItem = ((DateTime?)(currentItem)).HasValue ? ((DateTime?)(currentItem)).Value.ToShortDateString() : string.Empty;
            }
            else if (type.GetProperty(parts[index]).PropertyType == typeof(DateTime))
            {
                currentItem = ((DateTime)(currentItem)).ToShortDateString();
            }
            else if ((info.PropertyType).BaseType.IsGenericType)
            {
                // iterate through the collection and get the Corresponding column Name
                var items = currentItem as System.Collections.ICollection;
                string value = string.Empty;
                foreach (var item in items)
                {
                    var tempItem = GetPropValue(name.Replace(parts[index] + ".", ""), item);
                    value = (tempItem == null ? string.Empty : (tempItem + ",")) + value;

                }
                if (!string.IsNullOrEmpty(value))
                {
                    value = value.Substring(0, value.Length - 1);
                }
                currentItem = value;

            }
            return currentItem;
        }

        public static bool ToBoolean(string value)
        {
            string strValue = value.ToLower(GetCultureInfo);

            if (strValue.Equals("1") || strValue.Equals("true") || strValue.Equals("t") || strValue.Equals("y") || strValue.Equals("on") || strValue.Equals("yes") || strValue.Equals("ok"))
            {
                return true;
            }
            return false;
        }


        public static double ConvertToDouble(string value)
        {
            double result;
            try
            {
                result = Double.Parse(value);
            }
            catch (FormatException exception)
            {
                throw new FormatException(exception.Message + "Unable to format string :" + value);
            }
            return result;

        }


        public static decimal ConvertToDecimal(string value)
        {
            decimal result;
            try
            {
                result = Decimal.Parse(value);
            }
            catch (FormatException exception)
            {
                throw new FormatException(exception.Message + "Unable to format string :" + value);
            }
            return result;

        }

        public static Guid ConvertToGuid(string value)
        {
            var id = new Guid();
            try
            {
                if (!String.IsNullOrEmpty(value) && value != Guid.Empty.ToString())
                {
                    id = new Guid(value);
                }
            }
            catch (Exception exception)
            {
                //Logger.LogErrorMessage(exception.Message + "Unable to format string :" + value);
                throw;
            }
            return id;
        }


        public static string TrimCommaString(string selectedValues)
        {
            if (selectedValues.EndsWith(",", StringComparison.CurrentCulture))
            {
                selectedValues = selectedValues.Substring(0, selectedValues.Length - 1);
            }
            return selectedValues;
        }

        public static void LogInnerException(Exception ex)
        {
            Exception innerException = ex.InnerException;
            if (innerException != null)
            {
                Logger.Error(innerException.Message + Environment.NewLine);
                LogInnerException(innerException);
            }
        }
        public static string UploadPhoto(string folderPath, string fileName, string fileExtension, string fileContent, int contentLength)
        {
            //Used to create folder if its not exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileStream = new FileStream(Path.Combine(folderPath, fileName + fileExtension), FileMode.Create);
            var byteContent = System.Convert.FromBase64String(fileContent);
            fileStream.Write(byteContent, 0, contentLength);
            fileStream.Close();

            return String.Concat(folderPath, fileName, fileExtension);
        }


        public static string GetRandomPinNumber()
        {
            const string chars = "0123456789";
            var random = new Random();
            var randomId = new string(
                Enumerable.Repeat(chars, 4)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            return randomId;
        }


        public static string GetEmailTemplate(string physicalPath)
        {
            using (var sr = new StreamReader(physicalPath))
                return sr.ReadToEnd();
        }


        public static bool SendEmail(string templateName, string fromAddress, string toAddress, string attachmentPath, string subject, string referenceLink, IEnumerable<KeyValuePair<string, string>> replacements, string ccMailId, string bccMailId, string bodyText)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();

                string template;
                string subjectString = subject;
                string content = string.Empty;

                if (!String.IsNullOrEmpty(templateName))
                {
                    template = GetEmailTemplate(String.Concat(GetValueFromConfig("EmailTemplatesPath"), templateName, ".txt"));
                    content = template;

                    // Replace tokens in the message body and subject line
                    if (replacements != null)
                    {
                        foreach (var token in replacements)
                        {
                            content = content.Replace(token.Key, token.Value);
                            subjectString = subject.Replace(token.Key, token.Value);
                        }
                    }
                    mailMessage.Body = content;
                }
                else
                {
                    mailMessage.Body = bodyText;
                }

                switch (fromAddress)
                {
                    case "receipts@nooch.com":
                        mailMessage.From = new MailAddress(fromAddress, "Nooch Payments");
                        break;
                    case "support@nooch.com":
                        mailMessage.From = new MailAddress(fromAddress, "Nooch Support");
                        break;
                    case "hello@nooch.com":
                        mailMessage.From = new MailAddress(fromAddress, "Team Nooch");
                        break;
                    case "landlords@rentscene.com":
                        mailMessage.From = new MailAddress(fromAddress, "Rent Scene");
                        break;
                    case "team@rentscene.com":
                        mailMessage.From = new MailAddress(fromAddress, "Rent Scene Team");
                        break;
                    case "payments@rentscene.com":
                        mailMessage.From = new MailAddress(fromAddress, "Rent Scene Payments");
                        break;
                    default:
                        mailMessage.From = new MailAddress(fromAddress, "Nooch Admin");
                        break;
                }
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subjectString;

                if (Convert.ToBoolean(GetValueFromConfig("IsRunningOnSandBox")))
                    toAddress = GetValueFromConfig("SandboxEmailsRecepientEmail");

                mailMessage.To.Add(toAddress);

                if (!String.IsNullOrEmpty(bccMailId))
                {
                    mailMessage.Bcc.Add(bccMailId);
                }

                SmtpClient smtpClient = new SmtpClient();

                smtpClient.Host = GetValueFromConfig("SMTPAddress");
                smtpClient.UseDefaultCredentials = false;

                smtpClient.Credentials = new NetworkCredential(GetValueFromConfig("SMTPLogOn"), GetValueFromConfig("SMTPPassword"));
                smtpClient.Send(mailMessage);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("UtilityDataAccess -> SendEmail ERROR -> Template: [" + templateName + "], " +
                             "ToAddress: [" + toAddress + "], FromAddress: [" + fromAddress + "], Exception: [" + ex.Message + "]");
                return false;
            }
        }

        public static string SendSMS(string phoneto, string msg)
        {
            string AccountSid = GetValueFromConfig("AccountSid");
            string AuthToken = GetValueFromConfig("AuthToken");
            string from = GetValueFromConfig("AccountPhone");
            string to = "";

            if (!phoneto.Trim().Contains('+'))
                to = GetValueFromConfig("SMSInternationalCode") + phoneto.Trim();
            else
                to = phoneto.Trim();

            var client = new Twilio.TwilioRestClient(AccountSid, AuthToken);
            var sms = client.SendSmsMessage(from, to, msg);
            return sms.Status;
        }


        public static string SendNotificationMessage(string alertText, int badge, string sound, string devicetokens, string username, string password)
        {
            // Sample JSON Input...
            // string json = "{\"aps\":{\"badge\":356,\"alert\":\"this 4 rd post\"},\"device_tokens\":[\"DC59F629CBAF8D88418C9FCD813F240B72311C6EDF27FAED0F5CB4ADB9F4D3C9\"]}";
            try
            {
                string json = new JavaScriptSerializer().Serialize(new
                {
                    app_id = username,
                    isIos = true,
                    include_ios_tokens = new string[] { devicetokens },
                    contents = new GameThriveMsgContent() { en = alertText }
                });

                var cli = new WebClient();
                cli.Headers[HttpRequestHeader.ContentType] = "application/json";

                string response = cli.UploadString("https://gamethrive.com/api/v1/notifications", json);
                GameThriveResponseClass gamethriveresponse = JsonConvert.DeserializeObject<GameThriveResponseClass>(response);

                return "1";
            }
            catch (Exception ex)
            {
                Logger.Info("Utility -> SendNotificationMessage (For SMS) FAILED - Exception: [" + ex.Message + "]");
                return ex.Message;
            }
        }


        public static bool SendEmailCSV(string toAddress, string attachmentCSVString, string subject, string bodyText, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();

                string subjectString = subject;
                string content = string.Empty;
                string template;

                template = GetEmailTemplate(String.Concat(Utility.GetValueFromConfig("EmailTemplatesPath"), "TransactionHistoryTemplate", ".txt"));
                content = replacements.Aggregate(template, (current, token) => current.Replace(token.Key, token.Value));

                mailMessage.Body = content;

                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subjectString;
                //mailMessage.To.Add(new MailAddress(toAddress, "reports@nooch.com"));

                mailMessage.To.Add(new MailAddress(toAddress));
                mailMessage.From = (new MailAddress("reports@nooch.com"));


                using (MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(attachmentCSVString)))
                {
                    Attachment attachment = new Attachment(stream, new ContentType("text/csv"));
                    attachment.Name = "MyNoochTransactions.csv";
                    mailMessage.Attachments.Add(attachment);
                    SmtpClient smtpClient = new SmtpClient();
                    smtpClient.Host = GetValueFromConfig("SMTPAddressForCSVAttachment");
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(GetValueFromConfig("SMTPLogOnForCSVAttachment"), GetValueFromConfig("SMTPPasswordForCSVAttachment"));
                    smtpClient.Send(mailMessage);
                    return true;

                }

            }
            catch (SmtpException smtpException)
            {
                Logger.Error("Utility Data Access - SendEmail: smtpException [" + smtpException + "]");
                Logger.Error(smtpException.StackTrace);
                return false;
            }
            catch (Exception exception)
            {
                Logger.Error("Utility Data Access - SendEmail: Not an SMTP exception [" + exception + "]");
                Logger.Error(exception.StackTrace);
                throw exception;
            }
        }


        /// <summary>
        /// Used to log and throw exception
        /// </summary>
        /// <param name="exception">Exception raised at runtime</param>
        public static void ThrowFaultException(Exception exception)
        {
            Logger.Error(String.Format(Utility.GetFormatProvider, "Exception: [{0}], Message: [{1}], StackTrace: {2}", exception.GetType().FullName, exception.Message, exception.StackTrace.Replace(Environment.NewLine, string.Empty)));

            Utility.LogInnerException(exception);

            if (WebOperationContext.Current != null)
            {
                var outResponse = WebOperationContext.Current.OutgoingResponse;
                outResponse.StatusCode = HttpStatusCode.InternalServerError;
                outResponse.StatusDescription = exception.Message;

                Logger.Info("Status Code: [ " + outResponse.StatusCode + "], Status Description: [" + outResponse.StatusDescription + "]");

                throw new WebException(exception.Message);
            }
        }
        /// <summary>
        /// Used to log and throw exception with user message
        /// </summary>
        /// <param name="exception">Exception raised at runtime</param>
        /// <param name="errorMessage">User specified error message thrown to Web Project</param>

        public static void ThrowFaultException(Exception exception, string errorMessage)
        {
            Logger.Error(String.Format(Utility.GetFormatProvider, "Exception:{0} :{1} StackTrace:{2}", exception.GetType().FullName, exception.Message, exception.StackTrace.Replace(Environment.NewLine, string.Empty)));
            Utility.LogInnerException(exception);
            if (WebOperationContext.Current != null)
            {
                var outResponse = WebOperationContext.Current.OutgoingResponse;
                outResponse.StatusCode = HttpStatusCode.InternalServerError;
                outResponse.StatusDescription = errorMessage;
                Logger.Info("Status code:[ " + outResponse.StatusCode + "], Status description:[" + outResponse.StatusDescription + "].");
                throw new WebException(errorMessage);
            }
        }


      



    }
}
