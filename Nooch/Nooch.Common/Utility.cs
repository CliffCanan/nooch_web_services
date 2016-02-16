﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nooch.Common
{
    public static class Utility
    {

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
        public static bool SendEmail(string templateName, MailPriority priority, string fromAddress, string toAddress, string attachmentPath, string subject, string referenceLink, IEnumerable<KeyValuePair<string, string>> replacements, string ccMailId, string bccMailId, string bodyText)
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
                Logger.Error("UtilityDataAccess -> SendEmail ERROR -> [Template: " + templateName + "], " +
                                       "[ToAddress: " + toAddress + "],  [Exception: " + ex + "]");
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


    }
}
