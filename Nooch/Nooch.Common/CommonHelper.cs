using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Common.Resources;
using Nooch.Common.Rules;
using Nooch.Data;

using Nooch.Common.Entities.LandingPagesRelatedEntities;
//using System.Web.Mvc;
using synapseIdVerificationQuestionAnswerSet = Nooch.Common.Entities.SynapseRelatedEntities.synapseIdVerificationQuestionAnswerSet;
using System.Drawing.Imaging;
using System.Drawing;


namespace Nooch.Common
{
    public static class CommonHelper
    {
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static NOOCHEntities _dbContext = null;

        static CommonHelper()
        {
            _dbContext = new NOOCHEntities();
        }

        public static string GetEncryptedData(string sourceData)
        {
            if (!String.IsNullOrEmpty(sourceData))
            {
                try
                {
                    var aesAlgorithm = new AES();
                    string encryptedData = aesAlgorithm.Encrypt(sourceData, string.Empty);
                    return encryptedData.Replace(" ", "+");
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> GetEncryptedData FAILED - SourceData: [" + sourceData + "],  [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Error("Common Helper -> GetEncryptedData FAILED - SourceData was NULL or Empty: [" + sourceData + "]");
            }

            return string.Empty;
        }

        public static string GetDecryptedData(string sourceData)
        {
            if (!String.IsNullOrEmpty(sourceData))
            {
                try
                {
                    var aesAlgorithm = new AES();
                    string decryptedData = aesAlgorithm.Decrypt(sourceData.Replace(" ", "+"), string.Empty);
                    return decryptedData;
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> GetDecryptedData FAILED - SourceData: [" + sourceData + "],  [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Error("Common Helper -> GetDecryptedData FAILED - SourceData was NULL or Empty: [" + sourceData + "]");
            }

            return string.Empty;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string ForgotPassword(string userName)
        {
            Logger.Info("Common Helper -> ForgotPassword - [userName: " + userName + "]");

            var memberObj = GetMemberDetailsByUserName(userName);

            try
            {
                if (memberObj != null)
                {
                    bool status = false;

                    var fromAddress = Utility.GetValueFromConfig("adminMail");

                    var tokens = new Dictionary<string, string>
                    {
                        {
                            Constants.PLACEHOLDER_FIRST_NAME,
                            UppercaseFirst(GetDecryptedData(memberObj.FirstName)) + " " +
                            UppercaseFirst(GetDecryptedData(memberObj.LastName))
                        },
                        {Constants.PLACEHOLDER_LAST_NAME, GetDecryptedData(memberObj.LastName)},
                        {
                            Constants.PLACEHOLDER_PASSWORDLINK,
                            String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/ResetPassword?memberId=" + memberObj.MemberId)
                        }
                    };

                    PasswordResetRequest prr = new PasswordResetRequest();
                    prr.RequestedOn = DateTime.Now;
                    prr.MemberId = memberObj.MemberId;
                    _dbContext.PasswordResetRequests.Add(prr);
                    int i = _dbContext.SaveChanges();

                    if (i > 0)
                    {
                        _dbContext.Entry(prr).Reload();
                        status = true;
                        Utility.SendEmail(Constants.TEMPLATE_FORGOT_PASSWORD, fromAddress, GetDecryptedData(memberObj.UserName),
                                          null, "Reset your Nooch password", null, tokens, null, null, null);
                    }

                    return status
                        ? "Your reset password link has been sent to your mail successfully."
                        : "Problem occured while sending mail.";
                }
                return "Problem occured while sending mail.";
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> ForgotPassword FAILED - Exception: [" + ex.Message + "]");
                return "Problem occured while sending mail.";
            }
        }


        public static string FormatPhoneNumber(string sourcePhone)
        {
            if (!String.IsNullOrEmpty(sourcePhone))
            {
                try
                {
                    sourcePhone.Trim();
                    if (sourcePhone.Length != 10)
                    {
                        return sourcePhone;
                    }
                    sourcePhone = "(" + sourcePhone;
                    sourcePhone = sourcePhone.Insert(4, ")");
                    sourcePhone = sourcePhone.Insert(5, " ");
                    sourcePhone = sourcePhone.Insert(9, "-");
                    return sourcePhone;
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> FormatPhoneNumber FAILED - Exception: [" + ex.Message + "]");
                }
            }

            return null;
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
                Logger.Error("Common Helper -> RemovePhoneNumberFormatting Source String was NULL or EMPTY - [SourceData: " + sourceNum + "]");
            }
            return sourceNum;
        }


        public static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return char.ToUpper(s[0]) + s.Substring(1);
        }


        public static bool IsValidRequest(string accessToken, string memberId)
        {
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(memberId))
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
                            var toAddress = Common Helper.GetDecryptedData(member.UserName);
                            try
                            {
                                // email notification
                                UtilityDataAccess.SendEmail(null, MailPriority.High, fromAddress, toAddress, null, "Nooch automatic LogOut.", null, null, null, null, msg);
                            }
                            catch (Exception)
                            {
                                Logger.Info("InviteReminder - LogOut mail not sent to [" + toAddress + "]. Problem occured in sending mail.");
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
                    Logger.Error("Common Helper -> IsValidRequest FAILED - [Exception: " + ex + "]");
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
            Logger.Info("Common Helper -> GetMemberByUdId[ udId:" + udId + "].");

            var member = _dbContext.Members.FirstOrDefault(m => m.UDID1 == udId && m.IsDeleted == false);
            if (member != null)
            {
                _dbContext.Entry(member).Reload();
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
            try
            {
                var userNameLowerCase = GetEncryptedData(userName.ToLower());
                userName = GetEncryptedData(userName);

                var noochMember = _dbContext.Members.FirstOrDefault(m => (m.UserNameLowerCase == userNameLowerCase || m.UserName == userName) && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return noochMember.MemberId.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberIdByUserName FAILED - Exception: [" + ex.Message + "]");
            }
            return null;
        }


        public static string GetMemberUsernameByMemberId(string MemberId)
        {
            try
            {
                Guid memGuid = Utility.ConvertToGuid(MemberId);

                var noochMember =
                    _dbContext.Members.FirstOrDefault(
                        m => m.MemberId == memGuid && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                }

                return noochMember != null ? GetDecryptedData(noochMember.UserName) : null;
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMembersUsernameByMemberId FAILED - Exception: [" + ex.Message + "]");
            }

            return null;
        }


        public static string GetPhoneNumberByMemberId(string MemberId)
        {
            Guid memGuid = Utility.ConvertToGuid(MemberId);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.MemberId == memGuid && m.IsDeleted == false);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
            }

            return noochMember != null ? noochMember.ContactNumber : null;
        }


        public static string GetMemberIdByPhone(string memberPhone)
        {
            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.ContactNumber == memberPhone && m.IsDeleted == false);

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
            }

            return noochMember != null ? noochMember.MemberId.ToString() : null;
        }


        public static string GetMemberReferralCodeByMemberId(string MemberId)
        {
            Guid memGuid = Utility.ConvertToGuid(MemberId);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.MemberId == memGuid && m.IsDeleted == false);

            if (noochMember == null || noochMember.InviteCodeId == null) return "";
            Guid inviGuid = Utility.ConvertToGuid(noochMember.InviteCodeId.ToString());

            var inviteCodeREsult =
                _dbContext.InviteCodes.FirstOrDefault(
                    m => m.InviteCodeId == inviGuid);
            if (inviteCodeREsult != null)
            {
                _dbContext.Entry(inviteCodeREsult).Reload();
            }
            return inviteCodeREsult != null ? inviteCodeREsult.code : "";
        }


        public static string GetMemberNameByUserName(string userName)
        {
            try
            {
                var userNameLowerCase = GetEncryptedData(userName.ToLower());
                userName = GetEncryptedData(userName);

                var noochMember =
                    _dbContext.Members.FirstOrDefault(
                        m => m.UserNameLowerCase == userNameLowerCase && m.UserName == userName && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return UppercaseFirst(GetDecryptedData(noochMember.FirstName)) + " " + UppercaseFirst(GetDecryptedData(noochMember.LastName));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberNameByUserName FAILED - Exception: [" + ex.Message + "]");
            }
            return null;
        }


        /// <summary>
        /// Returns ENCRYPTED form of a user's PIN based on a given Username.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static string GetMemberPinByUserName(string userName)
        {
            try
            {
                if (!String.IsNullOrEmpty(userName) && userName.Length > 3 && userName.IndexOf('@') > 1)
                {
                    var userNameLowerCase = GetEncryptedData(userName.ToLower());
                    var userNameEnc = GetEncryptedData(userName);

                    var noochMember = _dbContext.Members.FirstOrDefault(m => (m.UserNameLowerCase == userNameLowerCase || m.UserName == userNameEnc) && m.IsDeleted == false);

                    if (noochMember != null)
                    {
                        _dbContext.Entry(noochMember).Reload();
                        return noochMember.PinNumber; // Return ENCRYPTED Pin Number
                    }
                    else
                    {
                        Logger.Error("Common Helper -> GetMemberPinByUserName FAILED - Couldn't find any Nooch user with the username of: [" + userName + "]");
                    }
                }
                else
                {
                    Logger.Error("Common Helper -> GetMemberPinByUserName FAILED - Username was either NULL or too short, or missing '@'");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberPinByUserName FAILED - [Username: " + userName + "], [Exception: " + ex.Message + "]");
            }

            return null;
        }


        /// <summary>
        /// Created 6/17/16: For the MakePayment page to check the user's Nooch PIN.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public static genericResponse CheckUserPin(string user, string pin)
        {
            genericResponse res = new genericResponse();
            res.success = false;

            try
            {
                if (String.IsNullOrEmpty(user))
                {
                    res.msg = "Missing user";
                    return res;
                }
                if (String.IsNullOrEmpty(pin))
                {
                    res.msg = "Missing PIN";
                    return res;
                }
                else if (pin.Length != 4)
                {
                    res.msg = "Incorrect PIN length";
                    return res;
                }

                string memId = string.Empty;

                if (user.ToLower() == "appjaxx")
                    memId = "8b4b4983-f022-4289-ba6e-48d5affb5484";
                else if (user.ToLower() == "rentscene")
                    memId = "852987e8-d5fe-47e7-a00b-58a80dd15b49";
                else if (user == "cliff")
                    memId = "b3a6cf7b-561f-4105-99e4-406a215ccf60";
                else
                {
                    res.msg = "Invalid user";
                    return res;
                }

                Member memberObj = GetMemberDetails(memId);

                if (memberObj != null)
                {
                    var pinEnc = GetEncryptedData(pin);

                    if (pinEnc == memberObj.PinNumber)
                    {
                        res.success = true;
                        res.msg = "PIN confirmed successfully";
                    }
                    else
                    {
                        res.msg = "Incorrect PIN";
                    }
                }
                else
                {
                    Logger.Error("Common Helper -> GetMemberPinByUserName FAILED - User not found - User: [" + user + "]");
                    res.msg = "User not found";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberPinByUserName FAILED - [User: " + user + "], [Exception: " + ex.Message + "]");
                res.msg = ex.Message;
            }

            return res;
        }


        public static bool IsMemberActivated(string tokenId)
        {
            var id = Utility.ConvertToGuid(tokenId);

            var noochMember =
                _dbContext.AuthenticationTokens.FirstOrDefault(m => m.TokenId == id && m.IsActivated == true);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
            }
            return noochMember != null;
        }

        public static bool IsNonNoochMemberActivated(string emailId)
        {
            var noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == emailId && m.IsDeleted == false);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
            }
            return noochMember != null;
        }

        public static string IsDuplicateMember(string userName)
        {
            try
            {
                Logger.Info("Common Helper -> IsDuplicateMember Initiated - [UserName to check: " + userName + "]");

                var userNameLowerCase = GetEncryptedData(userName.ToLower());

                var noochMember =
                    _dbContext.Members.FirstOrDefault(m => m.UserNameLowerCase == userNameLowerCase && m.IsDeleted == false);
                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                }

                return noochMember != null ? "Username already exists for the primary email you entered. Please try with some other email." : "Not a nooch member.";
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberNameByUserName FAILED - Exception: [" + ex.Message + "]");
                return ex.Message;
            }
        }

