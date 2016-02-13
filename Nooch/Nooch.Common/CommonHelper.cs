﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Resources;
using Nooch.Data;

namespace Nooch.Common
{
    public static class CommonHelper
    {
        private static NOOCHEntities _dbContext = new NOOCHEntities();
        public static string GetEncryptedData(string sourceData)
        {
            try
            {
                var aesAlgorithm = new AES();
                string encryptedData = aesAlgorithm.Encrypt(sourceData, string.Empty);
                return encryptedData.Replace(" ", "+");
            }
            catch (Exception ex)
            {
                Logger.Error("CommonHelper -> GetEncryptedData FAILED - [SourceData: " + sourceData + "],  [Exception: " + ex + "]");
            }
            return string.Empty;
        }

        public static string GetDecryptedData(string sourceData)
        {
            try
            {
                var aesAlgorithm = new AES();
                string decryptedData = aesAlgorithm.Decrypt(sourceData.Replace(" ", "+"), string.Empty);
                return decryptedData;
            }
            catch (Exception ex)
            {
                Logger.Error("CommonHelper -> GetDecryptedData FAILED - [SourceData: " + sourceData + "],  [Exception: " + ex + "]");
            }
            return string.Empty;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string FormatPhoneNumber(string sourcePhone)
        {
            sourcePhone.Trim();
            if (String.IsNullOrEmpty(sourcePhone) || sourcePhone.Length != 10)
            {
                return sourcePhone;
            }
            sourcePhone = "(" + sourcePhone;
            sourcePhone = sourcePhone.Insert(4, ")");
            sourcePhone = sourcePhone.Insert(5, " ");
            sourcePhone = sourcePhone.Insert(9, "-");
            return sourcePhone;
        }

        public static string RemovePhoneNumberFormatting(string sourceNum)
        {
            sourceNum.Trim();

            if (!String.IsNullOrEmpty(sourceNum))
            {
                // removing extra stuff from phone number
                sourceNum = sourceNum.Replace("(", "");
                sourceNum = sourceNum.Replace(")", "");
                sourceNum = sourceNum.Replace(" ", "");
                sourceNum = sourceNum.Replace("-", "");
                sourceNum = sourceNum.Replace("+", "");
            }
            else
            {
                Logger.Info("CommonHelper -> RemovePhoneNumberFormatting Source String was NULL or EMPTY - [SourceData: " + sourceNum + "]");
            }
            return sourceNum;
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }





        public static bool IsValidRequest(string accessToken, string memberId)
        {
            if (!string.IsNullOrEmpty(accessToken) || !string.IsNullOrEmpty(memberId))
            {
                Guid memGuid = new Guid(memberId);
                accessToken = accessToken.Replace(' ', '+');


                try
                {
                    //Get the member details

                    var noochMember = _dbContext.Members.FirstOrDefault(m => m.AccessToken == accessToken && m.IsDeleted == false && m.MemberId == memGuid);
                    if (noochMember != null)
                    {
                        return true;
                    }
                    else
                    {
                        #region codeforemailandsms
                        //Code to send sms and email when logged out
                        /*
                        MemberDataAccess mdA = new MemberDataAccess();
                        Members member = mdA.GetMemberDetails(memberId);
                        //This code was commented as it was sending email and sms twice to the user.
                        StringResult PhoneNumber = GetPhoneNumberByMemberId(memberId);
                        if (PhoneNumber.Result != "")
                        {
                            string msg = "Hi\n You are automatically logged out from your device because you signed in into another device.\n - Team Nooch";

                            var fromAddress = Utility.GetValueFromConfig("adminMail");
                            var toAddress = CommonHelper.GetDecryptedData(member.UserName);
                            try
                            {
                                // email notification
                                UtilityDataAccess.SendEmail(null, MailPriority.High, fromAddress, toAddress, null, "Nooch automatic LogOut.", null, null, null, null, msg);
                            }
                            catch (Exception)
                            {
                                Logger.LogDebugMessage("InviteReminder - LogOut mail not sent to [" + toAddress + "]. Problem occured in sending mail.");
                            }
                            //sms notification
                            StringResult smsResult = ApiSMS(PhoneNumber.Result, msg, accessToken, memberId);
                        }*/
                        #endregion
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> IsValidRequest FAILED - [Exception: " + ex + "]");
                    return false;
                }

            }
            else
            {
                return false;
            }
        }


        public static MemberBusinessDto GetMemberByUdId(string udId)
        {
            Logger.Info("MemberDataAccess - GetMemberByUdId[ udId:" + udId + "].");

            var member = _dbContext.Members.FirstOrDefault(m => m.UDID1 == udId && m.IsDeleted == false);
            if (member != null)
            {
                if (member.Status == Constants.STATUS_REGISTERED)
                {
                    return new MemberBusinessDto { Status = "Your nooch account is inactive." };
                }
                return new MemberBusinessDto { UserName = GetDecryptedData(member.UserName) };
            }

            return new MemberBusinessDto
            {
                Status = "You are not a nooch member. Please register to become a nooch member."
            };
        }


        public static string GetMemberIdByUserName(string userName)
        {

            var userNameLowerCase = GetEncryptedData(userName.ToLower());
            userName = GetEncryptedData(userName);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.UserNameLowerCase == userNameLowerCase && m.UserName == userName && m.IsDeleted == false);

            if (noochMember != null)
            {
                return noochMember.MemberId.ToString();

            }
            return null;
        }

        public static string GetMemberUsernameByMemberId(string MemberId)
        {

            Guid memGuid = Utility.ConvertToGuid(MemberId);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.MemberId == memGuid && m.IsDeleted == false);

            return noochMember != null ? GetDecryptedData(noochMember.UserName) : null;
        }

        public static string GetMemberNameByUserName(string userName)
        {

            var userNameLowerCase = GetEncryptedData(userName.ToLower());
            userName = GetEncryptedData(userName);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.UserNameLowerCase == userNameLowerCase && m.UserName == userName && m.IsDeleted == false);

            if (noochMember != null)
            {
                return UppercaseFirst(GetDecryptedData(noochMember.FirstName)) + " " + UppercaseFirst(GetDecryptedData(noochMember.LastName));

            }
            return null;
        }

        public static bool IsMemberActivated(string tokenId)
        {

            var id = Utility.ConvertToGuid(tokenId);

            var noochMember =
                _dbContext.AuthenticationTokens.FirstOrDefault(m => m.TokenId == id && m.IsActivated == true);
            return noochMember != null;
        }

        public static bool IsNonNoochMemberActivated(string emailId)
        {
            
                var noochMember = _dbContext.Members.FirstOrDefault(m=>m.UserName == emailId && m.IsDeleted==false);
                return noochMember != null;
        }

    }
}
