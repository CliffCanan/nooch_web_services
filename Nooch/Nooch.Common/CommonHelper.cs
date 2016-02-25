using System;
using System.Collections.Generic;
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

namespace Nooch.Common
{
    public static class CommonHelper
    {
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
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

        public static string GetPhoneNumberByMemberId(string MemberId)
        {

            Guid memGuid = Utility.ConvertToGuid(MemberId);

            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.MemberId == memGuid && m.IsDeleted == false);

            return noochMember != null ? noochMember.ContactNumber : null;
        }

        public static string GetMemberIdByPhone(string memberPhone)
        {



            var noochMember =
                _dbContext.Members.FirstOrDefault(
                    m => m.ContactNumber == memberPhone && m.IsDeleted == false);

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


        public static Member GetMemberDetails(string memberId)
        {

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (noochMember != null)
                {
                    return noochMember;
                }

            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetMemberDetails FAILED - Member ID: [" + memberId + "], [Exception: " + ex + "]");
            }
            return new Member();
        }

        public static List<SynapseBankLoginResult> GetSynapseBankLoginResulList(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBankLoginResults.Where(m => m.MemberId == id && m.IsDeleted== false).ToList();

            return memberAccountDetails;

        }

        public static SynapseBanksOfMember GetSynapseBankAccountDetails(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBanksOfMembers.FirstOrDefault(m => m.MemberId == id && m.IsDefault == true);

            return memberAccountDetails;

        }

        public static SynapseCreateUserResult GetSynapseCreateaUserDetails(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted== false);

