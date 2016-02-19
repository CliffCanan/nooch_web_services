using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities.MobileAppOutputEnities;
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

        public static SynapseBanksOfMember GetSynapseBankAccountDetails(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBanksOfMembers.FirstOrDefault(m => m.MemberId == id && m.IsDefault == true);

            return memberAccountDetails;

        }



        public static MemberNotification GetMemberNotificationSettingsByUserName(string userName)
        {
            //Logger.LogDebugMessage("MDA -> GetMemberNotificationSettingsByUserName - UserName: [" + userName + "]");

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
            Member mem= _dbContext.Members.Find(memberEntity);


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
                            return IncreaseInvalidPinAttemptCount( memberEntity, pinRetryCountInDb);
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
                        return IncreaseInvalidPinAttemptCount( memberEntity, pinRetryCountInDb);
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
                            Utility.SendEmail("userSuspended",  fromAddress, emailAddress,
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
                    return IncreaseInvalidPinAttemptCount( memberEntity, pinRetryCountInDb);
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
                    
                var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted==false);

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
            //Logger.LogDebugMessage("MDA -> GetMemberNotificationSettings Initiated - [MemberId: " + memberId + "]");



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
                    var transactionEntity = _dbContext.Transactions.FirstOrDefault(n=>n.TransactionTrackingId==randomId);
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
            //Logger.LogDebugMessage("MDA -> GetLandlordDetails - MemberId: [" + memberId + "]");
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
    }
}
