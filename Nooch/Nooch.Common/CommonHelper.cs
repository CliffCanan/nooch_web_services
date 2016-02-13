using System;
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

            var noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == emailId && m.IsDeleted == false);
            return noochMember != null;
        }


        public static string IsDuplicateMember(string userName)
        {
            Logger.Info("Common Helper -> IsDuplicateMember Initiated - [UserName to check: " + userName + "]");

            var userNameLowerCase = GetEncryptedData(userName.ToLower());

            var noochMember =
                _dbContext.Members.FirstOrDefault(m => m.UserNameLowerCase == userNameLowerCase && m.IsDeleted == false);

            return noochMember != null ? "Username already exists for the primary email you entered. Please try with some other email." : "Not a nooch member.";
        }


        public static bool IsWeeklyTransferLimitExceeded(Guid MemberId, decimal amount)
        {
            // Get max weekly value allowed 
            var WeeklyLimitAllowed = Utility.GetValueFromConfig("WeeklyTransferLimit");

            DateTime CurrentWeekStartDate = DateTime.Today.AddDays(-1 * (int)(DateTime.Today.DayOfWeek));

            
                
                    // We only want to find transactions where this user has SENT money successfully. Exclude any pending/disputed/cancelled/rejected transactions.


            var totalAmountSent =
                _dbContext.Transactions.Where(m => m.Member.MemberId == MemberId || m.Member1.MemberId == MemberId &&
                                                   m.TransactionStatus == "Success" &&
                                                   m.TransactionDate > CurrentWeekStartDate &&
                                                   (m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" ||
                                                    m.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                                                    m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="))
                    .ToList()
                    .Sum(t => t.Amount);
                    
                     

                if (totalAmountSent > 10)
                {
                    if (!(Convert.ToDecimal(WeeklyLimitAllowed) > (Convert.ToDecimal(totalAmountSent) + amount)))
                    {
                        #region Check For Exempt Users

                        if (MemberId.ToString().ToLower() == "00bd3972-d900-429d-8a0d-28a5ac4a75d7")
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for TEAM NOOCH, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }
                        if (MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for CLIFF CANAN, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }
                        if (MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Marvis Burns (RentScene)
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }
                        if (MemberId.ToString().ToLower() == "e44c13da-7705-4953-8431-8ab0b2511a77") // REALTY MARK's Account (Member name is 'Diane Torres')
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }
                        if (MemberId.ToString().ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484") // Josh Detweiler (AppJaxx)
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction is for APPJAXX, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }
                        if (MemberId.ToString().ToLower() == "2d0427d2-7f21-40d9-a5a2-ac3e973809ec") // Dana Kozubal (Dave Phillip's)
                        {
                            Logger.Info("****  TDA -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction is for DANA KOZUBAL, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                            return false;
                        }

                        #endregion Check For Exempt Users

                        return true;
                    }
                }
            

            return false;
        }


    }
}