            return memberAccountDetails;

        }



        public static MemberNotification GetMemberNotificationSettingsByUserName(string userName)
        {
            //Logger.Info("MDA -> GetMemberNotificationSettingsByUserName - UserName: [" + userName + "]");

            userName = GetEncryptedData(userName);

            var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.Member.UserName == userName);


            return memberNotifications;

        }

        public static string IncreaseInvalidLoginAttemptCount(
            string memGuid, int loginRetryCountInDb)
        {
            Logger.Info("MDA -> IncreaseInvalidLoginAttemptCount Initiated (User's PW was incorrect during login attempt) - " +
                                   "This is invalid attempt #: [" + (loginRetryCountInDb + 1).ToString() + "], " +
                                   "MemberId: [" + memGuid + "]");

            Member m = GetMemberDetails(memGuid);

            m.InvalidLoginTime = DateTime.Now;
            m.InvalidLoginAttemptCount = loginRetryCountInDb + 1;
            _dbContext.SaveChanges();
            return "The password you have entered is incorrect."; // incorrect password
        }


        public static bool UpdateAccessToken(string userName, string AccessToken)
        {
            Logger.Info("MDA -> UpdateAccessToken - userName: [" + userName + "]");


            try
            {
                userName = GetEncryptedData(userName);
                //Get the member details

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == userName && m.IsDeleted == false);

                if (noochMember != null)
                {
                    noochMember.AccessToken = AccessToken;
                    _dbContext.SaveChanges();

                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> UpdateAccessToken FAILED - [Exception: " + ex + "]");
                return false;
            }

        }

        public static bool CheckTokenExistance(string AccessToken)
        {

            try
            {
                //Get the member details

                var noochMember = _dbContext.Members.FirstOrDefault(m => m.AccessToken == AccessToken && m.IsDeleted == false);
                return noochMember != null;
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> CheckTokenExistance FAILED - [Exception: " + ex + "]");
                return false;
            }

        }



        public static bool IsListedInSDN(string lastName, Guid userId)
        {
            bool result = false;
            Logger.Info("MDA -> IsListedInSDNList - userName: [" + lastName + "]");


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

            }

            return result;
        }

        private static string IncreaseInvalidPinAttemptCount(
            Member memberEntity, int pinRetryCountInDb)
        {
            Member mem = _dbContext.Members.Find(memberEntity);


            mem.InvalidPinAttemptCount = pinRetryCountInDb + 1;
            mem.InvalidPinAttemptTime = DateTime.Now;
            _dbContext.SaveChanges();
            return memberEntity.InvalidPinAttemptCount == 1
                ? "PIN number you have entered is incorrect."
                : "PIN number you entered again is incorrect. Your account will be suspended for 24 hours if you enter wrong PIN number again.";
        }

        public static string ValidatePinNumber(string memberId, string pinNumber)
        {

            var id = Utility.ConvertToGuid(memberId);


            var memberEntity = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);


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
                    return !String.IsNullOrEmpty(memberObj.TransferLimit) ? memberObj.TransferLimit : "0";
                }
                return "0";
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> GetGivenMemberTransferLimit FAILED - [MemberID: " + memberId + "], [Exception: " + ex + "]");
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
                    Logger.Info("*****  TDA -> isOverTransactionLimit - Transaction for TEAM NOOCH, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }
                if (senderMemId.ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Marvis Burns (RentScene)
                    recipMemId.ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49")
                {
                    Logger.Info("*****  TDA -> isOverTransactionLimit - Transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }

                if (senderMemId.ToLower() == "c9839463-d2fa-41b6-9b9d-45c7f79420b1" || // Sherri Tan (RentScene - via Marvis Burns)
                    recipMemId.ToLower() == "c9839463-d2fa-41b6-9b9d-45c7f79420b1")
                {
                    Logger.Info("*****  TDA -> isOverTransactionLimit - Transaction for RENT SCENE, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }
                if (senderMemId.ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484" || // Josh Detweiler (AppJaxx)
                    recipMemId.ToLower() == "8b4b4983-f022-4289-ba6e-48d5affb5484")
                {
                    Logger.Info("*****  TDA -> isOverTransactionLimit - Transaction is for APPJAXX, so allowing transaction - [Amount: $" + amount.ToString() + "]  ****");
                    return false;
                }

                return true;
            }

            return false;
        }


        public static MemberNotification GetMemberNotificationSettings(string memberId)
        {
            //Logger.Info("MDA -> GetMemberNotificationSettings Initiated - [MemberId: " + memberId + "]");



            Guid memId = Utility.ConvertToGuid(memberId);


            var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.Member.MemberId == memId);

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
            //Logger.Info("MDA -> GetLandlordDetails - MemberId: [" + memberId + "]");
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var landlordObj = _dbContext.Landlords.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (landlordObj != null)
                {
                    return landlordObj;
                }

            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetLandlordDetails FAILED - Member ID: [" + memberId + "], [Exception: " + ex + "]");
            }

            return new Landlord();
        }


        public static String ConvertImageURLToBase64(String url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                Logger.Info("MDA -> ConvertImageURLToBase64 Initiated - Photo URL is: [" + url + "]");

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
                Logger.Error("MDA -> GetImage FAILED - Photo URL was: [" + url + "]. Exception: [" + ex + "]");
                buf = null;
            }

            return (buf);
        }

        public static string GetRecentOrDefaultIPOfMember(Guid MemberIdPassed)
        {
            string RecentIpOfUser = "";


            var memberIP = _dbContext.MembersIPAddresses.OrderByDescending(m => m.ModifiedOn).FirstOrDefault(m => m.MemberId == MemberIdPassed);

            RecentIpOfUser = memberIP != null ? memberIP.ToString() : "54.201.43.89";

            return RecentIpOfUser;
        }


     
        
        
        public static  submitIdVerificationInt sendUserSsnInfoToSynapseV3(string MemberId)
        {
            Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            submitIdVerificationInt res = new submitIdVerificationInt();
            res.success = false;

            kycInfoResponseFromSynapse synapseResponse = new kycInfoResponseFromSynapse();

            var id = Utility.ConvertToGuid(MemberId);

            var memberEntity = GetMemberDetails(MemberId);

                if (memberEntity != null)
                {
                    string usersFirstName = UppercaseFirst(GetDecryptedData(memberEntity.FirstName));
                    string usersLastName = UppercaseFirst(GetDecryptedData(memberEntity.LastName));

                    string usersAddress = "";
                    string usersCity = "";
                    string usersZip = "";

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
                        if (string.IsNullOrEmpty(memberEntity.UDID1.ToString()))
                        {
                            isMissingSomething = true;
                            res.message = "MDA - Missing UDID";
                        }
                        else
                        {
                            usersFingerprint = memberEntity.UDID1;
                        }

                        // Check for Address
                        if (string.IsNullOrEmpty(memberEntity.Address.ToString()))
                        {
                            isMissingSomething = true;
                            res.message += " MDA - Missing Address";
                        }
                        else
                        {
                            usersAddress = GetDecryptedData(memberEntity.Address);
                        }

                        // Check for City
                        if (string.IsNullOrEmpty(memberEntity.City.ToString()))
                        {
                            isMissingSomething = true;
                            res.message += " MDA - Missing City";
                        }
                        else
                        {
                            usersCity = GetDecryptedData(memberEntity.City);
                        }

                        // Check for ZIP
                        if (string.IsNullOrEmpty(memberEntity.Zipcode.ToString()))
                        {
                            isMissingSomething = true;
                            res.message += " MDA - Missing ZIP";
                        }
                        else
                        {
                            usersZip = GetDecryptedData(memberEntity.Zipcode);
                        }

                        // Check for SSN
                        if (string.IsNullOrEmpty(memberEntity.SSN.ToString()))
                        {
                            isMissingSomething = true;
                            res.message += " MDA - Missing SSN";
                        }
                        else
                        {
                            usersSsnLast4 = GetDecryptedData(memberEntity.SSN);
                        }

                        // Check for Date Of Birth (Not encrypted)
                        if (string.IsNullOrEmpty(memberEntity.DateOfBirth.ToString()))
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
                            Logger.Error("MDA -> sendUserSsnInfoToSynapseV3 ABORTED: Member has no DoB. [MemberId: " + MemberId + "], [Message: " + res.message + "]");
                            return res;
                        }


                        // Now check if user already has a Synapse User account (would have a record in SynapseCreateUserResults.dbo)
                        
                        var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m=>m.MemberId==id && m.IsDeleted==false);

                        if (usersSynapseDetails == null)
                        {
                            Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                            return res;
                        }
                        else
                        {
                            usersSynapseOauthKey = GetDecryptedData(usersSynapseDetails.access_token);
                        }

                        #endregion Check User For All Required Data
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> sendUserSsnInfoToSynapseV3 FAILED on checking for all required data - [MemberID: " +
                                               MemberId + "], [Exception: " + ex + "]");
                    }

                    // Update Member's DB record from NULL to false (update to true later on if Verification from Synapse is completely successful)
                    Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 - About to set IsVerifiedWithSynapse to False before calling Synapse: [MemberID: " + MemberId + "]");
                    memberEntity.IsVerifiedWithSynapse = false;

                    #region Send SSN Info To Synapse

                    try
                    {
                        #region Call Synapse V3 /user/doc/add API

                        Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 - Checkpoint 10693 - About To Query Synapse");

                        synapseAddKycInfoInputV3Class synapseInput = new synapseAddKycInfoInputV3Class();

                        SynapseV3Input_login login = new SynapseV3Input_login();
                        login.oauth_key = usersSynapseOauthKey;
                        synapseInput.login = login;

                        addKycInfoInput_user_doc doc = new addKycInfoInput_user_doc();
                        doc.birth_day = usersDobDay;
                        doc.birth_month = usersDobMonth;
                        doc.birth_year = usersDobYear;
                        doc.name_first = usersFirstName;
                        doc.name_last = usersLastName;
                        doc.address_street1 = usersAddress;
                        doc.address_postal_code = usersZip;
                        doc.address_country_code = "US";

                        doc.document_value = usersSsnLast4;
                        doc.document_type = "SSN";

                        addKycInfoInput_user user = new addKycInfoInput_user();
                        user.fingerprint = usersFingerprint;
                        user.doc = doc;

                        synapseInput.user = user;

                        string baseAddress = "https://sandbox.synapsepay.com/api/v3/user/doc/add";
                        //string baseAddress = "https://synapsepay.com/api/v3/user/doc/add";

                        // CLIFF (10/10/15): Adding the following for testing purposes only
                        #region For Testing
                        if (GetDecryptedData(memberEntity.UserName).IndexOf("jones00") > -1)
                        {
                            Logger.Info("****  sendUserSSNInfoToSynapseV3 -> JUST A TEST BLOCK REACHED!  ****");
                            baseAddress = "https://sandbox.synapsepay.com/api/v3/user/doc/add";
                        }
                        else if (memberEntity.MemberId.ToString().ToLower() == "b3a6cf7b-561f-4105-99e4-406a215ccf60")
                        {
                            doc.name_last = "Satell";
                            doc.document_value = "7562";
                        }

                        try
                        {
                            Logger.Info("Payload to send to Synapse: [OauthKey: " + login.oauth_key +
                                "], [Birth_day: " + doc.birth_day + "], [Birth_month: " + doc.birth_month +
                                "], [Birth_year: " + doc.birth_year + "], [name_first: " + doc.name_first +
                                "], [name_last: " + doc.name_last + "], [ssn: " + doc.document_value +
                                "], [address_street1: " + doc.address_street1 + "], [post_code: " + doc.address_postal_code +
                                "], [country_code: " + doc.address_country_code + "], [Fingerprint: " + user.fingerprint +
                                "], [BASE_ADDRESS: " + baseAddress + "].");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> sendUserSSNInfoToSynapseV3 - Couldn't log Synapse SSN Payload. [Exception: " + ex + "]");
                        }
                        #endregion For Testing


                        var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                        http.Accept = "application/json";
                        http.ContentType = "application/json";
                        http.Method = "POST";

                        string parsedContent = JsonConvert.SerializeObject(synapseInput);
                        ASCIIEncoding encoding = new ASCIIEncoding();
                        Byte[] bytes = encoding.GetBytes(parsedContent);

                        Stream newStream = http.GetRequestStream();
                        newStream.Write(bytes, 0, bytes.Length);
                        newStream.Close();

                        var response = http.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        synapseResponse = JsonConvert.DeserializeObject<kycInfoResponseFromSynapse>(content);

                        #endregion Call Synapse V3 /user/doc/add API


                        // NOW WE MUST PARSE THE SYNAPSE RESPONSE. THERE ARE 3 POSSIBLE SCENARIOS:
                        // 1.) SSN Validation was successful. Synapse returns {"success": true}
                        // 2.) SSN Validation was PARTLY successful.  Synapse returns: "success":true... 
                        //     plus an object "question_set", containing a series of questions and array of multiple choice answers for each question.
                        //     We will display the questions to the user via the IDVerification.aspx page (already built-in to the Add-Bank process)
                        // 3.) SSN Validation Failed  (not sure what Synapse returns for this... their docs are not clear).

                        if (synapseResponse != null)
                        {
                            if (synapseResponse.success == true)
                            {
                                Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 - Synapse returned SUCCESS = TRUE");

                                res.success = true;

                                // Great, we have at least partial success.  Now check if further verification is needed by checking if Synapse returned a 'question_set' object.
                                if (synapseResponse.question_set != null)
                                {
                                    // Further Verification is needed...
                                    res.message = "additional questions needed";

                                    // Now make sure an Array[] set of 'questions' was returned (could be up to 5 questions, each with 5 possible answer choices)
                                    if (synapseResponse.question_set.questions != null)
                                    {
                                        Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 - Question_Set was returned, further validation will be needed. Saving ID Verification Questions...");

                                        // Saving these questions in DB.  

                                        // UPDATE (9/29/15):
                                        // The user will have to answer these on the IDVerification.aspx page.
                                        // That's why I updated the sendSSN function to not be void and return success + a message. Based on that value,
                                        // the Add-Bank page will direct the user either to the IDVerification page (via iFrame), or not if questions are not needed.

                                        Guid memGuid = Utility.ConvertToGuid(MemberId);

                                        // Loop through each question set (question/answers/id)
                                        #region Iterate through each question to save in DB

                                        foreach (synapseIdVerificationQuestionAnswerSet question in synapseResponse.question_set.questions)
                                        {
                                            SynapseIdVerificationQuestion questionForDb = new SynapseIdVerificationQuestion();

                                            Guid memId = memGuid;
                                            questionForDb.MemberId = memId;
                                            questionForDb.QuestionSetId = synapseResponse.question_set.id;
                                            questionForDb.SynpQuestionId = question.id;

                                            questionForDb.DateCreated = DateTime.Now;
                                            questionForDb.submitted = false;

                                            questionForDb.person_id = synapseResponse.question_set.person_id;
                                            questionForDb.time_limit = synapseResponse.question_set.time_limit;
                                            questionForDb.score = synapseResponse.question_set.score;
                                            questionForDb.updated_at = synapseResponse.question_set.updated_at.ToString();
                                            questionForDb.livemode = synapseResponse.question_set.livemode;
                                            questionForDb.expired = synapseResponse.question_set.expired;
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
                                        #endregion Iterate through each question to save in DB
                                    }
                                }
                                else if (synapseResponse.user != null)
                                {
                                    // User is verified completely -- in this case response in same of register use with Synapse...
                                    // Just update permission in CreateSynapseUserResults table

                                    #region Update Permission in CreateSynapseUserResults Table
                                    try
                                    {
                                        // Get existing record from Create Synapse User Results table for this Member

                                        var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m=>m.MemberId==id && m.IsDeleted==false);

                                        if (synapseRes != null)
                                        {
                                            synapseRes.permission = synapseResponse.user.permission;
                                            _dbContext.SaveChanges();
                                            
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> sendUserSsnInfoToSynapseV3 - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
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
                                Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 FAILED: Synapse Result \"success != true\" - [MemberId: " + MemberId + "]");
                                res.message = "SSN response from synapse was false";
                            }
                        }
                        else
                        {
                            // Response from Synapse was null
                            Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 FAILED: Synapse Result was NULL - [MemberId: " + MemberId + "]");
                            res.message = "SSN response from synapse was null";
                        }

                    }
                    catch (WebException we)
                    {
                        var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;

                        Logger.Error("MDA -> sendUserSsnInfoToSynapseV3 FAILED (Outer) - [errorCode: " + httpStatusCode.ToString() + "], [Message" + we.Message + "]");

                        res.message = "MDA exception";

                        var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject errorJsonFromSynapse = JObject.Parse(response);

                        // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors
                        //                   We might have to do different things depending on which error is returned... for now just pass
                        //                   back the error number & msg to the function that called this method.
                        string error_code = errorJsonFromSynapse["error_code"].ToString();
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
                                st.Append("<tr><td><strong>Nooch_ID</strong></td><td>" + memberEntity.Nooch_ID + "</td></tr>");
                                st.Append("<tr><td><strong>DOB</strong></td><td>" + Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MMMM dd, yyyy") + "</td></tr>");
                                st.Append("<tr><td><strong>SSN</strong></td><td>" + usersSsnLast4 + "</td></tr>");
                                st.Append("<tr><td><strong>Address</strong></td><td>" + usersAddress + "</td></tr>");
                                st.Append("<tr><td><strong>City</strong></td><td>" + city + "</td></tr>");
                                st.Append("<tr><td><strong>ZIP</strong></td><td>" + usersZip + "</td></tr>");
                                st.Append("<tr><td><strong>Contact #</strong></td><td>" + CommonHelper.FormatPhoneNumber(memberEntity.ContactNumber) + "</td></tr>");
                                st.Append("<tr><td><strong>Phone Verified?</strong></td><td>" + memberEntity.IsVerifiedPhone.ToString() + "</td></tr>");
                                st.Append("<tr><td><strong>IsVerifiedWithSynapse</strong></td><td>" + memberEntity.IsVerifiedWithSynapse.ToString() + "</td></tr>");
                                st.Append("<tr><td><strong>Status</strong></td><td><strong>" + memberEntity.Status + "</strong></td></tr>");
                                st.Append("</table>");

                                StringBuilder completeEmailTxt = new StringBuilder();
                                string s = "<html><body><h2>Nooch SSN Verification Failure</h2><p style='margin:0 auto 20px;'>The following Nooch user just triggered an SSN Verification attempt, but failed:</p>"
                                           + st.ToString() +
                                           "<br/><br/><small>This email was generated automatically during <strong>[MDA -> sendUserSsnInfoToSynapse]</strong>.</small></body></html>";

                                completeEmailTxt.Append(s);
                                Utility.SendEmail(null,  "SSNFAILURE@nooch.com", "cliff@nooch.com",
                                                            null, "NOOCH USER'S SSN (V3) VALIDATION FAILED", null, null, null, null, completeEmailTxt.ToString());
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> sendUserSsnInfoToSynapseV3 FAILED - Attempted to notify Nooch Admin via email but got Exception: [" + ex + "]");
                            }
                            #endregion Notify Nooch Admin About Failed SSN Validation


                            // Now try to send ID verification document (IF AVAILABLE... WHICH IT PROBABLY WON'T BE)
                            // CLIFF (10/10/15): I guess we will have to add more code depending on what the response for this next line is...
                            submitDocumentToSynapseV3(memberEntity.MemberId.ToString(), memberEntity.Photo);
                        }
                    }

                    // Save changes to Members DB
                    memberEntity.DateModified = DateTime.Now;
                    _dbContext.SaveChanges();

                    #endregion Parse Synapse Response
                }
                else
                {
                    // Member not found in Nooch DB
                    Logger.Info("MDA -> sendUserSsnInfoToSynapseV3 FAILED: Member not found - [MemberId: " + MemberId + "]");
                    res.message = "Member not found";
                }
            

            return res;
        }


        public static GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
        {
            Logger.Info("MDA -> submitDocumentToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            var id = Utility.ConvertToGuid(MemberId);

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();

            
                #region Get User's Synapse OAuth Consumer Key

                string usersSynapseOauthKey = "";

                
                var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m=>m.MemberId==id && m.IsDeleted==false);

                if (usersSynapseDetails == null)
                {
                    Logger.Info("MDA -> submitDocumentToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");

                    res.message = "Could not find this member's account info";

                    return res;
                }
                else
                {
                    usersSynapseOauthKey = CommonHelper.GetDecryptedData(usersSynapseDetails.access_token);
                }

                #endregion Get User's Synapse OAuth Consumer Key


                #region Get User's Fingerprint

                string usersFingerprint = "";

                
                var member = GetMemberDetails(id.ToString());

                if (member == null)
                {
                    Logger.Info("MDA -> submitDocumentToSynapseV3 ABORTED: Member not found. [MemberId: " + MemberId + "]");
                    res.message = "Member not found";

                    return res;
                }
                else
                {
                    if (String.IsNullOrEmpty(member.UDID1))
                    {
                        Logger.Info("MDA -> submitDocumentToSynapseV3 ABORTED: Member's Fingerprint not found. [MemberId: " + MemberId + "]");
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
                //answers.login.oauth_key = usersSynapseOauthKey;


                submitDocToSynapse_user sdtu = new submitDocToSynapse_user();
                submitDocToSynapse_user_doc doc = new submitDocToSynapse_user_doc();
                doc.attachment = "data:text/csv;base64," + ConvertImageURLToBase64(ImageUrl).Replace("\\", "");

                sdtu.fingerprint = usersFingerprint;

                sdtu.doc = doc;

                answers.user = sdtu;

                //answers.user.doc.attachment = "data:text/csv;base64," + ConvertImageURLToBase64(ImageUrl).Replace("\\", ""); // NEED TO GET THE ACTUAL DOC... EITHER PASS THE BYTES TO THIS METHOD, OR GET FROM DB
                //answers.user.fingerprint = usersFingerprint;

                string baseAddress = "https://sandbox.synapsepay.com/api/v3/user/doc/attachments/add";
                //string baseAddress = "https://synapsepay.com/api/v3/user/doc/attachments/add";

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
                            Logger.Info("MDA -> submitDocumentToSynapseV3 SUCCESSFUL - [MemberID: " + MemberId + "]");

                            res.success = true;

                            res.message = "";
                        }
                        else
                        {
                            res.message = "Got a response, but success was not true";
                            Logger.Info("MDA -> submitDocumentToSynapseV3 FAILED - Got Synapse response, but success was NOT 'true' - [MemberID: " + MemberId + "]");
                        }
                    }
                    else
                    {
                        res.message = "Verification response was null";
                        Logger.Info("MDA -> submitDocumentToSynapseV3 FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");
                    }
                }
                catch (WebException ex)
                {
                    res.message = "MDA Exception #9575";
                    Logger.Info("MDA -> submitDocumentToSynapseV3 FAILED - Catch [Exception: " + ex + "]");
                }

                #endregion Call Synapse /user/doc/attachments/add API
            

            return res;
        }



        public static synapseSearchUserResponse getUserPermissionsForSynapseV3(string userEmail)
        {
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

                //string UrlToHit = "https://synapsepay.com/api/v3/user/search";
                string UrlToHit = "https://sandbox.synapsepay.com/api/v3/user/search";

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
                        Logger.Error("TDA -> getUserPermissionsForSynapseV3 FAILED - [Synapse Error Code: " + res.error_code +
                                               "], [Error Msg: " + res.errorMsg + "], [User Email: " + userEmail + "]");
                    }
                    else
                    {
                        Logger.Error("TDA -> getUserPermissionsForSynapseV3 FAILED: Synapse Error, but *error_code* was null for [User Email: " +
                                               userEmail + "], [Exception: " + we.InnerException + "]");
                    }

                    #endregion Synapse V3 Get User Permissions Exception
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> getUserPermissionsForSynapseV3 FAILED: Outer Catch Error - [User Email: " + userEmail +
                                       "], [Exception: " + ex.InnerException + "]");

                res.error_code = "Nooch Server Error: Outer Exception.";
            }

            return res;
        }


        public static NodePermissionCheckResult IsNodeActiveInGivenSetOfNodes(synapseSearchUserResponse_Node[] allNodes, string nodeToMatch)
        {
            NodePermissionCheckResult res = new NodePermissionCheckResult();

            res.IsPermissionfound = false;

            foreach (synapseSearchUserResponse_Node node in allNodes)
            {
                if (node._id != null && node._id.oid == nodeToMatch)
                {
                    if (!String.IsNullOrEmpty(node.allowed))
                    {
                        res.IsPermissionfound = true;
                        res.PermissionType = node.allowed;
                        break;
                    }
                }
            }

            return res;
        }
    }
}
