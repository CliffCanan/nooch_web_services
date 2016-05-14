using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Common.Resources;
using Nooch.Common.Rules;
using Nooch.Data;
using Nooch.Common.Entities.LandingPagesRelatedEntities;


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

        public static string ForgotPassword(string userName)
        {
            Logger.Info("Common Helper -> ForgotPassword - [userName: " + userName + "]");

            var getMember = GetMemberDetailsByUserName(userName);

            try
            {
                if (getMember != null)
                {
                    bool status = false;

                    var fromAddress = Utility.GetValueFromConfig("adminMail");

                    var tokens = new Dictionary<string, string>
                    {
                        {
                            Constants.PLACEHOLDER_FIRST_NAME,
                            UppercaseFirst(GetDecryptedData(getMember.FirstName)) + " " +
                            UppercaseFirst(GetDecryptedData(getMember.LastName))
                        },
                        {Constants.PLACEHOLDER_LAST_NAME, GetDecryptedData(getMember.LastName)},
                        {
                            Constants.PLACEHOLDER_PASSWORDLINK,
                            String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                "/ForgotPassword/ResetPassword.aspx?memberId=" + getMember.MemberId)
                        }
                    };
                    PasswordResetRequest prr = new PasswordResetRequest();
                    prr.RequestedOn = DateTime.Now;
                    prr.MemberId = getMember.MemberId;
                    _dbContext.PasswordResetRequests.Add(prr);
                    int i = _dbContext.SaveChanges();

                    if (i > 0)
                    {
                        _dbContext.Entry(prr).Reload();
                        status = true;
                        Utility.SendEmail(Constants.TEMPLATE_FORGOT_PASSWORD, fromAddress, GetDecryptedData(getMember.UserName), null, "Reset your Nooch password"
                    , null, tokens, null, null, null);
                    }

                    return status
                        ? "Your reset password link has been sent to your mail successfully."
                        : "Problem occured while sending mail.";
                }
                return "Problem occured while sending mail.";
            }
            catch (Exception)
            {
                return "Problem occured while sending mail.";
            }
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
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

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
            var userNameLowerCase = GetEncryptedData(userName.ToLower());
            userName = GetEncryptedData(userName);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.UserNameLowerCase == userNameLowerCase && m.UserName == userName && m.IsDeleted == false);

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
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

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
            }

            return noochMember != null ? GetDecryptedData(noochMember.UserName) : null;
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
            return null;
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
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for TEAM NOOCH, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction for CLIFF CANAN, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                        return false;
                    }
                    if (MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Marvis Burns (RentScene)
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
                    if (MemberId.ToString().ToLower() == "2d0427d2-7f21-40d9-a5a2-ac3e973809ec") // Dana Kozubal (Dave Phillip's)
                    {
                        Logger.Info("**** Common Helper -> IsWeeklyTransferLimitExceeded LIMIT EXCEEDED - But transaction is for DANA KOZUBAL, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
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

        public static Member GetMemberDetailsByUserName(string userName)
        {
            try
            {
                var id = GetEncryptedData(userName);

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == id && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return noochMember;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> GetMemberDetails FAILED - UserName [ENC:]: [" + userName + "], [Exception: " + ex + "]");
            }

            return null;
        }

        public static List<SynapseBankLoginResult> GetSynapseBankLoginResulList(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBankLoginResults.Where(m => m.MemberId == id && m.IsDeleted == false).ToList();

            return memberAccountDetails;
        }

        public static bool RemoveSynapseBankLoginResultsForGivenMemberId(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBankLoginResults.Where(m => m.MemberId == id && m.IsDeleted == false).ToList();

            try
            {
                foreach (SynapseBankLoginResult v in memberAccountDetails)
                {
                    v.IsDeleted = true;
                    _dbContext.SaveChanges();
                    _dbContext.Entry(memberAccountDetails).Reload();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static SynapseBanksOfMember GetSynapseBankAccountDetails(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var bankDetailsFromDB = _dbContext.SynapseBanksOfMembers.FirstOrDefault(m => m.MemberId == id && m.IsDefault == true);
            if (bankDetailsFromDB != null)
            {
                _dbContext.Entry(bankDetailsFromDB).Reload();
            }

            return bankDetailsFromDB;
        }

        public static SynapseCreateUserResult GetSynapseCreateaUserDetails(string memberId)
        {
            Logger.Info("Common Helper -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);
            if (memberAccountDetails != null)
            {
                _dbContext.Entry(memberAccountDetails).Reload();
            }

            return memberAccountDetails;
        }

        public static MemberNotification GetMemberNotificationSettingsByUserName(string userName)
        {
            Logger.Info("Common Helper -> GetMemberNotificationSettingsByUserName - UserName: [" + userName + "]");

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
            Logger.Info("Common Helper -> UpdateAccessToken - userName: [" + userName + "]");

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
                Logger.Error("Common Helper -> CheckTokenExistance FAILED - [Exception: " + ex + "]");
                return false;
            }
        }

        public static bool IsListedInSDN(string lastName, Guid userId)
        {
            bool result = false;
            Logger.Info("Common Helper -> IsListedInSDNList - userName: [" + lastName + "]");

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
                    string adminUserName = CommonHelper.GetEncryptedData(Utility.GetValueFromConfig("transfersMail"));
                    var fromAddress = CommonHelper.GetDecryptedData(adminUserName);

                    bool b = Utility.SendEmail(null, fromAddress,
                        Utility.GetValueFromConfig("SDNMailReciever"), null, "SDN Listed", null, null, null,
                        null, str.ToString());

                    Logger.Info(
                        "SDN Screening Alert - SDN Screening Results email sent to [" +
                        "SDN@nooch.com" + "].");

                    if (true)
                    {
                        Logger.Info(
                            "SDN Screening Alert - SDN Screening Results email sent to [" + "SDN@nooch.com" + "].");
                    }
                    {
                        Logger.Error(
                            "SDN Screening Alert - SDN Screening Results email NOT sent to [" + "SDN@nooch.com" + "].");
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

        private static string IncreaseInvalidPinAttemptCount(
            Member memberEntity, int pinRetryCountInDb)
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

                    var disputeStatus = CommonHelper.GetEncryptedData(Constants.DISPUTE_STATUS_REPORTED);
                    var disputeReviewStatus = CommonHelper.GetEncryptedData(Constants.DISPUTE_STATUS_REVIEW);

                    if (
                        !memberEntity.Transactions.Any(
                            transaction =>
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
                    ;

                    if (!memberEntity.PinNumber.Equals(pinNumber.Replace(" ", "+")))
                    // incorrect pinnumber after 24 hours
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
                //this is the first invalid try
                {
                    return IncreaseInvalidPinAttemptCount(memberEntity, pinRetryCountInDb);
                }

                if (pinRetryCountInDb == 3)
                {
                    return
                        "Your account has been suspended. Please contact admin or send a mail to support@nooch.com if you need to reset your PIN number immediately.";
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
                                Constants.PLACEHOLDER_FIRST_NAME,
                                CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName))
                            }
                        };

                    try
                    {
                        var fromAddress = Utility.GetValueFromConfig("adminMail");
                        string emailAddress = CommonHelper.GetDecryptedData(memberEntity.UserName);
                        Logger.Info(
                            "Validate PIN Number --> Attempt to send mail for Suspend Member[ memberId:" +
                            memberEntity.MemberId + "].");
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

                    return
                        "Your account has been suspended for 24 hours from now. Please contact admin or send a mail to support@nooch.com if you need to reset your PIN number immediately.";
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
            Logger.Info("Common Helper -> GetMemberNotificationSettings Initiated - [MemberId: " + memberId + "]");

            Guid memId = Utility.ConvertToGuid(memberId);

            var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.Member.MemberId == memId);
            if (memberNotifications != null)
            {
                _dbContext.Entry(memberNotifications).Reload();
            }

            return memberNotifications;
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
            if (!String.IsNullOrEmpty(url))
            {
                Logger.Info("Common Helper -> ConvertImageURLToBase64 Initiated - Photo URL is: [" + url + "]");

                StringBuilder _sb = new StringBuilder();

                Byte[] _byte = GetImage(url);

                _sb.Append(Convert.ToBase64String(_byte, 0, _byte.Length));

                return _sb.ToString();
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
            string RecentIpOfUser = "";


            var memberIP = _dbContext.MembersIPAddresses.OrderByDescending(m => m.ModifiedOn).FirstOrDefault(m => m.MemberId == MemberIdPassed);

            if (memberIP != null)
            {

                _dbContext.Entry(memberIP).Reload();
            }
            RecentIpOfUser = memberIP != null ? memberIP.Ip.ToString() : "54.201.43.89";


            return RecentIpOfUser;
        }


        /// <summary>
        /// For sending a user's SSN & DOB to Synapse using V3 API.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        public static submitIdVerificationInt sendUserSsnInfoToSynapseV3(string MemberId)
        {
            Logger.Info("CommonHelper -> sendUserSsnInfoToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            submitIdVerificationInt res = new submitIdVerificationInt();
            res.success = false;

            var id = Utility.ConvertToGuid(MemberId);

            var memberEntity = GetMemberDetails(MemberId);

            if (memberEntity != null)
            {
                var userNameDecrypted = GetDecryptedData(memberEntity.UserName);

                if (memberEntity.IsVerifiedWithSynapse != true)
                {
                    string usersFirstName = UppercaseFirst(GetDecryptedData(memberEntity.FirstName));
                    string usersLastName = UppercaseFirst(GetDecryptedData(memberEntity.LastName));

                    string usersAddress = "";
                    string usersZip = "";
                    //string usersCity = "";

                    DateTime usersDob;
                    string usersDobDay = "";
                    string usersDobMonth = "";
                    string usersDobYear = "";

                    string usersSsnLast4 = "";

                    string usersSynapseOauthKey = "";
                    string usersFingerprint = "";

                    try
                    {
                        #region Check User For All Required Data

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
                            usersSsnLast4 = GetDecryptedData(memberEntity.SSN);
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

                        #endregion Check User For All Required Data
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED on checking for all required data - [Username: " +
                                      userNameDecrypted + "], [Exception: " + ex + "]");
                    }

                    // Update Member's DB record from NULL to false (update to true later on if Verification from Synapse is completely successful)
                    Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - About to set IsVerifiedWithSynapse to False before calling Synapse: [Username: " +
                                 userNameDecrypted + "]");
                    memberEntity.IsVerifiedWithSynapse = false;

                    #region Send SSN Info To Synapse

                    try
                    {
                        #region Call Synapse V3 /user/doc/add API

                        Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - Checkpoint 1230 - About To Query Synapse");

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
                        doc.document_value = usersSsnLast4; // Can also be the user's Passport # or DL #

                        addKycInfoInput_user user = new addKycInfoInput_user();
                        user.fingerprint = usersFingerprint;
                        user.doc = doc;

                        synapseKycInput.user = user;

                        string baseAddress = "";
                        baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"))
                                      ? "https://sandbox.synapsepay.com/api/v3/user/doc/add"
                                      : "https://synapsepay.com/api/v3/user/doc/add";


                        #region For Testing

                        if (GetDecryptedData(memberEntity.UserName).IndexOf("jones00") > -1)
                        {
                            Logger.Info("****  sendUserSSNInfoToSynapseV3 -> JUST A TEST BLOCK REACHED! [" + userNameDecrypted + "] ****");
                            baseAddress = "https://sandbox.synapsepay.com/api/v3/user/doc/add";
                        }
                        else if (memberEntity.MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                        {
                            doc.name_last = "Satell";
                            doc.document_value = "7562";
                        }

                        try
                        {
                            Logger.Info("Send User's SSN Info To Synapse V3 -> Payload to send to Synapse: [OauthKey: " + login.oauth_key +
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

                        #endregion For Testing


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
                                    res.message = "additional questions needed";

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

                                        #endregion Iterate Through Each Question And Save in DB
                                    }
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
                                            // CLIFF (5/7/16): NEED TO ALSO STORE 3 MORE FIELDS: physical_doc, virtual_doc, & extra_security
                                            //                 But need to create those columns in the DB first...
                                            synapseRes.permission = synapseResponse.user.permission;
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

                                    res.message = "complete success";
                                }
                            }
                            else
                            {
                                // Response from Synapse had 'success' != true
                                // SHOULDN'T EVER GET HERE B/C IF SYNAPSE CAN'T VERIFY THE USER, IT RETURNS A 400 BAD REQUEST HTTP ERROR WITH A MESSAGE...SEE WEB EX BELOW
                                Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED: Synapse Result \"success != true\" - [Username: " + userNameDecrypted + "]");
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

                        Logger.Error("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED (Outer) - [errorCode: " + httpStatusCode.ToString() + "], [Message" + we.Message + "]");

                        res.message = "CommonHelper Exception";

                        var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject errorJsonFromSynapse = JObject.Parse(response);

                        // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors
                        //                   We might have to do different things depending on which error is returned... for now just pass
                        //                   back the error number & msg to the function that called this method.
                        string errorMsg = errorJsonFromSynapse["error"]["en"].ToString();

                        if (!String.IsNullOrEmpty(errorMsg) &&
                            (errorMsg.IndexOf("Unable to verify") > -1 ||
                             errorMsg.IndexOf("submit a valid copy of passport") > -1))
                        {
                            Logger.Info("****  THIS USER'S SSN INFO WAS NOT VERIFIED AT ALL. MUST INVESTIGATE WHY (COULD BE TYPO WITH PERSONAL INFO). " +
                                        "DETERMINE IF NECESSARY TO ASK FOR DRIVER'S LICENSE.  ****");

                            memberEntity.AdminNotes = "SSN INFO WAS INVALID WHEN SENT TO SYNAPSE. NEED TO COLLECT DRIVER'S LICENSE.";

                            // Email Nooch Admin about this user for manual follow-up (Send email to Cliff)
                            #region Notify Nooch Admin About Failed SSN Validation

                            try
                            {
                                StringBuilder st = new StringBuilder();

                                string city = !String.IsNullOrEmpty(memberEntity.City) ? CommonHelper.GetDecryptedData(memberEntity.City) : "NONE";

                                st.Append("<table border='1' cellpadding='6' style='border-collapse:collapse;text-align:center;'>" +
                                          "<tr><th>PARAMETER</th><th>VALUE</th></tr>");
                                st.Append("<tr><td><strong>Name</strong></td><td>" + usersFirstName + " " + usersLastName + "</td></tr>");
                                st.Append("<tr><td><strong>MemberId</strong></td><td>" + MemberId + "</td></tr>");
                                st.Append("<tr><td><strong>Nooch_ID</strong></td><td><a href=\"https://noochme.com/noochnewadmin/Member/Detail?NoochId=" + memberEntity.Nooch_ID + "\" target='_blank'>" + memberEntity.Nooch_ID + "</a></td></tr>");
                                st.Append("<tr><td><strong>Status</strong></td><td><strong>" + memberEntity.Status + "</strong></td></tr>");
                                st.Append("<tr><td><strong>DOB</strong></td><td>" + Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MMMM dd, yyyy") + "</td></tr>");
                                st.Append("<tr><td><strong>SSN</strong></td><td>" + usersSsnLast4 + "</td></tr>");
                                st.Append("<tr><td><strong>Address</strong></td><td>" + usersAddress + "</td></tr>");
                                st.Append("<tr><td><strong>City</strong></td><td>" + city + "</td></tr>");
                                st.Append("<tr><td><strong>ZIP</strong></td><td>" + usersZip + "</td></tr>");
                                st.Append("<tr><td><strong>Contact #</strong></td><td>" + CommonHelper.FormatPhoneNumber(memberEntity.ContactNumber) + "</td></tr>");
                                st.Append("<tr><td><strong>Phone Verified?</strong></td><td>" + memberEntity.IsVerifiedPhone.ToString() + "</td></tr>");
                                st.Append("<tr><td><strong>IsVerifiedWithSynapse</strong></td><td>" + memberEntity.IsVerifiedWithSynapse.ToString() + "</td></tr>");
                                st.Append("</table>");

                                StringBuilder completeEmailTxt = new StringBuilder();
                                string s = "<html><body><h3>Nooch SSN Verification Failure</h3><p style='margin:0 auto 20px;'>The following Nooch user just failed an SSN Verification attempt:</p>"
                                           + st.ToString() +
                                           "<br/><br/><small>This email was generated automatically during <strong>[CommonHelper -> sendUserSsnInfoToSynapseV3]</strong>.</small></body></html>";

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
                                Logger.Info("CommonHelper -> sendUserSsnInfoToSynapseV3 - ID Document Path found, so attempting submitDocumentToSynapseV3()");

                                // CLIFF (10/10/15): I guess we will have to add more code depending on what the response for this next line is...
                                submitDocumentToSynapseV3(memberEntity.MemberId.ToString(), memberEntity.VerificationDocumentPath);
                            }
                        }
                    }

                    // Save changes to Members DB
                    memberEntity.DateModified = DateTime.Now;
                    _dbContext.SaveChanges();

                    #endregion Parse Synapse Response
                }
                else
                {
                    Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 - User Already Verified With Synapse - [Username: " + userNameDecrypted +
                                "], [Validated On: " + Convert.ToDateTime(memberEntity.ValidatedDate).ToString("MMM dd yyyy") + "]");
                    res.message = "Already Verified";
                    res.success = true;
                }
            }
            else
            {
                // Member not found in Nooch DB
                Logger.Info("Common Helper -> sendUserSsnInfoToSynapseV3 FAILED: Member not found - [MemberId: " + MemberId + "]");
                res.message = "Member not found";
            }

            return res;
        }


        public static GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
        {
            Logger.Info("Common Helper -> submitDocumentToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            var id = Utility.ConvertToGuid(MemberId);

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();

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
                _dbContext.Entry(usersSynapseOauthKey).Reload();
                usersSynapseOauthKey = GetDecryptedData(usersSynapseDetails.access_token);
            }

            #endregion Get User's Synapse OAuth Consumer Key

            #region Get User's Fingerprint

            string usersFingerprint = "";

            var member = GetMemberDetails(id.ToString());

            if (member == null)
            {
                Logger.Info("Common Helper -> submitDocumentToSynapseV3 ABORTED: Member not found. [MemberId: " + MemberId + "]");
                res.message = "Member not found";

                return res;
            }
            else
            {
                if (String.IsNullOrEmpty(member.UDID1))
                {
                    Logger.Info("Common Helper -> submitDocumentToSynapseV3 ABORTED: Member's Fingerprint not found. [MemberId: " + MemberId + "]");
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

            submitDocToSynapseV3Class answers = new submitDocToSynapseV3Class();

            SynapseV3Input_login s_log = new SynapseV3Input_login();
            s_log.oauth_key = usersSynapseOauthKey;
            answers.login = s_log;

            submitDocToSynapse_user sdtu = new submitDocToSynapse_user();
            submitDocToSynapse_user_doc doc = new submitDocToSynapse_user_doc();
            doc.attachment = "data:text/csv;base64," + ConvertImageURLToBase64(ImageUrl).Replace("\\", "");

            sdtu.fingerprint = usersFingerprint;
            sdtu.doc = doc;

            answers.user = sdtu;

            string baseAddress = "";
            baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/doc/attachments/add" : "https://synapsepay.com/api/v3/user/doc/attachments/add";

            try
            {
                var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(answers);
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
                    if (resFromSynapse.success.ToString().ToLower() == "true")
                    {
                        Logger.Info("Common Helper -> submitDocumentToSynapseV3 SUCCESSFUL - [MemberID: " + MemberId + "]");

                        res.success = true;

                        res.message = "";
                    }
                    else
                    {
                        res.message = "Got a response, but success was not true";
                        Logger.Info("Common Helper -> submitDocumentToSynapseV3 FAILED - Got Synapse response, but success was NOT 'true' - [MemberID: " + MemberId + "]");
                    }
                }
                else
                {
                    res.message = "Verification response was null";
                    Logger.Info("Common Helper -> submitDocumentToSynapseV3 FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");
                }
            }
            catch (WebException ex)
            {
                res.message = "MDA Exception #1671";
                Logger.Info("Common Helper -> submitDocumentToSynapseV3 FAILED - Catch [Exception: " + ex + "]");
            }

            #endregion Call Synapse /user/doc/attachments/add API

            return res;
        }

        public static synapseSearchUserResponse getUserPermissionsForSynapseV3(string userEmail)
        {
            Logger.Info("CommonHelper -> getUserPermissionsForSynapseV3 Initiated - [Email: " + userEmail + "]");

            synapseSearchUserResponse res = new synapseSearchUserResponse();
            res.success = false;

            try
            {
                synapseSearchUserInputClass input = new synapseSearchUserInputClass();

                synapseSearchUser_Client client = new synapseSearchUser_Client();
                client.client_id = Utility.GetValueFromConfig("SynapseClientId");
                client.client_secret = Utility.GetValueFromConfig("SynapseClientSecret");

                synapseSearchUser_Filter filter = new synapseSearchUser_Filter();
                filter.page = 1;
                filter.exact_match = true; // we might want to set this to false to prevent error due to capitalization mis-match... (or make sure we only send all lowercase email when creating a Synapse user)
                filter.query = userEmail;

                input.client = client;
                input.filter = filter;

                string UrlToHit = "";
                UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/search"
                                                                                               : "https://synapsepay.com/api/v3/user/search";

                Logger.Info("CommonHelper -> getUserPermissionsForSynapseV3 - About to query Synapse's /user/search API - [UrlToHit: " + UrlToHit + "]");

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
                        JObject refreshResponse = JObject.Parse(content);
                        //Logger.Info("CommonHelper -> getUserPermissionsForSynapseV3 - JSON Result from Synapse: " + refreshResponse);
                        res = JsonConvert.DeserializeObject<synapseSearchUserResponse>(content);
                    }
                    else
                    {
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

            return res;
        }


        /// <summary>
        /// For checking if one bank within a set of Synapse V3 banks has an "allowed" value.
        /// </summary>
        /// <param name="allNodes"></param>
        /// <param name="nodeOid">MUST BE UN-ENCRYPTED!</param>
        /// <returns></returns>
        public static NodePermissionCheckResult IsNodeActiveInGivenSetOfNodes(synapseSearchUserResponse_Node[] allNodes, string nodeOid)
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
                Logger.Error("Common Helper -> IsNodeActiveInGivenSetOfNodes - [NodeToMatch: " + nodeOid + "]");
            }

            return res;
        }

        public static SynapseDetailsClass GetSynapseBankAndUserDetailsforGivenMemberId(string memberId)
        {
            SynapseDetailsClass res = new SynapseDetailsClass();
            res.wereUserDetailsFound = false;
            res.wereBankDetailsFound = false;

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                // Full Member Table Details
                Member memberObject = GetMemberDetails(memberId);

                // Check Synapse USER details for given MemberID
                var createSynapseUserObj = GetSynapseCreateaUserDetails(id.ToString());

                if (createSynapseUserObj != null &&
                    !String.IsNullOrEmpty(createSynapseUserObj.access_token))
                {
                    // This MemberId was found in the SynapseCreateUserResults DB
                    res.wereUserDetailsFound = true;

                    Logger.Info("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId - Checkpoint #1 - " +
                                "SynapseCreateUserResults Record Found! - Now about to check if Synapse OAuth Key is expired or still valid.");

                    // CLIFF (10/3/15): ADDING CALL TO NEW METHOD TO CHECK USER'S STATUS WITH SYNAPSE, AND REFRESHING OAUTH KEY IF NECESSARY


                    #region Check If Testing

                    // CLIFF (10/22/15): Added this block for testing - if you use an email that includes "jones00" in it, 
                    //                   then this method will use the Synapse (v3) SANDBOX.  Leaving this here in case we
                    //                   want to test in the future the same way.
                    bool shouldUseSynapseSandbox = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"));
                    string memberUsername = GetMemberUsernameByMemberId(memberId);

                    if (shouldUseSynapseSandbox)
                    {
                        shouldUseSynapseSandbox = true;
                        Logger.Info("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId -> TESTING USER DETECTED - [" +
                                     memberUsername + "], WILL USE SYNAPSE SANDBOX URL FOR CHECKING OAUTH TOKEN STATUS");
                    }

                    #endregion Check If Testing

                    #region Check If OAuth Key Still Valid

                    synapseV3checkUsersOauthKey checkTokenResult = refreshSynapseV3OautKey(createSynapseUserObj.access_token);

                    if (checkTokenResult != null)
                    {
                        if (checkTokenResult.success == true)
                        {
                            res.UserDetails = new SynapseDetailsClass_UserDetails();
                            res.UserDetails.MemberId = memberId;
                            res.UserDetails.access_token = GetDecryptedData(checkTokenResult.oauth_consumer_key);  // Note :: Giving in encrypted format
                            res.UserDetails.user_id = checkTokenResult.user_oid;
                            res.UserDetails.user_fingerprints = memberObject.UDID1;
                            res.UserDetailsErrMessage = "OK";
                        }
                        else
                        {
                            Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                         "CheckTokenResult.msg: [" + checkTokenResult.msg + "], MemberID: [" + memberId + "]");

                            res.UserDetailsErrMessage = checkTokenResult.msg;
                        }
                    }
                    else
                    {
                        Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                                   "CheckTokenResult was NULL, MemberID: [" + memberId + "]");

                        res.UserDetailsErrMessage = "Unable to check user's Oauth Token";

                    }

                    #endregion Check If OAuth Key Still Valid
                }
                else
                {
                    Logger.Error("Common Helper -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - Unable to find Synapse Create User Details - " +
                                 "MemberID: [" + memberId + "]");

                    res.UserDetails = null;
                    res.UserDetailsErrMessage = "User synapse details not found.";
                }

                #region Get The User's Synapse Bank Details

                // Now get the user's Synapse BANK account details
                var defaultBank = GetSynapseBankAccountDetails(id.ToString());

                if (defaultBank != null)
                {
                    // Found a Synapse bank account for this user
                    res.wereBankDetailsFound = true;
                    res.BankDetails = new SynapseDetailsClass_BankDetails();
                    res.BankDetails.AddedOn = defaultBank.AddedOn;
                    res.BankDetails.Status = defaultBank.Status; // "Verfified" or "Not Verified"

                    // Cliff (5/13/16): several other methods use this value which was from Synapse V2, so just udpating it to be the OID so nothing should break elsewhere :-)
                    res.BankDetails.bankid = GetDecryptedData(defaultBank.oid);
                    res.BankDetails.allowed = defaultBank.allowed;
                    res.BankDetails.bank_oid = GetDecryptedData(defaultBank.oid);
                    res.BankDetails.bankType = defaultBank.type_bank;
                    res.BankDetails.synapseType = defaultBank.type_synapse;
                    res.BankDetails.dateVerified = defaultBank.Status == "Verified" && defaultBank.VerifiedOn != null
                                                   ? Convert.ToDateTime(defaultBank.VerifiedOn).ToString("MMM D YYYY") : "n/a";
                    res.AccountDetailsErrMessage = "OK";
                }
                else
                {
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
                //string oauthKeyEnc = CommonHelper.GetEncryptedData(oauthKey);

                // Checking user details for given MemberID

                SynapseCreateUserResult synCreateUserObject = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.access_token == oauthKey && m.IsDeleted == false);

                // check for this is not needed.
                // Will be calling login/refresh access token service to confirm if saved oAtuh token matches with token coming in response, if not then will update the token.
                if (synCreateUserObject != null)
                {
                    _dbContext.Entry(synCreateUserObject).Reload();
                    var noochMemberObject = GetMemberDetails(synCreateUserObject.MemberId.ToString());

                    #region Found Refresh Token

                    Logger.Info("Common Helper -> synapseV3checkUsersOauthKey - Found Member By Original OAuth Key");

                    SynapseV3RefreshOauthKeyAndSign_Input input = new SynapseV3RefreshOauthKeyAndSign_Input();

                    string SynapseClientId = Utility.GetValueFromConfig("SynapseClientId");
                    string SynapseClientSecret = Utility.GetValueFromConfig("SynapseClientSecret");

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

                    string UrlToHit = "";
                    UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/signin" : "https://synapsepay.com/api/v3/user/signin";

                    if (Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")))
                    {
                        Logger.Info("Common Helper -> synapseV3checkUsersOauthKey - TEST USER DETECTED - About to ping Synapse Sandbox /user/refresh...");
                    }

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

                        //Logger.LogDebugMessage("Common Helper -> refreshSynapseV2OautKey Checkpoint #1 - About to parse Synapse Response");

                        synapseCreateUserV3Result_int refreshResultFromSyn = new synapseCreateUserV3Result_int();

                        refreshResultFromSyn = JsonConvert.DeserializeObject<synapseCreateUserV3Result_int>(content);

                        JObject refreshResponse = JObject.Parse(content);

                        //Logger.Info("Common Helper -> synapseV3checkUsersOauthKey - Just Parsed Synapse Response: [" + refreshResponse + "]");

                        if ((refreshResponse["success"] != null && Convert.ToBoolean(refreshResponse["success"])) ||
                             refreshResultFromSyn.success.ToString() == "true")
                        {
                            // checking if token is same as saved in db
                            if (synCreateUserObject.access_token ==
                                GetEncryptedData(refreshResultFromSyn.oauth.oauth_key))
                            {
                                // Access Token is same as whwat we already had... no change
                                synCreateUserObject.access_token = GetEncryptedData(refreshResultFromSyn.oauth.oauth_key);
                                synCreateUserObject.refresh_token = GetEncryptedData(refreshResultFromSyn.oauth.refresh_token);
                                synCreateUserObject.expires_in = refreshResultFromSyn.oauth.expires_in;
                                synCreateUserObject.expires_at = refreshResultFromSyn.oauth.expires_at;
                            }
                            else
                            {
                                // New Access Token... time to update
                                synCreateUserObject.access_token = GetEncryptedData(refreshResultFromSyn.oauth.oauth_key);
                                synCreateUserObject.refresh_token = GetEncryptedData(refreshResultFromSyn.oauth.refresh_token);
                                synCreateUserObject.expires_in = refreshResultFromSyn.oauth.expires_in;
                                synCreateUserObject.expires_at = refreshResultFromSyn.oauth.expires_at;
                            }

                            if (!String.IsNullOrEmpty(refreshResultFromSyn.user.permission))
                            {
                                synCreateUserObject.permission = refreshResultFromSyn.user.permission;
                            }

                            int a = _dbContext.SaveChanges();
                            _dbContext.Entry(synCreateUserObject).Reload();

                            if (a > 0)
                            {
                                Logger.Info(
                                    "Common Helper -> refreshSynapseV3OautKey - SUCCESS From Synapse and Successfully added to Nooch DB - " +
                                    "Orig. Oauth Key (encr): [" + oauthKey + "], " +
                                    "Refreshed OAuth Key (encr): [" + synCreateUserObject.access_token + "]");

                                res.success = true;
                                res.oauth_consumer_key = synCreateUserObject.access_token;
                                res.oauth_refresh_token = synCreateUserObject.refresh_token;
                                res.user_oid = synCreateUserObject.user_id;
                                res.msg = "Oauth key refreshed successfully";
                            }
                            else
                            {
                                Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Error saving new key in Nooch DB - " +
                                             "Orig. Oauth Key: [" + oauthKey + "], " +
                                             "Refreshed OAuth Key: [" + synCreateUserObject.access_token + "]");

                                res.msg = "Failed to save new OAuth key in Nooch DB.";
                            }
                        }
                        else
                        {
                            Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED - Error from Synapse service, no 'success' key found - " +
                                         "Orig. Oauth Key: [" + oauthKey + "]");
                            res.msg = "Service error.";
                        }
                    }
                    catch (WebException we)
                    {
                        #region Synapse V3 Sig in/ refresh  Exception

                        var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;
                        string http_code = httpStatusCode.ToString();

                        var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject errorJsonFromSynapse = JObject.Parse(response);

                        string reason = errorJsonFromSynapse["reason"].ToString();

                        Logger.Error("Common Helper -> synapseV3checkUsersOauthKey WEBEXCEPTION - HTTP Code: [" + http_code +
                                     "], Error Msg: [" + reason + "], Original Oauth Key (enc): [" + oauthKey + "]");

                        if (!String.IsNullOrEmpty(reason))
                        {
                            res.msg = "Webexception on refresh attempt: [" + reason + "]";
                        }
                        else
                        {
                            Logger.Error(
                                "Common Helper -> synapseV3checkUsersOauthKey FAILED: Synapse Error, but *reason* was null for [Original Oauth Key (enc): " +
                                oauthKey + "], [Exception: " + we.InnerException + "]");
                        }

                        #endregion Synapse V3 Sig in/ refresh  Exception
                    }

                    #endregion
                }
                else
                {
                    // no record found for given oAuth token in synapse createuser results table
                    Logger.Error("Common Helper -> refreshSynapseV3OautKey FAILED -  no record found for given oAuth key found - " +
                                 "Orig. Oauth Key: (enc) [" + oauthKey + "]");
                    res.msg = "Service error.";
                }

            }
            catch (Exception ex)
            {
                Logger.Error("Common Helper -> synapseV3checkUsersOauthKey FAILED: Outer Catch Error - Orig. OAuth Key (enc): [" + oauthKey +
                             "], [Exception: " + ex + "]");

                res.msg = "Nooch Server Error: Outer Exception.";
            }

            return res;
        }


        public static SynapseBankSetDefaultResult SetSynapseDefaultBank(string MemberId, string BankName, string BankOId)
        {
            Logger.Info("Common Helper -> SetSynapseDefaultBank Initiated. [MemberId: " + MemberId + "], [Bank Name: " +
                                    BankName + "], [BankOId: " + BankOId + "]");

            SynapseBankSetDefaultResult res = new SynapseBankSetDefaultResult();

            #region Check query data
            if (String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(BankName) ||
                String.IsNullOrEmpty(BankOId))
            {
                if (String.IsNullOrEmpty(BankName))
                {
                    res.Message = "Invalid data - need Bank Name";
                }
                else if (String.IsNullOrEmpty(MemberId))
                {
                    res.Message = "Invalid data - need MemberId";
                }
                else if (String.IsNullOrEmpty(BankOId))
                {
                    res.Message = "Invalid data - need Bank Id";
                }

                Logger.Info("Common Helper -> SetSynapseDefaultBank ERROR: [" + res.Message + "] for MemberId: [" + MemberId + "]");
                res.Is_success = false;
                return res;
            }
            #endregion Check query data

            else
            {
                Guid memId = Utility.ConvertToGuid(MemberId);


                // Get Nooch username (primary email address) from MemberId
                var noochUserName = GetMemberUsernameByMemberId(MemberId);
                var MemberInfoInNoochDb = GetMemberDetails(MemberId);

                #region Member Found

                if (MemberInfoInNoochDb != null)
                {
                    // Get bank from saved banks list

                    #region Find the bank to be set as Default
                    string bnaknameEncrypted = GetEncryptedData(BankName);

                    string bankOId = GetEncryptedData(BankOId);


                    //var selectedBank = synapseBankRepository.SelectAll(memberSpecification).FirstOrDefault();

                    // CLIFF (10/7/15): ADDING THIS CODE TO MAKE SURE WE SELECT THE *MOST RECENT* BANK (b/c it creates problems when a user
                    //                  re-attaches the same bank... it has the same ID from Synapse, there may be more than one match)
                    //                  So take the most recent addition...

                    var banksFound = _dbContext.SynapseBanksOfMembers.Where(memberTemp =>
                                        memberTemp.MemberId.Value.Equals(memId) &&
                                        memberTemp.bank_name == bnaknameEncrypted &&
                                        memberTemp.oid == bankOId).ToList();    /// or this would bankid ?? need to check... -Malkit

                    var selectedBank = (from c in banksFound select c)
                                      .OrderByDescending(bank => bank.AddedOn)
                                      .Take(1)
                                      .SingleOrDefault();

                    #endregion Find the bank to be set as Default

                    if (selectedBank != null)
                    {
                        // An existing Bank was found, now mark it as inactive
                        SetOtherBanksInactiveForGivenMemberId(memId);

                        selectedBank.IsDefault = true;

                        // CLIFF (7/13/15): Before we set the Bank's Status, we need to compare the user's Nooch info (name, email, phone, & maybe address)
                        // with the info that Synapse returned for this specific bank.  The problem is that sometimes Synapse will return NULL for 1 or more
                        // pieces of data (for example, for my current Default bank account (PNC), Synapse returned no name, no email, and no phone.
                        // We can only send the Verification email IF SYNAPSE RETURNED AN EMAIL for that bankId.
                        // HERE'S THE LOGIC:
                        // 1.) If name, email, phone (strip out punctuation) all match, then automatically mark this bank's status as "Verified".
                        // 2.) Otherwise (i.e. No match, OR null values from Synapse), mark this bank's status as "Not Verified".
                        // 3.) Check if Synapse returned any Email Address for the bankId.  If YES, send Verification Email to THAT email (NOT the user's Nooch email)
                        // 4.) If NO email returned from Synapse, then send the secondary Bank Verification Email (I'm making a new template, will add to server).
                        //     This will tell the user they must send Nooch any photo ID that matches the name on the bank.
                        //     Then I will have to manually update the bank's status to "Verified" (Need to add a button for this on the Member Details page in the Admin Dash).

                        #region Check if Bank included user info & Compare to Nooch info

                        string noochEmailAddress = GetDecryptedData(MemberInfoInNoochDb.UserName).ToLower();
                        string noochPhoneNumber = RemovePhoneNumberFormatting(MemberInfoInNoochDb.ContactNumber);
                        string noochFirstName = GetDecryptedData(MemberInfoInNoochDb.FirstName).ToLower();
                        string noochLastName = GetDecryptedData(MemberInfoInNoochDb.LastName).ToLower();
                        string noochFullName = noochFirstName + " " + noochLastName;

                        string fullNameFromBank = selectedBank.name_on_account.ToString();
                        string firstNameFromBank = "";
                        string lastNameFromBank = "";
                        string emailFromBank = selectedBank.email != null ? selectedBank.email.ToLower() : "";
                        string phoneFromBank = selectedBank.phone_number != null ? RemovePhoneNumberFormatting(selectedBank.phone_number) : "";

                        bool bankIncludedName = false;
                        bool bankIncludedEmail = false;
                        bool bankIncludedPhone = false;

                        bool nameMatchedExactly = false;
                        bool lastNameMatched = false;
                        bool firstNameMatched = false;
                        bool emailMatchedExactly = false;
                        bool emailMatchedPartly = false;
                        bool phoneMatched = false;

                        #region Check, Parse, & Compare Name from Bank Account
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
                                firstNameMatched = true;
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
                                firstNameMatched = true;
                            }

                            #endregion Compare Name
                        }
                        #endregion Check, Parse, & Compare Name from Bank Account

                        #region Check & Compare Email Address
                        if (!String.IsNullOrEmpty(emailFromBank))
                        {
                            bankIncludedEmail = true;

                            // Compare EMAIL from bank w/ EMAIL from Nooch

                            if (noochEmailAddress == emailFromBank) // Email matches exactly
                            {
                                emailMatchedExactly = true;
                            }
                            else
                            {
                                int emailCompare = noochEmailAddress.IndexOf(emailFromBank);  // Does Nooch EMAIL contain EMAIL from bank?
                                int emailCompare2 = emailFromBank.IndexOf(noochEmailAddress); // Does EMAIL from bank contain Nooch EMAIL?

                                if (emailCompare > -1 || emailCompare2 > -1)
                                {
                                    // This would be when the email addresses are nearly a match, i.e. one contains the other (in case there's some extra character at the beginning or end of the bank email)
                                    emailMatchedPartly = true;
                                }
                            }
                        }
                        #endregion Check & Compare Email Address

                        #region Check & Compare Phone
                        if (!String.IsNullOrEmpty(phoneFromBank))
                        {
                            bankIncludedPhone = true;

                            // Compare PHONE from bank w/ PHONE (i.e. 'ContactNumber' in DB) from Nooch
                            if (noochPhoneNumber == phoneFromBank) // Phone number matches exactly
                            {
                                phoneMatched = true;
                            }
                            else
                            {
                                int phoneCompare = noochPhoneNumber.IndexOf(phoneFromBank);  // Does Nooch PHONE contain PHONE from bank?
                                int phoneCompare2 = phoneFromBank.IndexOf(noochPhoneNumber); // Does PHONE from bank contain Nooch PHONE?

                                if (phoneCompare > -1 || phoneCompare2 > -1)
                                {
                                    // This would be when the phone #'s are nearly a match, i.e. one contains the other (in case there's some extra character at the beginning or end of the bank phone.)
                                    phoneMatched = true;
                                }
                            }
                        }
                        #endregion Check & Compare Phone

                        #endregion Check if Bank included user info & Compare to Nooch info

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


                        #region Scenarios for immediately VERIFYING this bank account

                        if ((bankIncludedName && nameMatchedExactly == true) &&
                            ((bankIncludedEmail && (emailMatchedExactly || emailMatchedPartly)) ||
                             (bankIncludedPhone && phoneMatched)))
                        {
                            // Name Matched exactly and EITHER email or phone matched
                            // Now set this bank account as 'Verified'
                            selectedBank.Status = "Verified";
                            selectedBank.VerifiedOn = DateTime.Now;

                            Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank VERIFIED (Case 1) - Names Matched EXACTLY - MemberId: [" + MemberId +
                                        "]; BankName: [" + BankName + "]; bankIncludedEmail: [" + bankIncludedEmail + "]; bankIncludedPhone: [" + bankIncludedPhone + "]");
                        }

                        else if (bankIncludedName && lastNameMatched &&
                                ((bankIncludedEmail && (emailMatchedExactly || emailMatchedPartly)) ||
                                 (bankIncludedPhone && phoneMatched)))
                        {
                            // Same as previous, except Last Name matched, not full name
                            // (separating this case for Logging purposes even though the outcome is the same as above)
                            selectedBank.Status = "Verified";
                            selectedBank.VerifiedOn = DateTime.Now;

                            Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank VERIFIED (Case 2) - Last Name Matched - [MemberId: " + MemberId +
                                        "];  [BankName: " + BankName + "];  [bankIncludedEmail: " + bankIncludedEmail + "];  [EmailFromBank: " + emailFromBank +
                                        ";  [bankIncludedPhone: " + bankIncludedPhone + "];  [PhoneFromBank: " + phoneFromBank + "]");
                        }

                        else if (bankIncludedName &&
                                 bankIncludedEmail &&
                                 bankIncludedPhone && phoneMatched)
                        {
                            // Name included but no match; email included but no match; phone included AND matches
                            // (separating this case for Logging purposes even though the outcome is the same as above)
                            selectedBank.Status = "Verified";
                            selectedBank.VerifiedOn = DateTime.Now;

                            Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank VERIFIED (Case 3) - Phone Matched - MemberId: [" + MemberId +
                                        "];  [BankName: " + BankName + "];  [bankIncludedEmail: " + bankIncludedEmail + "];  [EmailFromBank: " + emailFromBank +
                                        ";  [bankIncludedPhone: " + bankIncludedPhone + "];  [PhoneFromBank: " + phoneFromBank + "]");
                        }

                        #endregion Scenarios for immediately VERIFYING this bank account


                        #region Scenarios for cases where further verification needed

                        #region Non-Verified Scenario 1 - Email included from bank

                        // Non-Verifed Scenario #1: Some email was included from bank, so send Bank Verification Email Template #1 (With Link)
                        else if (bankIncludedEmail && emailFromBank.Length > 4)
                        {
                            // Bank included some Email which is at least 5 characters long (so not just a dummy letter that the bank might have)
                            // First, Mark as NOT Verified
                            selectedBank.Status = "Not Verified";

                            #region Logging

                            string caseNum = "";

                            if (bankIncludedName &&
                                (nameMatchedExactly || lastNameMatched || firstNameMatched))
                            {
                                caseNum = "4";
                            }
                            else
                            {
                                // Nothing matches, but at least an email was included from the Bank
                                caseNum = "5";
                            }

                            // Bank included some name with at least partial match
                            Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank NOT Verified (Case " + caseNum + ") -  MemberId: [" + MemberId +
                                                   "]; BankName: [" + BankName + "]; bankIncludedName: [" + bankIncludedName + "]; nameMatchedExactly: [" +
                                                   "]; nameMatchedExactly: [" + nameMatchedExactly + "]; lastNameMatched: [" + lastNameMatched + "];  nameFromBank: [" + fullNameFromBank +
                                                   "]; bankIncludedEmail: [" + bankIncludedEmail + "]; EmailFromBank: [" + emailFromBank + "]; bankIncludedPhone: [" + bankIncludedPhone +
                                                   "]; PhoneFromBank: [" + phoneFromBank + "]");

                            #endregion Logging

                            // Now send Bank Verification Email (with Link) to the EMAIL FROM THE BANK ACCOUNT
                            #region Send Verify Bank Email TO EMAIL FROM THE BANK

                            if (emailFromBank == "test@synapsepay.com")
                            {
                                Logger.Info("Common Helper -> SetSynapseDefaultBank TEST USER - EMAIL FROM BANK WAS [test@synapsepay.com] - NOT SENDING BANK VERIFICATION EMAIL");
                            }
                            else
                            {
                                var toAddress = emailFromBank;
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
                                                {Constants.PLACEHOLDER_Recepient_Email, emailFromBank},
                                                {Constants.PLACEHOLDER_BANK_BALANCE, bankLogoUrl},
                                                {Constants.PLACEHOLDER_OTHER_LINK, link}
                                            };

                                try
                                {
                                    Utility.SendEmail("bankEmailVerification",
                                        fromAddress, toAddress, null,
                                        "Your bank account was added to Nooch - Please Verify",
                                        null, tokens, null, "bankAdded@nooch.com", null);

                                    Logger.Info("Common Helper -> SetSynapseDefaultBank --> Bank Verification w/ Link Email sent to: [" +
                                                toAddress + "] for Nooch Username: [" + noochUserName + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Common Helper -> SetSynapseDefaultBank --> Bank Verification w/ Link Email NOT sent to [" +
                                                 toAddress + "] for Nooch Username: [" + noochUserName + "]; Exception: [" + ex.Message + "]");
                                }
                            }

                            #endregion Send Verify Bank Email TO EMAIL FROM THE BANK
                        }

                        #endregion Non-Verified Scenario 1 - Email included from bank

                        #region NonVerified Scanario 2 - Email NOT included from bank

                        // Non-Verifed Scenario #2: Email was NOT included from bank, so send Bank Verification Email Template #2 (No Link)
                        else
                        {
                            selectedBank.Status = "Not Verified";

                            Logger.Info("SetSynapseDefaultBank -> Bank NOT Verified (Case 6) -  MemberId: [" + MemberId +
                                                   "]; BankName: [" + BankName + "]; bankIncludedName: [" + bankIncludedName + "]; nameMatchedExactly: [" +
                                                   "]; nameMatchedExactly: [" + nameMatchedExactly + "]; lastNameMatched: [" + lastNameMatched + "];  nameFromBank: [" + fullNameFromBank +
                                                   "]; bankIncludedEmail: [" + bankIncludedEmail + "]; EmailFromBank: [" + emailFromBank + "]; bankIncludedPhone: [" + bankIncludedPhone +
                                                   "]; PhoneFromBank: [" + phoneFromBank + "]");

                            // If the User's ID was verified successfully, then don't send the Verfication email.  But keep the bank as "not verified" until a Nooch admin reviews
                            if (MemberInfoInNoochDb.IsVerifiedWithSynapse == true)
                            {
                                Logger.Info("Common Helper -> SetSynapseDefaultBank -> Bank was not verified, but user's SSN verification was successful, so " +
                                            "NOT sending the Verification Email - [MemberID: " + MemberId + "], [Username: " + noochEmailAddress + "]");

                                StringBuilder st = new StringBuilder("<br/><p><strong>This user's Nooch Account information is:</strong></p>" +
                                          "<table border='1' style='border-collapse:collapse;'>" +
                                          "<tr><td><strong>MemberID:</strong></td><td>" + MemberId + "</td></tr>" +
                                          "<tr><td><strongNooch_ID:</strong></td><td>" + MemberInfoInNoochDb.Nooch_ID + "</td></tr>" +
                                          "<tr><td><strong>Nooch Name:</strong></td><td>" + noochFullName + "</td></tr>" +
                                          "<tr><td><strong>Bank Included Name?:</strong></td><td>" + bankIncludedName + "</td></tr>" +
                                          "<tr><td><strong>Name From Bank:</strong></td><td>" + fullNameFromBank + "</td></tr>" +
                                          "<tr><td><strong>nameMatchedExactly:</strong></td><td>" + nameMatchedExactly + "</td></tr>" +
                                          "<tr><td><strong>lastNameMatched:</strong></td><td>" + lastNameMatched + "</td></tr>" +
                                          "<tr><td><strong>Nooch Email Address:</strong></td><td>" + noochEmailAddress + "</td></tr>" +
                                          "<tr><td><strong>Bank Included Email?:</strong></td><td>" + bankIncludedEmail + "</td></tr>" +
                                          "<tr><td><strong>Email Address From Bank:</strong></td><td>" + emailFromBank + "</td></tr>" +
                                          "<tr><td><strong>emailMatchedExactly:</strong></td><td>" + emailMatchedExactly + "</td></tr>" +
                                          "<tr><td><strong>Nooch Phone #:</strong></td><td>" + MemberInfoInNoochDb.ContactNumber + "</td></tr>" +
                                          "<tr><td><strong>Bank Included Phone #?:</strong></td><td>" + bankIncludedPhone + "</td></tr>" +
                                          "<tr><td><strong>Phone # From Bank:</strong></td><td>" + phoneFromBank + "</td></tr>" +
                                          "<tr><td><strong>Phone Matched?:</strong></td><td>" + phoneMatched + "</td></tr>" +
                                          "<tr><td><strong>Address:</strong></td><td>" + CommonHelper.GetDecryptedData(MemberInfoInNoochDb.Address) +
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
                                                {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(noochFirstName)},
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

                        #endregion Scenarios for cases where further verification needed

                        // FINALLY, UPDATE THIS BANK IN NOOCH DB
                        _dbContext.SaveChanges();
                        res.Message = "Success";
                        res.Is_success = true;
                        _dbContext.Entry(MemberInfoInNoochDb).Reload();

                        return res;
                    }
                    else
                    {
                        Logger.Info("Common Helper -> SetSynapseDefaultBank ERROR: Selected Bank not found in Nooch DB - MemberId: [" + MemberId + "]; BankId: [" + BankOId + "]");

                        res.Message = "Bank not found for given Member";
                        res.Is_success = false;
                        return res;
                    }
                }

                #endregion Member Found

                #region Member NOT Found
                else
                {
                    Logger.Info("Common Helper -> SetSynapseDefaultBank ERROR: Member not found in Nooch DB - MemberId: [" + MemberId + "]; BankId: [" + BankOId + "]");
                    res.Message = "Member not found";
                    res.Is_success = false;
                    return res;
                }
                #endregion Member NOT Found
            }
        }

        private static void SetOtherBanksInactiveForGivenMemberId(Guid memId)
        {

            var selectedBank =
                _dbContext.SynapseBanksOfMembers.Where(memberTemp =>
                        memberTemp.MemberId.Value.Equals(memId) && memberTemp.IsDefault != false).ToList();
            //if (selectedBank.Count>0)
            //{
            //    _dbContext.Entry(selectedBank).Reload();
            //}
            foreach (SynapseBanksOfMember sbank in selectedBank)
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
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (1) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - [MemberId: " + MemberId + "]");
                        }
                    }
                    else
                    {
                        // Otherwise, make a new entry
                        MembersIPAddress mip = new MembersIPAddress();
                        mip.MemberId = memId;
                        mip.ModifiedOn = DateTime.Now;
                        mip.Ip = IP;

                        int b = _dbContext.SaveChanges();

                        if (b > 0)
                        {
                            _dbContext.Entry(mip).Reload();
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (2) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - [MemberId: " + MemberId + "]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - [MemberId: " + MemberId + "]");
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
                        Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving Device ID - [MemberId: " + MemberId + "]");
                        udidIdSavedSuccessfully = true;
                    }
                    else
                    {
                        Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - [MemberId: " + MemberId + "]");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Info("Common Helper -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - [MemberId: " + MemberId + "]");
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
            Logger.Info("Common Helper -> GetMemberIdByContactNumber Initiated - [userContactNumber: " + userContactNumber + "]");

            string trimmedContactNum = CommonHelper.RemovePhoneNumberFormatting(userContactNumber);

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
                        // update nooch member password
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

    }
}