        public static bool IsWeeklyTransferLimitExceeded(Guid MemberId, decimal amount, string recipientId)
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
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for TEAM NOOCH, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for CLIFF CANAN, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Rent Scene
                        (!String.IsNullOrEmpty(recipientId) && recipientId.ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49"))
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "e44c13da-7705-4953-8431-8ab0b2511a77") // REALTY MARK's Account (Member name is 'Diane Torres')
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484") // Josh Detweiler (AppJaxx)
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction is for APPJAXX, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }

                    #endregion Check For Exempt Users

                    return true;
                }
            }

            return false;
        }

        public static Member GetMemberDetails(string memberId)
        {
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return noochMember;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberDetails FAILED - Member ID: [" + memberId + "], [Exception: " + ex + "]");
            }
            return new Member();
        }

        /// <summary>
        /// Looks up a member by an UNENCRYPTED email address. Will find any member based on Username OR SecondaryEmail fields.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns>Returns a User's Full details.</returns>
        public static Member GetMemberDetailsByUserName(string userName)
        {
            try
            {
                var userNameEnc = GetEncryptedData(userName);
                var userNameLowerEnc = GetEncryptedData(userName.ToLower());

                var noochMember = _dbContext.Members.FirstOrDefault(m => (m.UserName == userNameEnc || m.UserNameLowerCase == userNameLowerEnc || m.SecondaryEmail == userNameEnc) && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return noochMember;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberDetails FAILED - UserName: [" + userName + "], [Exception: " + ex + "]");
            }

            return null;
        }

        public static List<SynapseBankLoginResult> GetSynapseBankLoginResulList(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankLoginResulList - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBankLoginResults.Where(m => m.MemberId == id && m.IsDeleted == false).ToList();

            return memberAccountDetails;
        }


        public static bool RemoveSynapseBankLoginResults(string memberId)
        {
            Logger.Info("Common Helper -> RemoveSynapseBankLoginResultsForGivenMemberId Initiated - MemberId: [" + memberId + "]");

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var oldBankLoginRecords = _dbContext.SynapseBankLoginResults.Where(m => m.MemberId == id && m.IsDeleted == false).ToList();

                if (oldBankLoginRecords != null && oldBankLoginRecords.Count > 0)
                {
                    // CC (6/18/16): THIS BLOCK IS THROWING AN ERROR EVERY TIME... maybe b/c it's attempting to save
                    //               the changes to "v" which is not the actual DB object?
                    // ERROR is: "System.InvalidOperationException: The entity type List`1 is not part of the model for the current context."
                    // Malkit (20 June 2016) : This is fixed, you should load correct db context in such cases Remeber to call GetDbContextFromEntity method from CommonHelper.. ;)
                    foreach (SynapseBankLoginResult v in oldBankLoginRecords)
                    {
                        DbContext db = GetDbContextFromEntity(v);
                        v.IsDeleted = true;
                        db.SaveChanges();
                        //db.Entry(oldBankLoginRecords).Reload();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> RemoveSynapseBankLoginResultsForGivenMemberId FAILED - Exception: [" + ex + "]");
                return false;
            }
        }


        /// <summary>
        /// Gets the user's Bank account details from SynapseBanksOfMembers Table in Nooch's DB.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public static SynapseBanksOfMember GetSynapseBankDetails(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankDetails Fired - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var bankDetailsFromDB = _dbContext.SynapseBanksOfMembers.FirstOrDefault(m => m.MemberId == id &&
                                                                                         m.IsDefault == true);
            if (bankDetailsFromDB != null)
            {
                _dbContext.Entry(bankDetailsFromDB).Reload();
            }
            else
            {
                Logger.Error("Common Helper -> GetSynapseBankDetails FAILED - No Synapse Bank " +
                             "User Record found for MemberId: [" + memberId + "]");
            }

            return bankDetailsFromDB;
        }


        /// <summary>
        /// Gets the user's Synapse user details from SynapseCreateUserResults Table in Nooch's DB.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public static SynapseCreateUserResult GetSynapseCreateaUserDetails(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseCreateaUserDetails Fired - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberObj = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);
            if (memberObj != null)
            {
                _dbContext.Entry(memberObj).Reload();
            }
            else
            {
                Logger.Error("Common Helper -> GetSynapseCreateaUserDetails FAILED - No Synapse Create " +
                             "User Record found for MemberId: [" + memberId + "]");
            }

            return memberObj;
        }

        public static MemberNotification GetMemberNotificationSettingsByUserName(string userName)
        {
            Logger.Info("Common Helper -> GetMemberNotificationSettingsByUserName Initiated- UserName: [" + userName + "]");

            userName = GetEncryptedData(userName);

            var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.Member.UserName == userName);
            if (memberNotifications != null)
            {
                _dbContext.Entry(memberNotifications).Reload();
            }

            return memberNotifications;
        }

        public static string IncreaseInvalidLoginAttemptCount(string memGuid, int loginRetryCountInDb)
        {
            Logger.Info("Common Helper -> IncreaseInvalidLoginAttemptCount Initiated (User's PW was incorrect during login attempt) - " +
                        "This is invalid attempt #: [" + (loginRetryCountInDb + 1).ToString() + "], " +
                        "MemberId: [" + memGuid + "]");

            Member m = GetMemberDetails(memGuid);

            m.InvalidLoginTime = DateTime.Now;
            m.InvalidLoginAttemptCount = loginRetryCountInDb + 1;
            _dbContext.SaveChanges();
            _dbContext.Entry(m).Reload();

            return "The password you have entered is incorrect.";
        }

        public static bool UpdateAccessToken(string userName, string AccessToken)
        {
            Logger.Info("Common Helper -> UpdateAccessToken Initiated- userName: [" + userName + "]");

            try
            {
                userName = GetEncryptedData(userName);

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == userName && m.IsDeleted == false);

                if (noochMember != null)
                {
                    noochMember.AccessToken = AccessToken;
                    _dbContext.SaveChanges();
                    _dbContext.Entry(noochMember).Reload();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> UpdateAccessToken FAILED - [Exception: " + ex + "]");
                return false;
            }
        }

        public static bool CheckTokenExistance(string AccessToken)
        {
            try
            {
                var noochMember = _dbContext.Members.FirstOrDefault(m => m.AccessToken == AccessToken && m.IsDeleted == false);
                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                }
                return noochMember != null;
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> CheckTokenExistance FAILED - [Exception: " + ex.Message + "]");
                return false;
            }
        }

        public static bool IsListedInSDN(string lastName, Guid userId)
        {
            bool result = false;
            Logger.Info("Common Helper -> IsListedInSDNList Initiated- userName: [" + lastName + "]");

            var noochMemberN =
                _dbContext.Members.FirstOrDefault(
                    m => m.IsDeleted == false && (m.IsSDNSafe == false || m.IsSDNSafe == null) && m.MemberId == userId);

            if (noochMemberN != null)
            {
                result = true;

                StringBuilder st = new StringBuilder();
                //Check if the person exists in the SDN List or not
                List<SDNSearchResult> terrorlist = _dbContext.pGetSDNListing(GetDecryptedData(noochMemberN.LastName)).ToList();

                // hit matched send notification email to nooch admin and update member table
                if (terrorlist.Count > 0)
                {
                    noochMemberN.SDNCheckDateTime = DateTime.Now;
                    noochMemberN.AnyPriliminaryHit = true;
                    //  noochMemberN.ent_num = noochMember.ent_num;
                    st.Append("<table border='1'>");
                    st.Append("<tr>");
                    st.Append("<th>ENT Number</th>");
                    st.Append("<th>SDN Name</th>");
                    st.Append("<th>Percentage Matched</th>");
                    st.Append("<tr>");

                    foreach (var terrorist in terrorlist)
                    {
                        st.Append("<tr>");
                        st.Append("<td>" + terrorist.ent_num + "</td>");
                        st.Append("<td>" + terrorist.SDN_NAME + "</td>");
                        st.Append("<td>" + (terrorist.lastper + terrorist.subper) / 2 + "</td>");
                        st.Append("<tr>");
                    }
                    st.Append("</table>");

                    //- mail sending code---
                    // update his data and send email to Nooch admin
                    var sendername = GetDecryptedData(noochMemberN.FirstName) + " " +
                                     GetDecryptedData(noochMemberN.LastName);
                    var senderemail = GetDecryptedData(noochMemberN.UserName);
                    StringBuilder str = new StringBuilder();
                    string s =
                        "<html><body><strong>OFAC list Match</strong><br/>An automatic SDN screening returned the following details of a flagged user:<br/><br/>" +
                        st;

                    str.Append(s);

                    s = "<br/><p>This user's Nooch Account information is:</p><br/>" +
                        "<table border='1'><tr><td>Email Address: </td><td>" + senderemail + "</td></tr>" +
                        "<tr><td>Name: </td><td>" + sendername + "</td></tr>" +
                        "<tr><td>MemberID: </td><td>" + noochMemberN.MemberId + "</td></tr>" +
                        "<tr><td>Country: </td><td>" + GetDecryptedData(noochMemberN.Country) + "</td></tr>" +
                        "<tr><td>Address: </td><td>" + GetDecryptedData(noochMemberN.Address) + "</td></tr>" +
                        "<tr><td>Phone Number: </td><td>" + noochMemberN.ContactNumber + "</td></tr></table><br/>- Nooch SDN Check</body></html>";

                    str.Append(s);
                    string adminUserName = GetEncryptedData(Utility.GetValueFromConfig("transfersMail"));
                    var fromAddress = GetDecryptedData(adminUserName);

                    bool b = Utility.SendEmail(null, fromAddress,
                        Utility.GetValueFromConfig("SDNMailReciever"), null, "SDN Listed", null, null, null,
                        null, str.ToString());

                    if (b)
                    {
                        Logger.Info("Common Helper -> SDN Screening Alert - SDN Screening Results email sent to SDN@nooch.com");
                    }
                    {
                        Logger.Error("Common Helper -> SDN Screening Alert - SDN Screening Results email NOT sent to SDN@nooch.com.");
                    }
                }
                else
                {
                    noochMemberN.SDNCheckDateTime = DateTime.Now;
                    noochMemberN.AnyPriliminaryHit = false;
                    noochMemberN.ent_num = null;
                }

                // updatine record in members table
                _dbContext.SaveChanges();
                _dbContext.Entry(noochMemberN).Reload();
            }

            return result;
        }

        private static string IncreaseInvalidPinAttemptCount(Member memberEntity, int pinRetryCountInDb)
        {
            var mem = _dbContext.Members.Find(memberEntity.MemberId);

            mem.InvalidPinAttemptCount = pinRetryCountInDb + 1;
            mem.InvalidPinAttemptTime = DateTime.Now;
            _dbContext.SaveChanges();
            _dbContext.Entry(mem).Reload();
            return memberEntity.InvalidPinAttemptCount == 1
                ? "PIN number you have entered is incorrect."
                : "PIN number you entered again is incorrect. Your account will be suspended for 24 hours if you enter wrong PIN number again.";
        }


        public static string ValidatePinNumberToEnterForEnterForeground(string memberId, string pinNumber)
        {
            try
            {
                using (var noochConnection = new NOOCHEntities())
                {
                    var id = Utility.ConvertToGuid(memberId);

                    var memberEntity = noochConnection.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                    if (memberEntity != null)
                    {
                        if (memberEntity.PinNumber.Equals(pinNumber.Replace(" ", "+")))
                        {
                            return "Success";
                        }
                        else
                        {
                            return "Invalid Pin";
                        }
                    }
                    return "Member not found.";
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Common Helper - ValidatePinNumberToEnterForEnterForeground FAILED - Exception: [" + ex.Message + "]");
                return ex.Message;
            }
        }


        public static string ValidatePinNumber(string memberId, string pinNumber)
        {
            var id = Utility.ConvertToGuid(memberId);

            var memberEntity = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);
            // _dbContext.Entry(memberEntity).Reload();

            if (memberEntity != null)
            {
                int pinRetryCountInDb = 0;
                pinRetryCountInDb = memberEntity.InvalidPinAttemptCount.Equals(null)
                    ? 0
                    : memberEntity.InvalidPinAttemptCount.Value;
                var currentTimeMinus24Hours = DateTime.Now.AddHours(-24);

                //Check(InvalidPinAttemptTime) > CurrentTime - 24 hrs                  
                bool isInvalidPinAttempTimeOver =
                    (new InvalidAttemptDurationSpecification().IsSatisfiedBy(memberEntity.InvalidPinAttemptTime,
                        currentTimeMinus24Hours));

                if (isInvalidPinAttempTimeOver)
                {
                    //Reset attempt count
                    memberEntity.InvalidPinAttemptCount = null;
                    memberEntity.InvalidPinAttemptTime = null;
                    //if member has no dispute raised or under review, he can be made active, else he should remain suspended so that he cant do fund transfer or withdraw amount...

                    var disputeStatus = GetEncryptedData(Constants.DISPUTE_STATUS_REPORTED);
                    var disputeReviewStatus = GetEncryptedData(Constants.DISPUTE_STATUS_REVIEW);

                    if (
                        !memberEntity.Transactions.Any(transaction =>
                                (transaction.DisputeStatus == disputeStatus ||
                                 transaction.DisputeStatus == disputeReviewStatus) &&
                                 memberEntity.MemberId == transaction.RaisedById))
                    {
                        memberEntity.Status = Constants.STATUS_ACTIVE;
                    }

                    memberEntity.DateModified = DateTime.Now;
                    _dbContext.SaveChanges();
                    _dbContext.Entry(memberEntity).Reload();

                    pinRetryCountInDb = memberEntity.InvalidPinAttemptCount.Equals(null)
                                        ? 0
                                        : memberEntity.InvalidPinAttemptCount.Value;

                    // incorrect pinnumber after 24 hours
                    if (!memberEntity.PinNumber.Equals(pinNumber.Replace(" ", "+")))
                    {
                        return IncreaseInvalidPinAttemptCount(memberEntity, pinRetryCountInDb);
                    }
                }

                if (pinRetryCountInDb < 3 && memberEntity.PinNumber.Equals(pinNumber.Replace(" ", "+")))
                {
                    //Reset attempt count                       
                    memberEntity.InvalidPinAttemptCount = 0;
                    memberEntity.InvalidPinAttemptTime = null;

                    _dbContext.SaveChanges();
                    _dbContext.Entry(memberEntity).Reload();
                    return "Success"; // active nooch member  
                }

                //Username is there in db, whereas pin number entered by user is incorrect.
                if (memberEntity.InvalidPinAttemptCount == null || memberEntity.InvalidPinAttemptCount == 0)
                {
                    //this is the first invalid try
                    return IncreaseInvalidPinAttemptCount(memberEntity, pinRetryCountInDb);
                }

                if (pinRetryCountInDb == 3)
                {
                    return "Your account has been suspended. Please contact admin or send a mail to support@nooch.com if you need to reset your PIN number immediately.";
                }
                if (pinRetryCountInDb == 2)
                {
                    memberEntity.InvalidPinAttemptCount = pinRetryCountInDb + 1;
                    memberEntity.InvalidPinAttemptTime = DateTime.Now;
                    memberEntity.Status = Constants.STATUS_SUSPENDED;
                    _dbContext.SaveChanges();
                    _dbContext.Entry(memberEntity).Reload();

                    #region SendingEmailToUser

                    var tokens = new Dictionary<string, string>
                        {
                            {
                                Constants.PLACEHOLDER_FIRST_NAME, UppercaseFirst(GetDecryptedData(memberEntity.FirstName))
                            }
                        };

                    try
                    {
                        var fromAddress = Utility.GetValueFromConfig("adminMail");
                        string emailAddress = GetDecryptedData(memberEntity.UserName);
                        Logger.Info("Validate PIN Number --> Attempt to send mail for Suspend Member[ memberId:" + memberEntity.MemberId + "].");
                        Utility.SendEmail("userSuspended", fromAddress, emailAddress,
                            null, "Your Nooch account has been suspended", null, tokens, null, null, null);
                    }
                    catch (Exception)
                    {
                        Logger.Error("Validate PIN Number --> Suspend Member status email not send to [" +
                                               memberEntity.MemberId +
                                               "]. Problem occurred in sending Suspend Member status mail. ");
                    }

                    #endregion

                    return "Your account has been suspended for 24 hours from now. Please contact admin or send a mail to support@nooch.com if you need to reset your PIN number immediately.";
                    // this is 3rd try
                }
                return IncreaseInvalidPinAttemptCount(memberEntity, pinRetryCountInDb);
                // this is second try.
            }

            return "Member not found.";
        }


        public static string GetGivenMemberTransferLimit(string memberId)
        {
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                // checking user details for given id

                var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (memberObj != null)
                {
                    _dbContext.Entry(memberObj).Reload();
                    return !String.IsNullOrEmpty(memberObj.TransferLimit) ? memberObj.TransferLimit : "0";
                }
                return "0";
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetGivenMemberTransferLimit FAILED - [MemberID: " + memberId + "], [Exception: " + ex + "]");
                return "0";
            }
        }


        public static bool isOverTransactionLimit(decimal amount, string senderMemId, string recipMemId)
        {
            var maxTransferLimitPerPayment = Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction");

            if (amount > Convert.ToDecimal(maxTransferLimitPerPayment))
            {
                if (senderMemId.ToLower() == "00bd3972-d900-429d-8a0d-28a5ac4a75d7" || // TEAM NOOCH
                    recipMemId.ToLower() == "00bd3972-d900-429d-8a0d-28a5ac4a75d7")
                {
                    Logger.Info("*****  Common Helper -> isOverTransactionLimit - Transaction for TEAM NOOCH, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }
                if (senderMemId.ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Marvis Burns (RentScene)
                    recipMemId.ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49")
                {
                    Logger.Info("*****  Common Helper -> isOverTransactionLimit - Transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }

                if (senderMemId.ToLower() == "c9839463-d2fa-41b6-9b9d-45c7f79420b1" || // Sherri Tan (RentScene - via Marvis Burns)
                    recipMemId.ToLower() == "c9839463-d2fa-41b6-9b9d-45c7f79420b1")
                {
                    Logger.Info("*****  Common Helper -> isOverTransactionLimit - Transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }
                if (senderMemId.ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484" || // Josh Detweiler (AppJaxx)
                    recipMemId.ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484")
                {
                    Logger.Info("*****  Common Helper -> isOverTransactionLimit - Transaction is for APPJAXX, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }

                return true;
            }

            return false;
        }


        public static MemberNotification GetMemberNotificationSettings(string memberId)
        {
            //Logger.Info("Common Helper -> GetMemberNotificationSettings Initiated - MemberId: [" + memberId + "]");

            try
            {
                Guid memId = Utility.ConvertToGuid(memberId);

                var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.Member.MemberId == memId);

                if (memberNotifications != null)
                {
                    _dbContext.Entry(memberNotifications).Reload();
                }

                return memberNotifications;
            }
            catch (Exception ex)
            {
                Logger.Info("Common Helper -> GetMemberNotificationSettings FAILED - MemberId: [" + memberId +
                            "], Exception: [" + ex.Message + "]");
            }

            return null;
        }


        /// <summary>
        /// To get random alphanumeric 5 digit transaction tracking ID.
        /// </summary>
        /// <returns>Nooch Random ID</returns>
        public static string GetRandomTransactionTrackingId()
        {
            var random = new Random();
            int j = 1;

            for (int i = 0; i <= j; i++)
            {
                var randomId = new string(
                    Enumerable.Repeat(Chars, 9)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());
                var transactionEntity = _dbContext.Transactions.FirstOrDefault(n => n.TransactionTrackingId == randomId);
                if (transactionEntity == null)
                {
                    return randomId;
                }

                j += i + 1;
            }
            return null;
        }


        public static Landlord GetLandlordDetails(string memberId)
        {
            //Logger.Info("Common Helper -> GetLandlordDetails - MemberId: [" + memberId + "]");
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var landlordObj = _dbContext.Landlords.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (landlordObj != null)
                {
                    _dbContext.Entry(landlordObj).Reload();
                    return landlordObj;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetLandlordDetails FAILED - Member ID: [" + memberId + "], [Exception: " + ex + "]");
            }

            return new Landlord();
        }


        public static String ConvertImageURLToBase64(String url)
        {
            try
            {
                if (!String.IsNullOrEmpty(url))
                {
                    Logger.Info("Common Helper -> ConvertImageURLToBase64 Initiated - Photo URL is: [" + url + "]");

                    StringBuilder _sb = new StringBuilder();

                    Byte[] _byte = GetImage(url);

                    _sb.Append(Convert.ToBase64String(_byte, 0, _byte.Length));

                    return _sb.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> ConvertImageURLToBase64 FAILED - Exception: [" + ex.Message + "]");
            }

            return "";
        }


        private static byte[] GetImage(string url)
        {
            Stream stream = null;
            byte[] buf;

            try
            {
                WebProxy myProxy = new WebProxy();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

                HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                stream = response.GetResponseStream();

                using (BinaryReader br = new BinaryReader(stream))
                {
                    int len = (int)(response.ContentLength);
                    buf = br.ReadBytes(len);
                    br.Close();
                }

                stream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetImage FAILED - Photo URL was: [" + url + "]. Exception: [" + ex + "]");
                buf = null;
            }

            return (buf);
        }


        public static string GetRecentOrDefaultIPOfMember(Guid MemberIdPassed)
        {
            string lastIP = "";

            try
            {
                var memberIP = _dbContext.MembersIPAddresses.OrderByDescending(m => m.ModifiedOn).FirstOrDefault(m => m.MemberId == MemberIdPassed);

                if (memberIP != null)
                {
                    _dbContext.Entry(memberIP).Reload();
                }

                lastIP = memberIP != null ? memberIP.Ip : "54.201.43.89";
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetRecentOrDefaultIPOfMember FAILED - Exception: [" + ex + "]");
                lastIP = "Server exception on IP Lookup: [" + ex.Message + "]";
            }

            return lastIP;
        }


        /// <summary>
        /// For sending a user's SSN & DOB to Synapse using V3 API.
        /// UPDATE (JUNE 2016): WILL BE DEPRECATED THIS MONTH ONCE SYNAPSE FINSIHSES ADDING NEW /USER/DOCS/ADD SERVICE TO V3.0.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        public static submitIdVerificationInt sendUserSsnInfoToSynapseV3(string MemberId)
        {
            Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 Fired - [MemberId: " + MemberId + "]");

            submitIdVerificationInt res = new submitIdVerificationInt();
            res.success = false;

            var id = Utility.ConvertToGuid(MemberId);

            var memberEntity = GetMemberDetails(MemberId);

            if (memberEntity != null)
            {
                var userNameDecrypted = GetDecryptedData(memberEntity.UserName);

                string usersFirstName = UppercaseFirst(GetDecryptedData(memberEntity.FirstName));
                string usersLastName = UppercaseFirst(GetDecryptedData(memberEntity.LastName));

                string usersAddress = "";
                string usersZip = "";

                DateTime usersDob;
                string usersDobDay = "";
                string usersDobMonth = "";
                string usersDobYear = "";

                string usersSsn = "";

                string usersSynapseOauthKey = "";
                string usersFingerprint = "";

                #region Check User For All Required Data

                try
                {
                    bool isMissingSomething = false;
                    // Member found, now check that they have added a full Address (including city, zip), SSN, & DoB

                    // Check for Fingerprint (UDID1 in the database)
                    if (String.IsNullOrEmpty(memberEntity.UDID1))
                    {
                        isMissingSomething = true;
                        res.message = " Common Helper - Missing UDID";
                    }
                    else
                    {
                        usersFingerprint = memberEntity.UDID1;
                    }

                    // Check for Address
                    if (String.IsNullOrEmpty(memberEntity.Address))
                    {
                        isMissingSomething = true;
                        res.message += " Common Helper - Missing Address";
                    }
                    else
                    {
                        usersAddress = GetDecryptedData(memberEntity.Address);
                    }

                    // Check for ZIP
                    if (String.IsNullOrEmpty(memberEntity.Zipcode))
                    {
                        isMissingSomething = true;
                        res.message += " MDA - Missing ZIP";
                    }
                    else
                    {
                        usersZip = GetDecryptedData(memberEntity.Zipcode);
                    }

                    // Check for SSN
                    if (string.IsNullOrEmpty(memberEntity.SSN))
                    {
                        isMissingSomething = true;
                        res.message += " MDA - Missing SSN";
                    }
                    else
                    {
                        usersSsn = GetDecryptedData(memberEntity.SSN);
                        usersSsn = usersSsn.Replace(" ", "").Replace("-", "");
                    }

                    // Check for Date Of Birth (Not encrypted)
                    if (memberEntity.DateOfBirth == null)
                    {
                        isMissingSomething = true;
                        res.message += " MDA - Missing Date of Birth";
                    }
                    else
                    {
                        usersDob = Convert.ToDateTime(memberEntity.DateOfBirth);

                        // We have DOB, now we must parse it into day, month, & year
                        usersDobDay = usersDob.Day.ToString();
                        usersDobMonth = usersDob.Month.ToString();
                        usersDobYear = usersDob.Year.ToString();
                    }
                    // Return if any data was missing in previous block
                    if (isMissingSomething)
                    {
                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 ABORTED: Member has no DoB. [Username: " + userNameDecrypted + "], [Message: " + res.message + "]");
                        return res;
                    }


                    // Now check if user already has a Synapse User account (would have a record in SynapseCreateUserResults.dbo)
                    var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id &&
                                                                                                      m.IsDeleted == false);

                    if (usersSynapseDetails == null)
                    {
                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 ABORTED: Member's Synapse User Details not found. [Username: " + userNameDecrypted + "]");
                        res.message = "Users synapse details not found";
                        return res;
                    }
                    else
                    {
                        _dbContext.Entry(usersSynapseDetails).Reload();
                        usersSynapseOauthKey = GetDecryptedData(usersSynapseDetails.access_token);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED on checking for all required data - [Username: " +
                                  userNameDecrypted + "], [Exception: " + ex + "]");
                }

                #endregion Check User For All Required Data

                if (memberEntity.IsVerifiedWithSynapse == null)
                {
                    // Update Member's DB record from NULL to false (update to true later on if Verification from Synapse is completely successful)
                    Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - About to set IsVerifiedWithSynapse to False before calling Synapse: [Username: " +
                                 userNameDecrypted + "]");
                    memberEntity.IsVerifiedWithSynapse = false;
                }

                #region Send SSN Info To Synapse

                try
                {
                    #region Call Synapse V3 /user/doc/add API

                    synapseAddKycInfoInputV3Class synapseKycInput = new synapseAddKycInfoInputV3Class();

                    SynapseV3Input_login login = new SynapseV3Input_login();
                    login.oauth_key = usersSynapseOauthKey;
                    synapseKycInput.login = login;

                    addKycInfoInput_user_doc doc = new addKycInfoInput_user_doc();
                    doc.birth_day = usersDobDay;
                    doc.birth_month = usersDobMonth;
                    doc.birth_year = usersDobYear;
                    doc.name_first = usersFirstName;
                    doc.name_last = usersLastName;
                    doc.address_street1 = usersAddress;
                    doc.address_postal_code = usersZip;
                    doc.address_country_code = "US";

                    doc.document_type = "SSN"; // This can also be "PASSPORT" or "DRIVERS_LICENSE"... we need to eventually support all 3 options (Rent Scene has international clients that don't have SSN but do have a Passport)
                    doc.document_value = usersSsn; // Can also be the user's Passport # or DL #

                    addKycInfoInput_user user = new addKycInfoInput_user();
                    user.fingerprint = usersFingerprint;
                    user.doc = doc;

                    synapseKycInput.user = user;

                    string baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"))
                                         ? "https://sandbox.synapsepay.com/api/v3/user/doc/add"
                                         : "https://synapsepay.com/api/v3/user/doc/add";


                    #region For Testing & Logging

                    if (memberEntity.MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                    {
                        doc.name_last = "Satell";
                        doc.document_value = "195707562";
                    }

                    try
                    {
                        Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - About To Query Synapse (/v3/user/doc/add) -> Payload to send to Synapse: [OauthKey: " + login.oauth_key +
                            "], [Birth_day: " + doc.birth_day + "], [Birth_month: " + doc.birth_month +
                            "], [Birth_year: " + doc.birth_year + "], [name_first: " + doc.name_first +
                            "], [name_last: " + doc.name_last + "], [ssn: " + doc.document_value +
                            "], [address_street1: " + doc.address_street1 + "], [postal_code: " + doc.address_postal_code +
                            "], [country_code: " + doc.address_country_code + "], [Fingerprint: " + user.fingerprint +
                            "], [BASE_ADDRESS: " + baseAddress + "].");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Common Helper -> sendUserSSNInfoToSynapseV3 - Couldn't log Synapse SSN Payload. [Exception: " + ex + "]");
                    }

                    #endregion For Testing & Logging

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(synapseKycInput);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    kycInfoResponseFromSynapse synapseResponse = new kycInfoResponseFromSynapse();
                    synapseResponse = JsonConvert.DeserializeObject<kycInfoResponseFromSynapse>(content);

                    #endregion Call Synapse V3 /user/doc/add API


                    // NOW WE MUST PARSE THE SYNAPSE RESPONSE. THERE ARE 3 POSSIBLE SCENARIOS:
                    // 1.) SSN Validation was successful. Synapse returns {"success": true}
                    // 2.) SSN Validation was PARTLY successful.  Synapse returns: "success":true... 
                    //     plus an object "question_set", containing a series of questions and array of multiple choice answers for each question.
                    //     We will display the questions to the user via the IDVerification.aspx page (already built-in to the Add-Bank process)
                    // 3.) SSN Validation Failed:  Synapse will return HTTP Error 400 Bad Request
                    //     with an "error" object, and then a message in "error.en" that should be: "Invalid SSN information supplied. Request user to submit a copy of passport/divers license and SSN via user/doc/attachments/add"

                    #region Parse Synapse Response

                    if (synapseResponse != null)
                    {
                        if (synapseResponse.success == true)
                        {
                            Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - Synapse returned SUCCESS = TRUE. Now checking if additional Verification questions are required...");

                            // Great, we have at least partial success. Now check if further verification is needed by checking if Synapse returned a 'question_set' object.

                            res.success = true;

                            if (synapseResponse.question_set != null)
                            {
                                // Further Verification is needed...
                                #region Additional Verification Questions Returned

                                // Now make sure an Array[] set of 'questions' was returned (could be up to 5 questions, each with 5 possible answer choices)
                                if (synapseResponse.question_set.questions != null)
                                {
                                    Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - Question_Set was returned, further validation will be needed. Saving ID Verification Questions...");

                                    // Saving these questions in DB.  

                                    // UPDATE (9/29/15):
                                    // The user will have to answer these on the IDVerification.aspx page.
                                    // That's why I updated the sendSSN function to not be void and return success + a message. Based on that value,
                                    // the Add-Bank page will direct the user either to the IDVerification page (via iFrame), or not if questions are not needed.

                                    // Loop through each question set (question/answers/id)
                                    #region Iterate Through Each Question And Save in DB

                                    foreach (synapseIdVerificationQuestionAnswerSet question in synapseResponse.question_set.questions)
                                    {
                                        SynapseIdVerificationQuestion questionForDb = new SynapseIdVerificationQuestion();
                                        questionForDb.MemberId = id;
                                        questionForDb.QuestionSetId = synapseResponse.question_set.id;
                                        questionForDb.SynpQuestionId = question.id;

                                        questionForDb.DateCreated = DateTime.Now;
                                        questionForDb.submitted = false;

                                        questionForDb.person_id = synapseResponse.question_set.person_id;
                                        questionForDb.time_limit = synapseResponse.question_set.time_limit;
                                        questionForDb.score = synapseResponse.question_set.score; // THIS COULD BE NULL...
                                        questionForDb.updated_at = synapseResponse.question_set.updated_at.ToString();
                                        questionForDb.livemode = synapseResponse.question_set.livemode; // NO IDEA WHAT THIS IS FOR...
                                        questionForDb.expired = synapseResponse.question_set.expired; // SHOULD ALWAYS BE false
                                        questionForDb.created_at = synapseResponse.question_set.created_at.ToString();

                                        questionForDb.Question = question.question;

                                        questionForDb.Choice1Id = question.answers[0].id;
                                        questionForDb.Choice1Text = question.answers[0].answer;

                                        questionForDb.Choice2Id = question.answers[1].id;
                                        questionForDb.Choice2Text = question.answers[1].answer;

                                        questionForDb.Choice3Id = question.answers[2].id;
                                        questionForDb.Choice3Text = question.answers[2].answer;

                                        questionForDb.Choice4Id = question.answers[3].id;
                                        questionForDb.Choice4Text = question.answers[3].answer;

                                        questionForDb.Choice5Id = question.answers[4].id;
                                        questionForDb.Choice5Text = question.answers[4].answer;

                                        _dbContext.SynapseIdVerificationQuestions.Add(questionForDb);
                                        _dbContext.SaveChanges();
                                    }

                                    res.message = "additional questions needed";

                                    #endregion Iterate Through Each Question And Save in DB
                                }

                                #endregion Additional Verification Questions Returned
                            }
                            else if (synapseResponse.user != null)
                            {
                                // User is verified completely. In this case response is same as Register User With Synapse...
                                // Just update permission in CreateSynapseUserResults table
                                #region Update Permission in SynapseCreateUserResults Table

                                try
                                {
                                    // Get existing records from dbo.SynapseCreateUserResults for this Member
                                    var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id &&
                                                                                                             m.IsDeleted == false);

                                    if (synapseRes != null)
                                    {
                                        synapseRes.permission = synapseResponse.user.permission;
                                        synapseRes.physical_doc = synapseResponse.user.doc_status != null ? synapseResponse.user.doc_status.physical_doc : null;
                                        synapseRes.virtual_doc = synapseResponse.user.doc_status != null ? synapseResponse.user.doc_status.virtual_doc : null;
                                        synapseRes.extra_security = synapseResponse.user.extra != null ? synapseResponse.user.extra.extra_security.ToString() : null;

                                        _dbContext.SaveChanges();
                                        _dbContext.Entry(synapseRes).Reload();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
                                                 "[MemberID: " + MemberId + "], [Exception: " + ex + "]");
                                }

                                #endregion Update Permission in CreateSynapseUserResults Table

                                // Update Member's DB record
                                memberEntity.IsVerifiedWithSynapse = true;
                                memberEntity.ValidatedDate = DateTime.Now;
                                memberEntity.DateModified = DateTime.Now;

                                res.message = "complete success";
                            }
                        }
                        else
                        {
                            // Response from Synapse had 'success' != true
                            // SHOULDN'T EVER GET HERE B/C IF SYNAPSE CAN'T VERIFY THE USER, IT RETURNS A 400 BAD REQUEST HTTP ERROR WITH A MESSAGE...SEE WEB EX BELOW
                            Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED: Synapse Result \"success != true\" - [Username: " + userNameDecrypted + "]");
                            res.message = "SSN response from synapse was false";
                        }
                    }
                    else
                    {
                        // Response from Synapse was null
                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED: Synapse Result was NULL - [Username: " + userNameDecrypted + "]");
                        res.message = "SSN response from synapse was null";
                    }

                    #endregion Parse Synapse Response
                }
                catch (WebException we)
                {
                    var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;

                    var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    JObject errorJsonFromSynapse = JObject.Parse(response);

                    // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors
                    //                   We might have to do different things depending on which error is returned... for now just pass
                    //                   back the error number & msg to the function that called this method.
                    string errorMsg = errorJsonFromSynapse["error"]["en"].ToString();

                    if (errorMsg != null)
                    {
                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED (Outer) - [errorCode: " + httpStatusCode.ToString() +
                                     "], [Error Message from Synapse: " + errorMsg + "]");

                        res.message = errorMsg;

                        if (!String.IsNullOrEmpty(errorMsg) &&
                            (errorMsg.IndexOf("Unable to verify") > -1 ||
                             errorMsg.IndexOf("submit a valid copy of passport") > -1))
                        {
                            Logger.Info("**  THIS USER'S SSN INFO WAS NOT VERIFIED AT ALL. MUST INVESTIGATE WHY (COULD BE TYPO WITH PERSONAL INFO). " +
                                        "DETERMINE IF NECESSARY TO ASK FOR DRIVER'S LICENSE.  **");

                            memberEntity.AdminNotes = "SSN INFO WAS INVALID WHEN SENT TO SYNAPSE. NEED TO COLLECT DRIVER'S LICENSE.";

                            // Email Nooch Admin about this user for manual follow-up (Send email to Cliff)
                            #region Notify Nooch Admin About Failed SSN Validation

                            try
                            {
                                StringBuilder st = new StringBuilder();

                                string city = !String.IsNullOrEmpty(memberEntity.City) ? GetDecryptedData(memberEntity.City) : "NONE";

                                st.Append("<table border='1' cellpadding='6' style='border-collapse:collapse;text-align:center;'>" +
                                          "<tr><th>PARAMETER</th><th>VALUE</th></tr>");
                                st.Append("<tr><td><strong>Name</strong></td><td>" + usersFirstName + " " + usersLastName + "</td></tr>");
                                st.Append("<tr><td><strong>MemberId</strong></td><td>" + MemberId + "</td></tr>");
                                st.Append("<tr><td><strong>Nooch_ID</strong></td><td><a href=\"https://noochme.com/noochnewadmin/Member/Detail?NoochId=" + memberEntity.Nooch_ID + "\" target='_blank'>" + memberEntity.Nooch_ID + "</a></td></tr>");
                                st.Append("<tr><td><strong>Status</strong></td><td><strong>" + memberEntity.Status + "</strong></td></tr>");
                                st.Append("<tr><td><strong>DOB</strong></td><td>" + Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MMMM dd, yyyy") + "</td></tr>");
                                st.Append("<tr><td><strong>SSN</strong></td><td>" + usersSsn + "</td></tr>");
                                st.Append("<tr><td><strong>Address</strong></td><td>" + usersAddress + "</td></tr>");
                                st.Append("<tr><td><strong>City</strong></td><td>" + city + "</td></tr>");
                                st.Append("<tr><td><strong>ZIP</strong></td><td>" + usersZip + "</td></tr>");
                                st.Append("<tr><td><strong>Contact #</strong></td><td>" + FormatPhoneNumber(memberEntity.ContactNumber) + "</td></tr>");
                                st.Append("<tr><td><strong>Phone Verified?</strong></td><td>" + memberEntity.IsVerifiedPhone.ToString() + "</td></tr>");
                                st.Append("<tr><td><strong>IsVerifiedWithSynapse</strong></td><td>" + memberEntity.IsVerifiedWithSynapse.ToString() + "</td></tr>");
                                st.Append("</table>");

                                StringBuilder completeEmailTxt = new StringBuilder();
                                string s = "<html><body><h3>Nooch SSN Verification Failure</h3><p style='margin:0 auto 20px;'>The following Nooch user just failed an SSN Verification attempt:</p>"
                                           + st.ToString() +
                                           "<br/><br/><small>This email was generated automatically during <strong>[Common Helper -> sendUserSsnInfoToSynapseV3]</strong>.</small></body></html>";

                                completeEmailTxt.Append(s);
                                Utility.SendEmail(null, "SSNFAILURE@nooch.com", "cliff@nooch.com",
                                                  null, "NOOCH USER'S SSN (V3) VALIDATION FAILED", null, null, null, null, completeEmailTxt.ToString());
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED - Attempted to notify Nooch Admin via email but got Exception: [" + ex + "]");
                            }

                            #endregion Notify Nooch Admin About Failed SSN Validation


                            // Now try to send ID verification document (IF VerificationDoc is AVAILABLE... WHICH IT PROBABLY WON'T BE)
                            if (!String.IsNullOrEmpty(memberEntity.VerificationDocumentPath))
                            {
                                Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - ID Document Path found, so attempting submitDocumentToSynapseV3()");

                                // CC (5/31/16): submitDocumentToSynapseV3 is in MDA... but for some strange reason there is a duplicate in Common Helper
                                //               It should really be only in Common Helper, but the one in MDA is the one actually used right now.
                                //submitDocumentToSynapseV3(memberEntity.MemberId.ToString(), memberEntity.VerificationDocumentPath);
                            }
                        }
                    }
                    else
                    {
                        res.message = "Common Helper Exception #1768";
                    }
                }

                // Save changes to Members DB
                memberEntity.DateModified = DateTime.Now;
                _dbContext.SaveChanges();

                #endregion Send SSN Info To Synapse
            }
            else
            {
                // Member not found in Nooch DB
                Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED: Member not found - [MemberId: " + MemberId + "]");
                res.message = "Member not found";
            }

            return res;
        }


        /// <summary>
        /// For sending all user 'Documents' (Virtual, Physical, Social) to Synapse.
        /// Added 5/31/16 by CC.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        public static submitIdVerificationInt sendDocsToSynapseV3(string MemberId)
        {
            Logger.Info("Common Helper -> sendDocsToSynapseV3 Initiated - [MemberId: " + MemberId + "]");

            submitIdVerificationInt res = new submitIdVerificationInt();
            res.success = false;

            var id = Utility.ConvertToGuid(MemberId);

            var memberEntity = GetMemberDetails(MemberId);

            if (memberEntity != null)
            {
                var userNameDecrypted = GetDecryptedData(memberEntity.UserName);

                // Cliff (6/2/16): commenting out the check if the user already has isVerifiedWithSynapse = true.
                //                 Even verified users might need to send another document, so we might as well
                //                 run this methond fully for any user - unless there is missing data.
                //if (memberEntity.IsVerifiedWithSynapse != true)
                //{
                #region Check User For All Required Data

                string usersFirstName = UppercaseFirst(GetDecryptedData(memberEntity.FirstName));
                string usersLastName = UppercaseFirst(GetDecryptedData(memberEntity.LastName));

                string usersAddress = "";
                string usersCity = "";
                string usersState = "";
                string usersZip = "";

                DateTime usersDob;
                int usersDobDay = 0;
                int usersDobMonth = 0;
                int usersDobYear = 0;

                string usersPhone = "";
                string usersSsn = "";
                string usersFBID = "";
                string usersPhotoIDurl = "";

                bool hasSSN = false;
                bool hasFBID = false;
                bool hasPhotoID = false;

                string usersSynapseOauthKey = "";
                string usersFingerprint = "";
                string ipAddress = GetRecentOrDefaultIPOfMember(id);

                try
                {
                    bool isMissingSomething = false;
                    // Member found, now check that they have added
                    // • full Address (including city, zip),
                    // • SSN  *OR*  FB User ID
                    // • DOB
                    // • Fingerprint (UDID1)

                    // Check for Fingerprint (UDID1 in the database)
                    if (String.IsNullOrEmpty(memberEntity.UDID1))
                    {
                        isMissingSomething = true;
                        res.message = " - Missing UDID";
                    }
                    else
                    {
                        usersFingerprint = memberEntity.UDID1; // (Not Encrypted)
                    }
                    // Check for Address
                    if (String.IsNullOrEmpty(memberEntity.Address))
                    {
                        isMissingSomething = true;
                        res.message += " - Missing Address";
                    }
                    else
                    {
                        usersAddress = GetDecryptedData(memberEntity.Address); // (Encrypted)
                    }
                    // Check for ZIP
                    if (String.IsNullOrEmpty(memberEntity.Zipcode))
                    {
                        isMissingSomething = true;
                        res.message += " - Missing ZIP";
                    }
                    else
                    {
                        usersZip = GetDecryptedData(memberEntity.Zipcode); // (Encrypted)
                    }
                    // Check for City
                    if (String.IsNullOrEmpty(memberEntity.City))
                    {
                        // Missing City, so if user has a ZIP, try getting the City & States from Google
                        if (!String.IsNullOrEmpty(usersZip))
                        {
                            var googleMapsRes = GetCityAndStateFromZip(usersZip);
                            if (googleMapsRes != null && !String.IsNullOrEmpty(googleMapsRes.city))
                            {
                                usersCity = googleMapsRes.city;
                                usersState = googleMapsRes.stateAbbrev;
                            }
                            else
                            {
                                isMissingSomething = true;
                                res.message += " - Missing City";
                            }
                        }
                        else
                        {
                            isMissingSomething = true;
                            res.message += " - Missing City";
                        }
                    }
                    else
                    {
                        usersCity = GetDecryptedData(memberEntity.City); // (Encrypted)
                    }
                    if (String.IsNullOrEmpty(memberEntity.State) && String.IsNullOrEmpty(usersState))
                    {
                        // Missing State, so if use does have a ZIP, try getting the City & States from Google
                        if (!String.IsNullOrEmpty(usersZip))
                        {
                            var googleMapsRes = GetCityAndStateFromZip(usersZip);
                            if (googleMapsRes != null && !String.IsNullOrEmpty(googleMapsRes.stateAbbrev))
                                usersState = googleMapsRes.stateAbbrev;
                        }
                        // isMissingSomething = true; Don't abort if missing state.
                    }
                    else
                    {
                        usersState = GetDecryptedData(memberEntity.State);
                    }
                    // Check for Phone
                    if (string.IsNullOrEmpty(memberEntity.ContactNumber))
                    {
                        isMissingSomething = true;
                        res.message += " - Missing Phone";
                    }
                    else
                    {
                        usersPhone = memberEntity.ContactNumber; // (Not Encrypted)
                    }
                    // Check for Date Of Birth
                    if (memberEntity.DateOfBirth == null)
                    {
                        isMissingSomething = true;
                        res.message += " MDA - Missing Date of Birth";
                    }
                    else
                    {
                        usersDob = Convert.ToDateTime(memberEntity.DateOfBirth); // (Not Encrypted)
                        // We have DOB, now we must parse it into day, month, & year
                        usersDobDay = usersDob.Day;
                        usersDobMonth = usersDob.Month;
                        usersDobYear = usersDob.Year;
                    }

                    // Check for SSN & FBID - need at least 1
                    if (String.IsNullOrEmpty(memberEntity.SSN) && String.IsNullOrEmpty(memberEntity.FacebookUserId))
                    {
                        isMissingSomething = true;
                        res.message += " - Missing SSN and FBID";
                    }
                    if (!String.IsNullOrEmpty(memberEntity.SSN))
                    {
                        usersSsn = GetDecryptedData(memberEntity.SSN); // (Encrypted)
                        hasSSN = true;
                    }
                    if (!String.IsNullOrEmpty(memberEntity.FacebookUserId))
                    {
                        usersFBID = memberEntity.FacebookUserId; // (Not Encrypted)
                        hasFBID = true;
                    }
                    // Now check for ID verification document (Checking the one in the Members Table here -
                    // could also be an ID img in SynapseCreateUserResults table
                    if (!String.IsNullOrEmpty(memberEntity.VerificationDocumentPath))
                    {
                        usersPhotoIDurl = memberEntity.VerificationDocumentPath;
                        hasPhotoID = true;
                    }
                    // Return if any data was missing in previous block
                    if (isMissingSomething)
                    {
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 ABORTED: Member has no DoB. [Username: " + userNameDecrypted + "], [Message: " + res.message + "]");
                        return res;
                    }

                    // Update Member's DB record from NULL to false (update to true later on if Verification from Synapse is completely successful)
                    if (memberEntity.IsVerifiedWithSynapse == null)
                    {
                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - Setting IsVerifiedWithSynapse to FALSE since it was NULL: [Username: " + userNameDecrypted + "]");
                        memberEntity.IsVerifiedWithSynapse = false;
                    }

                    // Now check if user already has a Synapse User account (would have a record in SynapseCreateUserResults.dbo)
                    var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id &&
                                                                                                      m.IsDeleted == false);

                    if (usersSynapseDetails == null)
                    {
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 ABORTED: Member's Synapse User Details not found. [Username: " + userNameDecrypted + "]");
                        res.message = "Users synapse details not found";
                        return res;
                    }
                    else
                    {
                        _dbContext.Entry(usersSynapseDetails).Reload();
                        usersSynapseOauthKey = GetDecryptedData(usersSynapseDetails.access_token);

                        // Now check again for ID verification document, now in SynapseCreateUserResults table
                        if (!String.IsNullOrEmpty(usersSynapseDetails.photos))
                        {
                            Logger.Info("Common Helper -> sendDocsToSynapseV3 - Found Photo in SynapseCreateUserResults Table - PhotoURL: [" + usersSynapseDetails.photos + "]");
                            usersPhotoIDurl = usersSynapseDetails.photos; // Override the img from the Member's table if an img is found here - this one would have been set from the landing pages.
                            hasPhotoID = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED on checking for all required data - [Username: " +
                                  userNameDecrypted + "], [Exception: " + ex + "]");
                }

                Logger.Info("Common Helper -> sendDocsToSynapseV3 - Completed all initial data checks - All Data Found!");

                #endregion Check User For All Required Data


                #region Send All Docs To Synapse

                try
                {
                    #region Call Synapse V3 /user/doc/add API

                    synapseAddDocsV3InputClass synapseAddDocsV3Input = new synapseAddDocsV3InputClass();

                    SynapseV3Input_login login = new SynapseV3Input_login();
                    login.oauth_key = usersSynapseOauthKey;

                    synapseAddDocsV3InputClass_user_docs documents = new synapseAddDocsV3InputClass_user_docs();
                    documents.email = userNameDecrypted;
                    documents.phone_number = memberEntity.ContactNumber;
                    documents.ip = ipAddress;
                    documents.name = usersFirstName + " " + usersLastName;
                    documents.alias = usersFirstName + " " + usersLastName;
                    documents.entity_type = "NOT_KNOWN";
                    documents.entity_scope = memberEntity.isRentScene == true ? "Real Estate" : "Not Known";
                    documents.day = usersDobDay;
                    documents.month = usersDobMonth;
                    documents.year = usersDobYear;
                    documents.address_street = usersAddress;
                    documents.address_city = usersCity;
                    documents.address_subdivision = usersState; // State
                    documents.address_postal_code = usersZip;
                    documents.address_country_code = "US";


                    #region Set All Document Values

                    try
                    {
                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - About to attempt to set Document Values - " +
                                    " hasSSN: [" + hasSSN + "], hasFBID: [" + hasFBID + "], hasPhotoID: [" + hasPhotoID + "], MemberID: [" + MemberId + "]");

                        // VIRTUAL DOCS: Synapse lists 6 acceptable "virtual_docs" types: SSN, PASSPORT #, DRIVERS_LICENSE #, PERSONAL_IDENTIFICATION # (not sure what this is), TIN #, DUNS #
                        //               But we are only going to use SSN. For any business users, we will also need to use TIN (Tax ID) #.
                        synapseAddDocsV3InputClass_user_docs_doc virtualDocObj = new synapseAddDocsV3InputClass_user_docs_doc();
                        virtualDocObj.document_type = "SSN"; // This can also be "PASSPORT" or "DRIVERS_LICENSE"... we need to eventually support all 3 options (Rent Scene has international clients that don't have SSN but do have a Passport)
                        virtualDocObj.document_value = usersSsn; // Can also be the user's Passport # or DL #

                        documents.virtual_docs = new synapseAddDocsV3InputClass_user_docs_doc[1];
                        documents.virtual_docs[0] = virtualDocObj;

                        if (!hasSSN)
                        {
                            // If user has no SSN, still need to send an empty array to Synapse or else we get an error
                            documents.virtual_docs = documents.virtual_docs.Where(val => val.document_value != virtualDocObj.document_value).ToArray();
                        }

                        // SOCIAL DOCS: Send Facebook Profile URL by appending user's FBID to base FB URL
                        synapseAddDocsV3InputClass_user_docs_doc socialDocObj = new synapseAddDocsV3InputClass_user_docs_doc();
                        socialDocObj.document_type = "FACEBOOK";
                        socialDocObj.document_value = "https://www.facebook.com/" + usersFBID;

                        documents.social_docs = new synapseAddDocsV3InputClass_user_docs_doc[1];
                        documents.social_docs[0] = socialDocObj;

                        if (!hasFBID)
                        {
                            // If user has no FBID, still need to send an empty array to Synapse or else we get an error
                            documents.social_docs = documents.social_docs.Where(val => val.document_value != socialDocObj.document_value).ToArray();
                        }

                        // PHYSICAL DOCS: Send User's Photo ID if available
                        if (hasPhotoID)
                        {
                            synapseAddDocsV3InputClass_user_docs_doc physicalDocObj = new synapseAddDocsV3InputClass_user_docs_doc();
                            physicalDocObj.document_type = "GOVT_ID";
                            physicalDocObj.document_value = "data:text/csv;base64," + ConvertImageURLToBase64(usersPhotoIDurl).Replace("\\", "");

                            documents.physical_docs = new synapseAddDocsV3InputClass_user_docs_doc[1];
                            documents.physical_docs[0] = physicalDocObj;
                        }
                        else
                        {
                            synapseAddDocsV3InputClass_user_docs_doc physicalDocObj = new synapseAddDocsV3InputClass_user_docs_doc();
                            physicalDocObj.document_type = "GOVT_ID";
                            physicalDocObj.document_value = "data:text/csv;base64," + ConvertImageURLToBase64(usersPhotoIDurl).Replace("\\", "");

                            documents.physical_docs = new synapseAddDocsV3InputClass_user_docs_doc[1];
                            documents.physical_docs[0] = physicalDocObj;

                            documents.physical_docs = documents.physical_docs.Where(val => val.document_value != physicalDocObj.document_value).ToArray();
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 - Exception while setting Documents - MemberID: [" + MemberId +
                                     "], Exception: [" + ex + "]");
                    }

                    #endregion Set All Document Values


                    synapseAddDocsV3InputClass_user user = new synapseAddDocsV3InputClass_user();
                    user.fingerprint = usersFingerprint;

                    user.documents = new synapseAddDocsV3InputClass_user_docs[1];
                    user.documents[0] = documents;

                    synapseAddDocsV3Input.login = login;
                    synapseAddDocsV3Input.user = user;


                    string baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"))
                                         ? "https://sandbox.synapsepay.com/api/v3/user/docs/add"
                                         : "https://synapsepay.com/api/v3/user/docs/add";


                    #region For Testing & Logging

                    if (memberEntity.MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60") documents.name = "Clifford Satell";

                    try
                    {
                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - About To Query Synapse (/v3/user/docs/add) -> Payload to send to Synapse: [OauthKey: " + login.oauth_key +
                                    "], Name: [" + documents.name + "], Email: [" + documents.email +
                                    "], Phone: [" + documents.phone_number + "], IP: [" + documents.ip +
                                    "], Alias: [" + documents.name + "], Entity_Type: [" + documents.entity_type +
                                    "], Entity_Scope: [" + documents.entity_scope + "], Day: [" + documents.day +
                                    "], Month: [" + documents.month + "], Year: [" + documents.year +
                                    "], address_street: [" + documents.address_street + "], Postal_code: [" + documents.address_postal_code +
                                    "], City: [" + documents.address_city + "], State: [" + documents.address_subdivision +
                                    "], country_code: [" + documents.address_country_code + "], Fingerprint: [" + user.fingerprint +
                                    "], HasSSN?: [" + hasSSN + "], SSN: [" + usersSsn + "], HasFBID?: [" + hasFBID +
                                    "], FBID: " + usersFBID + "], HasPhotoID?: " + hasPhotoID + "], BASE_ADDRESS: [" + baseAddress + "].");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 - Couldn't log Synapse SSN Payload. [Exception: " + ex + "]");
                    }

                    #endregion For Testing & Logging

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(synapseAddDocsV3Input);

                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    addDocsResFromSynapse synapseResponse = JsonConvert.DeserializeObject<addDocsResFromSynapse>(content);

                    #endregion Call Synapse V3 /user/docs/add API


                    // NOW WE MUST PARSE THE SYNAPSE RESPONSE. THERE ARE 3 POSSIBLE SCENARIOS:
                    // 1.) Validation was successful - No further validation needed. Synapse returns {"success": true}
                    // 2.) Validation was PARTLY successful.  Need to do further validation.  Synapse returns: "success":true... 
                    //     plus an object "question_set", containing a series of questions and array of multiple choice answers for each question.
                    //     We will display the questions to the user via the IDVerification page (already built-in to the Add-Bank process)
                    //     NOTE: WITH NEW SYNAPSE METHOD, THE QUESTION_SET ONLY IS RETURNED IF WE SEND THE USER'S SSN. question_set will be 
                    //           in  ["user"]["documents"]["virtual_docs"]["meta"]
                    // 3.) Validation Failed:  Synapse will return HTTP Error 400 Bad Request
                    //     with an "error" object, and then a message in "error.en" that should be: "Invalid SSN information supplied. Request user to submit a copy of passport/divers license and SSN via user/doc/attachments/add"

                    #region Parse Synapse Response

                    if (synapseResponse != null)
                    {
                        JObject refreshResponse = JObject.Parse(content);
                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - SYNAPSE RESPONSE IS: [" + refreshResponse + "]");

                        if (synapseResponse.success == true)
                        {
                            // Great, we have at least partial success

                            #region Update Permission in SynapseCreateUserResults Table

                            try
                            {
                                // Preparing to update values in CreateSynapseUserResults table
                                // Get existing records from dbo.SynapseCreateUserResults for this Member
                                var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id &&
                                                                                                         m.IsDeleted == false);

                                if (synapseRes != null)
                                {
                                    synapseRes.permission = synapseResponse.user.permission;
                                    synapseRes.physical_doc = synapseResponse.user.doc_status != null ? synapseResponse.user.doc_status.physical_doc : null;
                                    synapseRes.virtual_doc = synapseResponse.user.doc_status != null ? synapseResponse.user.doc_status.virtual_doc : null;
                                    synapseRes.extra_security = synapseResponse.user.extra.extra_security != null ? synapseResponse.user.extra.extra_security.ToString() : null;

                                    int save = _dbContext.SaveChanges();
                                    _dbContext.Entry(synapseRes).Reload();

                                    if (save > 0)
                                    {
                                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - SUCCESS response form Synapse - And Successfully updated user's record in SynapseCreateUserRes Table");
                                    }
                                    else
                                    {
                                        Logger.Error("Common Helper -> sendDocsToSynapseV3 - SUCCESS response form Synapse - But FAILED to update user's record in SynapseCreateUserRes Table");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Common Helper -> sendDocsToSynapseV3 - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
                                             "[MemberID: " + MemberId + "], [Exception: " + ex + "]");
                            }

                            #endregion Update Permission in CreateSynapseUserResults Table

                            // Now check if further verification is needed by checking if Synapse returned a 'question_set' object.
                            Logger.Info("Common Helper -> sendDocsToSynapseV3 - Synapse returned SUCCESS = TRUE. Now checking if additional Verification questions are required...");

                            res.success = true;

                            if (synapseResponse.user.documents != null &&
                                synapseResponse.user.documents[0].virtual_docs != null &&
                                synapseResponse.user.documents[0].virtual_docs.Length > 0 &&
                                synapseResponse.user.documents[0].virtual_docs[0].meta != null)
                            {
                                // Further Verification is needed...
                                #region Additional Verification Questions Returned

                                // Now make sure an Array[] set of 'questions' was returned (could be up to 5 questions, each with 5 possible answer choices)
                                if (synapseResponse.user.documents[0].virtual_docs[0].meta.question_set != null)
                                {
                                    Logger.Info("Common Helper -> sendDocsToSynapseV3 - Question_Set was returned, further validation will be needed. Saving ID Verification Questions...");

                                    // Saving these questions in DB.  
                                    // The user will have to answer these on the IDVerification page.
                                    // The Add-Bank page will direct the user either to the IDVerification page (via iFrame), or not if questions are not needed.

                                    // Loop through each question set (question/answers/id)
                                    #region Iterate Through Each Question And Save in DB

                                    addDocsResFromSynapse_user_docs_virtualdoc_meta_qset questionSetObj = synapseResponse.user.documents[0].virtual_docs[0].meta.question_set;

                                    foreach (synapseIdVerificationQuestionAnswerSet question in questionSetObj.questions)
                                    {
                                        SynapseIdVerificationQuestion questionForDb = new SynapseIdVerificationQuestion();
                                        questionForDb.MemberId = id;
                                        questionForDb.QuestionSetId = questionSetObj.id;
                                        questionForDb.DateCreated = DateTime.Now;
                                        questionForDb.submitted = false;

                                        questionForDb.SynpQuestionId = question.id;
                                        questionForDb.Question = question.question;

                                        questionForDb.Choice1Id = question.answers[0].id;
                                        questionForDb.Choice1Text = question.answers[0].answer;

                                        questionForDb.Choice2Id = question.answers[1].id;
                                        questionForDb.Choice2Text = question.answers[1].answer;

                                        questionForDb.Choice3Id = question.answers[2].id;
                                        questionForDb.Choice3Text = question.answers[2].answer;

                                        questionForDb.Choice4Id = question.answers[3].id;
                                        questionForDb.Choice4Text = question.answers[3].answer;

                                        questionForDb.Choice5Id = question.answers[4].id;
                                        questionForDb.Choice5Text = question.answers[4].answer;

                                        _dbContext.SynapseIdVerificationQuestions.Add(questionForDb);
                                        _dbContext.SaveChanges();
                                    }

                                    res.message = "additional questions needed";

                                    #endregion Iterate Through Each Question And Save in DB
                                }
                                else
                                {
                                    res.message = "Server error: [Couldn't find question_set to save]";
                                    Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED - Found 'meta' object in Synapse Response, but missing 'question_set'");
                                }

                                #endregion Additional Verification Questions Returned
                            }
                            else if (synapseResponse.user != null)
                            {
                                // No KBA Questions returned. Response will be the same as Register User With Synapse...

                                // Update Member's DB record 
                                memberEntity.IsVerifiedWithSynapse = true;
                                memberEntity.ValidatedDate = DateTime.Now;
                                memberEntity.DateModified = DateTime.Now;

                                res.message = "complete success";
                            }
                        }
                        else
                        {
                            // Response from Synapse had 'success' != true
                            // SHOULDN'T EVER GET HERE B/C IF SYNAPSE CAN'T VERIFY THE USER, IT RETURNS A 400 BAD REQUEST HTTP ERROR WITH A MESSAGE...SEE WEB EX BELOW
                            Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED: Synapse Result \"success != true\" - [Username: " + userNameDecrypted + "]");
                            res.message = "Add Docs response from synapse was false";
                        }
                    }
                    else
                    {
                        // Response from Synapse was null
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED: Synapse Result was NULL - [Username: " + userNameDecrypted + "]");
                        res.message = "Add Docs response from synapse was null";
                    }

                    #endregion Parse Synapse Response
                }
                catch (WebException we)
                {
                    #region Synapse Error Returned

                    var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;

                    var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    JObject errorJsonFromSynapse = JObject.Parse(response);

                    // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors.
                    //                   We might have to do different things depending on which error is returned (like re-submitting a specific
                    //                   Document Type.  For now just pass back the error number & msg to the function that called this method.
                    string errorMsg = errorJsonFromSynapse["error"]["en"].ToString();

                    if (errorMsg != null)
                    {
                        Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED (Outer) - [errorCode: " + httpStatusCode.ToString() +
                                     "], [Error Message from Synapse: " + errorMsg + "]");

                        res.message = errorMsg;

                        if (!String.IsNullOrEmpty(errorMsg) &&
                            (errorMsg.IndexOf("Unable to verify") > -1 ||
                             errorMsg.IndexOf("submit a valid copy of passport") > -1))
                        {
                            Logger.Info("** THIS USER'S SSN INFO WAS NOT VERIFIED AT ALL. MUST INVESTIGATE WHY (COULD BE TYPO WITH PERSONAL INFO). " +
                                        "DETERMINE IF NECESSARY TO ASK FOR DRIVER'S LICENSE. **");

                            memberEntity.AdminNotes = "SSN INFO WAS INVALID WHEN SENT TO SYNAPSE. NEED TO COLLECT DRIVER'S LICENSE.";

                            // Email Nooch Admin about this user for manual follow-up (Send email to Cliff)
                            #region Notify Nooch Admin About Failed SSN Validation

                            try
                            {
                                StringBuilder st = new StringBuilder();

                                string city = !String.IsNullOrEmpty(memberEntity.City) ? GetDecryptedData(memberEntity.City) : "NONE";

                                st.Append("<table border='1' cellpadding='6' style='border-collapse:collapse;text-align:center;'>" +
                                          "<tr><th>PARAMETER</th><th>VALUE</th></tr>");
                                st.Append("<tr><td><strong>Name</strong></td><td>" + usersFirstName + " " + usersLastName + "</td></tr>");
                                st.Append("<tr><td><strong>MemberId</strong></td><td>" + MemberId + "</td></tr>");
                                st.Append("<tr><td><strong>Nooch_ID</strong></td><td><a href=\"https://noochme.com/noochnewadmin/Member/Detail?NoochId=" + memberEntity.Nooch_ID + "\" target='_blank'>" + memberEntity.Nooch_ID + "</a></td></tr>");
                                st.Append("<tr><td><strong>Status</strong></td><td><strong>" + memberEntity.Status + "</strong></td></tr>");
                                st.Append("<tr><td><strong>DOB</strong></td><td>" + Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MMMM dd, yyyy") + "</td></tr>");
                                st.Append("<tr><td><strong>SSN</strong></td><td>" + usersSsn + "</td></tr>");
                                st.Append("<tr><td><strong>Address</strong></td><td>" + usersAddress + "</td></tr>");
                                st.Append("<tr><td><strong>City</strong></td><td>" + city + "</td></tr>");
                                st.Append("<tr><td><strong>ZIP</strong></td><td>" + usersZip + "</td></tr>");
                                st.Append("<tr><td><strong>Contact #</strong></td><td>" + FormatPhoneNumber(memberEntity.ContactNumber) + "</td></tr>");
                                st.Append("<tr><td><strong>Phone Verified?</strong></td><td>" + memberEntity.IsVerifiedPhone.ToString() + "</td></tr>");
                                st.Append("<tr><td><strong>IsVerifiedWithSynapse</strong></td><td>" + memberEntity.IsVerifiedWithSynapse.ToString() + "</td></tr>");
                                st.Append("</table>");

                                StringBuilder completeEmailTxt = new StringBuilder();
                                string s = "<html><body><h3>Nooch SSN Verification Failure (V3)</h3><p style='margin:0 auto 20px;'>The following Nooch user just failed an SSN Verification attempt:</p>"
                                           + st.ToString() +
                                           "<br/><br/><small>This email was generated automatically during <strong>[Common Helper -> sendUserSsnInfoToSynapseV3]</strong>.</small></body></html>";

                                completeEmailTxt.Append(s);
                                Utility.SendEmail(null, "SSNFAILURE@nooch.com", "cliff@nooch.com",
                                                  null, "NOOCH USER'S SSN (V3) VALIDATION FAILED", null, null, null, null, completeEmailTxt.ToString());
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Common Helper -> sendDocsToSynapseV3 FAILED - Attempted to notify Nooch Admin via email but got Exception: [" + ex + "]");
                            }

                            #endregion Notify Nooch Admin About Failed SSN Validation


                            // Now try to send ID verification document (IF VerificationDoc is AVAILABLE... WHICH IT PROBABLY WON'T BE)
                            if (!String.IsNullOrEmpty(memberEntity.VerificationDocumentPath))
                            {
                                Logger.Info("Common Helper -> sendDocsToSynapseV3 - ID Document Path found, so attempting submitDocumentToSynapseV3()");

                                // CC (5/31/16): submitDocumentToSynapseV3 is in MDA... but for some strange reason there is a duplicate in Common Helper
                                //               It should really be only in Common Helper, but the one in MDA is the one actually used right now.
                                //submitDocumentToSynapseV3(memberEntity.MemberId.ToString(), memberEntity.VerificationDocumentPath);
                            }
                        }
                    }
                    else
                    {
                        res.message = "Common Helper Exception #1660";
                    }
                }

                    #endregion Synapse Error Returned

                // Save changes to Members DB
                memberEntity.DateModified = DateTime.Now;
                int saveMember = _dbContext.SaveChanges();

                if (saveMember > 0)
                {
                    Logger.Info("Common Helper -> sendDocsToSynapseV3 - Successfully updated user's record in Members Table");
                }
                else
                {
                    Logger.Error("Common Helper -> sendDocsToSynapseV3 - FAILED to update user's record in Members Table");
                }

                #endregion Send All Docs To Synapse
                //}
                //else
                //{
                //    var validatedDate = Convert.ToDateTime(memberEntity.ValidatedDate).ToString("MMM dd yyyy");
                //    Logger.Info("Common Helper -> sendDocsToSynapseV3 - User Already Verified With Synapse - [Username: " + userNameDecrypted +
                //                "], [Validated On: " + validatedDate + "]");
                //    res.message = "Already Verified on " + validatedDate;
                //    res.success = true;
                //}
            }
            else
            {
                // Member not found in Nooch DB
                Logger.Info("Common Helper -> sendDocsToSynapseV3 FAILED: Member not found - [MemberId: " + MemberId + "]");
                res.message = "Member not found";
            }

            return res;
        }


        /************************************************************************************************************************/
        //
        // CC (5/31/16): submitDocumentToSynapseV3 is in MDA... but for some strange reason there is a duplicate in Common Helper
        //               It should really be only in Common Helper, but the one in MDA is the one actually used right now.
        //
        /************************************************************************************************************************/
        public static GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
        {
            Logger.Info("Common Helper -> submitDocumentToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            var id = Utility.ConvertToGuid(MemberId);

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();
            res.success = false;

            #region Get User's Synapse OAuth Consumer Key

            string usersSynapseOauthKey = "";

            var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

            if (usersSynapseDetails == null)
            {
                Logger.Error("Common Helper -> submitDocumentToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                res.message = "Could not find this member's account info";
                return res;
            }
            else
            {
                //_dbContext.Entry(usersSynapseDetails).Reload();
                usersSynapseOauthKey = GetDecryptedData(usersSynapseDetails.access_token);
            }

            #endregion Get User's Synapse OAuth Consumer Key

            #region Get User's Fingerprint

            string usersFingerprint = "";

            var member = GetMemberDetails(id.ToString());

            if (member == null)
            {
                Logger.Error("Common Helper -> submitDocumentToSynapseV3 ABORTED: Member not found. [MemberId: " + MemberId + "]");
                res.message = "Member not found";
                return res;
            }
            else
            {
                if (String.IsNullOrEmpty(member.UDID1))
                {
                    Logger.Error("Common Helper -> submitDocumentToSynapseV3 ABORTED: Member's Fingerprint not found. [MemberId: " + MemberId + "]");
                    res.message = "Could not find this member's fingerprint";
                    return res;
                }
                else
                {
                    usersFingerprint = member.UDID1;
                }
            }

            #endregion Get User's Fingerprint


            #region Call Synapse /user/doc/attachments/add API

            try
            {
                submitDocToSynapseV3Class submitDocObj = new submitDocToSynapseV3Class();

                SynapseV3Input_login login = new SynapseV3Input_login();
                login.oauth_key = usersSynapseOauthKey;
                submitDocObj.login = login;

                submitDocToSynapse_user user = new submitDocToSynapse_user();
                submitDocToSynapse_user_doc doc = new submitDocToSynapse_user_doc();
                doc.attachment = "data:text/csv;base64," + ConvertImageURLToBase64(ImageUrl).Replace("\\", "");

                user.fingerprint = usersFingerprint;
                user.doc = doc;

                submitDocObj.user = user;

                string baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/doc/attachments/add" : "https://synapsepay.com/api/v3/user/doc/attachments/add";

                var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(submitDocObj);
                ASCIIEncoding encoding = new ASCIIEncoding();
                Byte[] bytes = encoding.GetBytes(parsedContent);

                Stream newStream = http.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                var response = http.GetResponse();
                var stream = response.GetResponseStream();
                var sr = new StreamReader(stream);
                var content = sr.ReadToEnd();

                kycInfoResponseFromSynapse resFromSynapse = new kycInfoResponseFromSynapse();

                resFromSynapse = JsonConvert.DeserializeObject<kycInfoResponseFromSynapse>(content);

                if (resFromSynapse != null)
                {
                    if (resFromSynapse.success == true || resFromSynapse.success.ToString().ToLower() == "true")
                    {
                        var permission = resFromSynapse.user.permission != null ? resFromSynapse.user.permission : "NOT FOUND";

                        res.success = true;
                        res.message = "Permission is: [" + permission + "]";

                        // Update User's "Permission" in SynapseCreateUserResults Table
                        if (permission != "NOT FOUND")
                        {
                            usersSynapseDetails.permission = permission;
                            usersSynapseDetails.photos = ImageUrl;
                            usersSynapseDetails.physical_doc = resFromSynapse.user.doc_status != null ? resFromSynapse.user.doc_status.physical_doc : null;
                            usersSynapseDetails.virtual_doc = resFromSynapse.user.doc_status != null ? resFromSynapse.user.doc_status.virtual_doc : null;
                            usersSynapseDetails.extra_security = resFromSynapse.user.extra != null ? resFromSynapse.user.extra.extra_security.ToString() : null;

                            Logger.Info("Common Helper -> submitDocumentToSynapseV3 SUCCESSFUL - Permission: [" + permission +
                                        "], Virtual_Doc: [" + usersSynapseDetails.virtual_doc + "], Physical_Doc: [" + usersSynapseDetails.physical_doc +
                                        "], [MemberID: " + MemberId + "]");

                            int save = _dbContext.SaveChanges();
                            _dbContext.Entry(usersSynapseDetails).Reload();

                            // Now update users IsVerifiedWithSynapse value if response's permission = "SEND-AND-RECEIVE"
                            if (member.IsVerifiedWithSynapse != true && permission == "SEND-AND-RECEIVE")
                            {
                                Logger.Info("Common Helper-> SubmitDocumentToSynapseV3 - User's IsVerifiedWithSynapse was not true, " +
                                            "but Permission from was 'SEND-AND-RECEIVE', so updating Member's record in DB - [MemberID: " + MemberId + "]");

                                var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                                memberObj.IsVerifiedWithSynapse = true;
                                memberObj.TransferLimit = "5000";
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        res.message = "Got a response, but success was not true";
                        Logger.Error("Common Helper -> submitDocumentToSynapseV3 FAILED - Got Synapse response, but success was NOT 'true' - [MemberID: " + MemberId + "]");
                    }
                }
                else
                {
                    res.message = "Verification response was null";
                    Logger.Error("Common Helper -> submitDocumentToSynapseV3 FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");
                }
            }
            catch (WebException we)
            {
                var errorCode = ((HttpWebResponse)we.Response).StatusCode;
                var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                JObject jsonFromSynapse = JObject.Parse(resp);

                Logger.Error("Common Helper -> submitDocumentToSynapseV3 FAILED - HTTP ErrorCode: [" + errorCode.ToString() + "], WebException was: [" + we.Message + "]");
                Logger.Error(jsonFromSynapse.ToString());

                var error_code = jsonFromSynapse["error_code"].ToString();
                res.message = jsonFromSynapse["error"]["en"].ToString();

                if (!String.IsNullOrEmpty(error_code))
                {
                    Logger.Error("Common Helper -> submitDocumentToSynapseV3 FAILED - [Synapse Error Code: " + error_code + "]");
                }

                if (!String.IsNullOrEmpty(res.message))
                {
                    // Synapse Error could be:
                    // "Incorrect oauth_key/fingerprint"
                    Logger.Error("Common Helper -> submitDocumentToSynapseV3 FAILED. Synapse Error Msg was: [" + res.message + "]");
                }
                else
                {
                    res.message = "Error in Synapse response - #1889";
                }
            }

            #endregion Call Synapse /user/doc/attachments/add API

            return res;
        }


        public static void ResetSearchData()
        {
            SEARCHUSER_CURRENT_PAGE = 1;
            SEARCHUSER_TOTAL_PAGES_COUNT = 0;
            SEARCHED_USERS.Clear();
        }

        // Malkit (17 May 2016) : Added these flags to keep track of pagination result being sent by synapse after hitting search url.
        static int SEARCHUSER_CURRENT_PAGE = 1;
        static int SEARCHUSER_TOTAL_PAGES_COUNT = 0;
        static List<synapseSearchUserResponse_User> SEARCHED_USERS = new List<synapseSearchUserResponse_User>();

        public static synapseSearchUserResponse getUserPermissionsForSynapseV3(string userEmail)
        {
            Logger.Info("Common Helper -> getUserPermissionsForSynapseV3 Initiated - [Email: " + userEmail + "]");

            synapseSearchUserResponse res = new synapseSearchUserResponse();
            res.success = false;

            try
            {
                String memberId = GetMemberIdByUserName(userEmail);
                List<string> clientIds = getClientSecretId(memberId);

                string SynapseClientId = clientIds[0];
                string SynapseClientSecret = clientIds[1];
                synapseSearchUserInputClass input = new synapseSearchUserInputClass();

                synapseSearchUser_Client client = new synapseSearchUser_Client();
                client.client_id = SynapseClientId;
                client.client_secret = SynapseClientSecret;

                synapseSearchUser_Filter filter = new synapseSearchUser_Filter();
                filter.page = SEARCHUSER_CURRENT_PAGE;
                filter.exact_match = true; // we might want to set this to false to prevent error due to capitalization mis-match... (or make sure we only send all lowercase email when creating a Synapse user)
                filter.query = userEmail;

                input.client = client;
                input.filter = filter;

                string UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/search"
                                                                                                      : "https://synapsepay.com/api/v3/user/search";

                Logger.Info("Common Helper -> getUserPermissionsForSynapseV3 - About to query Synapse's /user/search API - [UrlToHit: " + UrlToHit + "]");

                var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(input);
                ASCIIEncoding encoding = new ASCIIEncoding();
                Byte[] bytes = encoding.GetBytes(parsedContent);

                Stream newStream = http.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                try
                {
                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    JObject checkPermissionResponse = JObject.Parse(content);

                    if (checkPermissionResponse["success"] != null &&
                        Convert.ToBoolean(checkPermissionResponse["success"]) == true)
                    {
                        //Logger.Info("Common Helper -> getUserPermissionsForSynapseV3 - JSON Result from Synapse: [" + checkPermissionResponse + "]");
                        res = JsonConvert.DeserializeObject<synapseSearchUserResponse>(content);

                        if (res.page != res.page_count || res.page == res.page_count)
                        {
                            if (SEARCHUSER_CURRENT_PAGE == 1)
                            {
                                SEARCHED_USERS = res.users.ToList<synapseSearchUserResponse_User>();
                            }
                            else
                            {
                                List<synapseSearchUserResponse_User> temp = res.users.ToList<synapseSearchUserResponse_User>();
                                SEARCHED_USERS.AddRange(temp);
                            }

                            // Cliff (5/17/16): In theory SEACHUSER_CURRENT_PAGE and res.page should always be the same...
                            SEARCHUSER_CURRENT_PAGE = res.page + 1;
                            SEARCHUSER_TOTAL_PAGES_COUNT = res.page_count;

                            // If there are more pages left, loop back into this same method (I assume this is safe to do?)
                            if (res.page < res.page_count)
                                getUserPermissionsForSynapseV3(userEmail);
                        }
                    }
                    else
                    {
                        Logger.Error("Common Helper -> getUserPermissionsForSynapseV3 FAILED - Got response from Synapse /user/search, but 'success' was null or not 'true'");
                        res.error_code = "Service error.";
                    }
                }
                catch (WebException we)
                {
                    #region Synapse V3 Get User Permissions Exception

                    var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;
                    res.http_code = httpStatusCode.ToString();

                    var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    JObject errorJsonFromSynapse = JObject.Parse(response);

                    // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors
                    //                   We might have to do different things depending on which error is returned... for now just pass
                    //                   back the error number & msg to the function that called this method.
                    res.error_code = errorJsonFromSynapse["error_code"].ToString();
                    res.errorMsg = errorJsonFromSynapse["error"]["en"].ToString();

                    if (!String.IsNullOrEmpty(res.error_code))
                    {
                        Logger.Error("Common Helper -> getUserPermissionsForSynapseV3 FAILED - [Synapse Error Code: " + res.error_code +
                                     "], [Error Msg: " + res.errorMsg + "], [User Email: " + userEmail + "]");
                    }
                    else
                    {
                        Logger.Error("Common Helper -> getUserPermissionsForSynapseV3 FAILED: Synapse Error, but *error_code* was null for [User Email: " +
                                     userEmail + "], [Exception: " + we.InnerException + "]");
                    }

                    #endregion Synapse V3 Get User Permissions Exception
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> getUserPermissionsForSynapseV3 FAILED: Outer Catch Error - [User Email: " + userEmail +
                             "], [Exception: " + ex.InnerException + "]");

                res.error_code = "Nooch Server Error: Outer Exception.";
            }

            res.users = SEARCHED_USERS.ToArray();

            return res;
        }


        /// <summary>
        /// For checking if one bank within a set of Synapse V3 banks has an "allowed" value.
        /// </summary>
        /// <param name="allNodes"></param>
        /// <param name="nodeOid">MUST BE UN-ENCRYPTED!</param>
        /// <returns></returns>
        public static NodePermissionCheckResult IsNodeAllowedInGivenSetOfNodes(synapseSearchUserResponse_Node[] allNodes, string nodeOid)
        {
            NodePermissionCheckResult res = new NodePermissionCheckResult();
            res.IsPermissionfound = false;

            try
            {
                foreach (synapseSearchUserResponse_Node node in allNodes)
                {
                    if (node._id != null && node._id.oid.Trim() == nodeOid.Trim())
                    {
                        if (!String.IsNullOrEmpty(node.allowed))
                        {
                            res.IsPermissionfound = true;
                            res.PermissionType = node.allowed;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> IsNodeActiveInGivenSetOfNodes FAILED - [NodeToMatch: " + nodeOid + "], Exception: [" + ex.Message + "]");
            }

            Logger.Info("Common Helper -> IsNodeActiveInGivenSetOfNodes - About to return - IsPermissionFound: [" + res.IsPermissionfound +
                        "], Permission: [" + res.PermissionType + "], NodeOID Checked: [" + nodeOid + "]");
            return res;
        }


        public static SynapseDetailsClass GetSynapseBankAndUserDetailsforGivenMemberId(string memberId)
        {
            SynapseDetailsClass res = new SynapseDetailsClass();
            res.wereUserDetailsFound = false;
            res.wereBankDetailsFound = false;

            Logger.Info("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId Fired - MemberID: [" + memberId + "]");

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                // Full Member Table Details
                Member memberObject = GetMemberDetails(memberId);

                // Check Synapse USER details from Nooch's DB
                #region Get Users Synapse USER Details

                var createSynapseUserObj = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (createSynapseUserObj != null && !String.IsNullOrEmpty(createSynapseUserObj.access_token))
                {
                    res.wereUserDetailsFound = true;

                    Logger.Info("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId - Checkpoint #1 - " +
                                "SynapseCreateUserResults Record Found - MemberID: [" + memberId + "], Access_Token: [" + createSynapseUserObj.access_token + "] - Now about to check if Synapse OAuth Key is still valid.");

                    // CLIFF (10/3/15): ADDING CALL TO NEW METHOD TO CHECK USER'S STATUS WITH SYNAPSE, AND REFRESHING OAUTH KEY IF NECESSARY
                    synapseV3checkUsersOauthKey checkTokenResult = refreshSynapseV3OautKey(createSynapseUserObj.access_token);

                    if (checkTokenResult != null)
                    {
                        if (checkTokenResult.success == true)
                        {
                            res.UserDetails = new SynapseDetailsClass_UserDetails();
                            res.UserDetails.MemberId = memberId;
                            res.UserDetails.access_token = GetDecryptedData(checkTokenResult.oauth_consumer_key); // Note: Giving in encrypted format
                            res.UserDetails.user_id = checkTokenResult.user_oid;
                            res.UserDetails.user_fingerprints = memberObject.UDID1;
                            res.UserDetails.permission = createSynapseUserObj.permission;
                            res.UserDetailsErrMessage = "OK";
                        }
                        else
                        {
                            Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                         "CheckTokenResult.msg: [" + checkTokenResult.msg + "], MemberID: [" + memberId + "]");
                            res.UserDetailsErrMessage = checkTokenResult.msg;
                            return res;
                        }
                    }
                    else
                    {
                        Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                     "CheckTokenResult was NULL, MemberID: [" + memberId + "]");
                        res.UserDetailsErrMessage = "Unable to check user's Oauth Token";
                    }
                }
                else
                {
                    Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - Unable to find Synapse " +
                                 "USER Details - MemberID: [" + memberId + "] - CONTINUING ON TO CHECK FOR SYNAPSE BANK...");
                    res.UserDetails = null;
                    res.UserDetailsErrMessage = "User synapse details not found.";
                }

                #endregion Get Users Synapse USER Details


                #region Get Users Synapse Bank Details

                var defaultBank = _dbContext.SynapseBanksOfMembers.FirstOrDefault(m => m.MemberId == id &&
                                                                                       m.IsDefault == true);

                if (defaultBank != null && !String.IsNullOrEmpty(defaultBank.oid))
                {
                    //Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId - CHECKPOINT#1 - Bank ID: [" + defaultBank.Id.ToString() +
                    //             "], Allowed: [" + defaultBank.allowed + "]");

                    // Found a Synapse bank account for this user
                    res.wereBankDetailsFound = true;
                    res.BankDetails = new SynapseDetailsClass_BankDetails();
                    res.BankDetails.AddedOn = defaultBank.AddedOn;
                    res.BankDetails.Status = defaultBank.Status; // "Verfified" or "Not Verified"

                    // Cliff (5/13/16): several other methods use this value which was from Synapse V2, so just udpating it to be the OID so nothing should break elsewhere :-)
                    res.BankDetails.bankid = GetDecryptedData(defaultBank.oid);
                    res.BankDetails.bank_oid = GetDecryptedData(defaultBank.oid);
                    res.BankDetails.allowed = defaultBank.allowed;
                    res.BankDetails.bankType = defaultBank.type_bank;
                    res.BankDetails.synapseType = defaultBank.type_synapse;
                    res.BankDetails.dateVerified = defaultBank.Status == "Verified" && defaultBank.VerifiedOn != null
                                                   ? Convert.ToDateTime(defaultBank.VerifiedOn).ToString("MMM D YYYY") : "n/a";
                    res.AccountDetailsErrMessage = "OK";
                }
                else
                {
                    Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - Unable to find Synapse " +
                                 "BANK Details - MemberID: [" + memberId + "]");
                    res.BankDetails = null;
                    res.AccountDetailsErrMessage = "User synapse bank not found.";
                }

                #endregion Get The User's Synapse Bank Details
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - MemberID: [" + memberId + "], Outer Exception: [" + ex + "]");
            }

            return res;
        }


        // oAuth token needs to be in encrypted format
        public static synapseV3checkUsersOauthKey refreshSynapseV3OautKey(string oauthKey)
        {
            Logger.Info("Common Helper -> refreshSynapseV3OautKey Initiated - User's Original OAuth Key (enc): [" + oauthKey + "]");

            synapseV3checkUsersOauthKey res = new synapseV3checkUsersOauthKey();
            res.success = false;

            try
            {
                SynapseCreateUserResult synCreateUserObject = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.access_token == oauthKey && m.IsDeleted == false);

                // Will be calling login/refresh access token service to confirm if saved oAtuh token matches with token coming in response, if not then will update the token.
                if (synCreateUserObject != null)
                {
                    _dbContext.Entry(synCreateUserObject).Reload();

                    var noochMemberObject = GetMemberDetails(synCreateUserObject.MemberId.ToString());

                    #region Found Refresh Token

                    //Logger.Info("Common Helper -> refreshSynapseV3OautKey - Found Member By Original OAuth Key");

                    SynapseV3RefreshOauthKeyAndSign_Input input = new SynapseV3RefreshOauthKeyAndSign_Input();

                    List<string> clientIds = getClientSecretId(noochMemberObject.MemberId.ToString());

                    string SynapseClientId = clientIds[0];
                    string SynapseClientSecret = clientIds[1];

                    input.login = new createUser_login2()
                    {
                        email = GetDecryptedData(noochMemberObject.UserName),
                        refresh_token = GetDecryptedData(synCreateUserObject.refresh_token)
                    };

                    input.client = new createUser_client()
                    {
                        client_id = SynapseClientId,
                        client_secret = SynapseClientSecret
                    };

                    SynapseV3RefreshOAuthToken_User_Input user = new SynapseV3RefreshOAuthToken_User_Input();

                    user._id = new synapseSearchUserResponse_Id1()
                    {
                        oid = synCreateUserObject.user_id
                    };
                    user.fingerprint = noochMemberObject.UDID1;

                    user.ip = GetRecentOrDefaultIPOfMember(noochMemberObject.MemberId);

                    input.user = user;

                    string UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/signin" : "https://synapsepay.com/api/v3/user/signin";

                    Logger.Info("Common Helper -> refreshSynapseV3OautKey - Payload to send to Synapse /v3/user/signin: [" + JsonConvert.SerializeObject(input) + "]");

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(input);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    try
                    {
                        var response = http.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        synapseCreateUserV3Result_int refreshResultFromSyn = new synapseCreateUserV3Result_int();
                        refreshResultFromSyn = JsonConvert.DeserializeObject<synapseCreateUserV3Result_int>(content);

                        JObject refreshResponse = JObject.Parse(content);

                        //Logger.Info("Common Helper -> synapseV3checkUsersOauthKey - Just Parsed Synapse Response: [" + refreshResponse + "]");

                        #region Signed Into Synapse Successfully

                        if ((refreshResponse["success"] != null && Convert.ToBoolean(refreshResponse["success"])) ||
                             refreshResultFromSyn.success == true)
                        {
                            //Logger.Info("Common Helper -> synapseV3checkUsersOauthKey - Signed User In With Synapse Successfully!");

                            // Check if Token from Synapse /user/signin is same as the one we already have saved in DB for this suer
                            if (synCreateUserObject.access_token == GetEncryptedData(refreshResultFromSyn.oauth.oauth_key))
                            {
                                res.success = true;
                                Logger.Info("Common Helper -> refreshSynapseV3OautKey - Access_Token from Synapse MATCHES whats in DB");
                            }
                            else // New Access Token...
                            {
                                Logger.Info("Common Helper -> refreshSynapseV3OautKey - Access_Token from Synapse is NEW");
                            }

                            // Update all values no matter what, even if access_token hasn't changed - possible one of the other values did
                            synCreateUserObject.access_token = GetEncryptedData(refreshResultFromSyn.oauth.oauth_key);
                            synCreateUserObject.refresh_token = GetEncryptedData(refreshResultFromSyn.oauth.refresh_token);
                            synCreateUserObject.expires_in = refreshResultFromSyn.oauth.expires_in;
                            synCreateUserObject.expires_at = refreshResultFromSyn.oauth.expires_at;
                            synCreateUserObject.physical_doc = refreshResultFromSyn.user.doc_status != null ? refreshResultFromSyn.user.doc_status.physical_doc : null;
                            synCreateUserObject.virtual_doc = refreshResultFromSyn.user.doc_status != null ? refreshResultFromSyn.user.doc_status.virtual_doc : null;
                            synCreateUserObject.extra_security = refreshResultFromSyn.user.extra != null ? refreshResultFromSyn.user.extra.extra_security.ToString() : null;

                            if (!String.IsNullOrEmpty(refreshResultFromSyn.user.permission))
                            {
                                synCreateUserObject.permission = refreshResultFromSyn.user.permission;
                            }

                            int save = _dbContext.SaveChanges();
                            _dbContext.Entry(synCreateUserObject).Reload();

                            if (save > 0)
                            {
                                Logger.Info("Common Helper -> refreshSynapseV3OautKey - SUCCESS From Synapse & saved updates to DB.");

                                res.success = true;
                                res.oauth_consumer_key = synCreateUserObject.access_token;
                                res.oauth_refresh_token = synCreateUserObject.refresh_token;
                                res.user_oid = synCreateUserObject.user_id;
                                res.msg = "Oauth key refreshed successfully";
                            }
                            else
                            {
                                Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Error saving key in Nooch DB - " +
                                             "Orig. Oauth Key: [" + oauthKey + "], " +
                                             "Refreshed OAuth Key: [" + synCreateUserObject.access_token + "]");
                                res.msg = "Failed to save new OAuth key in Nooch DB.";
                            }
                        }

                        #endregion Signed Into Synapse Successfully

                        else if (refreshResponse["success"] != null && Convert.ToBoolean(refreshResponse["success"]) == false)
                        {
                            if (refreshResponse["error"] != null && refreshResponse["error"]["en"] != null)
                            {
                                res.msg = refreshResponse["error"]["en"].ToString();

                                Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Synapse Error Msg: [" + res.msg + "]");

                                if (res.msg.ToLower().IndexOf("fingerprint not verified") > -1)
                                {
                                    // USER'S FINGERPRINT MUST HAVE CHANGED SINCE THE USER WAS ORIGINALLY CREATED (WHICH IS BAD AND UNLIKELY, BUT STILL POSSIBLE)
                                    // NEED TO CALL THE NEW SERVICE FOR HANDLING Synapse 2FA and generating a new Fingerprint (NOT BUILT YET)

                                    // Make sure the Phone # given by Synapse matches what we have for this user in the DB
                                    if (refreshResponse["phone_numbers"][0] != null)
                                    {
                                        var synapsePhone = RemovePhoneNumberFormatting(refreshResponse["phone_numbers"][0].ToString());
                                        var usersPhoneinDB = RemovePhoneNumberFormatting(noochMemberObject.ContactNumber);

                                        if (synapsePhone == usersPhoneinDB)
                                        {
                                            // Good, phone #'s matched - proceed with 2FA process
                                            Logger.Info("Common Helper -> refreshSynapseV3OautKey - About to attempt 2FA process by querying SynapseV3SignIn()");

                                            // Return response from 2nd Signin attempt w/ phone number (should trigger Synapse to send a PIN to the user)
                                            return SynapseV3SignIn(oauthKey, noochMemberObject, null);
                                        }
                                        else
                                        {
                                            // Bad - Synapse has a different phone # than we do for this user,
                                            // which means it probably changed since we created the user with Synapse...
                                            res.msg = "Phone number from Synapse doesn't match Nooch phone number";
                                            Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Phone # Array returned from Synapse - " +
                                                         "But didn't match user's ContactNumber in DB - Can't attempt 2FA flow - ABORTING");
                                        }
                                    }
                                    else
                                    {
                                        res.msg = "Phone number not found from synapse";
                                        Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - No Phone # Array returned from Synapse - " +
                                                     "Can't attempt 2FA flow - ABORTING");
                                    }
                                }
                                else
                                {
                                    res.msg = "Error from Synapse, but didn't includ 'fingerprint not verified'";
                                    Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Synapse Returned Error other than - " +
                                                 "'fingerprint not verified' - Can't attempt 2FA flow - ABORTING");
                                }
                            }
                        }
                        else
                        {
                            Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Attempted to Sign user into Synapse, but got " +
                                         "error from Synapse service, no 'success' key found - Orig. Oauth Key: [" + oauthKey + "]");
                            res.msg = "Service error.";
                        }
                    }
                    catch (WebException we)
                    {
                        #region Synapse V3 Signin Exception

                        var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;
                        string http_code = httpStatusCode.ToString();

                        var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(response);

                        var error_code = jsonFromSynapse["error_code"].ToString();
                        res.msg = jsonFromSynapse["error"]["en"].ToString();

                        if (!String.IsNullOrEmpty(error_code))
                        {
                            Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED (Exception)- [Synapse Error Code: " + error_code +
                                         "], [Error Msg: " + res.msg + "]");
                        }

                        if (!String.IsNullOrEmpty(res.msg))
                        {
                            Logger.Error("Common Helper -> synapseV3checkUsersOauthKey FAILED (Exception) - HTTP Code: [" + http_code +
                                         "], Error Msg: [" + res.msg + "]");
                        }
                        else
                        {
                            Logger.Error("Common Helper -> synapseV3checkUsersOauthKey FAILED (Exception) - Synapse Error msg was null or not found - [Original Oauth Key (enc): " +
                                         oauthKey + "], [Exception: " + we.Message + "]");
                        }

                        #endregion Synapse V3 Signin Exception
                    }

                    #endregion
                }
                else
                {
                    // no record found for given oAuth token in synapse createuser results table
                    Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - no record found for given oAuth key found - " +
                                 "Orig. Oauth Key: (enc) [" + oauthKey + "]");
                    res.msg = "Service error - no record found for give OAuth Key.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED: Outer Catch Error - Orig. OAuth Key (enc): [" + oauthKey +
                             "], [Exception: " + ex + "]");

                res.msg = "Nooch Server Error: Outer Exception #2326.";
            }

            return res;
        }


        // Method to change user fingerprint
        // this required user's member id and new fingerprint
        // from member id, we will get synapse id and password if any given
        // UPDATE (Cliff - 5/31/16): This will be almost exactly the same as the above refreshSynapseOautKey()
        //                           This will ONLY be called from that method when the 1st attempt at signing in fails.
        //                           When that happens, Synapse returns an array of phone numbers for that user (should only ever be 1 in our case),
        //                           then the user is supposed to "pick" which # to verify. But we'll skip that and assume it's the only 1 in the array.
        //                           So then we query the /user/signin API again, this time with the phone number included.  Synapse then sends a code to the user's phone via SMS.
        //                           Then the user must enter that code (I'll make the interface) and we submit it to Synapse using the same API: /user/signin.
        // oAuth token needs to be in encrypted format
        public static synapseV3checkUsersOauthKey SynapseV3SignIn(string oauthKey, Member memberObj, string validationPin)
        {
            bool isPinIncluded = false;
            if (String.IsNullOrEmpty(validationPin))
            {
                Logger.Info("Common Helper -> SynapseV3SignIn Initiated - Oauth Key (enc): [" + oauthKey + "] - No ValidationPIN Passed.");
            }
            else
            {
                isPinIncluded = true;
                Logger.Info("Common Helper -> SynapseV3SignIn Initiated - Submitting Validation PIN: [" + validationPin + "], Oauth Key (enc): [" + oauthKey + "]");
            }

            synapseV3checkUsersOauthKey res = new synapseV3checkUsersOauthKey();
            res.success = false;
            res.is2FA = false;

            #region Initial Data Checks

            if (String.IsNullOrEmpty(oauthKey))
            {
                Logger.Error("Common Helper -> SynapseV3SignIn FAILED - Missing Oauth Key - Oauth Key: [" + oauthKey + "]");
                res.msg = "Missing Oauth Key";
                return res;
            }
            if (memberObj == null)
            {
                Logger.Error("Common Helper -> SynapseV3SignIn FAILED - Missing MemberObj - Oauth Key: [" + oauthKey + "]");
                res.msg = "Missing Member to Signin";
                return res;
            }
            else if (String.IsNullOrEmpty(memberObj.ContactNumber))
            {
                Logger.Error("Common Helper -> SynapseV3SignIn FAILED - No Phone Number Found for this User - MemberID: [" + memberObj.MemberId.ToString() + "]");
                res.msg = "User is Missing a Phone Number";
                return res;
            }

            #endregion Initial Data Checks

            try
            {
                SynapseCreateUserResult synCreateUserObject = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.access_token == oauthKey && m.IsDeleted == false);

                if (synCreateUserObject != null)
                {
                    #region Found Synapse User In DB

                    _dbContext.Entry(synCreateUserObject).Reload();

                    Logger.Info("Common Helper -> SynapseV3SignIn - Found Member By Original OAuth Key");

                    List<string> clientIds = getClientSecretId(memberObj.MemberId.ToString());

                    string SynapseClientId = clientIds[0];
                    string SynapseClientSecret = clientIds[1];

                    var client = new createUser_client()
                    {
                        client_id = SynapseClientId,
                        client_secret = SynapseClientSecret
                    };

                    var login = new createUser_login2()
                    {
                        email = GetDecryptedData(memberObj.UserName),
                        refresh_token = GetDecryptedData(synCreateUserObject.refresh_token)
                    };

                    // Cliff (5/31/16): Have to do it this way because using 1 class causes a problem with Synapse because
                    //                  it doesn't like a NULL value for Validation_PIN if it's not there.  Maybe I'm doing it wrong though...
                    var inputNoPin = new SynapseV3Signin_InputNoPin();
                    var inputWithPin = new SynapseV3Signin_InputWithPin();

                    if (isPinIncluded)
                    {
                        SynapseV3Signin_Input_UserWithPin user = new SynapseV3Signin_Input_UserWithPin();

                        user._id = new synapseSearchUserResponse_Id1()
                        {
                            oid = synCreateUserObject.user_id
                        };
                        user.fingerprint = memberObj.UDID1; // This would be the "new" fingerprint for the user - it's already been saved in the DB for this user
                        user.ip = GetRecentOrDefaultIPOfMember(memberObj.MemberId);
                        user.phone_number = RemovePhoneNumberFormatting(memberObj.ContactNumber); // Inluding the user's Phone #
                        user.validation_pin = validationPin;

                        inputWithPin.client = client;
                        inputWithPin.login = login;
                        inputWithPin.user = user;
                    }
                    else
                    {
                        SynapseV3Signin_Input_UserNoPin user = new SynapseV3Signin_Input_UserNoPin();

                        user._id = new synapseSearchUserResponse_Id1()
                        {
                            oid = synCreateUserObject.user_id
                        };
                        user.fingerprint = memberObj.UDID1; // This would be the "new" fingerprint for the user - it's already been saved in the DB for this user
                        user.ip = GetRecentOrDefaultIPOfMember(memberObj.MemberId);
                        user.phone_number = RemovePhoneNumberFormatting(memberObj.ContactNumber); // Inluding the user's Phone #

                        inputNoPin.user = user;
                        inputNoPin.client = client;
                        inputNoPin.login = login;
                    }

                    string UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/signin" : "https://synapsepay.com/api/v3/user/signin";
                    string parsedContent = isPinIncluded ? JsonConvert.SerializeObject(inputWithPin) : JsonConvert.SerializeObject(inputNoPin);

                    Logger.Info("Common Helper -> SynapseV3SignIn - isPinIncluded: [" + isPinIncluded + "] - Payload to send to Synapse /v3/user/signin: [" + parsedContent + "]");

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    try
                    {
                        var response = http.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        synapseCreateUserV3Result_int refreshResultFromSyn = new synapseCreateUserV3Result_int();
                        refreshResultFromSyn = JsonConvert.DeserializeObject<synapseCreateUserV3Result_int>(content);

                        JObject refreshResponse = JObject.Parse(content);

                        Logger.Info("Common Helper -> SynapseV3SignIn - Synapse Response: HTTP_CODE: [" + refreshResponse["http_code"] +
                                    "], Success: [" + refreshResponse["success"] + "]");

                        if ((refreshResponse["success"] != null && Convert.ToBoolean(refreshResponse["success"])) ||
                             refreshResultFromSyn.success.ToString() == "true")
                        {
                            Logger.Info("Common Helper -> SynapseV3SignIn - Signed User In With Synapse Successfully - Oauth Key: [" +
                                        oauthKey + "] - Checking Synapse Message...");

                            #region Response That PIN Was Sent To User's Phone

                            if (refreshResponse["message"] != null && refreshResponse["message"]["en"] != null)
                            {
                                res.msg = refreshResponse["message"]["en"].ToString();
                                res.is2FA = true;

                                Logger.Info("Common Helper -> SynapseV3SignIn - Synapse Message: [" + res.msg + "]");

                                if (res.msg.ToLower().IndexOf("validation pin sent") > -1)
                                {
                                    res.msg = "Validation PIN sent to: " + FormatPhoneNumber(memberObj.ContactNumber);
                                    res.success = true;
                                }

                                return res;
                            }

                            #endregion Response That PIN Was Sent To User's Phone


                            #region Response With Full Signin Information

                            // Check if Token from Synapse /user/signin is same as the one we already have saved in DB for this suer
                            if (synCreateUserObject.access_token == GetEncryptedData(refreshResultFromSyn.oauth.oauth_key))
                            {
                                res.success = true;
                                Logger.Info("Common Helper -> SynapseV3SignIn - Access_Token from Synapse MATCHES what we already had in DB.");
                            }
                            else // New Access Token...
                            {
                                Logger.Info("Common Helper -> SynapseV3SignIn - Access_Token from Synapse DOES NOT MATCH what we already had in DB, updating with New value.");
                            }

                            // Update all values no matter what, even if access_token hasn't changed - possible one of the other values did
                            synCreateUserObject.access_token = GetEncryptedData(refreshResultFromSyn.oauth.oauth_key);
                            synCreateUserObject.refresh_token = GetEncryptedData(refreshResultFromSyn.oauth.refresh_token);
                            synCreateUserObject.expires_in = refreshResultFromSyn.oauth.expires_in;
                            synCreateUserObject.expires_at = refreshResultFromSyn.oauth.expires_at;
                            synCreateUserObject.physical_doc = refreshResultFromSyn.user.doc_status != null ? refreshResultFromSyn.user.doc_status.physical_doc : null;
                            synCreateUserObject.virtual_doc = refreshResultFromSyn.user.doc_status != null ? refreshResultFromSyn.user.doc_status.virtual_doc : null;
                            synCreateUserObject.extra_security = refreshResultFromSyn.user.extra != null ? refreshResultFromSyn.user.extra.extra_security.ToString() : null;

                            if (!String.IsNullOrEmpty(refreshResultFromSyn.user.permission))
                            {
                                synCreateUserObject.permission = refreshResultFromSyn.user.permission;
                            }

                            int save = _dbContext.SaveChanges();
                            _dbContext.Entry(synCreateUserObject).Reload();

                            if (save > 0)
                            {
                                Logger.Info("Common Helper -> SynapseV3SignIn - SUCCESS From Synapse and Successfully saved updates to Nooch DB.");

                                res.success = true;
                                res.oauth_consumer_key = synCreateUserObject.access_token;
                                res.oauth_refresh_token = synCreateUserObject.refresh_token;
                                res.user_oid = synCreateUserObject.user_id;
                                res.msg = "Oauth key refreshed successfully";
                            }
                            else
                            {
                                Logger.Error("Common Helper -> SynapseV3SignIn FAILED - Error saving new key in Nooch DB - " +
                                             "Orig. Oauth Key: [" + oauthKey + "], " +
                                             "Refreshed OAuth Key: [" + synCreateUserObject.access_token + "]");
                                res.msg = "Failed to save new OAuth key in Nooch DB.";
                            }

                            #endregion Response With Full Signin Information
                        }
                        else if (refreshResponse["success"] != null && Convert.ToBoolean(refreshResponse["success"]) == false)
                        {
                            if (refreshResponse["error"] != null && refreshResponse["error"]["en"] != null)
                            {
                                res.msg = refreshResponse["error"]["en"].ToString();

                                Logger.Error("Common Helper -> SynapseV3SignIn FAILED - Synapse Error Msg: [" + res.msg + "]");

                                if (res.msg.ToLower().IndexOf("fingerprint not verified") > -1)
                                {
                                    // USER'S FINGERPRINT MUST HAVE CHANGED SINCE THE USER WAS ORIGINALLY CREATED (WHICH IS BAD AND UNLIKELY, BUT STILL POSSIBLE)
                                    // NEED TO CALL THE NEW SERVICE FOR HANDLING Synapse 2FA and generating a new Fingerprint (NOT BUILT YET)
                                }
                            }
                        }
                        else
                        {
                            Logger.Error("Common Helper -> SynapseV3SignIn FAILED - Attempted to Sign user into Synapse, but got " +
                                         "error from Synapse service, no 'success' key found - Orig. Oauth Key: [" + oauthKey + "]");
                            res.msg = "Service error.";
                        }
                    }
                    catch (WebException we)
                    {
                        #region Synapse V3 Signin Exception

                        var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;
                        string http_code = httpStatusCode.ToString();

                        var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(response);

                        var error_code = jsonFromSynapse["error_code"].ToString();
                        res.msg = jsonFromSynapse["error"]["en"].ToString();

                        if (!String.IsNullOrEmpty(error_code))
                        {
                            Logger.Error("Common Helper -> SynapseV3SignIn FAILED (Exception)- [Synapse Error Code: " + error_code +
                                         "], [Error Msg: " + res.msg + "]");
                        }

                        if (!String.IsNullOrEmpty(res.msg))
                        {
                            Logger.Error("Common Helper -> SynapseV3SignIn FAILED (Exception) - HTTP Code: [" + http_code +
                                         "], Error Msg: [" + res.msg + "]");
                        }
                        else
                        {
                            Logger.Error("Common Helper -> SynapseV3SignIn FAILED (Exception) - Synapse Error msg was null or not found - [Original Oauth Key (enc): " +
                                         oauthKey + "], [Exception: " + we.Message + "]");
                        }

                        #endregion Synapse V3 Signin Exception
                    }

                    #endregion Found Synapse User In DB
                }
                else
                {
                    // No record found for given oAuth token in SynapseCreateUserResults table
                    Logger.Error("Common Helper -> SynapseV3SignIn FAILED - no record found for given oAuth key found - Orig. Oauth Key: (enc) [" + oauthKey + "]");
                    res.msg = "Service error - no record found for give OAuth Key.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> SynapseV3SignIn FAILED: Outer Catch Error - Orig. OAuth Key (enc): [" + oauthKey + "], [Exception: " + ex + "]");
                res.msg = "Nooch Server Error: Outer Exception #3330.";
            }

            return res;
        }


        public static SynapseBankSetDefaultResult SetSynapseDefaultBank(string MemberId, string BankName, string BankOId)
        {
            Logger.Info("Common Helper -> SetSynapseDefaultBank Fired - MemberId: [" + MemberId + "], Bank Name: [" +
                        BankName + "], BankOId: [" + BankOId + "]");

            SynapseBankSetDefaultResult res = new SynapseBankSetDefaultResult();
            res.Is_success = false;

            #region Check Query Data

            if (String.IsNullOrEmpty(MemberId) ||
                //String.IsNullOrEmpty(BankName) ||
                String.IsNullOrEmpty(BankOId))
            {
                //if (String.IsNullOrEmpty(BankName))
                //{
                //    res.Message = "Invalid data - need Bank Name";
                //}
                if (String.IsNullOrEmpty(MemberId))
                {
                    res.Message = "Invalid data - need MemberId";
                }
                else if (String.IsNullOrEmpty(BankOId))
                {
                    res.Message = "Invalid data - need Bank Id";
                }

                Logger.Error("Common Helper -> SetSynapseDefaultBank ERROR: [" + res.Message + "] for MemberId: [" + MemberId + "]");
                return res;
            }

            #endregion Check Query Data

            else
            {
                // Get Nooch UserName (primary email address) from MemberId
                var noochUserName = GetMemberUsernameByMemberId(MemberId);
                var MemberInfoInNoochDb = GetMemberDetails(MemberId);

                #region Member Found

                if (MemberInfoInNoochDb != null)
                {
                    #region Find the bank to be set as Default

                    string bankNameEncrypted = GetEncryptedData(BankName);
                    string bankOId = GetEncryptedData(BankOId);

                    var banksFound = _dbContext.SynapseBanksOfMembers.Where(bank =>
                                        bank.MemberId == MemberInfoInNoochDb.MemberId &&
                                            //memberTemp.bank_name == bankNameEncrypted &&
                                        bank.oid == bankOId).ToList();

                    // CLIFF (10/7/15): ADDING THIS CODE TO MAKE SURE WE SELECT THE *MOST RECENT* BANK (b/c it creates problems when a user
                    //                  re-attaches the same bank... it has the same ID from Synapse, there may be more than one match)
                    //                  So take the one that was added most recently.
                    var selectedBank = (from c in banksFound select c)
                                      .OrderByDescending(bank => bank.AddedOn)
                                      .Take(1)
                                      .SingleOrDefault();

                    #endregion Find the bank to be set as Default

                    if (selectedBank != null)
                    {
                        // An existing Bank was found, now mark all banks for this user as inactive
                        SetOtherBanksInactiveForGivenMemberId(MemberInfoInNoochDb.MemberId);

                        selectedBank.IsDefault = true;

                        // CLIFF (7/13/15): Before we set the Bank's Status, we need to compare the user's Nooch info (name, email, phone, & maybe address)
                        // with the info that Synapse returned for this specific bank.  The problem is that sometimes Synapse will return NULL for 1 or more
                        // pieces of data (for example, for my current Default bank account (PNC), Synapse returned no name, no email, and no phone.
                        // We can only send the Verification email IF SYNAPSE RETURNED AN EMAIL for that bankId.
                        // HERE'S THE LOGIC:
                        // 1.) If name, email, phone (strip out punctuation) all match, then automatically mark this bank's status as "Verified".
                        // 2.) Otherwise (i.e. No match, OR null values from Synapse), mark this bank's status as "Not Verified".
                        // 3.) Check if Synapse returned any Email Address for the bankId.  If YES, send Verification Email to THAT email (NOT the user's Nooch email)
                        // 4.) If NO email returned from Synapse, then send the secondary Bank Verification Email.
                        //     This will tell the user they must send Nooch any photo ID that matches the name on the bank.
                        //     Then I will have to manually update the bank's status to "Verified" (Need to add a button for this on the Member Details page in the Admin Dash).

                        // UPDATE (5/21/16) CLIFF: Synapse no longer passes phone or email from the bank b/c each back is different and there was too much
                        //                         inconsistency.

                        #region Check, Parse, & Compare Name from Bank Account

                        string noochEmailAddress = GetDecryptedData(MemberInfoInNoochDb.UserName).ToLower();
                        string noochPhoneNumber = RemovePhoneNumberFormatting(MemberInfoInNoochDb.ContactNumber);
                        string noochFirstName = GetDecryptedData(MemberInfoInNoochDb.FirstName).ToLower();
                        string noochLastName = GetDecryptedData(MemberInfoInNoochDb.LastName).ToLower();
                        string noochFullName = noochFirstName + " " + noochLastName;

                        var fullNameFromBank = selectedBank.name_on_account != null ? selectedBank.name_on_account : "";
                        string firstNameFromBank = "";
                        string lastNameFromBank = "";

                        bool bankIncludedName = false;
                        bool nameMatchedExactly = false;
                        bool lastNameMatched = false;

                        var bankAllowed = selectedBank.allowed != null ? selectedBank.allowed : "";

                        if (!String.IsNullOrEmpty(fullNameFromBank))
                        {
                            bankIncludedName = true;

                            // Name was included with Bank, now decrypt it to compare with User's Name
                            fullNameFromBank = GetDecryptedData(fullNameFromBank).ToLower();

                            #region Parse Name

                            // Parse & compare NAME from Nooch account w/ NAME from this bank account
                            string[] nameFromBank_splitUp = fullNameFromBank.Split(' ');

                            if (nameFromBank_splitUp.Length == 1)
                            {
                                lastNameFromBank = nameFromBank_splitUp[0];
                            }
                            else if (nameFromBank_splitUp.Length == 2)
                            {
                                firstNameFromBank = nameFromBank_splitUp[0];
                                lastNameFromBank = nameFromBank_splitUp[1];
                            }
                            else if (nameFromBank_splitUp.Length >= 3)
                            {
                                firstNameFromBank = nameFromBank_splitUp[0];
                                // Take the last string in the array and set as Last Name From Bank (So, if a bank name was "John W. Smith", this would make 'Smith' the last name)
                                lastNameFromBank = nameFromBank_splitUp[(nameFromBank_splitUp.Length - 1)];
                            }

                            #endregion Parse Name

                            #region Compare Name

                            int fullNameCompare = noochFullName.IndexOf(fullNameFromBank);  // Does Nooch FULL name contain FULL name from bank?
                            int fullNameCompare2 = fullNameFromBank.IndexOf(noochFullName); // Does FULL name from bank contain Nooch FULL name?
                            int firstNameCompare = fullNameFromBank.IndexOf(noochFirstName);// Does FULL name from bank contain Nooch FIRST name?
                            int lastNameCompare = fullNameFromBank.IndexOf(noochLastName);  // Does FULL name from bank contain Nooch LAST name?
                            int lastNameCompare2 = noochFullName.IndexOf(lastNameFromBank); // Does FULL Nooch name contain LAST name from bank?

                            if (noochFullName == fullNameFromBank || fullNameCompare > -1 || fullNameCompare2 > -1) // Name matches exactly
                            {
                                nameMatchedExactly = true;
                                lastNameMatched = true;
                            }
                            else if (noochLastName == lastNameFromBank || lastNameCompare > -1 || lastNameCompare2 > -1)
                            {
                                // This would be when the bank name is not an exact full match, but the last names match...
                                // Ex.: "Bob Smith" in Nooch vs. "Robert Smith" or "Bob A. Smith" or even "Smith, Bob A." from the bank
                                // ... the full names don't match exactly, but the last names do match, so that's better than no match at all
                                lastNameMatched = true;
                            }
                            else if (noochFirstName == firstNameFromBank || firstNameCompare > -1)
                            {
                                // This would be when the bank name is not an exact full match, and the last names also did not match, but bank name includes Nooch first name
                                // This is very weak though, and could be true by accident if it's a common first name.  So may not use this as evidence of anything, just checking.
                                // Ex.: "Clifford Smith" in Nooch vs. "Clifford S. Johnson" or "Smith, Clifford S." from the bank
                                //firstNameMatched = true;
                            }

                            #endregion Compare Name
                        }

                        #endregion Check, Parse, & Compare Name from Bank Account


                        #region Set Bank Logo URL Variable for Either Email Template

                        string appPath = Utility.GetValueFromConfig("ApplicationURL");
                        var bankLogoUrl = "";

                        switch (BankName)
                        {
                            case "Ally":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/ally.png");
                                }
                                break;
                            case "Bank of America":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/bankofamerica.png");
                                }
                                break;
                            case "Wells Fargo":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/WellsFargo.png");
                                }
                                break;
                            case "Chase":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/chase.png");
                                }
                                break;
                            case "Citibank":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/citibank.png");
                                }
                                break;
                            case "TD Bank":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/td.png");
                                }
                                break;
                            case "Capital One 360":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/capone360.png");
                                }
                                break;
                            case "US Bank":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/usbank.png");
                                }
                                break;
                            case "PNC":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/pnc.png");
                                }
                                break;
                            case "SunTrust":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/suntrust.png");
                                }
                                break;
                            case "USAA":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/usaa.png");
                                }
                                break;

                            case "First Tennessee":
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/firsttennessee.png");
                                }
                                break;
                            default:
                                {
                                    bankLogoUrl = String.Concat(appPath, "Assets/Images/bankPictures/no.png");
                                }
                                break;
                        }

                        #endregion Set Bank Logo URL Variable for Either Email Template


                        #region Scenarios for Immediately VERIFYING this bank account

                        // Cliff (5/21/16): Re-doing the rules for when a Synapse bank should be automatically verified b/c Synapse V3
                        //                  doesn't return any phone or email info from the bank.  So need to check if the user's SSN
                        //                  was verified successfully ('IsVerifiedWithSynapse'), and compare any name returned from the bank.
                        if (MemberInfoInNoochDb.IsVerifiedWithSynapse == true && bankAllowed == "CREDIT-AND-DEBIT")
                        {
                            selectedBank.Status = "Verified";
                            selectedBank.VerifiedOn = DateTime.Now;

                            Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank VERIFIED (Case 1) - [Names Matched Exactly: " + nameMatchedExactly +
                                        "], [Bank Included Name: " + bankIncludedName + "], [Name From Bank: " + fullNameFromBank +
                                        "], [Last Name Match: " + lastNameMatched + "], [Allowed: " + bankAllowed + "], [- MemberId: [" + MemberId +
                                        "]; BankName: [" + BankName + "]");
                        }

                        #endregion Scenarios for Immediately VERIFYING this bank account


                        #region Scenarios Where Further Verification Needed

                        #region Non-Verified Scenario 1 - Email Included From Bank - NO LONGER USED W/ SYNAPSE V3

                        // Non-Verifed Scenario #1: Some email was included from bank, so send Bank Verification Email Template #1 (With Link)
                        /*else if (bankIncludedEmail && emailFromBank.Length > 4)
                        {
                            // Bank included some Email which is at least 5 characters long (so not just a dummy letter that the bank might have)
                            // First, Mark as NOT Verified
                            selectedBank.Status = "Not Verified";

                            // Now send Bank Verification Email (with Link) to the EMAIL FROM THE BANK ACCOUNT
                            #region Send Verify Bank Email TO EMAIL FROM THE BANK

                            var toAddress = "";//emailFromBank;
                            var fromAddress = Utility.GetValueFromConfig("adminMail");

                            var firstNameForEmail = String.IsNullOrEmpty(firstNameFromBank)
                                                    ? ""
                                                    : " " + UppercaseFirst(firstNameFromBank); // Adding the extra space at the beginning of the FirstName for the Email template: So it's either "Hi," or "Hi John,"
                            var fullNameFromBankTitleCase = UppercaseFirst(firstNameFromBank) + " " +
                                                            UppercaseFirst(lastNameFromBank);
                            var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                    "Nooch/BankVerification?tokenId=" + GetEncryptedData(selectedBank.Id.ToString()));

                            var tokens = new Dictionary<string, string>
                                            {
                                                {Constants.PLACEHOLDER_FIRST_NAME, firstNameForEmail},
                                                {Constants.PLACEHOLDER_BANK_NAME, BankName},
                                                {Constants.PLACEHOLDER_RECIPIENT_FULL_NAME, fullNameFromBankTitleCase},
                                                {Constants.PLACEHOLDER_Recepient_Email, ""},//emailFromBank},
                                                {Constants.PLACEHOLDER_BANK_BALANCE, bankLogoUrl},
                                                {Constants.PLACEHOLDER_OTHER_LINK, link}
                                            };

                            Utility.SendEmail("bankEmailVerification", fromAddress, toAddress, null,
                                              "Your bank account was added to Nooch - Please Verify", null,
                                              tokens, null, "bankAdded@nooch.com", null);

                            Logger.Info("Common Helper -> SetSynapseDefaultBank --> Bank Verification w/ Link Email sent to: [" +
                                        toAddress + "] for Nooch Username: [" + noochUserName + "]");

                            #endregion Send Verify Bank Email TO EMAIL FROM THE BANK
                        }*/

                        #endregion Non-Verified Scenario 1 - Email Included From Bank - NO LONGER USED W/ SYNAPSE V3

                        #region NonVerified Scanario 2 - Email NOT included from bank

                        // Send Bank Verification Email Template #2 (No Link)
                        else
                        {
                            selectedBank.Status = "Not Verified";

                            Logger.Info("SetSynapseDefaultBank -> Bank NOT Verified (Case 6) -  MemberId: [" + MemberId +
                                        "]; BankName: [" + BankName + "]; bankIncludedName: [" + bankIncludedName +
                                        "]; nameMatchedExactly: [" + nameMatchedExactly + "]; lastNameMatched: [" + lastNameMatched +
                                        "]; nameFromBank: [" + fullNameFromBank + "]");

                            // If the User's ID was verified successfully, then don't send the Verfication email.  But keep the bank as "not verified" until a Nooch admin reviews
                            if (MemberInfoInNoochDb.IsVerifiedWithSynapse == true)
                            {
                                Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank was not verified, but user's SSN verification was successful, so " +
                                            "NOT sending the Verification Email - [MemberID: " + MemberId + "], [Username: " + noochEmailAddress + "]");

                                StringBuilder st = new StringBuilder("<br/><p><strong>This user's Nooch Account information is:</strong></p>" +
                                          "<table border='1' cellpadding='3' style='border-collapse:collapse;'>" +
                                          "<tr><td><strong>MemberID:</strong></td><td>" + MemberId + "</td></tr>" +
                                          "<tr><td><strongNooch_ID:</strong></td><td>" + MemberInfoInNoochDb.Nooch_ID + "</td></tr>" +
                                          "<tr><td><strong>Nooch Name:</strong></td><td>" + noochFullName + "</td></tr>" +
                                          "<tr><td><strong>Bank Included Name?:</strong></td><td>" + bankIncludedName + "</td></tr>" +
                                          "<tr><td><strong>Name From Bank:</strong></td><td>" + fullNameFromBank + "</td></tr>" +
                                          "<tr><td><strong>nameMatchedExactly:</strong></td><td>" + nameMatchedExactly + "</td></tr>" +
                                          "<tr><td><strong>lastNameMatched:</strong></td><td>" + lastNameMatched + "</td></tr>" +
                                          "<tr><td><strong>Nooch Email Address:</strong></td><td>" + noochEmailAddress + "</td></tr>" +
                                          "<tr><td><strong>Nooch Phone #:</strong></td><td>" + MemberInfoInNoochDb.ContactNumber + "</td></tr>" +
                                          "<tr><td><strong>Address:</strong></td><td>" + GetDecryptedData(MemberInfoInNoochDb.Address) +
                                          "</td></tr></table><br/><br/>- Nooch Bot</body></html>");

                                // Notify Nooch Admin
                                StringBuilder completeEmailTxt = new StringBuilder();
                                string s = "<html><body><h2>Non-Verified Syanpse Bank Account</h2><p>The following Nooch user just attached a Synapse bank account, which was unable " +
                                           "to be verified because the bank did not return an email address, BUT this user's SSN info was verified successfully with Synapse.</p>" +
                                           "<p>The bank account has beem marked \"Not Verified\" and is now awaiting Admin verification:</p>"
                                           + st.ToString() +
                                           "<br/><br/><small>This email was generated automatically in [Common Helper -> SetSynapseBankDefault -> Bank Not Verified (Case 6).</small></body></html>";

                                completeEmailTxt.Append(s);

                                Utility.SendEmail(null, "admin-autonotify@nooch.com", "bankAdded@nooch.com", null,
                                                  "Nooch Admin Alert: Bank Added, Awaiting Admin Approval",
                                                  null, null, null, null, completeEmailTxt.ToString());
                            }
                            else
                            {
                                // SEND VERIFICATION EMAIL to the Nooch user's userName (email address).
                                // User will have to provide alternative documentation (i.e. Driver's License) to verify their account.
                                #region Send Verify Bank Email to Nooch username

                                var toAddress = noochUserName;
                                var fromAddress = Utility.GetValueFromConfig("adminMail");

                                var tokens = new Dictionary<string, string>
                                            {
                                                {Constants.PLACEHOLDER_FIRST_NAME, UppercaseFirst(noochFirstName)},
                                                {Constants.PLACEHOLDER_BANK_NAME, BankName},
                                                {Constants.PLACEHOLDER_BANK_BALANCE, bankLogoUrl}
                                            };

                                try
                                {
                                    Utility.SendEmail("bankEmailVerificationNoLink",
                                        fromAddress, toAddress, null,
                                        "Your bank account was added to Nooch - Additional Verification Needed",
                                        null, tokens, null, "bankAdded@nooch.com", null);

                                    Logger.Info("Common Helper -> SetSynapseDefaultBank --> Bank Verification No Link Email sent to: [" +
                                                toAddress + "] for Nooch Username: [" + noochUserName + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Common Helper -> SetSynapseDefaultBank --> Bank Verification No Link Email NOT sent to [" +
                                                 toAddress + "] for Nooch Username: [" + noochUserName + "]; Exception: [" + ex.Message + "]");
                                }

                                #endregion Send Verify Bank Email to Nooch username
                            }
                        }

                        #endregion NonVerified Scanario 2 - Email NOT included from bank

                        #endregion Scenarios Where Further Verification Needed

                        // FINALLY, UPDATE THIS BANK IN NOOCH DB
                        try
                        {
                            if (_dbContext.SaveChanges() > 0)
                            {
                                res.Message = "Success";
                                res.Is_success = true;
                                _dbContext.Entry(MemberInfoInNoochDb).Reload();
                            }
                            else
                            {
                                Logger.Error("Commong Helper -> SetSynapseDefaultBank - FAILED while trying to save new bank in DB - Error #2751");
                                res.Message = "Server Error: Unable to save bank - #2751";
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Commong Helper -> SetSynapseDefaultBank - FAILED while trying to save new bank in DB - Exception: [" + ex.Message + "]");
                            res.Message = "Server Error: Unable to save bank - [" + ex.Message + "]";
                        }
                    }
                    else
                    {
                        Logger.Error("Common Helper -> SetSynapseDefaultBank ERROR: Selected Bank not found in Nooch DB - MemberId: [" + MemberId + "]; BankId: [" + BankOId + "]");
                        res.Message = "Bank not found for given Member";
                    }
                }

                #endregion Member Found

                else
                {
                    Logger.Error("Common Helper -> SetSynapseDefaultBank ERROR: Member not found in Nooch DB - MemberId: [" + MemberId + "]; BankId: [" + BankOId + "]");
                    res.Message = "Member not found";
                }
            }

            return res;
        }


        private static void SetOtherBanksInactiveForGivenMemberId(Guid memId)
        {
            var existingBanks = _dbContext.SynapseBanksOfMembers.Where(bank => bank.MemberId.Value.Equals(memId) &&
                                                                               bank.IsDefault != false).ToList();

            foreach (SynapseBanksOfMember sbank in existingBanks)
            {
                sbank.IsDefault = false;
                _dbContext.SaveChanges();
            }
        }


        public static DbContext GetDbContextFromEntity(object entity)
        {
            var object_context = GetObjectContextFromEntity(entity);

            if (object_context == null)
                return null;

            return new DbContext(object_context, false);
        }


        private static ObjectContext GetObjectContextFromEntity(object entity)
        {
            var field = entity.GetType().GetField("_entityWrapper");

            if (field == null)
                return null;

            var wrapper = field.GetValue(entity);
            var property = wrapper.GetType().GetProperty("Context");
            var context = (ObjectContext)property.GetValue(wrapper, null);

            return context;
        }


        public static string UpdateMemberIPAddressAndDeviceId(string MemberId, string IP, string DeviceId)
        {
            //Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId Initiated - MemberID: [" + MemberId + "], IP: [" + IP + "], DeviceID: [" + DeviceId + "]");

            if (String.IsNullOrEmpty(MemberId))
            {
                return "MemberId not supplied.";
            }

            Guid memId = Utility.ConvertToGuid(MemberId);

            bool ipSavedSuccessfully = false;
            bool udidIdSavedSuccessfully = false;

            #region Save IP Address

            if (!String.IsNullOrEmpty(IP))
            {
                try
                {
                    var ipAddressesFound = _dbContext.MembersIPAddresses.Where(memberTemp => memberTemp.MemberId.Value.Equals(memId)).ToList();

                    if (ipAddressesFound.Count > 5)
                    {
                        // If there are already 5 entries, update the one added first (the oldest)
                        var lastIpFound = (from c in ipAddressesFound select c)
                                          .OrderBy(m => m.ModifiedOn)
                                          .Take(1)
                                          .SingleOrDefault();

                        lastIpFound.ModifiedOn = DateTime.Now;
                        lastIpFound.Ip = IP;

                        int a = _dbContext.SaveChanges();

                        if (a > 0)
                        {
                            _dbContext.Entry(lastIpFound).Reload();
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS - IP Address Saved (1) - MemberID: [" + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Error("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - MemberID: [" + MemberId + "]");
                        }
                    }
                    else
                    {
                        // Otherwise, make a new entry
                        MembersIPAddress mip = new MembersIPAddress();
                        mip.MemberId = memId;
                        mip.ModifiedOn = DateTime.Now;
                        mip.Ip = IP;

                        _dbContext.MembersIPAddresses.Add(mip);

                        int b = _dbContext.SaveChanges();

                        if (b > 0)
                        {
                            _dbContext.Entry(mip).Reload();
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS - IP Address Saved (2) - MemberID: [" + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Error("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - MemberID: [" + MemberId + "]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - Exception: [" + ex + "]");
                }
            }
            else
            {
                Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - MemberID: [" + MemberId + "]");
            }

            #endregion Save IP Address


            #region Save Device ID

            if (!String.IsNullOrEmpty(DeviceId))
            {
                try
                {
                    // CLIFF (8/12/15): This "Device ID" will be stored in Nooch's DB as "UDID1" and is specifically for Synapse's "Fingerprint" requirement...
                    //                  NOT for push notifications, which should use the "DeviceToken" in Nooch's DB.  (Confusing, but they are different values)

                    var member = _dbContext.Members.FirstOrDefault(memberTemp => memberTemp.MemberId == memId);
                    if (member != null)
                    {
                        member.UDID1 = DeviceId;
                        member.DateModified = DateTime.Now;
                    }
                    int c = _dbContext.SaveChanges();

                    if (c > 0)
                    {
                        _dbContext.Entry(member).Reload();
                        Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS - Device ID Saved - MemberID: [" + MemberId + "]");
                        udidIdSavedSuccessfully = true;
                    }
                    else
                    {
                        Logger.Error("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - MemberID: [" + MemberId + "]");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - Exception: [" + ex + "]");
                }
            }
            else
            {
                Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - MemberID: [" + MemberId + "]");
            }

            #endregion Save Device ID

            if (ipSavedSuccessfully && udidIdSavedSuccessfully)
            {
                return "Both IP and DeviceID saved successfully.";
            }
            else if (ipSavedSuccessfully)
            {
                return "Only IP address saved successfully, not DeviceID.";
            }
            else if (udidIdSavedSuccessfully)
            {
                return "Only DeviceID saved successfully, not IP Address.";
            }

            return "Neither IP address nor DeviceID were saved.";
        }


        public static string GetMemberIdByContactNumber(string userContactNumber)
        {
            Logger.Info("Common Helper -> GetMemberIdByContactNumber Initiated - ContactNumber: [" + userContactNumber + "]");

            string trimmedContactNum = RemovePhoneNumberFormatting(userContactNumber);

            var noochMember = _dbContext.Members.Where(memberTemp =>
                                memberTemp.ContactNumber.Equals(trimmedContactNum) &&
                                memberTemp.IsDeleted == false).FirstOrDefault();

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                return noochMember.MemberId.ToString();
            }

            return null;
        }


        public static Member GetMemberByContactNumber(string contactNumber)
        {
            Logger.Info("Common Helper -> GetMemberByContactNumber Initiated - contactNumber: [" + contactNumber + "]");

            string trimmedContactNum = RemovePhoneNumberFormatting(contactNumber);

            var noochMember = _dbContext.Members.Where(memberTemp =>
                                memberTemp.ContactNumber.Equals(trimmedContactNum) &&
                                memberTemp.IsDeleted == false).FirstOrDefault();

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                return noochMember;
            }

            return null;
        }


        public static string SaveMemberFBId(string MemberId, string MemberFBId, string IsConnect)
        {
            Logger.Info("Common Helper -> SaveMembersFBId - MemberId: [" + MemberId + "]");

            try
            {
                MemberFBId = GetEncryptedData(MemberFBId.ToLower());

                var noochMember = GetMemberDetails(MemberId);

                if (noochMember != null)
                {
                    if (IsConnect == "YES")
                    {
                        noochMember.FacebookAccountLogin = MemberFBId.Replace(" ", "+");
                    }
                    else
                    {
                        noochMember.FacebookAccountLogin = null;
                    }
                    noochMember.DateModified = DateTime.Now;

                    DbContext dbc = GetDbContextFromEntity(noochMember);
                    _dbContext.Entry(dbc).Reload();
                    int i = dbc.SaveChanges();
                    return i > 0 ? "Success" : "Failure";
                }
                else
                {
                    return "Failure";
                }
            }
            catch (Exception)
            {
                return "Failure";
            }
        }


        public static RemoveNodeGenricResult RemoveBankNodeFromSynapse(string userOAuth, string userFingerPrint, string nodeIdToRemove, string MemberId)
        {
            RemoveNodeGenricResult res = new RemoveNodeGenricResult();
            res.IsSuccess = false;
            Logger.Info("Common Helper -> RemoveBankNodeFromSynapse - MemberId: [" + MemberId + "] - NodeId: [" + nodeIdToRemove + "]");

            try
            {
                string baseAddress = "";
                baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/remove" : "https://synapsepay.com/api/v3/node/remove";

                RemoveBankNodeRootClass rootObject = new RemoveBankNodeRootClass
                {
                    login = new Login { oauth_key = userOAuth },
                    node = new Node { _id = new _Id { oid = nodeIdToRemove } },
                    user = new Entities.SynapseRelatedEntities.User { fingerprint = userFingerPrint }
                };

                var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(rootObject);
                ASCIIEncoding encoding = new ASCIIEncoding();
                Byte[] bytes = encoding.GetBytes(parsedContent);

                Stream newStream = http.GetRequestStream();
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                try
                {
                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    JObject checkPermissionResponse = JObject.Parse(content);

                    if (checkPermissionResponse["success"] != null &&
                        Convert.ToBoolean(checkPermissionResponse["success"]) == true)
                    {
                        res.IsSuccess = true;
                        res.Message = "Node removed from synapse successfully.";
                    }
                    else
                    {
                        res.IsSuccess = false;
                        res.Message = "Error removing node from synapse.";
                    }
                }
                catch (WebException we)
                {
                    Logger.Error("Common Helper -> RemoveBankNodeFromSynapse - MemberId: [" + MemberId + "] - NodeId: [" + nodeIdToRemove + "] - Error: [" + we + "]");
                    res.IsSuccess = false;
                    res.Message = "Error removing node from synapse.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> RemoveBankNodeFromSynapse - MemberId: [" + MemberId + "] - NodeId: [" + nodeIdToRemove + "] - Error: [" + ex + "]");
                res.IsSuccess = false;
                res.Message = "Error removing node from synapse.";
            }

            return res;
        }


        /// <summary>
        /// Gets the City & State values for a given ZIP code from Google's Maps API.
        /// </summary>
        /// <param name="zipCode"></param>
        /// <returns></returns>
        public static GoogleGeolocationOutput GetCityAndStateFromZip(string zipCode)
        {
            GoogleGeolocationOutput res = new GoogleGeolocationOutput();
            res.IsSuccess = false;

            if (!String.IsNullOrEmpty(zipCode))
            {
                res.Zip = zipCode;
                res.city = "";

                try
                {
                    string googleUrlLink = "https://maps.googleapis.com/maps/api/geocode/json?address=" + zipCode + "&key=" +
                                           Utility.GetValueFromConfig("GoogleGeolocationKey");

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(googleUrlLink));
                    http.Method = "GET";

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    JObject jsonFromSynapse = JObject.Parse(content);

                    if (jsonFromSynapse["status"].ToString() == "OK")
                    {
                        JToken addressComponentsArray = jsonFromSynapse["results"][0]["address_components"];

                        foreach (JToken item in addressComponentsArray)
                        {
                            if (item["types"][0].ToString() == "administrative_area_level_1")
                            {
                                res.stateFull = item["long_name"].ToString();
                                res.stateAbbrev = item["short_name"].ToString();
                                res.GoogleStatus = jsonFromSynapse["status"].ToString();
                                res.ErrorMessage = "OK";
                                res.IsSuccess = true;
                            }

                            // Also get the CITY
                            if (item["types"][0].ToString() == "locality")
                            {
                                res.city = item["short_name"].ToString();
                            }
                        }

                        if (res.IsSuccess)
                        {
                            res.CompleteAddress = jsonFromSynapse["results"][0]["formatted_address"].ToString();
                        }
                    }
                    else
                    {
                        res.ErrorMessage = "Error with Google Maps API.";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Common Helper -> GetStateNameByZipcode FAILED (Google API Exception) - zipCode: [" + zipCode + "], Exception: [" + ex + "]");
                    res.ErrorMessage = "Server Exception: [" + ex.Message + "]";
                }
            }
            else
            {
                Logger.Error("Common Helper -> GetStateNameByZipcode FAILED - No Zipcode passed - zipCode: [" + zipCode + "]");
                res.ErrorMessage = "No zipcode passed";
            }

            return res;
        }


        public static List<string> getClientSecretId(string memId)
        {
            List<string> clientIds = new List<string>();
            try
            {
                Member member = GetMemberDetails(memId);

                if (member.isRentScene == true)
                {
                    string SynapseClientId = Utility.GetValueFromConfig("SynapseClientIdRentScene");
                    string SynapseClientSecret = Utility.GetValueFromConfig("SynapseClientSecretRentScene");
                    clientIds.Add(SynapseClientId);
                    clientIds.Add(SynapseClientSecret);
                }
                else
                {
                    string SynapseClientId = Utility.GetValueFromConfig("SynapseClientId");
                    string SynapseClientSecret = Utility.GetValueFromConfig("SynapseClientSecret");
                    clientIds.Add(SynapseClientId);
                    clientIds.Add(SynapseClientSecret);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> getClientSecretId FAILED - MemberID: [" + memId + "], Exception: [" + ex + "]");
            }

            return clientIds;
        }


        public static CancelTransactionAtSynapseResult CancelTransactionAtSynapse(string TransationId, string MemberId)
        {
            Logger.Info("CancelTransactionAtSynapse -> - TransationId: [" + TransationId + "]");

            TransactionsStatusAtSynapse transactionsStatusAtSynapse = new TransactionsStatusAtSynapse();

            CancelTransactionAtSynapseResult CancelTransaction = new CancelTransactionAtSynapseResult();

            try
            {
                transactionsStatusAtSynapse = getTransationDetailsAtSynapse(TransationId);

                if (transactionsStatusAtSynapse == null)
                {
                    CancelTransaction.errorMsg = "Transation Not Found";
                    return CancelTransaction;
                }
                if ((transactionsStatusAtSynapse.status == "QUEUED-BY-SYNAPSE") || (transactionsStatusAtSynapse.status == "QUEUED-BY-RECEIVER") || (transactionsStatusAtSynapse.status == "CREATED"))
                {
                    var MemberObj = GetMemberDetails(MemberId);
                    var OauthObj = GetSynapseCreateaUserDetails(MemberId);

                    string baseAddress = "";
                    baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/trans/cancel" : "https://synapsepay.com/api/v3/trans/cancel";

                    CancelTransactionClass rootObject = new CancelTransactionClass
                    {
                        login = new Login1 { oauth_key = GetDecryptedData(OauthObj.access_token) },
                        trans = new Trans { _id = new _ID { oid = transactionsStatusAtSynapse.Transaction_oid } },
                        user = new User1 { fingerprint = MemberObj.UDID1 }
                    };

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(rootObject);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    try
                    {
                        var response = http.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        JObject checkPermissionResponse = JObject.Parse(content);

                        if (checkPermissionResponse["success"] != null &&
                            Convert.ToBoolean(checkPermissionResponse["success"]) == true)
                        {
                            CancelTransaction.IsSuccess = true;
                            CancelTransaction.Message = "Transation cancelled successfully.";
                        }
                        else
                        {
                            CancelTransaction.IsSuccess = false;
                            CancelTransaction.Message = checkPermissionResponse["success"]["error"]["en"].ToString();
                        }
                    }
                    catch (WebException we)
                    {
                        Logger.Error("Common Helper -> CancelTransactionAtSynapse - TransationId: [" + TransationId + "] - Error: [" + we + "]");
                        CancelTransaction.IsSuccess = false;
                        CancelTransaction.Message = "Error cancelling Transation.";
                    }
                }
                else
                {
                    CancelTransaction.errorMsg = "Transation can't be cancelled, its no more in cancellable state.";
                    return CancelTransaction;
                }


            }
            catch (Exception ex)
            {

                Logger.Error("CancelTransactionAtSynapse CodeBehind -> OUTER EXCEPTION - [Exception: " + ex.Message + "]");
            }

            return CancelTransaction;

        }


        public static TransactionsStatusAtSynapse getTransationDetailsAtSynapse(string TransationId)
        {
            TransactionsStatusAtSynapse transactionsStatusAtSynapse = new TransactionsStatusAtSynapse();

            try
            {
                transactionsStatusAtSynapse = _dbContext.TransactionsStatusAtSynapses.OrderByDescending(m => m.Id).FirstOrDefault(m => m.Nooch_Transaction_Id == TransationId);
                if (transactionsStatusAtSynapse != null)
                {
                    _dbContext.Entry(transactionsStatusAtSynapse).Reload();
                    return transactionsStatusAtSynapse;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> getTransationDetailsAtSynapse FAILED - Exception: [" + ex.Message + "]");
            }

            return null;
        }


        public static string notifyCliffAboutError(string bodyTxt)
        {
            try
            {
                StringBuilder completeEmailTxt = new StringBuilder();
                string s = "<html><body><h2>Error Occurred</h2><p>The following error just occurred (at [" + DateTime.Now.ToString("MMMM dd, yyyy MM/dd/yy H:mm:ss") +
                            "]:<br/><br>" + bodyTxt +
                           "<br/><br/><small>This email was generated automatically in [Common Helper -> notifyCliffAboutError]</small></body></html>";

                completeEmailTxt.Append(s);

                Utility.SendEmail(null, "admin-autonotify@nooch.com", "errors@nooch.com", null,
                                  "Nooch Admin Alert: Error",
                                  null, null, null, null, completeEmailTxt.ToString());

                return "Notification sent";
            }
            catch (Exception ex)
            {
                Logger.Info("Common Helper -> notifyCliffAboutError FAILED - Exception: [" + ex + "]");
                return ex.Message;
            }
        }


        public static string SendMincroDepositsVerificationReminderEmail(string MemberId, string BankId, bool IsRs)
        {
            Logger.Info("Common Helper -> SendMincroDepositsVerificationReminderEmail Initiated. MemberID: [" + MemberId + "], " +
                        "Node OID (enc): [" + BankId + "], " +
                        "IsRs: [" + IsRs + "]");

            try
            {
                using (NOOCHEntities db = new NOOCHEntities())
                {
                    var memGuid = Utility.ConvertToGuid(MemberId);

                    var memberObj = GetMemberDetails(MemberId);
                    if (memberObj == null)
                    {
                        return "Member not found.";
                    }

                    var bankAccountDetails = db.SynapseBanksOfMembers.FirstOrDefault(b =>
                                                                                     b.IsDefault == true &&
                                                                                     b.IsAddedUsingRoutingNumber == true &&
                                                                                     b.oid == BankId &&
                                                                                     b.MemberId == memGuid &&
                                                                                     b.Status == "Not Verified");

                    if (bankAccountDetails == null)
                    {
                        return "Bank account not found.";
                    }

                    if (memberObj != null && bankAccountDetails != null)
                    {
                        #region Send Reminder Email

                        string fromAddress = Utility.GetValueFromConfig("transfersMail");
                        string toAddress = GetDecryptedData(memberObj.UserName);

                        // User Details
                        string userFirstName = UppercaseFirst(GetDecryptedData(memberObj.FirstName));
                        string userLastName = UppercaseFirst(GetDecryptedData(memberObj.LastName));

                        // Bank Account Details
                        string accountNum = GetDecryptedData(bankAccountDetails.account_number_string);
                        accountNum = "**** - " + accountNum.Substring(accountNum.Length - 4);
                        string bankName = GetDecryptedData(bankAccountDetails.bank_name);
                        string nameOnAccount = GetDecryptedData(bankAccountDetails.name_on_account);
                        string accountNickName = GetDecryptedData(bankAccountDetails.nickname);
                        string routingNum = GetDecryptedData(bankAccountDetails.routing_number_string);

                        string verifyLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/MicroDepositsVerification?mid=" + MemberId);

                        var tokens = new Dictionary<string, string>
                                {
								    {Constants.PLACEHOLDER_FIRST_NAME, userFirstName },
									{Constants.PLACEHOLDER_LAST_NAME, userLastName },
									{Constants.PLACEHOLDER_BANK_ACCOUNT_NUMBER, accountNum},
									{Constants.PLACEHOLDER_BANK_NAME, bankName},
									{Constants.MEMO, accountNickName},
                                    {Constants.PLACEHOLDER_EXISTINGUSER, routingNum},
                                    {Constants.PLACEHOLDER_PAY_LINK, verifyLink}
								};

                        var templateToUse = IsRs ? "MicroDepositsReminderEmailToRentSceneUser" : "MicroDepositsReminderEmail";

                        try
                        {
                            Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                              "Verify Your Bank - Reminder",
                                              null, tokens, null, null, null);

                            Logger.Info("Common Helper -> SendMincroDepositsVerificationReminderEmail - [" + templateToUse + "] sent to [" + toAddress + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Common Helper -> SendMincroDepositsVerificationReminderEmail - [" + templateToUse + "] NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                        }

                        #endregion Send Reminder Email
                    }

                    return "OK";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> SendMincroDepositsVerificationReminderEmail FAILED - Outer Exception - MemberID: [" + MemberId + "], " +
                            "Exception: [" + ex + "]");
                return "Error";
            }
        }


        public static string GetTransSynapseStatusNote(string transId)
        {
            try
            {
                if (!String.IsNullOrEmpty(transId))
                {
                    var lastStatusObj = (from statusObj in _dbContext.TransactionsStatusAtSynapses
                                         where statusObj.Nooch_Transaction_Id == transId
                                         select statusObj).OrderByDescending(n => n.Id).FirstOrDefault();

                    if (lastStatusObj != null)
                    {
                        return lastStatusObj.status_note;
                    }
                    else return "No status note";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetTransSynapseStatus FAILED - Member ID: [" + transId + "], [Exception: " + ex + "]");
            }

            return "Failure";
        }


        public static suggestedUsers GetSuggestedUsers(string memberId)
        {
            Logger.Info("Common Helper -> GetSuggestedUsers - MemberID: [" + memberId + "]");

            suggestedUsers suggestedUsers = new suggestedUsers();
            suggestedUsers.success = false;
            suggestedUsers.suggestions = new List<suggestions>();

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var member = _dbContext.Members.FirstOrDefault(u => u.MemberId == id);

                if (member != null)
                {
                    var transactions = new List<Transaction>();

                    transactions = _dbContext.Transactions.Where(trans => trans.TransactionStatus == "Success" &&
                                                                         (trans.SenderId == id ||
                                                                          trans.RecipientId == id))
                                                                          .OrderByDescending(r => r.TransactionDate).Take(30).ToList();

                    if (transactions != null && transactions.Count > 0)
                    {
                        int i = 0;

                        #region Loop Through Transaction List

                        foreach (var trans in transactions)
                        {
                            try
                            {
                                Member otherUser = new Member();

                                if (trans.SenderId == id && trans.SenderId != trans.RecipientId) // Sent to existing user, get recipient
                                {
                                    otherUser = _dbContext.Members.FirstOrDefault(m => m.MemberId == trans.RecipientId && m.IsDeleted == false);
                                }
                                else if (trans.SenderId != id && trans.SenderId != trans.RecipientId) // Received from existing user, get sender
                                {
                                    otherUser = _dbContext.Members.FirstOrDefault(m => m.MemberId == trans.SenderId && m.IsDeleted == false);
                                }
                                else if (trans.SenderId == id && trans.InvitationSentTo != null) // Sent to non-Nooch user, get recipient by email
                                {
                                    var invitedUsersEmailEnc = GetEncryptedData(trans.InvitationSentTo);
                                    otherUser = _dbContext.Members.FirstOrDefault(m => (m.UserName == invitedUsersEmailEnc ||
                                                                                        m.UserNameLowerCase == invitedUsersEmailEnc ||
                                                                                        m.SecondaryEmail == invitedUsersEmailEnc) &&
                                                                                        m.IsDeleted == false);
                                }

                                if (otherUser == null) continue;

                                // Check if this user has already been added to the Array
                                bool keepGoing = true;

                                if (i > 0)
                                {
                                    foreach (suggestions s in suggestedUsers.suggestions)
                                    {
                                        if (s != null && s.data.nooch_id != null && s.data.nooch_id == otherUser.Nooch_ID)
                                        {
                                            keepGoing = false;
                                            break;
                                        }
                                    }
                                }

                                if (!keepGoing) continue;

                                var name = UppercaseFirst(GetDecryptedData(otherUser.FirstName)) + " " + UppercaseFirst(GetDecryptedData(otherUser.LastName));

                                suggestions suggestion = new suggestions();
                                suggestion.value = name;
                                suggestion.data = new suggestions_data
                                {
                                    nooch_id = otherUser.Nooch_ID,
                                    name = name,
                                    cip = otherUser.cipTag,
                                    email = GetDecryptedData(otherUser.UserName),
                                    imgUrl = otherUser.Photo
                                };

                                suggestedUsers.suggestions.Add(suggestion);

                                i++;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Common Helper -> GetSuggestedUsers - EXCEPTION inside FOREACH loop - [" + ex.Message + "]");
                            }
                        }

                        #endregion Loop Through Transaction List

                        Logger.Info("Common Helper -> GetSuggestedUsers SUCCESS - COUNT: [" + suggestedUsers.suggestions.Count + "], MemberID: [" + memberId + "]");

                        suggestedUsers.success = true;
                        suggestedUsers.msg = "Found [" + suggestedUsers.suggestions.Count.ToString() + "]";
                    }
                    else
                    {
                        suggestedUsers.msg = "No transactions found";
                    }
                }
                else
                {
                    suggestedUsers.msg = "Member not found";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper - GetSuggestedUsers FAILED - [MemberID: " + memberId + "], Exception: [" + ex + "]");
                suggestedUsers.msg = "Exception: [" + ex.Message + "]";
            }

            return suggestedUsers;
        }
    }
}
