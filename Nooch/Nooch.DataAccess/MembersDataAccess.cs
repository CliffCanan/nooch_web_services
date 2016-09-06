using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nooch.Common;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Common.Resources;
using Nooch.Common.Rules;
using Nooch.Data;
using System.Web;
using System.Web.Hosting;

using System.Drawing;

namespace Nooch.DataAccess
{
    public class MembersDataAccess
    {
        private readonly NOOCHEntities _dbContext = null;


        public MembersDataAccess()
        {
            _dbContext = new NOOCHEntities();
        }


        public Member GetMemberByGuid(Guid memberGuid)
        {
            return _dbContext.Members.FirstOrDefault(m => m.MemberId == memberGuid);
        }


        public bool resetlinkvalidationcheck(string memberId)
        {
            Logger.Info("MDA -> resetLinkValidationCheck Fired - MemberID: [" + memberId + "]");

            using (var noochConnection = new NOOCHEntities())
            {
                try
                {
                    var id = Utility.ConvertToGuid(memberId);

                    var noochMember = noochConnection.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                    if (noochMember != null)
                    {
                        // checking password reset request time
                        var resereq = noochConnection.PasswordResetRequests.OrderByDescending(m => m.Id)
                                                     .Take(1)
                                                     .FirstOrDefault(m => m.MemberId == id);

                        if (resereq == null) return false;
                        else
                        {
                            DateTime req = Convert.ToDateTime(resereq.RequestedOn == null ? DateTime.Now : resereq.RequestedOn);

                            if (DateTime.Now < req.AddHours(3)) return true;
                            else return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> resetlinkvalidationcheck FAILED - Exception: [" + ex + "]");
                }

                return false;
            }
        }


        /// <summary>
        /// To change a user's password.
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="newPassword"></param>
        /// <param name="newUser"></param>
        /// <returns>Bool for success/failure</returns>
        public bool ResetPassword(string memberId, string newPassword, string newUser)
        {
            Logger.Info("MDA -> ResetPassword Fired - MemberID: [" + memberId + "]");

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id &&
                                                                         m.IsDeleted == false);

                if (memberObj != null)
                {
                    bool shouldReset = false;

                    if (String.IsNullOrEmpty(newUser))
                    {
                        // checking password reset request time
                        var resetRequestTime = _dbContext.PasswordResetRequests.OrderByDescending(m => m.Id).Take(1)
                                                                                    .FirstOrDefault(m => m.MemberId == id);

                        if (resetRequestTime == null) return false;

                        DateTime req = resetRequestTime.RequestedOn == null
                                       ? DateTime.Now
                                       : Convert.ToDateTime(resetRequestTime.RequestedOn);

                        if (DateTime.Now < req.AddHours(3)) shouldReset = true;
                    }
                    else if (newUser.ToLower().Trim() == "true")
                        shouldReset = true;

                    if (shouldReset)
                    {
                        // Now update the user's password
                        memberObj.Password = newPassword.Replace(" ", "+");
                        memberObj.DateModified = DateTime.Now;

                        var emailAddress = CommonHelper.GetDecryptedData(memberObj.UserName);

                        if (_dbContext.SaveChanges() > 0)
                        {
                            // Cliff (1/21/16): Only send the confirmation email if it's truly a Reset... as opposed to a new user setting their PW
                            // from one of the browser pages, which also uses this service to set the pw after linking a bank.
                            if (!String.IsNullOrEmpty(newUser) && newUser.ToLower().Trim() != "true")
                                SendPasswordUpdatedMail(memberObj, emailAddress);

                            Logger.Info("MDA -> ResetPassword - PW Reset Successfully for Username: [" + emailAddress +
                                        "], MemberID: [" + memberId + "]");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> ResetPassword FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
            }

            return false;
        }


        private static bool SendPasswordUpdatedMail(Member member, string primaryMail)
        {
            var fromAddress = Utility.GetValueFromConfig("adminMail");
            var tokens = new Dictionary<string, string>
            {
                {
                    Constants.PLACEHOLDER_FIRST_NAME,
                    CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName))
                }
            };

            return Utility.SendEmail("passwordChanged", fromAddress, primaryMail, null,
                                     "Your Nooch password was changed", null, tokens, null, null, null);
        }


        /// <summary>
        /// For resending a verification SMS message to the user to verify their phone number.
        /// </summary>
        /// <param name="Username"></param>
        /// <returns></returns>
        public string ResendVerificationSMS(string Username)
        {
            //1. Check if user exists
            //2. Check if phone is already verified

            Member memberObj = CommonHelper.GetMemberDetailsByUserName(Username);

            if (memberObj != null)
            {
                // Member found, now check if Phone exists and is already verified
                if (memberObj.Status == "Suspended" || memberObj.Status == "Temporarily_Blocked")
                    return "Suspended";

                if (!String.IsNullOrEmpty(memberObj.ContactNumber))
                {
                    if (memberObj.ContactNumber.Length > 9)
                    {
                        if (memberObj.IsVerifiedPhone == true)
                        {
                            return memberObj.PhoneVerifiedOn != null
                                   ? "Looks like your phone number was already verified on " + Convert.ToDateTime(memberObj.PhoneVerifiedOn).ToString("MM/dd/yyyy")
                                   : "Looks like your phone number was already verified.";
                        }
                        else
                        {
                            var fname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                            var MessageBody = "Hi " + fname + ", This is Nooch - just making sure this is your phone number. Please reply 'Go' to verify your phone number.";
                            var result = Utility.SendSMS(memberObj.ContactNumber, MessageBody);
                            return "Success";
                        }
                    }
                    else return "Invalid phone number";
                }
                else return "No phone number found";
            }
            else return "Not a Nooch member.";
        }


        /// <summary>
        /// For resending the email verification email to an existing user.
        /// </summary>
        /// <param name="Username"></param>
        /// <returns></returns>
        public string ResendVerificationLink(string Username)
        {
            //1. Check if user exists or not
            //2. Check if already verified or not
            try
            {
                // Get MemberId from Username
                var MemberId = CommonHelper.GetMemberIdByUserName(Username);

                if (!String.IsNullOrEmpty(MemberId))
                {
                    var userNameLowerCase = CommonHelper.GetEncryptedData(Username.ToLower());

                    using (var noochConnection = new NOOCHEntities())
                    {
                        // Member exists, now check if user's account is already activated or not
                        Guid MemId = Utility.ConvertToGuid(MemberId);

                        var authToken = noochConnection.AuthenticationTokens.FirstOrDefault(
                                        m => m.MemberId == MemId && m.IsActivated == false);

                        if (authToken != null)
                        {
                            try
                            {
                                // send registration email to member with autogenerated token 
                                var fromAddress = Utility.GetValueFromConfig("welcomeMail");
                                var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                    "Nooch/Activation?tokenId=" + authToken.TokenId);

                                var firstName = CommonHelper.GetMemberNameByUserName(Username);

                                var tokens = new Dictionary<string, string>
                                    {
                                        {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                        {Constants.PLACEHOLDER_LAST_NAME, ""},
                                        {Constants.PLACEHOLDER_OTHER_LINK, link}
                                    };

                                Utility.SendEmail(Constants.TEMPLATE_REGISTRATION, fromAddress, Username, null,
                                    "Confirm Nooch Registration", link,
                                    tokens, null, null, null);
                                return "Success";
                            }
                            catch (Exception)
                            {
                                Logger.Error("MDA -> ResendVerificationLink FAILED - Member Activation " +
                                             "email not sent to [" + Username + "]");
                                return "Failure";
                            }
                        }
                        else
                            return "Already Activated.";
                    }
                }
                else
                    return "Not a nooch member.";
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> ResendVerificationLink FAILED - Username: [" + Username + "], Exception: [" + ex + "]");
                return ex.Message;
            }
        }


        public string UpdateUserLocation(string memberId, string Lat, string Long)
        {
            var id = Utility.ConvertToGuid(memberId);
            using (var noochConnection = new NOOCHEntities())
            {
                var memberEntity = noochConnection.Members.FirstOrDefault(m => m.MemberId == id);
                if (memberEntity != null)
                {
                    memberEntity.LastLocationLat = Decimal.Parse(Lat);
                    memberEntity.LastLocationLng = Decimal.Parse(Long);
                    noochConnection.SaveChanges();

                    return "success";
                }
                else return "Member not found";
            }
        }


        public string SaveImmediateRequire(string memberId, Boolean IsImmediate)
        {
            var id = Utility.ConvertToGuid(memberId);
            using (var noochConnection = new NOOCHEntities())
            {
                var memberEntity = noochConnection.Members.FirstOrDefault(m => m.MemberId == id);
                if (memberEntity != null)
                {
                    memberEntity.IsRequiredImmediatley = IsImmediate;
                    noochConnection.SaveChanges();

                    return "success";
                }
                else return "Member not found";
            }
        }


        public string SetAllowSharing(string memberId, bool allowSharing)
        {
            Logger.Info("MDA -> SetAllowSharing - memberId: [" + memberId + "]");

            using (var noochConnection = new NOOCHEntities())
            {

                string fileName = memberId;

                var id = Utility.ConvertToGuid(memberId);

                var memberPrivacySettings = noochConnection.MemberPrivacySettings.FirstOrDefault(m => m.MemberId == id);


                if (memberPrivacySettings == null)
                {
                    var privacySettings = new MemberPrivacySetting
                    {
                        MemberId = id,
                        AllowSharing = allowSharing,
                        DateCreated = DateTime.Now
                    };

                    noochConnection.MemberPrivacySettings.Add(memberPrivacySettings);
                    return noochConnection.SaveChanges() > 0
                        ? "AllowSharing flag is added successfully."
                        : "Failure";
                }
                memberPrivacySettings.AllowSharing = allowSharing;
                memberPrivacySettings.DateModified = DateTime.Now;
                return noochConnection.SaveChanges() > 0
                    ? "AllowSharing flag is updated successfully."
                    : "Failure";
            }
        }


        /// <summary>
        /// Get Member's Privacy settings
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public MemberPrivacySetting GetMemberPrivacySettings(string memberId)
        {
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var privacySettings = _dbContext.MemberPrivacySettings.FirstOrDefault(m => m.MemberId == id);

                return privacySettings;
            }
            catch (Exception ex)
            {
                Logger.Error("MDA - GetMemberPrivacySettings FAILED - MemberID: [" + memberId + "], Exception: " + ex + "]");
            }
            return null;
        }


        public string getReferralCode(String memberId)
        {
            Logger.Info("MDA -> getReferralCode Fired - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var noochMember = _dbContext.Members.FirstOrDefault(mm => mm.MemberId == id && mm.IsDeleted == false);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                if (noochMember.InviteCodeId != null)
                {
                    Guid v = Utility.ConvertToGuid(noochMember.InviteCodeId.ToString());

                    var inviteMember = _dbContext.InviteCodes.FirstOrDefault(m => m.InviteCodeId == v);
                    if (inviteMember != null)
                        return inviteMember.code;
                    else return "";
                }
                //No referal code
                return "";
            }
            return "Invalid";
        }


        public string setReferralCode(Guid memberId)
        {
            Logger.Info("MDA -> setReferralCode - MemberID: [" + memberId + "]");

            try
            {
                // Get the member details

                var noochMember = _dbContext.Members.FirstOrDefault(mm => mm.MemberId == memberId);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    // Check if the user already has an invite code generted or not
                    string existing = getReferralCode(memberId.ToString());

                    if (existing == "")
                    {
                        // Generate random code
                        Random rng = new Random();
                        int value = rng.Next(1000);
                        string text = value.ToString("000");
                        string fName = CommonHelper.GetDecryptedData(noochMember.FirstName);

                        // Make sure First name is at least 4 letters
                        if (fName.Length < 4)
                        {
                            string lname = CommonHelper.GetDecryptedData(noochMember.LastName);

                            fName = fName + lname.Substring(0, 4 - fName.Length).ToUpper();
                        }
                        string code = fName.Substring(0, 4).ToUpper() + text;

                        // Insert into invites
                        InviteCode obj = new InviteCode();
                        obj.InviteCodeId = Guid.NewGuid();
                        obj.code = code;
                        obj.totalAllowed = 10;
                        obj.count = 0;
                        _dbContext.InviteCodes.Add(obj);
                        int result = _dbContext.SaveChanges();

                        // Update the inviteid into the members table's InviteCodeID column
                        noochMember.InviteCodeId = obj.InviteCodeId;
                        _dbContext.SaveChanges();
                        _dbContext.Entry(noochMember).Reload();
                        return "Success";
                    }
                    return "Invite Code Already Exists";
                }
                return "Invalid";
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> setReferralCode FAILED - [Exception: " + ex + "]");
                return "Error";
            }
        }


        public bool MemberActivation(string tokenId)
        {
            Logger.Info("MDA -> MemberActivation Fired - TokenID: [" + tokenId + "]");

            var id = Utility.ConvertToGuid(tokenId);

            var authenticationToken = _dbContext.AuthenticationTokens.FirstOrDefault(m => m.TokenId == id && m.IsActivated == false);

            if (authenticationToken != null)
            {
                _dbContext.Entry(authenticationToken).Reload();
                authenticationToken.IsActivated = true;
                authenticationToken.DateModified = DateTime.Now;

                Guid memGuid = authenticationToken.MemberId;

                if (_dbContext.SaveChanges() > 0)
                {
                    Logger.Info("MDA -> MemberActivation - Member activated successfully - [" + memGuid + "]");

                    var memberObj = _dbContext.Members.FirstOrDefault(mm => mm.MemberId == memGuid);

                    if (memberObj != null)
                    {
                        _dbContext.Entry(memberObj).Reload();
                        // Set a Referral Code/Invitation Code
                        string result = setReferralCode(memberObj.MemberId);

                        memberObj.Status = Constants.STATUS_ACTIVE;
                        memberObj.DateModified = DateTime.Now;

                        #region Update Tenant Record If For A Tenant

                        if (memberObj.Type == "Tenant")
                        {
                            try
                            {
                                var tenantdObj =
                                    _dbContext.Tenants.FirstOrDefault(
                                        m => m.MemberId == memGuid && m.IsDeleted == false);

                                if (tenantdObj != null)
                                {
                                    _dbContext.Entry(tenantdObj).Reload();
                                    Logger.Info("MDA -> MemberActivation - This is a TENANT - About to update Tenants Table " +
                                                "MemberID: [" + memGuid + "]");

                                    tenantdObj.eMail = memberObj.UserName;
                                    tenantdObj.IsEmailVerified = true;
                                    tenantdObj.DateModified = DateTime.Now;

                                    int saveChangesToTenant = _dbContext.SaveChanges();
                                    _dbContext.Entry(tenantdObj).Reload();
                                    if (saveChangesToTenant > 0)
                                        Logger.Info("MDA -> MemberActivation - Saved changes to TENANT table successfully - MemberID: [" + memGuid + "]");
                                    else
                                        Logger.Error("MDA -> MemberActivation - FAILED to save changes to TENANT table - MemberID: [" + memGuid + "]");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> MemberActivation EXCEPTION on checking if this user is a TENANT - " +
                                             "MemberID: [" + memGuid + "], Exception: [" + ex + "]");
                            }
                        }

                        #endregion Update Tenant Record If For A Tenant


                        #region Send Welcome Email

                        string fromAddress = Utility.GetValueFromConfig("welcomeMail");
                        string toAddress = CommonHelper.GetDecryptedData(memberObj.UserName);

                        var tokens = new Dictionary<string, string>
                            {
                                {
                                    Constants.PLACEHOLDER_FIRST_NAME,
                                    CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName))
                                },
                                {
                                    Constants.PLACEHOLDER_LAST_NAME,
                                    CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName))
                                }
                            };
                        try
                        {
                            Utility.SendEmail("WelcomeEmailTemplate",
                                fromAddress, toAddress, null, "Welcome to Nooch", null, tokens, null, null, null);

                            Logger.Info("MDA -> MemberActivation - Welcome email sent to [" +
                                                   toAddress + "] successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> MemberActivation Welcome mail NOT sent to [" +
                                                   toAddress + "], Exception: [" + ex + "]");
                        }

                        #endregion Send Welcome Email

                        return _dbContext.SaveChanges() > 0;
                    }
                }
            }

            return false; // If user is already activated
        }


        public PhoneEmailListDto GetMemberIds(PhoneEmailListDto phoneEmailListDto)
        {
            Logger.Info("MDA -> GetMemberIds Fired - phoneEmailList: [" + phoneEmailListDto.phoneEmailList + "]");

            foreach (PhoneEmailMemberPair phoneEmailObj in phoneEmailListDto.phoneEmailList)
            {
                // First check for a PHONE match
                var noochMember = _dbContext.Members.FirstOrDefault(m => m.ContactNumber == phoneEmailObj.phoneNo && m.IsDeleted == false);

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    phoneEmailObj.memberId = noochMember.MemberId.ToString();
                }
                else
                {
                    // Then check for an EMAIL match
                    var tempEmailEnc = CommonHelper.GetEncryptedData(phoneEmailObj.emailAddy.ToLower());

                    noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == tempEmailEnc || m.FacebookAccountLogin == tempEmailEnc && m.IsDeleted == false);

                    if (noochMember != null)
                    {
                        _dbContext.Entry(noochMember).Reload();
                        phoneEmailObj.memberId = noochMember.MemberId.ToString();
                    }
                }
            }

            return phoneEmailListDto;
        }


        public List<GetMostFrequentFriends_Result> GetMostFrequentFriends(string MemberId)
        {
            try
            {
                //Logger.Info("Service layer -> GetMostFrequentFriends - MemberId: [" + MemberId + "]");
                return _dbContext.GetMostFrequentFriends(MemberId).ToList();
            }
            catch (Exception ex)
            {
                Logger.Info("MDA -> GetMostFrequentFriends FAILED - MemberID: [" + MemberId + "], Exception: [" + ex + "]");
                return null;
            }
        }


        public string GetMemberStats(string MemberId, string query)
        {
            try
            {
                return _dbContext.GetReportsForMember(MemberId, query).SingleOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Info("MDA -> GetMemberStats FAILED - MemberID: [" + MemberId + "], Exception: [" + ex + "]");
            }
            return "";
        }


        public List<Member> getInvitedMemberList(string memberId)
        {
            Logger.Info("MDA -> getInvitedMemberList Fired - MemberID: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);
            //Get the member details

            var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                Guid n = Utility.ConvertToGuid(noochMember.InviteCodeId.ToString());

                var referredUsers = _dbContext.Members.Where(m => m.InviteCodeIdUsed == noochMember.InviteCodeId).ToList();
                return referredUsers;
            }
            else return null;
        }


        public string SaveMemberDeviceToken(string MemberId, string DeviceToken)
        {
            if (DeviceToken.Length < 10) return "Invalid Device Token passed - too short.";

            Guid MemId = Utility.ConvertToGuid(MemberId);

            var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == MemId && m.IsDeleted == false);

            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                noochMember.DeviceToken = DeviceToken;
                noochMember.DateModified = DateTime.Now;
                _dbContext.SaveChanges();
                return "DeviceToken saved successfully.";
            }
            else
                return "Member ID not found or Member status deleted.";
        }


        private static void ChangeStatus(Member member, Boolean rememberMeEnabled)
        {
            if (member.Status == Constants.STATUS_TEMPORARILY_BLOCKED)
                member.Status = Constants.STATUS_ACTIVE;
            member.RememberMeEnabled = rememberMeEnabled;
        }


        /// <summary>
        /// For logging in a user using their linked Facebook account.
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="FBId"></param>
        /// <param name="rememberMeEnabled"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="udid"></param>
        /// <param name="devicetoken"></param>
        public string LoginwithFB(string userEmail, string FBId, Boolean rememberMeEnabled, decimal lat, decimal lng, string udid, string devicetoken)
        {
            Logger.Info("MDA -> LoginwithFB Initiated - [UserEmail: " + userEmail + "],  [FBId: " + FBId + "]");

            FBId = CommonHelper.GetEncryptedData(FBId.ToLower());

            using (var noochConnection = new NOOCHEntities())
            {
                var memberEntity =
                    noochConnection.Members.FirstOrDefault(
                        m => m.FacebookAccountLogin.Equals(FBId) && m.IsDeleted == false);

                if (memberEntity == null)
                {
                    Logger.Info("MDA -> LoginwithFB - No User Found for this FB ID - [UserEmail: " + userEmail + "],  [FBId: " + FBId + "]");

                    return "FBID or EmailId not registered with Nooch";
                }
                else
                {
                    var emailFromServerForGivenFbId = CommonHelper.GetDecryptedData(memberEntity.UserName);

                    // generating and sending access token
                    //var memberNotifications = GetMemberNotificationSettingsByUserName(emailFromServerForGivenFbId);

                    if (memberEntity.Status == "Temporarily_Blocked")
                        return "Temporarily_Blocked";
                    else if (memberEntity.Status == "Suspended")
                        return "Suspended";
                    else if (memberEntity.Status == "Active" || memberEntity.Status == "Registered")
                    {
                        #region

                        #region Check If User Is Already Logged In

                        // Check if user already logged in or not.  If yes, then send Auto Logout email
                        if (!String.IsNullOrEmpty(memberEntity.AccessToken) &&
                            memberEntity.IsOnline == true &&
                            memberEntity.UDID1 != udid &&
                            !String.IsNullOrEmpty(memberEntity.UDID1))
                        {
                            var fromAddress = Utility.GetValueFromConfig("adminMail");
                            var toAddress = emailFromServerForGivenFbId;
                            var userFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName));

                            string msg = "Hi,\n  You have been automatically logged out from your Nooch account because you signed in from another device.\n" +
                                         "If this is a mistake and you feel your account may be compromised, please contact support@nooch.com immediately  - Team Nooch";

                            try
                            {
                                Utility.SendEmail("", fromAddress, toAddress, null,
                                    "Nooch Automatic Logout", null, null, null, null, msg);

                                Logger.Info("MDA -> LoginwithFB - Automatic Log Out Email sent to [" + toAddress + "] successfully.");

                                // Checking if phone exists and isVerified before sending SMS to user
                                if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
                                {
                                    var GetPhoneNumberByMemberId_wFormatting = CommonHelper.FormatPhoneNumber(memberEntity.ContactNumber);

                                    try
                                    {
                                        //var GetPhoneNumberByMemberId = CommonHelper.RemovePhoneNumberFormatting(memberEntity.ContactNumber);

                                        //string result = UtilityDataAccess.SendSMS(GetPhoneNumberByMemberId, msg, memberEntity.AccessToken, memberEntity.MemberId.ToString());

                                        //Logger.LogDebugMessage("MDA -> LoginwithFB - Automatic Log Out SMS sent to [" + GetPhoneNumberByMemberId_wFormatting + "] successfully.");
                                    }
                                    catch (Exception)
                                    {
                                        Logger.Error("MDA -> LoginwithFB - Automatic Log Out SMS NOT sent to [" + GetPhoneNumberByMemberId_wFormatting + "]. Problem occured in sending SMS.");
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Logger.Error("MDA -> LoginwithFB - Automatic Log Out email NOT sent to [" + toAddress + "]. Problem occured in sending email.");
                            }
                        }

                        #endregion Check If User Is Already Logged In

                        //Update UDID of device from which the user has logged in.
                        if (!String.IsNullOrEmpty(udid))
                        {
                            memberEntity.UDID1 = udid;
                        }
                        if (!String.IsNullOrEmpty(devicetoken))
                        {
                            memberEntity.DeviceToken = devicetoken;
                        }

                        memberEntity.LastLocationLat = lat;
                        memberEntity.LastLocationLng = lng;
                        memberEntity.IsOnline = true;

                        // Reset attempt count
                        memberEntity.InvalidLoginTime = null;
                        memberEntity.InvalidLoginAttemptCount = null;

                        // resetting invalid login attempt count to 0
                        memberEntity.InvalidPinAttemptCount = null;
                        memberEntity.InvalidPinAttemptTime = null;
                        noochConnection.SaveChanges();


                        return "Success";

                        #endregion
                    }
                    else return "Invalid user id or password.";
                }
            }
        }


        public string LoginwithFBGeneric(string userEmail, string FBId, Boolean rememberMeEnabled, decimal lat, decimal lng, string udid, string devicetoken)
        {
            Logger.Info("MDA -> LoginwithFBGeneric Initiated - [UserEmail: " + userEmail + "],  [FBId: " + FBId + "]");

            FBId = CommonHelper.GetEncryptedData(FBId.ToLower());

            using (var noochConnection = new NOOCHEntities())
            {
                var memberEntity = noochConnection.Members.FirstOrDefault(
                        m => m.FacebookAccountLogin.Equals(FBId) && m.IsDeleted == false);

                if (memberEntity == null)
                {
                    //Logger.Info("MDA -> LoginwithFB - No User Found for this FB ID - [UserEmail: " + userEmail + "],  [FBId: " + FBId + "]");
                    //return "FBID or EmailId not registered with Nooch";

                    var noochRandomId = GetRandomNoochId();

                    var member = new Member()
                      {
                          Nooch_ID = noochRandomId,
                          MemberId = Guid.NewGuid(),
                          FirstName = CommonHelper.GetEncryptedData(" "),
                          LastName = CommonHelper.GetEncryptedData(" "),
                          UserName = CommonHelper.GetEncryptedData(userEmail),
                          UserNameLowerCase = CommonHelper.GetEncryptedData(userEmail),
                          SecondaryEmail = userEmail,
                          RecoveryEmail = CommonHelper.GetEncryptedData(userEmail),
                          ContactNumber = null,
                          Address = CommonHelper.GetEncryptedData(" "),
                          City = CommonHelper.GetEncryptedData(" "),
                          State = CommonHelper.GetEncryptedData(" "),
                          Zipcode = CommonHelper.GetEncryptedData(" "),
                          SSN = CommonHelper.GetEncryptedData(" "),
                          DateOfBirth = null,
                          Password = CommonHelper.GetEncryptedData("jibb3r;jawn"), // Malkit 19 Aug 2016 -- Not sure if we need to set some password for users signingup wih FB
                          PinNumber = CommonHelper.GetEncryptedData(Utility.GetRandomPinNumber()),
                          Status = Constants.STATUS_ACTIVE,
                          IsDeleted = false,
                          DateCreated = DateTime.Now,
                          Type = "Personal - Browser",
                          Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png",
                          UDID1 = !String.IsNullOrEmpty(udid) ? udid : null,
                          IsVerifiedWithSynapse = false,
                          cipTag = CommonHelper.GetEncryptedData(" "),
                          FacebookUserId = userEmail,
                          FacebookAccountLogin = FBId,
                          isRentScene = false,
                      };

                    int i = 0;
                    try
                    {
                        _dbContext.Members.Add(member);
                        i = _dbContext.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        var error = "MDA -> LoginwithFBGeneric - FAILED to save new member in DB - Email: [" + userEmail +
                                     "], MemberID: [" + member.MemberId + "], Exception: [" + ex + "]";
                        Logger.Error(error);
                        throw ex;
                    }

                    var NewMemberEntity = noochConnection.Members.FirstOrDefault(m => m.FacebookAccountLogin.Equals(FBId) && m.IsDeleted == false);


                    var emailFromServerForGivenFbId = CommonHelper.GetDecryptedData(NewMemberEntity.UserName);

                    // generating and sending access token
                    //var memberNotifications = GetMemberNotificationSettingsByUserName(emailFromServerForGivenFbId);                

                    #region
                    //Update UDID of device from which the user has logged in.
                    //if (!String.IsNullOrEmpty(udid))
                    //{
                    //    memberEntity.UDID1 = udid;
                    //}
                    if (!String.IsNullOrEmpty(devicetoken))
                    {
                        NewMemberEntity.DeviceToken = devicetoken;
                    }

                    NewMemberEntity.LastLocationLat = lat;
                    NewMemberEntity.LastLocationLng = lng;
                    NewMemberEntity.IsOnline = true;

                    // Reset attempt count
                    NewMemberEntity.InvalidLoginTime = null;
                    NewMemberEntity.InvalidLoginAttemptCount = null;

                    // resetting invalid login attempt count to 0
                    NewMemberEntity.InvalidPinAttemptCount = null;
                    NewMemberEntity.InvalidPinAttemptTime = null;
                    noochConnection.SaveChanges();


                    // email newly generated pin number
                    #region Send Temp PIN Email
                    try
                    {
                        var tokens2 = new Dictionary<string, string>
                                        {
                                            {Constants.PLACEHOLDER_FIRST_NAME,CommonHelper.GetDecryptedData( NewMemberEntity.FirstName)},
                                            {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(NewMemberEntity.PinNumber)}
                                        };

                        Utility.SendEmail("pinSetForNewUser",
                            "support@nooch.com", CommonHelper.GetDecryptedData(NewMemberEntity.UserName), null, "Your temporary Nooch PIN",
                            null, tokens2, null, null, null);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations EXCEPTION - Member Temp PIN email NOT sent to [" +
                                              CommonHelper.GetDecryptedData(NewMemberEntity.UserName) + "], Exception: [" + ex + "]");
                    }
                    #endregion Send Temp PIN Email

                    return "Success";
                    #endregion
                }
                else
                {
                    var emailFromServerForGivenFbId = CommonHelper.GetDecryptedData(memberEntity.UserName);

                    // generating and sending access token
                    //var memberNotifications = GetMemberNotificationSettingsByUserName(emailFromServerForGivenFbId);

                    if (memberEntity.Status == "Temporarily_Blocked")
                        return "Temporarily_Blocked";
                    else if (memberEntity.Status == "Suspended")
                        return "Suspended";
                    else if (memberEntity.Status == "Active" || memberEntity.Status == "Registered")
                    {
                        #region

                        #region Check If User Is Already Logged In

                        // Check if user already logged in or not.  If yes, then send Auto Logout email
                        if (!String.IsNullOrEmpty(memberEntity.AccessToken) &&
                            memberEntity.IsOnline == true &&
                            memberEntity.UDID1 != udid &&
                            !String.IsNullOrEmpty(memberEntity.UDID1))
                        {
                            var fromAddress = Utility.GetValueFromConfig("adminMail");
                            var toAddress = emailFromServerForGivenFbId;
                            var userFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName));

                            string msg = "Hi,\n  You have been automatically logged out from your Nooch account because you signed in from another device.\n" +
                                         "If this is a mistake and you feel your account may be compromised, please contact support@nooch.com immediately  - Team Nooch";

                            try
                            {
                                Utility.SendEmail("", fromAddress, toAddress, null,
                                    "Nooch Automatic Logout", null, null, null, null, msg);

                                Logger.Info("MDA -> LoginwithFB - Automatic Log Out Email sent to [" + toAddress + "] successfully.");

                                // Checking if phone exists and isVerified before sending SMS to user
                                if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
                                {
                                    var GetPhoneNumberByMemberId_wFormatting = CommonHelper.FormatPhoneNumber(memberEntity.ContactNumber);

                                    try
                                    {
                                        //var GetPhoneNumberByMemberId = CommonHelper.RemovePhoneNumberFormatting(memberEntity.ContactNumber);

                                        //string result = UtilityDataAccess.SendSMS(GetPhoneNumberByMemberId, msg, memberEntity.AccessToken, memberEntity.MemberId.ToString());

                                        //Logger.LogDebugMessage("MDA -> LoginwithFB - Automatic Log Out SMS sent to [" + GetPhoneNumberByMemberId_wFormatting + "] successfully.");
                                    }
                                    catch (Exception)
                                    {
                                        Logger.Error("MDA -> LoginwithFB - Automatic Log Out SMS NOT sent to [" + GetPhoneNumberByMemberId_wFormatting + "]. Problem occured in sending SMS.");
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Logger.Error("MDA -> LoginwithFB - Automatic Log Out email NOT sent to [" + toAddress + "]. Problem occured in sending email.");
                            }
                        }

                        #endregion Check If User Is Already Logged In

                        //Update UDID of device from which the user has logged in.
                        if (!String.IsNullOrEmpty(udid))
                        {
                            memberEntity.UDID1 = udid;
                        }
                        if (!String.IsNullOrEmpty(devicetoken))
                        {
                            memberEntity.DeviceToken = devicetoken;
                        }

                        memberEntity.LastLocationLat = lat;
                        memberEntity.LastLocationLng = lng;
                        memberEntity.IsOnline = true;

                        // Reset attempt count
                        memberEntity.InvalidLoginTime = null;
                        memberEntity.InvalidLoginAttemptCount = null;

                        // resetting invalid login attempt count to 0
                        memberEntity.InvalidPinAttemptCount = null;
                        memberEntity.InvalidPinAttemptTime = null;
                        noochConnection.SaveChanges();



                        return "Success";

                        #endregion
                    }
                    else return "Invalid user id or password.";
                }
            }
        }


        public string LoginRequest(string userName, string password, Boolean rememberMeEnabled, decimal lat, decimal lng, string udid, string devicetoken, string deviceOS)
        {
            Logger.Info("MDA -> LoginRequest Fired - UserName: [" + userName + "], UDID: [" + udid + "]");

            var userEmail = userName;
            var userNameLowerCase = CommonHelper.GetEncryptedData(userName.ToLower());
            userName = CommonHelper.GetEncryptedData(userName);


            var memberEntity = _dbContext.Members.FirstOrDefault(m => m.UserNameLowerCase == userNameLowerCase &&
                                                                      m.IsDeleted == false);

            if (memberEntity != null)
            {
                _dbContext.Entry(memberEntity).Reload();

                var memberNotifications = CommonHelper.GetMemberNotificationSettingsByUserName(userEmail);

                switch (memberEntity.Status)
                {
                    case "Temporarily_Blocked":
                        Logger.Info("MDA -> LoginRequest FAILED - User is Already TEMPORARILY_BLOCKED - UserName: [" + userName + "]");
                        return "Temporarily_Blocked";
                    case "Suspended":
                        Logger.Info("MDA -> LoginRequest FAILED - User is Already SUSPENDED - UserName: [" + userName + "]");
                        return "Suspended";
                    default:
                        if (memberEntity.Status == "Active" ||
                            memberEntity.Status == "Registered" ||
                            memberEntity.Status == "NonRegistered" ||
                            memberEntity.Type == "Personal - Browser")
                        {
                            #region

                            #region Check If User Is Already Logged In

                            // Check if user already logged in or not.  If yes, then send Auto Logout email
                            if (!String.IsNullOrEmpty(memberEntity.AccessToken) &&
                                !String.IsNullOrEmpty(memberEntity.UDID1) &&
                                memberEntity.IsOnline == true &&
                                memberEntity.UDID1.ToLower() != udid.ToLower())
                            {
                                Logger.Info("MDA -> LoginRequest - Sending Automatic Logout Notification - UserName: [" + userEmail +
                                            "], UDID: [" + udid +
                                            "], AccessToken: [" + memberEntity.AccessToken + "]");

                                var fromAddress = Utility.GetValueFromConfig("adminMail");
                                var toAddress = userEmail;
                                var userFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName));

                                string msg = "Hi,\n\nYou have been automatically logged out from your Nooch account because you signed in from a new device.\n" +
                                             "If this is a mistake and you feel your account may be compromised, please contact support@nooch.com immediately.  - Team Nooch";

                                try
                                {
                                    Utility.SendEmail("", fromAddress, toAddress, null,
                                                      "Nooch Automatic Logout", null, null, null, null, msg);

                                    Logger.Info("MDA -> LoginRequest - Automatic Log Out Email sent to [" + toAddress + "] successfully.");

                                    // Checking if phone exists and isVerified before sending SMS to user
                                    /*if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
                                    {
                                        try
                                        {
                                            //msg = "Hi, You were automatically logged out from your Nooch account b/c you signed in from another device. " +
                                            // "If this is a mistake, contact support@nooch.com immediately. - Nooch";
                                            //string result = UtilityDataAccess.SendSMS(memberEntity.ContactNumber, msg, memberEntity.AccessToken, memberEntity.MemberId.ToString());

                                            //Logger.Info("MDA -> LoginRequest - Automatic Log Out SMS sent to [" + memberEntity.ContactNumber + "] successfully. [SendSMS Result: " + result + "]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("MDA -> LoginRequest - Automatic Log Out SMS NOT sent to [" + memberEntity.ContactNumber + "], " +
                                                                   "Exception: [" + ex.Message + "]");
                                        }
                                    }*/
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> LoginRequest - Automatic Log Out email NOT sent to: [" + toAddress + "] - Exception: [" + ex.Message + "]");
                                }
                            }

                            #endregion Check If User Is Already Logged In

                            // Update UDID of device from which the user has logged in.
                            if (!String.IsNullOrEmpty(udid))
                                memberEntity.UDID1 = udid;

                            if (!String.IsNullOrEmpty(devicetoken))
                                memberEntity.DeviceToken = devicetoken;
                            if (!String.IsNullOrEmpty(deviceOS))
                                memberEntity.DeviceType = deviceOS;

                            memberEntity.LastLocationLat = lat;
                            memberEntity.LastLocationLng = lng;
                            memberEntity.IsOnline = true;


                            var currentTimeMinus24Hours = DateTime.Now.AddHours(-24);
                            int loginRetryCountInDb = memberEntity.InvalidLoginAttemptCount.Equals(null)
                                                      ? 0
                                                      : memberEntity.InvalidLoginAttemptCount.Value;

                            // Check (FPTime || InvalidLoginAttemptTime) > CurrentTime - 24 hrs { if true, delete past records and insert new}                    
                            bool isInvalidLoginTimeOver = new InvalidAttemptDurationSpecification().IsSatisfiedBy(memberEntity.InvalidLoginTime,
                                                                                                                  currentTimeMinus24Hours);

                            if (isInvalidLoginTimeOver)
                            {
                                ChangeStatus(memberEntity, rememberMeEnabled);

                                //Reset attempt count
                                memberEntity.InvalidLoginTime = null;
                                memberEntity.InvalidLoginAttemptCount = null;

                                _dbContext.SaveChanges();
                                _dbContext.Entry(memberEntity).Reload();

                                loginRetryCountInDb = memberEntity.InvalidLoginAttemptCount.Equals(null)
                                                      ? 0
                                                      : memberEntity.InvalidLoginAttemptCount.Value;

                                if (!memberEntity.Password.Equals(password.Replace(" ", "+")))
                                    return CommonHelper.IncreaseInvalidLoginAttemptCount(memberEntity.MemberId.ToString(), loginRetryCountInDb);
                            }

                            if (loginRetryCountInDb < 4 && memberEntity.Password.Equals(password.Replace(" ", "+")))
                            {
                                //Reset attempt count
                                memberEntity.InvalidLoginTime = null;
                                memberEntity.InvalidLoginAttemptCount = null;
                                memberEntity.InvalidPinAttemptCount = null;
                                memberEntity.InvalidPinAttemptTime = null;

                                memberEntity.Status = Constants.STATUS_ACTIVE;
                                _dbContext.SaveChanges();

                                _dbContext.Entry(memberEntity).Reload();
                                return "Success"; // active nooch member  
                            }

                            if (memberEntity.InvalidLoginAttemptCount == null ||
                                memberEntity.InvalidLoginAttemptCount == 0)
                            {
                                // This is the first invalid try
                                Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - 1st Invalid Attempt - UserName: [" + userName + "]");

                                return CommonHelper.IncreaseInvalidLoginAttemptCount(memberEntity.MemberId.ToString(), loginRetryCountInDb);
                            }

                            if (loginRetryCountInDb == 4)
                            {
                                // Already Suspended
                                Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - User Already Suspended - UserName: [" + userName + "]");

                                return String.Concat("Your account has been temporarily blocked.  You can login only after 24 hours from this time: ",
                                    memberEntity.InvalidLoginTime);
                            }

                            else if (loginRetryCountInDb == 3)
                            {
                                // This is 4th try, so suspend the member
                                Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - 3rd Invalid Attempt, now suspending user - UserName: [" + userName + "]");

                                memberEntity.InvalidLoginTime = DateTime.Now;
                                memberEntity.InvalidLoginAttemptCount = loginRetryCountInDb + 1;
                                memberEntity.Status = Constants.STATUS_TEMPORARILY_BLOCKED;

                                _dbContext.SaveChanges();
                                _dbContext.Entry(memberEntity).Reload();

                                // email to user after 3 invalid login attemt
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
                                        "SupendMember - Attempt to send mail for Supend Member[ memberId:" +
                                        memberEntity.MemberId + "].");
                                    Utility.SendEmail("userSuspended", fromAddress,
                                        emailAddress, null, "Your Nooch account has been suspended", null, tokens, null,
                                        null, null);
                                }
                                catch (Exception)
                                {
                                    Logger.Info("SupendMember - Supend Member status email not send to [" +
                                                           memberEntity.MemberId +
                                                           "]. Problem occured in sending Supend Member status mail. ");
                                }

                                #endregion

                                return String.Concat(
                                    "Your account has been temporarily blocked.  You can login only after 24 hours from this time: ",
                                    memberEntity.InvalidLoginTime);
                            }

                            return CommonHelper.IncreaseInvalidLoginAttemptCount(memberEntity.MemberId.ToString(), loginRetryCountInDb);

                            #endregion
                        }
                        else
                            return "Invalid user id or password.";
                }
            }
            else
                return "Invalid user id or password.";
        }


        public List<LocationSearch> GetLocationSearch(string MemberId, int Radius)
        {
            Logger.Info("MDA -> GetLocationSearch - MemberID: [" + MemberId + "], Radius: [" + Radius + "]");

            try
            {
                List<GetLocationSearch_Result> list = _dbContext.GetLocationSearch(MemberId, Radius).ToList();
                List<LocationSearch> list1 = new List<LocationSearch>();

                foreach (GetLocationSearch_Result loc in list)
                {
                    try
                    {
                        var config =
                            new MapperConfiguration(cfg => cfg.CreateMap<GetLocationSearch_Result, LocationSearch>()
                                .BeforeMap((src, dest) => src.FirstName = CommonHelper.GetDecryptedData(src.FirstName))
                                .BeforeMap((src, dest) => src.LastName = CommonHelper.GetDecryptedData(src.LastName))
                                );

                        var mapper = config.CreateMapper();

                        LocationSearch obj = mapper.Map<LocationSearch>(loc);

                        obj.FirstName = CommonHelper.UppercaseFirst(loc.FirstName);
                        obj.LastName = CommonHelper.UppercaseFirst(loc.LastName);
                        decimal miles = obj.Miles;
                        obj.Miles = decimal.Parse(miles > 0 ? miles.ToString("###.##") : "000.00");
                        obj.Photo = loc.Photo;
                        obj.MemberId = loc.MemberId.ToString();
                        list1.Add(obj);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> GetLocationSearch FAILED - Exception: [" + ex + "]");
                    }
                }

                return list1;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }


        public Boolean validateInvitationCode(String invitationCode)
        {
            Logger.Info("MDA -> validateInvitationCode Fired - InvitationCode: [" + invitationCode + "]");

            var noochInviteMember = _dbContext.InviteCodes.FirstOrDefault(m => m.code == invitationCode && m.count < m.totalAllowed);

            if (noochInviteMember != null)
                _dbContext.Entry(noochInviteMember).Reload();

            return noochInviteMember != null;
        }


        public bool getTotalReferralCode(String referalCode)
        {
            Logger.Info("MDA -> getReferralCode Fired - referalCode: [" + referalCode + "]");

            var inviteMember = _dbContext.InviteCodes.FirstOrDefault(m => m.code == referalCode);

            if (inviteMember != null)
            {
                _dbContext.Entry(inviteMember).Reload();
                return inviteMember.count < inviteMember.totalAllowed;
            }
            return false;
        }


        public string SaveMediaPosts(string MemberId, string PostTo, string PostContent)
        {
            try
            {
                var member = new SocialMediaPost
                {
                    Id = Guid.NewGuid(),
                    PostContent = PostContent,
                    PostedBy = Utility.ConvertToGuid(MemberId),
                    PostedOn = DateTime.Now,
                    PostTo = PostTo
                };

                int result = 0;

                _dbContext.SocialMediaPosts.Add(member);
                result = _dbContext.SaveChanges();
                _dbContext.Entry(member).Reload();

                return result > 0 ? "Success" : "Error";
            }
            catch
            {
                return "Error";
            }
        }


        public string GMTTimeZoneConversion(string serverDateTime, string timeZoneKey)
        {
            Logger.Info("MDA - GMTTimeZoneConversion - TimeZoneKey: [" + timeZoneKey + "]");
            // Convert Server time to GMT Time
            DateTime utcDateTime = Convert.ToDateTime(serverDateTime).ToUniversalTime();
            DateTime dateTime = default(DateTime);
            timeZoneKey = CommonHelper.GetDecryptedData(timeZoneKey);
            try
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneKey);
                dateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            }
            catch (TimeZoneNotFoundException e)
            {
                return timeZoneKey + " not found on the local server: " + e;
            }
            catch (InvalidTimeZoneException e)
            {
                return timeZoneKey + " is corrupt on the local server: " + e;
            }

            return dateTime.ToString();
        }


        public bool IsPhoneNumberAlreadyRegistered(string PhoneNumberToSearch)
        {
            string NumAltOne = "+" + PhoneNumberToSearch;
            string NumAltTwo = "+1" + PhoneNumberToSearch;
            string BlankNumCase = CommonHelper.GetEncryptedData(" ");

            if (!String.IsNullOrEmpty(PhoneNumberToSearch))
            {

                var MemberSerachedWithPhone = _dbContext.Members.Where(
                        inviteTemp =>
                            (inviteTemp.ContactNumber == PhoneNumberToSearch ||
                             inviteTemp.ContactNumber == NumAltOne || inviteTemp.ContactNumber == NumAltTwo) &&
                             inviteTemp.IsDeleted == false &&
                             inviteTemp.ContactNumber != BlankNumCase).FirstOrDefault();
                if (MemberSerachedWithPhone == null) return false;
                else return true;
            }
            else return false;
        }


        public int UpdateMember(Member m)
        {
            Member member = m;
            return _dbContext.SaveChanges();
        }


        public Member GetMemberDetails(string memberId)
        {
            //Logger.LogDebugMessage("MDA -> GetMemberDetails - MemberId: [" + memberId + "]");
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var noochMember = _dbContext.Members.Where(memberTemp =>
                                     memberTemp.MemberId == id &&
                                     memberTemp.IsDeleted == false).FirstOrDefault();

                if (noochMember != null)
                {
                    _dbContext.Entry(noochMember).Reload();
                    return noochMember;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetMemberDetails FAILED - Member ID: [" + memberId + "], Exception: [" + ex + "]");
            }
            return new Member();
        }


        public SynapseBanksOfMember GetSynapseBankAccountDetails(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails Initiated - MemberId: [" + memberId + "]");
            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var bankDetails = _dbContext.SynapseBanksOfMembers.Where(bank =>
                                    (bank.MemberId.Value == id &&
                                     bank.IsDefault == true)).FirstOrDefault();

                if (bankDetails != null)
                    _dbContext.Entry(bankDetails).Reload();
                return bankDetails;
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetSynapseBankAccountDetails FAILED - Exception: [" + ex.Message + "]");
                return null;
            }
        }


        public string CreateNonNoochUserPasswordForPhoneInvitations(string TransId, string password, string EmailId)
        {
            Logger.Info("MDA -> CreateNonNoochUserPasswordForPhoneInvitations - TransId: [" + TransId + "]");

            try
            {
                Guid tid = Utility.ConvertToGuid(TransId);
                password = CommonHelper.GetEncryptedData(password);

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);

                var transactionDetail = _dbContext.Transactions.Where(memberTemp =>
                                     memberTemp.TransactionId == tid &&
                                    (memberTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                                     memberTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")).FirstOrDefault();

                if (transactionDetail != null)
                {
                    _dbContext.Entry(transactionDetail).Reload();
                    var userNameLowerCase = CommonHelper.GetEncryptedData(EmailId.ToLower());

                    var memdetails = _dbContext.Members.FirstOrDefault(memberTemp => (memberTemp.UserName == userNameLowerCase || memberTemp.UserNameLowerCase == userNameLowerCase) &&
                                                                                     memberTemp.IsDeleted == false);

                    if (memdetails == null)
                    {
                        #region Username Not Already Registered

                        #region Add New Member Record To DB

                        var noochRandomId = GetRandomNoochId();
                        var newUserPhoneNumber = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        newUserPhoneNumber = CommonHelper.RemovePhoneNumberFormatting(newUserPhoneNumber);

                        var inviteCode = transactionDetail.Member.InviteCodeId.ToString();

                        var emailEncrypted = userNameLowerCase;

                        var member = new Member
                        {
                            Nooch_ID = noochRandomId,
                            MemberId = Guid.NewGuid(),
                            UserName = emailEncrypted,
                            FirstName = CommonHelper.GetEncryptedData(""),
                            LastName = CommonHelper.GetEncryptedData(""),
                            SecondaryEmail = emailEncrypted,
                            Password = password.Replace(" ", "+"),
                            PinNumber = pinNumber.Replace(" ", "+"),
                            Status = Constants.STATUS_REGISTERED,
                            IsDeleted = false,
                            DateCreated = DateTime.Now,
                            DateModified = DateTime.Now,
                            UserNameLowerCase = userNameLowerCase,
                            // FacebookAccountLogin = facebookAccountLogin,
                            Type = "Personal",
                            // contact number verified because already coming from SMS url delivered in phone
                            ContactNumber = newUserPhoneNumber,
                            IsVerifiedPhone = true,
                            IsVerifiedWithSynapse = false,
                            AdminNotes = "Created via Phone Invitation Landing page"
                        };
                        member.Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                        if (!String.IsNullOrEmpty(inviteCode) && inviteCode.Length > 1)
                        {
                            member.InviteCodeIdUsed = Utility.ConvertToGuid(inviteCode);
                        }

                        int result = 0;

                        try
                        {
                            _dbContext.Members.Add(member);
                            result = _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return "Exception " + ex.ToString();
                        }

                        #endregion Add New Member Record To DB

                        if (result > 0)
                        {
                            #region Create Auth Token
                            //// var requestId = Guid.Empty;
                            var tokenId = Guid.NewGuid();

                            var token = new AuthenticationToken
                            {
                                TokenId = tokenId,
                                MemberId = member.MemberId,
                                IsActivated = false,
                                DateGenerated = DateTime.Now
                                // FriendRequestId = requestId
                            };
                            //var tokensRepository = new Repository<AuthenticationTokens, NoochDataEntities>(noochConnection);

                            _dbContext.AuthenticationTokens.Add(token);

                            bool status = Convert.ToBoolean(_dbContext.SaveChanges());
                            #endregion Create Auth Token

                            #region Set Notification Settings
                            // for member notification settings
                            //var memberNotificationsRepository = new Repository<MemberNotifications, NoochDataEntities>(noochConnection);

                            var memberNotification = new MemberNotification
                            {
                                NotificationId = Guid.NewGuid(),

                                MemberId = member.MemberId,

                                FriendRequest = true,
                                InviteRequestAccept = true,
                                TransferSent = true,
                                TransferReceived = true,
                                TransferAttemptFailure = true,
                                NoochToBank = true,
                                BankToNooch = true,
                                EmailFriendRequest = true,
                                EmailInviteRequestAccept = true,
                                EmailTransferSent = true,
                                EmailTransferReceived = true,
                                EmailTransferAttemptFailure = true,
                                TransferUnclaimed = true,
                                BankToNoochRequested = true,
                                BankToNoochCompleted = true,
                                NoochToBankRequested = true,
                                NoochToBankCompleted = true,
                                InviteReminder = true,
                                LowBalance = true,
                                ValidationRemainder = true,
                                ProductUpdates = true,
                                NewAndUpdate = true,
                                DateCreated = DateTime.Now
                            };
                            _dbContext.MemberNotifications.Add(memberNotification);
                            _dbContext.SaveChanges();

                            #endregion Set Notification Settings

                            #region Set Privacy Settings


                            var memberPrivacySettings = new MemberPrivacySetting
                            {

                                MemberId = member.MemberId,

                                AllowSharing = true,
                                ShowInSearch = true,
                                DateCreated = DateTime.Now
                            };
                            _dbContext.MemberPrivacySettings.Add(memberPrivacySettings);

                            _dbContext.SaveChanges();


                            #endregion Set Privacy Settings

                            if (status)
                            {
                                var fromAddress = "welcome@nooch.com";
                                var toAddress = CommonHelper.GetDecryptedData(userNameLowerCase);
                                var firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName));
                                var lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName));
                                #region Send Registration Email

                                try
                                {
                                    //send registration email to member with autogenerated token
                                    var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                             "Nooch/Activation?tokenId=" + tokenId);

                                    var tokens = new Dictionary<string, string>
                                        {
                                            {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                            {Constants.PLACEHOLDER_LAST_NAME, lastName},
                                            {Constants.PLACEHOLDER_OTHER_LINK, link}
                                        };

                                    Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                                        fromAddress, toAddress, null, "Confirm your email on Nooch", link,
                                        tokens, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations EXCEPTION - Member activation " +
                                                 "email NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                                }

                                #endregion Send Registration Email

                                #region Send Temp PIN Email

                                try
                                {
                                    var tokens2 = new Dictionary<string, string>
                                        {
                                            {Constants.PLACEHOLDER_FIRST_NAME, toAddress},
                                            {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                                        };

                                    Utility.SendEmail("pinSetForNewUser", fromAddress, toAddress, null,
                                                      "Your temporary Nooch PIN", null, tokens2, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations EXCEPTION - Member Temp PIN email NOT sent to [" +
                                                 toAddress + "], Exception: [" + ex + "]");
                                }

                                #endregion Send Temp PIN Email

                                #region Send Email Notification To Referrer (If Applicable)

                                if (!String.IsNullOrEmpty(inviteCode))
                                {
                                    try
                                    {
                                        Guid invideCodeGuid = Utility.ConvertToGuid(inviteCode);

                                        var inviteCodeObj = _dbContext.InviteCodes.FirstOrDefault(inviteTemp => inviteTemp.InviteCodeId == invideCodeGuid);

                                        if (inviteCodeObj == null)
                                            Logger.Info("MDA -> CreateNonNoochUserPasswordForPhoneInvitations Attempted but invite code not found");
                                        else if (inviteCodeObj.count >= inviteCodeObj.totalAllowed)
                                            Logger.Info("MDA -> CreateNonNoochUserPasswordForPhoneInvitations Attempted to notify referrer but Allowable limit of [" +
                                                        inviteCodeObj.totalAllowed + "] already reached for Code: [" + inviteCode + "]");
                                        else // Invite Code record found!
                                        {
                                            if (inviteCode.ToLower() != "nocode")
                                            {
                                                try
                                                {
                                                    // Sending email to user who invited this user (Based on the invite code provided during registration)

                                                    // NOTE (CLIFF 10/9/15): THIS METHOD ISN'T CURRENLTLY TAKING FIRST/LAST NAME AS PARAMAETERS, BUT IT NEEDS TO...
                                                    //string fullName = CommonHelper.UppercaseFirst(firstName) + " " + CommonHelper.UppercaseFirst(lastName);
                                                    string fullName = "";
                                                    SendEmailToInvitor(inviteCodeObj.InviteCodeId, toAddress, fullName);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations - Exception trying to send Email To Referrer - [Exception: " + ex + "]");
                                                }
                                            }

                                            // updating invite code count
                                            inviteCodeObj.count++;
                                            _dbContext.SaveChanges();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations Attempted but got [Exception: " + ex + "]");
                                    }
                                }

                                #endregion Send Email Notification To Referrer (If Applicable)

                                return "Thanks for registering! Check your email to complete activation.";
                            }
                            else
                            {
                                Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations FAILED - Could not save Auth Token to DB.");
                                return "Password creation failed.";
                            }
                        }
                        else
                        {
                            Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations FAILED - Could not save Member to DB.");
                            return "Error Saving Member record.";
                        }

                        #endregion Username Not Already Registered
                    }
                    else return "User already exists";
                }
                else
                {
                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations FAILED - No transaction found for [TransID: " + TransId + "]");
                    return "No TransId found";
                }
            }
            catch (Exception ex)
            {
                return "Failure " + ex.ToString();
            }
        }


        public void SendEmailToInvitor(Guid InviteCodeIdUsed, string userJoinedEmail, string userJoinedName)
        {
            Logger.Info("MDA -> SendEmailToInvitor Fired - Invite Code Used [" + InviteCodeIdUsed + "], New User Name: [" +
                        userJoinedName + "], New User Email: [" + userJoinedEmail + "]");

            try
            {
                // Get member who sent invitation

                var memberObj = _dbContext.Members.Where(member => member.InviteCodeId == InviteCodeIdUsed).FirstOrDefault();

                if (memberObj != null)
                {
                    _dbContext.Entry(memberObj).Reload();
                    var fromAddress = Utility.GetValueFromConfig("welcomeMail");
                    var toAddress = CommonHelper.GetDecryptedData(memberObj.UserName);

                    var tokens = new Dictionary<string, string>
                        {
                            {
                                Constants.PLACEHOLDER_FIRST_NAME,
                                CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName))
                            },
                            {
                                Constants.PLACEHOLDER_LAST_NAME,
                                CommonHelper.UppercaseFirst(userJoinedEmail)
                            },
                            {
                                Constants.PLACEHOLDER_FRIEND_FIRST_NAME,
                                CommonHelper.UppercaseFirst(userJoinedName)
                            }
                        };
                    try
                    {
                        Utility.SendEmail("EmailToInvitorAfterSignup", "hello@nooch.com", toAddress, null,
                            userJoinedName + " joined Nooch with your invite code", null, tokens, null, null, null);

                        Logger.Info("MDA -> SendEmailToInvitor - Email sent to Referrer [" + toAddress + "], InviteCode: [" +
                                    InviteCodeIdUsed + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> SendEmailToInvitor email NOT sent to Referrer - [" + toAddress + "], InviteCode: [" +
                                     InviteCodeIdUsed + "], New User's Name: [" + userJoinedName + "], Exception: [" + ex + "]");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> SendEmailToInvitor FAILED (Outer) - Email NOT sent to Referrer - [New User's Email: " + userJoinedEmail +
                             "], [InviteCode:" + InviteCodeIdUsed + "], Exception: [" + ex + "]");
            }
        }


        public string GetRandomNoochId()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            int j = 1;

            for (int i = 0; i <= j; i++)
            {
                var randomId = new string(
                    Enumerable.Repeat(chars, 8)
                        .Select(s => s[random.Next(s.Length)])
                        .ToArray());

                var memberEntity = _dbContext.Members.FirstOrDefault(memberTemp => memberTemp.Nooch_ID.Equals(randomId));

                if (memberEntity == null)
                    return randomId;

                j += i + 1;
            }

            return null;
        }


        #region Synapse Related Methods

        /// <summary>
        /// This is a utility method for actually querying SynapseV3's /user/create API.
        /// This is referenced in multiple other services.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public synapseCreateUserV3Result_int RegisterUserWithSynapseV3(string memberId)
        {
            Logger.Info("MDA -> RegisterUserWithSynapseV3 Fired - Member: [" + memberId + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;

            Guid guid = Utility.ConvertToGuid(memberId);

            var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == guid && m.IsDeleted == false);

            if (noochMember != null)
            {
                // Malkit (5/17/16): Added this check here to see if we have user already in our SynapseCreateUsersResult table.
                //                   This will prevent calling synapse/user/create service for single user multiple times

                #region User Already Has Synapse Account

                var synapseCreateUserObjIfExists = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == guid &&
                                                                                                           m.IsDeleted == false);

                if (synapseCreateUserObjIfExists != null)
                {
                    try
                    {
                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - User ALREADY has a Synapse account - About to query refreshSynapseV3OautKey()");

                        var refreshTokenResult = CommonHelper.refreshSynapseV3OautKey(synapseCreateUserObjIfExists.access_token);

                        if (refreshTokenResult != null && refreshTokenResult.success)
                        {
                            _dbContext.Entry(synapseCreateUserObjIfExists).Reload();

                            if (refreshTokenResult.is2FA)
                            {
                                // 2FA was triggered during /user/signin (Refresh Service), probably b/c the user's Fingerprint has changed since the Synapse user was created.
                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - Got 2FA from Refresh Service - Msg: [" + refreshTokenResult.msg + "]");
                                res.success = false;
                                res.reason = refreshTokenResult.msg;
                            }
                            else
                            {
                                #region Refresh was successful - No 2FA

                                res.oauth = new createUserV3Result_oauth()
                                {
                                    expires_at = synapseCreateUserObjIfExists.expires_at,
                                    oauth_key = CommonHelper.GetDecryptedData(synapseCreateUserObjIfExists.access_token),
                                    refresh_token = CommonHelper.GetDecryptedData(synapseCreateUserObjIfExists.refresh_token),
                                    expires_in = synapseCreateUserObjIfExists.expires_in
                                };

                                res.user = new synapseV3Result_user()
                                {
                                    _id = new synapseV3Result_user_id() { id = synapseCreateUserObjIfExists.user_id },
                                    extra = new synapseV3Result_user_extra()
                                    {
                                        is_business = synapseCreateUserObjIfExists.is_business != null && Convert.ToBoolean(synapseCreateUserObjIfExists.is_business)
                                    },
                                    legal_names = new[] { synapseCreateUserObjIfExists.legal_name },
                                    phone_numbers = new[] { synapseCreateUserObjIfExists.Phone_number },
                                    photos = new[] { synapseCreateUserObjIfExists.photos },
                                    permission = synapseCreateUserObjIfExists.permission
                                };
                                res.user_id = synapseCreateUserObjIfExists.user_id;

                                if (noochMember.IsVerifiedWithSynapse == true)
                                {
                                    Logger.Info("MDA -> RegisterUserWithSynapseV3 - ID Already Verified on [" + noochMember.ValidatedDate +
                                                "] - RETURNING - MemberID: " + memberId + "], ssn_verify_status: [id already verified]");
                                    res.ssn_verify_status = "id already verified";
                                }
                                else if (res.user.permission == "SEND-AND-RECEIVE")
                                {
                                    #region Update IsVerifiedWithSynapse Value In Member Table

                                    try
                                    {
                                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - IsVerifiedWithSynapse was false, but found 'SEND-AND-RECEIVE' " +
                                                    "permission already in SynapseCreateUser Table - [MemberID: " + memberId + "]");

                                        noochMember.IsVerifiedWithSynapse = true;
                                        _dbContext.SaveChanges();

                                        res.ssn_verify_status = "id already verified";
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - IsVerifiedWithSynapse is false, but Synapse returned Permission of \"SEND-AND-RECEIVE\" - " +
                                                     "[Exception: " + ex + "]");
                                    }

                                    #endregion Update IsVerifiedWithSynapse Value In Member Table
                                }

                                // Now check if the user has provided an SSN or FBID or Photo ID.  If yes, call sendDocsToSynapseV3()
                                else
                                {
                                    try
                                    {
                                        if (!String.IsNullOrEmpty(noochMember.SSN) ||
                                            !String.IsNullOrEmpty(noochMember.FacebookUserId) ||
                                            !String.IsNullOrEmpty(noochMember.VerificationDocumentPath))
                                        {
                                            Logger.Info("MDA -> RegisterUserWithSynapseV3 - Found at least 1 document for Synapse (SSN, FBID, or ID Img in Members Table), " +
                                                        "attempting to send SSN info to SynapseV3 - MemberID: [" + memberId + "]");
                                        }

                                        // (CC - 6/1/16): UPDATED TO USE NEW METHOD FOR SENDING *ALL* DOCS AT THE SAME TIME
                                        // (CC - 7/19/16): Synapse is finally ready for us to update this to the NEW (KYC 2.0) API which includes FB and full SSN

                                        // (CC - 8/05/16): I set this up initially to only query senDocsToSynapseV3 if the user had submitted one of: SSN, FB, ID Img...
                                        //                 However, we need to ALWAYS submit this API to Synapse, even if the user hasn't provided any of those.

                                        //submitIdVerificationInt submitAllDocs = CommonHelper.sendUserSsnInfoToSynapseV3(memberId); // OLD - using till Synapse tells us to use the new one (6/5/16)
                                        submitIdVerificationInt submitAllDocs = CommonHelper.sendDocsToSynapseV3(memberId);          // NEW for KYC 2.0
                                        res.ssn_verify_status = submitAllDocs.message;
                                        res.errorMsg = submitAllDocs.message;

                                        #region Logging

                                        if (submitAllDocs.success == true)
                                        {
                                            if (!String.IsNullOrEmpty(submitAllDocs.message) &&
                                                submitAllDocs.message.IndexOf("additional") > -1)
                                            {
                                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - KYC Doc Info Verified Partially - Additional Questions Required - [Email: " +
                                                            CommonHelper.GetDecryptedData(noochMember.UserName) + "], submitSsn.message: [" + submitAllDocs.message + "]");
                                            }
                                            else
                                            {
                                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - KYC Doc Info Verified completely :-) - Email: [" +
                                                            CommonHelper.GetDecryptedData(noochMember.UserName) + "], submitSsn.message: [" + submitAllDocs.message + "]");
                                            }
                                        }
                                        else
                                        {
                                            Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified FAILED :-(  Email: [" +
                                                        CommonHelper.GetDecryptedData(noochMember.UserName) + "], submitSsn.message: [" + submitAllDocs.message + "]");
                                            res.ssn_verify_status = "Not Verified";
                                        }

                                        #endregion Logging
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - Attempted sendUserSsnInfoToSynapseV3 but got Exception: [" + ex.Message + "]");
                                    }
                                }

                                // Cliff (5/17/16): This returns true even if the SSN was unable to be verified above, as long as an Access_Token was found in Nooch's DB to send back/
                                //                  The user may be adding a bank and might still need to answer the ID Verification questions afterwards,
                                //                  or it could be a Rent Scene user who don't use the iOS app, so we can deal with fixing their Permissions after they connect a bank.
                                res.success = true;

                                #endregion Refresh was successful - No 2FA
                            }
                        }
                        else
                        {
                            Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED - Error from Refresh Oauth Key service - Msg: [" + res.errorMsg + "]");
                            res.errorMsg = refreshTokenResult.msg;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED - User already had Synapse Create User record, but got Exception - " +
                                     "MemberID: [" + memberId + "], Exception: [" + res.errorMsg + "]");
                        res.errorMsg = ex.Message;
                    }

                    return res;
                }

                #endregion User Already Has Synapse Account


                #region Initial Checks
                /* cc (6/5/16): Commenting out these checks - not sure what this really accomplishes tbh... preventing a user from registering w/ Synapse
                 *              if they haven't verified their email/phone?  But then there are all the exceptions (NonRegistered, Personal - Browser, Tenant, Landlord)
                 *              so what's the point?
                // Checks on user account: Is user status 'Active'?
                if (noochMember.Status != "Active" &&
                    noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Personal - Browser" &&
                    noochMember.Type != "Landlord" &&
                    noochMember.Type != "Tenant")
                {
                    Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED. Member is not Active but '" + noochMember.Status + "");
                    res.errorMsg = "user status is not active but, " + noochMember.Status;
                    return res;
                }

                // Is phone verified?
                if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                    noochMember.Type != "Landlord" &&
                    noochMember.Type != "Tenant" &&
                    noochMember.Type != "Personal - Browser" &&
                    noochMember.Status != "NonRegistered")
                {
                    Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED. Member's phone is not Verified for Member: [" + memberId + "]");
                    res.errorMsg = "user phone not verified";

                    if (!String.IsNullOrEmpty(noochMember.ContactNumber))
                    {
                        var attemptToResendSms = "TEMP PLACEHOLDER";//ResendVerificationSMS(noochMember.ContactNumber);
                        if (attemptToResendSms == "Success")
                        {
                            res.reason = "User phone not verified - Verification SMS re-sent successfully";
                        }
                        else
                        {
                            res.reason = "User phone not verified - Attmept to resend verification SMS failed";
                        }
                    }
                    else
                    {
                        res.reason = "User phone not verified - no phone number found, cannot attempt to re-send verification SMS";
                    }

                    Logger.Error("MDA -> RegisterUserWithSynapse FAILED. Res.reason is: [" + res.reason + "]");


                    return res;
                }*/

                #endregion Initial Checks


                #region Call Synapse V3 API: /v3/user/create

                List<string> clientIds = CommonHelper.getClientSecretId(memberId);

                string SynapseClientId = clientIds[0];
                string SynapseClientSecret = clientIds[1];

                string fullname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName)) + " " +
                                  CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName));

                synapseCreateUserInput_int payload = new synapseCreateUserInput_int();

                createUser_client client = new createUser_client();
                client.client_id = SynapseClientId;
                client.client_secret = SynapseClientSecret;

                createUser_login logins = new createUser_login();
                logins.email = CommonHelper.GetDecryptedData(noochMember.UserName);
                logins.read_only = false;

                payload.client = client;

                payload.logins = new createUser_login[1];
                payload.logins[0] = logins; // REQUIRED BY SYNAPSE

                payload.phone_numbers = new string[] { noochMember.ContactNumber }; // REQUIRED BY SYNAPSE
                payload.legal_names = new string[] { fullname }; // REQUIRED BY SYNAPSE

                #region Fingerprint

                createUser_fingerprints fingerprints = new createUser_fingerprints();
                if (!String.IsNullOrEmpty(noochMember.UDID1))
                {
                    fingerprints.fingerprint = noochMember.UDID1;
                }
                else if (!String.IsNullOrEmpty(noochMember.DeviceToken))
                {
                    fingerprints.fingerprint = noochMember.DeviceToken;
                }
                else
                {
                    // If for some reason we don't have a value for either UDID1 or DeviceToken, then let's just send a random string of text...
                    // Found this trick online for generating "random" (not technically, but close enough for this purpose) string... make a GUID and remove the "-" (with "n")
                    string randomTxt = Guid.NewGuid().ToString("n").Substring(0, 24);
                    fingerprints.fingerprint = randomTxt;

                    // Now save this new value for the user in the DB for UDID1
                    try
                    {
                        noochMember.UDID1 = randomTxt;
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - Had to create a Fingerprint, but failed on saving new value in DB - Continuing on - " +
                                     "MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                    }
                }
                payload.fingerprints = new createUser_fingerprints[1];
                payload.fingerprints[0] = fingerprints;

                #endregion Fingerprint

                payload.ips = new string[] { CommonHelper.GetRecentOrDefaultIPOfMember(guid) };

                createUser_extra extra = new createUser_extra();
                extra.note = "";
                extra.supp_id = noochMember.Nooch_ID;
                extra.is_business = noochMember.Nooch_ID == "ykDjbVj5" ? true : false; // CLIFF (10/10/12): For Landlords, this could potentially be true... but we'll figure that out later

                if (!String.IsNullOrEmpty(noochMember.cipTag))
                {
                    var cipTag = noochMember.cipTag.ToLower();

                    if (cipTag == "renter" || cipTag == "1") { extra.cip_tag = 1; }
                    else if (cipTag == "vendor" || cipTag == "2") { extra.cip_tag = 2; }
                    else if (cipTag == "landlord" || cipTag == "3") { extra.cip_tag = 3; }
                    else extra.cip_tag = 1; // default
                }
                else
                {
                    try
                    {
                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - No CIP Tag found in DB for this user - Setting to 'RENTER' as default and continuing on - MemberID: [" + memberId + "]");
                        extra.cip_tag = 1;

                        // Update Members Table too
                        noochMember.cipTag = "renter";
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        var error = "MDA -> RegisterUserWithSynapseV3 -> User had no CIP_TAG yet, so attempted to update Members table but failed - MemberID: [" + memberId +
                                    "], Exception: [" + ex + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                    }
                }

                payload.extra = extra;

                try
                {
                    var baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/create" : "https://synapsepay.com/api/v3/user/create";

                    Logger.Info("MDA -> RegisterUserWithSynapseV3 - Payload to send to Synapse /v3/user/create: [" + JsonConvert.SerializeObject(payload) + "]");

                    var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(payload);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    res = JsonConvert.DeserializeObject<synapseCreateUserV3Result_int>(content);
                }
                catch (WebException we)
                {
                    #region Synapse Create User V3 Exception

                    var error = "";

                    res.success = false;

                    var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;
                    res.http_code = httpStatusCode.ToString();

                    var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    JObject errorJsonFromSynapse = JObject.Parse(response);

                    // CLIFF (10/10/15): Synapse lists all possible V3 error codes in the docs -> Introduction -> Errors
                    //                   We might have to do different things depending on which error is returned... 
                    res.error_code = errorJsonFromSynapse["error_code"].ToString();
                    res.errorMsg = errorJsonFromSynapse["error"]["en"].ToString();

                    if (!String.IsNullOrEmpty(res.error_code))
                    {
                        error = "MDA -> RegisterUserWithSynapseV3 FAILED - Synapse Error Code: [" + res.error_code +
                                "], Error Msg: [" + res.errorMsg + "], MemberID: [" + memberId + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);

                        #region Email Already Registered

                        // CLIFF (10/10/15): NOT SURE WHAT SYNAPSE'S RESPONSE/ERROR WILL LOOK LIKE FOR THIS CASE (Docs don't say and can't simulate in Sandbox)
                        if (res.errorMsg == "Email already registered.")
                        {
                            // case when synapse returned email already registered...chances are we have user id in SynapseCreateUserResults table
                            // checking Nooch DB

                            var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == guid && m.IsDeleted == false);

                            if (synapseRes != null)
                            {
                                res.success = true;
                                res.errorMsg = "Account already in Nooch DB for that email";
                                res.user_id = synapseRes.user_id.ToString();
                                res.oauth.oauth_key = CommonHelper.GetDecryptedData(synapseRes.access_token);
                                return res;
                            }
                            else
                            {
                                // WHAT ABOUT THIS CASE? THIS HAPPENS IF SYNAPSE HAS A RECORD FOR THIS EMAIL ADDRESS, BUT NOOCH DOESN'T IN OUR DB...
                                // THIS IS EXTREMELY UNLIKELY TO EVER OCCUR, BUT COULD IF NOOCH EVER HAS A DB PROBLEM FOR 1 OR MORE USERS.
                                // SYNAPSE DOES *NOT* HAVE THE force_create OPTION IN V3...

                                Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED - MemberId: [" + memberId + "]. Synapse Error User Already Registered BUT not found in Nooch DB.");
                                res.errorMsg = "Account already registered, but not found in Nooch DB.";
                            }
                        }

                        #endregion Email Already Registered

                        // CLIFF (10/10/15): Not sure if we actually need to parse this much, but these are all the error types Synapse lists in V3 documentation
                        if (res.error_code == "100") // Incorrect Client Credentials
                        { }
                        else if (res.error_code == "110") // Incorrect User Credentials
                        { }
                        else if (res.error_code == "120") // Unauthorized Fingerprint
                        { }
                        else if (res.error_code == "200") // Error In Payload (Formatting error somewhere)
                        { }
                        else if (res.error_code == "300") // Unauthorized action (User/Client not allowed to perform this action)
                        { }
                        else if (res.error_code == "400") // Incorrect Values Supplied (eg. Insufficient balance, wrong MFA response, incorrect micro deposits)
                        { }
                        else if (res.error_code == "404") // Object not found
                        { }
                        else if (res.error_code == "500") // Synapse Server Error
                        { }
                    }
                    else
                    {
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED: Synapse Error, but *error_code* was null for " +
                                     "MemberID: [" + memberId + "], Exception: [" + we.InnerException + "]");
                    }

                    return res;

                    #endregion Synapse Create User V3 Exception
                }

                #endregion Call Synapse V3 API: /v3/user/create


                #region Synapse Create User Response was SUCCESSFUL

                if (res.success == true && !String.IsNullOrEmpty(res.oauth.oauth_key))
                {
                    res.user_id = !String.IsNullOrEmpty(res.user._id.id) ? res.user._id.id : "";
                    res.ssn_verify_status = "did not check yet";

                    // Mark any existing Synapse 'Create User' results for this user as 'Deleted'
                    #region Delete Any Old Synapse Create User Records Already in DB

                    try
                    {
                        var synapseCreateUserObj = _dbContext.SynapseCreateUserResults.Where(m => m.MemberId == guid && m.IsDeleted == false).ToList();

                        foreach (var scur in synapseCreateUserObj)
                        {
                            scur.IsDeleted = true;
                            scur.ModifiedOn = DateTime.Now;
                            _dbContext.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - Failed To Delete Existing 'Synapse Create User Results' Table - " +
                                     "MemberID: [" + memberId + "], Exception: [" + ex + "]");
                    }

                    #endregion Delete Any Old DB Records & Create New Record


                    #region Add New Entry To SynapseCreateUserResults DB Table

                    bool save = false;

                    try
                    {
                        SynapseCreateUserResult newSynapseUser = new SynapseCreateUserResult();
                        newSynapseUser.MemberId = guid;
                        newSynapseUser.DateCreated = DateTime.Now;
                        newSynapseUser.IsDeleted = false;
                        newSynapseUser.access_token = CommonHelper.GetEncryptedData(res.oauth.oauth_key);
                        newSynapseUser.success = true;
                        newSynapseUser.expires_in = res.oauth.expires_at;
                        newSynapseUser.refresh_token = CommonHelper.GetEncryptedData(res.oauth.refresh_token);
                        newSynapseUser.username = CommonHelper.GetEncryptedData(res.user.logins[0].email);
                        newSynapseUser.user_id = res.user._id.id;

                        // Adding data for new fields in Synapse V3
                        newSynapseUser.is_business = res.user.extra.is_business;
                        newSynapseUser.legal_name = res.user.legal_names.Length > 0 ? res.user.legal_names[0] : null;
                        newSynapseUser.permission = res.user.permission ?? null;
                        newSynapseUser.Phone_number = res.user.phone_numbers.Length > 0 ? res.user.phone_numbers[0] : null;
                        newSynapseUser.photos = res.user.photos.Length > 0 ? res.user.photos[0] : null;

                        newSynapseUser.physical_doc = res.user.doc_status.physical_doc;
                        newSynapseUser.virtual_doc = res.user.doc_status.virtual_doc;
                        newSynapseUser.extra_security = res.user.extra != null ? res.user.extra.extra_security.ToString() : null;
                        newSynapseUser.cip_tag = res.user.extra.cip_tag;

                        // Now add the new record to the DB
                        _dbContext.SynapseCreateUserResults.Add(newSynapseUser);
                        _dbContext.SaveChanges();
                        _dbContext.Entry(newSynapseUser).Reload();
                        save = true;

                        // CC (8/17/16): I DON'T THINK WE NEED TO CREATE A NEW SUBSCRIPTION FOR EVERY BANK.
                        //               I THINK WE JUST CREATE THE SUBSCRIPTION *ONCE* (...EVER) AND THAT APPLIES TO
                        //               *ALL* USERS/BANKS/TRANSACTIONS CREATED USING OUR CLIENT ID/SECRET
                        // subscripe this user on synapse
                        //setSubcriptionToUser(newSynapseUser.user_id.ToString(), guid.ToString());
                    }
                    catch (Exception ex)
                    {
                        var error = "MDA -> RegisterUserWithSynapseV3 - FAILED To Save New Record in 'Synapse Create User Results' Table - " +
                                     "MemberID: [" + memberId + "], Exception: [" + ex + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                        save = false;
                    }

                    #endregion Add New Entry To SynapseCreateUserResults DB Table

                    if (save)
                    {
                        Logger.Info("MDA -> RegisterUserWithSynapseV3 SUCCESS - Synapse User ID: [" + res.user_id + "], Permission: [" + res.user.permission + "]");

                        if (noochMember.IsVerifiedWithSynapse == true)
                        {
                            Logger.Info("MDA -> RegisterUserWithSynapseV3 - ** ID Already Verified ** - MemberID: [" + memberId + "]");
                            res.ssn_verify_status = "id already verified";
                        }
                        else if (res.user.permission == "SEND-AND-RECEIVE") // Probobly wouldn't ever be this b/c I don't think Synapse ever returns this for brand new users
                        {
                            #region User Not Previously Verified But Got Send-Receive Permissions This Time

                            try
                            {
                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - ** ID Already Verified b/c Synapse returned 'Permission' of [" + res.user.permission + "] ** - MemberID: [" + memberId + "]");

                                noochMember.IsVerifiedWithSynapse = true;
                                noochMember.TransferLimit = "5000";
                                _dbContext.SaveChanges();

                                res.ssn_verify_status = "id already verified";
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> RegisterUserWithSynapseV3 - IsVerifiedWithSynapse is false, but Synapse returned Permission of \"SEND-AND-RECEIVE\" - " +
                                             "Exception: [" + ex + "]");
                            }

                            #endregion User Not Previously Verified But Got Send-Receive Permissions This Time
                        }
                        else
                        {
                            try
                            {
                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - ID NOT Already Verified (Expected) - About to send ALL DOCS to SynapseV3 - SSN: [" + noochMember.SSN +
                                            "], FBID: [" + noochMember.FacebookUserId + "], VerificationDocPath: [" + noochMember.VerificationDocumentPath + "], MemberID: [" + memberId + "]");

                                // Check whether the user provided an SSN or Facebook Login
                                if (noochMember.cipTag != "vendor" &&
                                     String.IsNullOrEmpty(noochMember.SSN) &&
                                    (String.IsNullOrEmpty(noochMember.FacebookUserId) || noochMember.FacebookUserId != "not connected") &&
                                     String.IsNullOrEmpty(noochMember.VerificationDocumentPath))
                                {
                                    // User doesn't have any SSN or FB ID saved
                                    var error = "MDA -> RegisterUserWithSynapseV3 - User has no SSN or FB ID to submit to Synapse! SSN: [" + noochMember.SSN +
                                                 "], FB ID: [" + noochMember.FacebookUserId + "], CIP Tag: [" + noochMember.cipTag + "] - CONTINUING ON...";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error);
                                }

                                // (CC - 6/1/16): UPDATED TO USE NEW METHOD FOR SENDING *ALL* DOCS AT THE SAME TIME
                                // (CC - 7/19/16): Synapse is finally ready for us to update this to the NEW (KYC 2.0) API which includes FB and full SSN

                                // (CC - 8/05/16): I set this up initially to only query senDocsToSynapseV3 if the user had submitted one of: SSN, FB, ID Img...
                                //                 However, we need to ALWAYS submit this API to Synapse, even if the user hasn't provided any of those.

                                //submitIdVerificationInt submitAllDocs = CommonHelper.sendUserSsnInfoToSynapseV3(memberId); // OLD - using till Synapse tells us to use the new one (6/5/16)
                                submitIdVerificationInt submitAllDocs = CommonHelper.sendDocsToSynapseV3(memberId);      // NEW
                                res.ssn_verify_status = submitAllDocs.message;
                                res.errorMsg = submitAllDocs.message;

                                #region Logging

                                if (submitAllDocs.success == true)
                                {
                                    if (!String.IsNullOrEmpty(submitAllDocs.message) &&
                                        submitAllDocs.message.IndexOf("additional") > -1)
                                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified, but have additional questions - Email: [" + logins.email +
                                                    "], submitSsn.message: [" + submitAllDocs.message + "]");
                                    else
                                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified completely :-) - Email: [" + logins.email +
                                                    "], submitSsn.message: [" + submitAllDocs.message + "]");
                                }
                                else
                                    Logger.Error("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified FAILED :-(  Email: [" + logins.email + "], submitSsn.message: [" + submitAllDocs.message + "]");

                                #endregion Logging
                            }
                            catch (Exception ex)
                            {
                                var error = "MDA -> RegisterUserWithSynapseV3 FAILED - Attempted CommonHelper.sendDocsToSynapseV3() but got Exception: [" + ex +
                                            "], MemberID: [" + memberId + "]";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                            }
                        }
                    }
                    else
                    {
                        // FAILED TO ADD SYNAPSE RECORD TO DB
                        var error = "MDA -> RegisterUserWithSynapseV3 FAILED - Unable to save record in SynapseCreateUserResult Table - MemberID: [" + memberId + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);

                        res.errorMsg = "Unable to save record in SynapseCreateUserResult Table";
                    }

                    return res;
                }

                #endregion Synapse Create User Response was SUCCESSFUL


                // CLIFF (10/10/15) - DON'T THINK THIS REGION IS NECESSARY FOR SYNAPSE V3... If the call was ever unsuccessful, it would
                //                    return one of the HTTP errors and be handled in Catch block above
                #region Synapse Create User Response Success Was False

                if (res.success == false)
                {
                    // Check if we have user id in SynapseCreateUserResults table in Nooch DB

                    var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == guid && m.IsDeleted == false);

                    if (synapseRes != null)
                    {
                        _dbContext.Entry(synapseRes).Reload();
                        res.success = true;
                        res.errorMsg = "Account already in nooch db.";
                        res.user_id = synapseRes.user_id;
                        res.oauth.oauth_key = CommonHelper.GetDecryptedData(synapseRes.access_token);
                        res.oauth.refresh_token = CommonHelper.GetDecryptedData(synapseRes.refresh_token);
                    }
                    else
                    {
                        var error = "MDA -> RegisterUserWithSynapseV3 FAILED - Synapse returned succes = false, and user not in Nooch DB for [" + memberId + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);

                        res.errorMsg = "Synapse returned succes = false, and user not in Nooch DB";
                    }
                }

                #endregion Synapse Create User Response Success Was False & Reason = Email Already Registered

                else
                {
                    res.errorMsg = !String.IsNullOrEmpty(res.errorMsg) ? res.errorMsg : "Unknown failure :-(";
                    var error = "MDA -> RegisterUserWithSynapseV3 FAILED - MemberId: [" + memberId + "], errorMsg: [" + res.errorMsg + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);
                }
            }
            else // Nooch member was not found in Members.dbo
            {
                var error = "MDA -> RegisterUserWithSynapseV3 FAILED - Member Not Found in DB - MemberID: [" + memberId + "]. Error #2441.";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);

                res.errorMsg = "Given Member ID not found or Member is deleted.";
            }

            return res;
        }


        public synapseCreateUserV3Result_int RegisterExistingUserWithSynapseV3(string transId, string memberId, string userEmail, string userPhone,
                                                                               string userName, string pw, string ssn, string dob, string address,
                                                                               string zip, string fngprnt, string ip, string cip, string fbid,
                                                                               bool isRentScene, string isIdImageAdded = "0", string idImageData = "")
        {
            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 Fired - Name: [" + userName +
                        "], Email: [" + userEmail + "], Phone: [" + userPhone +
                        "], DOB: [" + dob + "], SSN: [" + ssn +
                        "], Address: [" + address + "], ZIP: [" + zip +
                        "], IP: [" + ip + "], Fngprnt: [" + fngprnt +
                        "], TransId: [" + transId + "], CIP: [" + cip +
                        "], FBID: [" + fbid + "], isIdImageAdded: [" + isIdImageAdded + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;
            res.reason = "Initial";
            res.ssn_verify_status = "did not check yet";

            #region Check To Make Sure All Data Was Passed

            // First check critical data necessary for just creating the user
            if (String.IsNullOrEmpty(userName) ||
                String.IsNullOrEmpty(userEmail) ||
                String.IsNullOrEmpty(userPhone))
            {
                string missingData = "";

                if (String.IsNullOrEmpty(userName))
                    missingData = missingData + "Users' Name";
                if (String.IsNullOrEmpty(userEmail))
                    missingData = missingData + " Email";
                if (String.IsNullOrEmpty(userPhone))
                    missingData = missingData + " Phone";

                Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 FAILED - Missing Critical Data: [" + missingData.Trim() + "]");

                res.reason = "Missing critical data: " + missingData.Trim();
                return res;
            }

            if (String.IsNullOrEmpty(ssn) || String.IsNullOrEmpty(dob) ||
                String.IsNullOrEmpty(address) || String.IsNullOrEmpty(zip) ||
                String.IsNullOrEmpty(fngprnt) || String.IsNullOrEmpty(ip))
            {
                string missingData2 = "";

                if (String.IsNullOrEmpty(ssn))
                    missingData2 = missingData2 + "SSN";
                if (String.IsNullOrEmpty(dob))
                    missingData2 = missingData2 + " DOB";
                if (String.IsNullOrEmpty(address))
                    missingData2 = missingData2 + "Address";
                if (String.IsNullOrEmpty(zip))
                    missingData2 = missingData2 + " ZIP";
                if (String.IsNullOrEmpty(fngprnt))
                    missingData2 = missingData2 + " Fingerprint";
                if (String.IsNullOrEmpty(ip))
                    missingData2 = missingData2 + " IP";

                Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Missing Non-Critical Data: [" + missingData2.Trim() + "]");
            }

            #endregion Check To Make Sure All Data Was Passed

            Guid memGuid = Utility.ConvertToGuid(memberId);

            var memberObj = _dbContext.Members.FirstOrDefault(memberTemp => memberTemp.MemberId.Equals(memGuid) &&
                                                                            memberTemp.IsDeleted == false);

            if (memberObj != null)
            {
                _dbContext.Entry(memberObj).Reload();

                #region Check If Given Phone Already Exists

                string memberIdFromPhone = CommonHelper.GetMemberIdByContactNumber(userPhone);
                if (!String.IsNullOrEmpty(memberIdFromPhone))
                {
                    // Now check if the member found for that Phone # is this user or another user,
                    // If another user, abort.
                    if (memberIdFromPhone.ToLower() != memberId.ToLower())
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 FAILED - Phone number registered to a different user - " +
                                     "Phone number submitted: [" + userPhone + "], Other Member Found in DB: [" + memberIdFromPhone + "]");
                        res.reason = "Given phone number already registered to another user.";
                        //return res;
                    }
                }

                #endregion Check if given email or phone already exists


                #region Update Member's Record in Members.dbo

                // Get state from ZIP via Google Maps API
                var googleMapsResult = CommonHelper.GetCityAndStateFromZip(zip.Trim());
                var stateAbbrev = googleMapsResult != null && googleMapsResult.stateAbbrev != null ? googleMapsResult.stateAbbrev : "";
                var cityFromGoogle = googleMapsResult != null && !String.IsNullOrEmpty(googleMapsResult.city) ? googleMapsResult.city : "";

                // Add member details based on given name, email, phone, & other parameters
                if (userName.IndexOf('+') > -1)
                    userName.Replace("+", " ");
                string[] namearray = userName.Split(' ');
                string FirstName = CommonHelper.GetEncryptedData(namearray[0]);
                string LastName = " ";

                // Example Name Formats: Most Common: 1.) Charles Smith
                //                       Possible Variations: 2.) Charles   3.) Charles H. Smith
                //                       4.) CJ Smith   5.) C.J. Smith   6.)  Charles Andrew Thomas Smith

                if (namearray.Length > 1)
                {
                    if (namearray.Length == 2) // For regular First & Last name: Charles Smith
                        LastName = CommonHelper.GetEncryptedData(namearray[1]);
                    else if (namearray.Length == 3)
                        // For 3 names, could be a middle name or middle initial: Charles H. Smith or Charles Andrew Smith
                        LastName = CommonHelper.GetEncryptedData(namearray[2]);
                    else
                        // For more than 3 names (some people have 2 or more middle names)
                        LastName = CommonHelper.GetEncryptedData(namearray[namearray.Length - 1]);
                }

                // Convert string Date of Birth to DateTime
                if (String.IsNullOrEmpty(dob)) // ...it shouldn't ever be empty for this method
                {
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - DOB was NULL, reassigning it to 'Jan 20, 1981' - Name: [" + userName + "], TransId: [" + transId + "]");
                    dob = "01/20/1981";
                }
                DateTime dateofbirth;
                if (!DateTime.TryParse(dob, out dateofbirth))
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - DOB was NULL - Name: [" + userName + "], TransId: [" + transId + "]");

                //Rajat Puri 14/6/2016 The following region is a kind of duplicate as the same work will get done at line number 2845 
                // in the region *saving user image if provided* This will  upload same image again on the server.

                #region

                if (isIdImageAdded == "1" && !String.IsNullOrEmpty(idImageData))
                {
                    // We have ID Doc image... Now save on server and get URL to save
                    try
                    {
                        Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 -> Image Was Passed, About to Query SaveBase64AsImage() -> " +
                                    "Email: [" + userEmail + "], MemberID: [" + memberObj.MemberId.ToString() + "]");

                        var saveImageOnServer = SaveBase64AsImage(memberObj.MemberId.ToString(), idImageData);

                        if (saveImageOnServer.success && !String.IsNullOrEmpty(saveImageOnServer.msg))
                            memberObj.VerificationDocumentPath = saveImageOnServer.msg;
                        else
                            Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 -> Attempted to Save ID Doc on server but failed - SaveBase64AsImage Msg: [" + saveImageOnServer.msg + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 -> Attempted to Save ID Doc on server but failed -> Exception: [" + ex +
                                     "], Email: [" + userEmail + "], MemberID: [" + res.memberIdGenerated + "]");
                    }
                }

                #endregion

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);
                memberObj.SecondaryEmail = memberObj.UserName; // In case the supplied email is different than what the Landlord used to invite, saving the original email here as secondary, and updating UserName in next line
                memberObj.UserName = CommonHelper.GetEncryptedData(userEmail.Trim()); // Username might be different if user changes the email on the payRequest page
                memberObj.UserNameLowerCase = CommonHelper.GetEncryptedData(userEmail.Trim().ToLower());
                memberObj.FirstName = FirstName;
                memberObj.LastName = LastName;
                memberObj.ContactNumber = userPhone;
                memberObj.Address = !String.IsNullOrEmpty(address) ? CommonHelper.GetEncryptedData(address) : null;
                memberObj.City = !String.IsNullOrEmpty(cityFromGoogle) ? CommonHelper.GetEncryptedData(cityFromGoogle) : null;
                memberObj.State = !String.IsNullOrEmpty(stateAbbrev) ? CommonHelper.GetEncryptedData(stateAbbrev) : null;
                memberObj.Zipcode = !String.IsNullOrEmpty(zip) ? CommonHelper.GetEncryptedData(zip) : null;
                memberObj.SSN = !String.IsNullOrEmpty(ssn) ? CommonHelper.GetEncryptedData(ssn) : null;
                memberObj.DateOfBirth = dateofbirth;
                memberObj.Status = "Active";
                memberObj.UDID1 = !String.IsNullOrEmpty(fngprnt) ? fngprnt : memberObj.UDID1; // if this has changed since what it originally was, then the user will eventually have to complete the 2FA for Synapse
                memberObj.DateModified = DateTime.Now;
                memberObj.cipTag = !String.IsNullOrEmpty(cip) ? cip : memberObj.cipTag;
                memberObj.FacebookUserId = !String.IsNullOrEmpty(fbid) ? fbid : memberObj.FacebookUserId;
                if (memberObj.isRentScene == null)
                    memberObj.isRentScene = isRentScene == true ? true : false;
                else
                    memberObj.isRentScene = isRentScene == true ? true : memberObj.isRentScene;

                if (!String.IsNullOrEmpty(pw))
                    memberObj.Password = CommonHelper.GetEncryptedData(pw);

                int save = 0;
                try
                {
                    save = _dbContext.SaveChanges();
                    save++;
                    _dbContext.Entry(memberObj).Reload();
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - FAILED to add new Member to DB - Exception: [" + ex + "]");
                    res.reason = "Failed to update Member in DB - [" + ex.Message + "]";
                    return res;
                }

                // Now add the IP address record to the MembersIPAddress Table
                try
                {
                    CommonHelper.UpdateMemberIPAddressAndDeviceId(memberId, ip, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 EXCEPTION on trying to save new Member's IP Address - " +
                                 "MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }

                #endregion Update Member's Record in Members.dbo

                #region Member Updated In Nooch DB Successfully

                #region Update Tenant Record If For A Tenant

                if (memberObj.Type == "Tenant")
                {
                    try
                    {

                        var tenantObj = _dbContext.Tenants.FirstOrDefault(memberTemp => memberTemp.MemberId == memGuid &&
                                                                                        memberTemp.IsDeleted == false);

                        if (tenantObj != null)
                        {
                            _dbContext.Entry(tenantObj).Reload();
                            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - This is a TENANT - About to update Tenants Table " +
                                                  "MemberID: [" + memberId + "]");

                            tenantObj.FirstName = FirstName;
                            tenantObj.LastName = LastName;
                            tenantObj.eMail = memberObj.UserName;

                            tenantObj.DateOfBirth = dateofbirth;

                            tenantObj.PhoneNumber = userPhone;
                            tenantObj.AddressLineOne = memberObj.Address;
                            tenantObj.Zip = memberObj.Zipcode;
                            tenantObj.SSN = memberObj.SSN;

                            #region Check If Email Should Be Verified

                            // Now compare the Email address this user entered with the email the Landlord entered to invite this tenant.
                            // If the emails match, we can set this user's "IsEmailVerified" to "true" because they would have had to click the 
                            // link in the 'RequestReceived' email in order to submit their info.
                            try
                            {
                                Guid transGuid = new Guid(transId);

                                var emailUsedToInvite = (from trans in _dbContext.Transactions
                                                         where trans.TransactionId == transGuid
                                                         select trans.InvitationSentTo).FirstOrDefault();

                                if (!String.IsNullOrEmpty(emailUsedToInvite))
                                {
                                    if (emailUsedToInvite.Trim().ToLower() == userEmail.Trim().ToLower())
                                    {
                                        Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - Email provided by new user [" + userEmail +
                                                    "] matches Email from transaction ('InvitationSentTo') [" + emailUsedToInvite + "] - " +
                                                    "Marking Tenant's Email as Verified in Tenants Table.");

                                        tenantObj.IsEmailVerified = true;
                                    }
                                    else
                                    {
                                        Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - Email provided by new user [" + userEmail +
                                                    "] does NOTE match Email from transaction ('InvitationSentTo') [" + emailUsedToInvite + "] - " +
                                                    "Marking Tenant's Email as UN-Verified in Tenants Table.");

                                        tenantObj.IsEmailVerified = false;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Error on comparing email provided by new user [" + userEmail +
                                             "] with Email from transaction ('InvitationSentTo') - Exception: [" + ex.Message + "]");

                                tenantObj.IsEmailVerified = false;
                            }

                            #endregion Check If Email Should Be Verified

                            tenantObj.DateModified = DateTime.Now;
                            _dbContext.Entry(tenantObj).Reload();

                            if (_dbContext.SaveChanges() > 0)
                            {
                                Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - Saved changes to TENANT table successfully - " +
                                            "MemberID: [" + memberId + "]");
                            }
                            else
                            {
                                Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - FAILED to save changes to TENANT table - " +
                                             "MemberID: [" + memberId + "]");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 EXCEPTION on checking if this user is a TENANT - " +
                                     "MemberID: [" + memberId + "], Exception: [" + ex + "]");
                    }
                }

                #endregion Update Tenant Record If For A Tenant


                Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - Nooch Member UPDATED SUCCESSFULLY IN DB (via Browser Landing Page) - " +
                            "UserName: [" + userEmail + "], MemberID: [" + memberId + "]");

                //bool didUserAddPw = false;

                #region Check If PW Was Supplied To Create Full Account

                if (!String.IsNullOrEmpty(pw))
                {
                    //didUserAddPw = true;

                    #region Send New Account Email To New User

                    var fromAddress = Utility.GetValueFromConfig("welcomeMail");
                    var firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                    var lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));

                    var link = "";

                    var tokens = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                {Constants.PLACEHOLDER_LAST_NAME, lastName},
                                {Constants.PLACEHOLDER_OTHER_LINK, link}
                            };
                    try
                    {
                        Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                            fromAddress, userEmail, null, "Confirm your email on Nooch", link,
                            tokens, null, null, null);

                        Logger.Info("MDA - Registration mail sent to [" + userEmail + "] successfully.");
                    }
                    catch (Exception)
                    {
                        Logger.Error("MDA - Member activation mail NOT sent to [" + userEmail + "]");
                    }

                    #endregion Send New Account Email To New User


                    #region Temp PIN Email

                    // Email user the auto-generated PIN
                    var tokens2 = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                            };
                    try
                    {
                        Utility.SendEmail("pinSetForNewUser", fromAddress, userEmail, null,
                            "Your temporary Nooch PIN", null, tokens2, null, null, null);

                        Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - Temp PIN email sent to [" + userEmail + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Temp PIN email NOT sent to [" + userEmail + "], Exception: [" + ex + "]");
                    }

                    #endregion Temp PIN Email
                }

                #endregion Check If PW Was Supplied To Create Full Account


                #region Saving ID Image If Provided


                if (isIdImageAdded == "1" && !String.IsNullOrEmpty(idImageData))
                {
                    // We have ID Doc image... saving it on Nooch's Server and making entry in Members table.
                    try
                    {
                        Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 -> About to save ID Doc on Nooch's server -> Email: [" + userEmail + "], MemberID: [" + memberObj.MemberId + "]");

                        var saveImageOnServer = SaveBase64AsImage(memberObj.MemberId.ToString(), idImageData);

                        if (saveImageOnServer.success && !String.IsNullOrEmpty(saveImageOnServer.msg))
                        {
                            memberObj.VerificationDocumentPath = saveImageOnServer.msg;
                            _dbContext.SaveChanges();
                            _dbContext.Entry(memberObj).Reload();

                            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 -> Successfully saved ID Doc at: [" +
                                        memberObj.VerificationDocumentPath + "]");
                        }
                        else
                            Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 -> Attempted to Save ID Doc on server but failed - " +
                                         "SaveBase64AsImage Msg: [" + saveImageOnServer.msg + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 -> Attempted to Save ID Doc on server but failed -> Exception: [" + ex.Message +
                                     "], Email: [" + userEmail + "], MemberID: [" + memberObj.MemberId + "]");
                    }
                }
                else
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - NO IMAGE SENT");

                #endregion Saving ID Image If Provided


                // NOW WE HAVE CREATED A NEW NOOCH USER RECORD AND SENT THE REGISTRATION EMAIL (IF THE USER
                // PROVIDED A PW TO CREATE AN ACCOUNT) NEXT, ATTEMPT TO CREATE A SYNAPSE ACCOUNT FOR THIS USER.
                #region Create User with Synapse

                synapseCreateUserV3Result_int createSynapseUserResult = new synapseCreateUserV3Result_int();
                try
                {
                    // Call Synapse create user service
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 ABOUT TO CALL CREATE SYNAPSE USER METHOD...");
                    createSynapseUserResult = RegisterUserWithSynapseV3(memberObj.MemberId.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - createSynapseUser FAILED - Username: [" + userEmail + "], Exception: [" + ex.Message + "]");
                }

                if (createSynapseUserResult != null)
                {
                    res = createSynapseUserResult;

                    if (createSynapseUserResult.success == true &&
                        !String.IsNullOrEmpty(createSynapseUserResult.oauth.oauth_key))
                    {
                        #region Created Synapse User Successfully

                        if (!String.IsNullOrEmpty(res.reason) &&
                            res.reason.IndexOf("Email already registered") > -1)
                        {
                            res.reason = "User already existed, successfully received consumer_key.";
                            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 SUCCESS -> Reason: [" + res.reason + "], Email: [" + userEmail +
                                        "], user_id: [" + res.user_id + "], oauth_consumer_key: [" + createSynapseUserResult.oauth.oauth_key + "]");
                        }

                        // EXPECTED OUTCOME for most users creating a NEW Synapse Account.
                        // Synapse doesn't always return a "reason" anymore (they used to but stopped sending it for newly created users apparently)
                        else
                        {
                            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 SUCCESS - Email: [" + userEmail + "], User_id: [" + createSynapseUserResult.user_id +
                                        "], oauth_consumer_key: [" + createSynapseUserResult.oauth.oauth_key + "]");
                        }

                        createUserV3Result_oauth oath = new createUserV3Result_oauth();
                        oath.oauth_key = createSynapseUserResult.oauth.oauth_key; // Already know it's not NULL, so don't need to re-check
                        oath.expires_in = !String.IsNullOrEmpty(createSynapseUserResult.oauth.expires_in) ? createSynapseUserResult.oauth.expires_in : "";

                        res.oauth = oath;
                        res.user_id = !String.IsNullOrEmpty(createSynapseUserResult.user._id.id) ? createSynapseUserResult.user._id.id : "";
                        res.memberIdGenerated = memberObj.MemberId.ToString();
                        res.error_code = createSynapseUserResult.error_code;
                        res.errorMsg = createSynapseUserResult.errorMsg;

                        res.success = true;

                        #endregion Created Synapse User Successfully
                    }
                    else
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 FAILED - Synapse Create user service failed (but wasn't null) - " +
                                     "Reason: [" + res.reason + "], ErrorMsg: [" + res.errorMsg + "], MemberID: [" + memberId + "]");
                        res.reason = createSynapseUserResult.reason;
                    }
                }
                else
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - createSynapseUser FAILED & Returned NULL");
                    res.reason = !String.IsNullOrEmpty(createSynapseUserResult.reason) ? createSynapseUserResult.reason : "Reg NonNooch User w/ Syn: Error 2962.";
                }

                #endregion Create User with Synapse


                #endregion Member Updated In Nooch DB Successfully
            }
            else
            {
                Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - FAILED - MemberID not found in DB - MemberID: [" + memberId + "]");
                res.reason = "MemberID not found in DB.";
            }

            return res;
        }


        public synapseCreateUserV3Result_int RegisterNonNoochUserWithSynapseV3(string transId, string userEmail, string userPhone, string userName, string pw,
                                                                               string ssn, string dob, string address, string zip, string fngprnt, string ip,
                                                                               string cip, string fbid, bool isRentScene, string isIdImageAdded = "0", string idImageData = "")
        {
            // What's the plan? -- Store new Nooch member, then create Synpase user, then check if user supplied a (is password.Length > 0)
            // then store data in new added field in SynapseCreateUserResults table for later use

            Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 Initiated - Name: [" + userName +
                        "], Email: [" + userEmail + "], Phone: [" + userPhone +
                        "], DOB: [" + dob + "], SSN: [" + ssn +
                        "], Address: [" + address + "], ZIP: [" + zip +
                        "], IP: [" + ip + "], Fngprnt: [" + fngprnt +
                        "], TransId: [" + transId + "], CIP: [" + cip +
                        "], FBID: [" + fbid + "], isRentScene: [" + isRentScene +
                        "], isIdImageAdded: [" + isIdImageAdded + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;
            res.reason = "Initial";

            string NewUsersNoochMemId = "";


            #region Check To Make Sure All Data Was Passed

            // First check critical data necessary for just creating the user
            if (String.IsNullOrEmpty(userName) || String.IsNullOrEmpty(userEmail) ||
                String.IsNullOrEmpty(userPhone) || String.IsNullOrEmpty(zip) || String.IsNullOrEmpty(fngprnt))
            {
                string missingData = "";

                if (String.IsNullOrEmpty(userName))
                    missingData = missingData + "Users' Name";
                if (String.IsNullOrEmpty(userEmail))
                    missingData = missingData + " Email";
                if (String.IsNullOrEmpty(userPhone))
                    missingData = missingData + " Phone";
                if (String.IsNullOrEmpty(zip))
                    missingData = missingData + " ZIP";
                if (String.IsNullOrEmpty(fngprnt))
                    missingData = missingData + " Fingerprint";

                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 FAILED - Missing Critical Data: [" + missingData.Trim() + "]");

                res.reason = "Missing critical data: " + missingData.Trim();
                return res;
            }

            if (String.IsNullOrEmpty(ssn) || String.IsNullOrEmpty(dob) ||
                String.IsNullOrEmpty(address) || String.IsNullOrEmpty(ip) || String.IsNullOrEmpty(fngprnt))
            {
                string missingData2 = "";

                if (String.IsNullOrEmpty(ssn))
                    missingData2 = missingData2 + "SSN";
                if (String.IsNullOrEmpty(dob))
                    missingData2 = missingData2 + " DOB";
                if (String.IsNullOrEmpty(address))
                    missingData2 = missingData2 + "Address";
                if (String.IsNullOrEmpty(fngprnt))
                    missingData2 = missingData2 + " Fingerprint";
                if (String.IsNullOrEmpty(ip))
                    missingData2 = missingData2 + " IP";

                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - Missing Non-Critical Data: [" + missingData2.Trim() + "]. Continuing on...");
            }

            #endregion Check To Make Sure All Data Was Passed


            #region Check if given Email and Phone already exist

            string memberIdFromEmail = CommonHelper.GetMemberIdByUserName(userEmail);
            if (!String.IsNullOrEmpty(memberIdFromEmail))
            {
                // Check if the user was created w/in the last 60 minutes... then we can assume it's the same person
                Member memberObj = CommonHelper.GetMemberDetails(memberIdFromEmail);

                //if (memberObj.DateCreated > )
                DateTime todaysDateTime = DateTime.Now;
                TimeSpan span = todaysDateTime.Subtract(Convert.ToDateTime(memberObj.DateCreated));
                double totalMins = span.TotalMinutes;

                if (totalMins < 60)
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - Email already registered, but user created < 60 mins ago [" +
                                totalMins + "], so sending to RegisterExistingUserWithSynapseV3()");

                    // Consider the user valid, send to RegisterExistingUserWithSynapseV3()
                    return RegisterExistingUserWithSynapseV3(transId, memberObj.MemberId.ToString(), userEmail,
                                                             userPhone, userName, pw, ssn, dob, address, zip, fngprnt,
                                                             ip, cip, fbid, isRentScene, isIdImageAdded, idImageData);
                }
                else
                {
                    // CC (8/4/16): We really shouldn't abort in this case. Instead, we should verify that the user actually is the owner
                    //              of the email by sending a 5-digit numeric code to the email address and prompting the user to input it
                    //              on the page (which could be CreateAccount, PayRequest or DepositMoney)
                    var error = "MDA -> RegisterNonNoochUserWithSynapseV3 FAILED - EMAIL Already Registered: [" + userEmail + "] - ABORTING";
                    CommonHelper.notifyCliffAboutError(error);
                    Logger.Error(error);

                    res.reason = "Given email already registered.";
                    return res;
                }
            }

            string memberIdFromPhone = CommonHelper.GetMemberIdByContactNumber(userPhone);
            if (!String.IsNullOrEmpty(memberIdFromPhone))
            {
                // CC (8/4/16): We really shouldn't abort in this case. Instead, we should verify that the user actually is the owner
                //              of the Phone # by sending  a 5-digit numeric code to the Phone # and prompting the user to input it
                //              on the page (which could be CreateAccount, PayRequest or DepositMoney)
                var error = "MDA -> RegisterNonNoochUserWithSynapseV3 FAILED - PHONE Already Registered: [" + userEmail + "] - ABORTING";
                CommonHelper.notifyCliffAboutError(error);
                Logger.Error(error);

                res.reason = "Given phone number already registered.";
                return res;
            }

            #endregion Check if given email or phone already exists


            // Set up member details based on given name, email, phone, & other parameters
            string noochRandomId = GetRandomNoochId();

            if (!String.IsNullOrEmpty(noochRandomId))
            {
                #region Get Invite Code ID from transaction

                string inviteCode = "";
                string inviteCodeMemberName = "";

                Transaction trans = new Transaction();

                try
                {
                    Guid tid = Utility.ConvertToGuid(transId);

                    trans = _dbContext.Transactions.FirstOrDefault(transIdTemp => transIdTemp.TransactionId == tid);

                    if (trans != null)
                    {
                        _dbContext.Entry(trans).Reload();
                        inviteCode = trans.Member.InviteCodeId.ToString();

                        // Now get the Member's Name associated with the Invite Code from Members Table
                        inviteCodeMemberName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName)) + " " +
                                               CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 -> FAILED to lookup Invite Code from Transaction - " +
                                 "TransID: [" + transId + "], Exception: [" + ex + "]");
                }

                #endregion Get Invite Code ID from transaction


                #region Create New Nooch Member Record in Members.dbo

                #region Parse And Format Data To Save

                // Get state from ZIP via Google Maps API
                var googleMapsResult = CommonHelper.GetCityAndStateFromZip(zip.Trim());
                var stateAbbrev = googleMapsResult != null && googleMapsResult.stateAbbrev != null ? googleMapsResult.stateAbbrev : "";
                var cityFromGoogle = googleMapsResult != null && !String.IsNullOrEmpty(googleMapsResult.city) ? googleMapsResult.city : "";

                if (userName.IndexOf('+') > -1)
                {
                    userName.Replace("+", " ");
                }
                string[] namearray = userName.Split(' ');
                string FirstName = CommonHelper.GetEncryptedData(namearray[0]);
                string LastName = " ";

                // Example Name Formats: Most Common: 1.) Charles Smith
                //                       Possible Variations: 2.) Charles   3.) Charles H. Smith
                //                       4.) CJ Smith   5.) C.J. Smith   6.)  Charles Andrew Thomas Smith

                if (namearray.Length > 1)
                {
                    if (namearray.Length == 2)
                    {
                        // For regular First & Last name: Charles Smith
                        LastName = CommonHelper.GetEncryptedData(namearray[1]);
                    }
                    else if (namearray.Length == 3)
                    {
                        // For 3 names, could be a middle name or middle initial: Charles H. Smith or Charles Andrew Smith
                        LastName = CommonHelper.GetEncryptedData(namearray[2]);
                    }
                    else
                    {
                        // For more than 3 names (some people have 2 or more middle names)
                        LastName = CommonHelper.GetEncryptedData(namearray[namearray.Length - 1]);
                    }
                }

                // Convert string Date of Birth to DateTime
                if (String.IsNullOrEmpty(dob)) // ...it shouldn't ever be empty for this method
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - DOB was NULL, reassigning it to 'Jan 20, 1980' - Name: [" + userName + "], TransID: [" + transId + "]");
                    dob = "01/20/1981";
                }
                DateTime dateofbirth;
                if (!DateTime.TryParse(dob, out dateofbirth))
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - DOB was NULL - Name: [" + userName + "], TransId: [" + transId + "]");
                }

                string userNameLowerCase = userEmail.Trim().ToLower();
                string userNameLowerCaseEncr = CommonHelper.GetEncryptedData(userNameLowerCase);
                string secondaryEmail;

                // CLIFF (5/21/16): If this new user was invited via Email, then set the SecondaryEmail as the InvitationSentTo value.
                //                  This helps if the user enters a different email during ID Verification form.
                secondaryEmail = trans != null && !String.IsNullOrEmpty(trans.InvitationSentTo)
                                 ? trans.InvitationSentTo
                                 : userNameLowerCaseEncr;

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);

                if (!String.IsNullOrEmpty(cip))
                {
                    if (cip == "1") cip = "renter";
                    else if (cip == "2") cip = "vendor";
                    else if (cip == "3") cip = "landlord";
                }

                #endregion Parse And Format Data To Save

                var member = new Member
                {
                    Nooch_ID = noochRandomId,
                    MemberId = Guid.NewGuid(),
                    FirstName = FirstName,
                    LastName = LastName,
                    UserName = userNameLowerCaseEncr,
                    UserNameLowerCase = userNameLowerCaseEncr,
                    SecondaryEmail = secondaryEmail,
                    RecoveryEmail = userNameLowerCaseEncr,
                    ContactNumber = userPhone,
                    Address = !String.IsNullOrEmpty(address) ? CommonHelper.GetEncryptedData(address) : null,
                    City = !String.IsNullOrEmpty(cityFromGoogle) ? CommonHelper.GetEncryptedData(cityFromGoogle) : null,
                    State = !String.IsNullOrEmpty(stateAbbrev) ? CommonHelper.GetEncryptedData(stateAbbrev) : null,
                    Zipcode = !String.IsNullOrEmpty(zip) ? CommonHelper.GetEncryptedData(zip) : null,
                    SSN = !String.IsNullOrEmpty(ssn) ? CommonHelper.GetEncryptedData(ssn) : null,
                    DateOfBirth = dateofbirth,
                    Password = !String.IsNullOrEmpty(pw) ? CommonHelper.GetEncryptedData(pw) : CommonHelper.GetEncryptedData("jibb3r;jawn"),
                    PinNumber = pinNumber,
                    Status = Constants.STATUS_NON_REGISTERED,
                    IsDeleted = false,
                    DateCreated = DateTime.Now,
                    Type = "Personal - Browser",
                    Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png",
                    UDID1 = !String.IsNullOrEmpty(fngprnt) ? fngprnt : null,
                    IsVerifiedWithSynapse = false,
                    cipTag = !String.IsNullOrEmpty(cip) ? cip : "renter",
                    FacebookUserId = fbid,
                    FacebookAccountLogin = fbid,
                    isRentScene = isRentScene == true ? true : false,
                };

                if (inviteCode.Length > 0)
                    member.InviteCodeIdUsed = Utility.ConvertToGuid(inviteCode);

                NewUsersNoochMemId = member.MemberId.ToString();

                // ADD NEWLY CREATED MEMBER TO NOOCH DB

                int addNewMemberToDB = 0;
                try
                {
                    _dbContext.Members.Add(member);
                    addNewMemberToDB = _dbContext.SaveChanges();
                    _dbContext.Entry(member).Reload();
                }
                catch (Exception ex)
                {
                    var error = "MDA -> RegisterNonNoochUserWithSynapseV3 - FAILED to save new member in DB - Email: [" + userEmail +
                                 "], MemberID: [" + NewUsersNoochMemId + "], Exception: [" + ex + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);
                    throw ex;
                }

                #endregion Create New Nooch Member Record in Members.dbo


                #region Member Added to Nooch DB Successfully

                if (addNewMemberToDB > 0)
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - New Member successfully added to DB (via Browser Landing Page) - " +
                                "UserName: [" + userEmail + "], MemberID: [" + member.MemberId + "]");

                    // NOW ADD THE IP ADDRESS RECORD TO THE MembersIPAddress Table =
                    // (waited until after the member was actually added to the Members table above... shouldn't actually matter b/c 
                    // the UpdateIPAddress method will just create a new record in the IP Address table if one doesn't exist, but just to be safe.)
                    try
                    {
                        CommonHelper.UpdateMemberIPAddressAndDeviceId(NewUsersNoochMemId, ip, fngprnt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 EXCEPTION on trying to save new Member's IP Address - " +
                                     "MemberID: [" + NewUsersNoochMemId + "], Exception: [" + ex + "]");
                    }

                    #region Save Notification & Privacy Settings

                    try
                    {
                        var memberNotificationSettings = new MemberNotification
                        {
                            NotificationId = Guid.NewGuid(),
                            MemberId = member.MemberId,
                            FriendRequest = true,
                            InviteRequestAccept = true,
                            TransferSent = true,
                            TransferReceived = true,
                            TransferAttemptFailure = true,
                            NoochToBank = true,
                            BankToNooch = true,
                            EmailFriendRequest = true,
                            EmailInviteRequestAccept = true,
                            EmailTransferSent = true,
                            EmailTransferReceived = true,
                            EmailTransferAttemptFailure = true,
                            TransferUnclaimed = true,
                            BankToNoochRequested = true,
                            BankToNoochCompleted = true,
                            NoochToBankRequested = true,
                            NoochToBankCompleted = true,
                            InviteReminder = true,
                            LowBalance = true,
                            ValidationRemainder = true,
                            ProductUpdates = true,
                            NewAndUpdate = true,
                            DateCreated = DateTime.Now
                        };

                        _dbContext.MemberNotifications.Add(memberNotificationSettings);
                        _dbContext.SaveChanges();
                        _dbContext.Entry(memberNotificationSettings).Reload();

                        var memberPrivacySettings = new MemberPrivacySetting
                        {
                            MemberId = member.MemberId,
                            AllowSharing = true,
                            ShowInSearch = true,
                            DateCreated = DateTime.Now
                        };

                        _dbContext.MemberPrivacySettings.Add(memberPrivacySettings);
                        _dbContext.SaveChanges();
                        _dbContext.Entry(memberPrivacySettings).Reload();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 ERROR -> Attempted to Create / Save Notification & Privacy " +
                                     "Settings for new Member but failed - Exception: [" + ex.Message + "]");
                    }

                    #endregion Save Notification & Privacy Settings

                    // WE ARE ADDING EVERY PERSON TO DB IN ORDER TO HAVE THE INFO TO CREATE A SYNAPSE USER
                    // ...EVEN IF THE NON-NOOCH USER DOES NOT PROVIDE A PW TO 'CREATE' A NOOCH ACCOUNT.
                    // SO NOW THAT THE USER HAS JUST BEEN CREATED, CHECK IF THEY WANTED A FULL NOOCH ACCOUNT
                    // BY CHECKING IF A PW WAS PROVIDED & THEN SEND NEW USER EMAILS

                    //bool didUserAddPw = false;

                    #region Check If PW Was Supplied To Create Full Account

                    if (!String.IsNullOrEmpty(pw))
                    {
                        //didUserAddPw = true;
                        #region Generate & Save Nooch Authentication Token

                        var tokenId = Guid.NewGuid();
                        var requestId = Guid.Empty;
                        // save the token details into authentication tokens table  
                        var token = new AuthenticationToken
                        {
                            TokenId = tokenId,
                            MemberId = member.MemberId,
                            IsActivated = false,
                            DateGenerated = DateTime.Now,
                            FriendRequestId = requestId  // CLIFF (7/28/15): What is this? It's always being set to an empty GUID...
                        };

                        bool status = _dbContext.SaveChanges() > 0;
                        // _dbContext.Entry(token).Reload();

                        #endregion Generate & Save Nooch Authentication Token

                        #region Send New Account Email To New User

                        var fromAddress = Utility.GetValueFromConfig("welcomeMail");
                        var firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName));
                        var lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName));

                        // Add link with autogenerated token for the email verification URL
                        var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                "Nooch/Activation?tokenId=" + tokenId);

                        var tokens = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                {Constants.PLACEHOLDER_LAST_NAME, lastName},
                                {Constants.PLACEHOLDER_OTHER_LINK, link}
                            };
                        try
                        {
                            Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                                fromAddress, userEmail, null,
                                "Confirm your email on Nooch", link,
                                tokens, null, null, null);

                            Logger.Info("MDA - Registration mail sent to [" + userEmail + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Info("MDA - Member activation mail NOT sent to [" + userEmail + "]");
                        }

                        #endregion Send New Account Email To New User


                        #region Temp PIN Email

                        // Email user the auto-generated PIN
                        var tokens2 = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                            };
                        try
                        {
                            Utility.SendEmail("pinSetForNewUser", fromAddress, userEmail, null,
                                "Your temporary Nooch PIN", null, tokens2, null, null, null);

                            Logger.Info("MDA -> RegisterNonNoochUserWithSynapse - Temp PIN email sent to [" + userEmail + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Info("MDA -> RegisterNonNoochUserWithSynapse - Temp PIN email NOT sent to [" + userEmail + "]. Problem sending email.");
                        }

                        #endregion Temp PIN Email
                    }

                    #endregion Check If PW Was Supplied To Create Full Account


                    #region Save User ID Image If Provided

                    if (isIdImageAdded == "1" && !String.IsNullOrEmpty(idImageData))
                    {
                        // We have ID Doc image... saving it on Nooch's Server and making entry in Members table.
                        try
                        {
                            Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 -> About to save ID Doc on Nooch's server - Email: [" + userEmail + "], MemberID: [" + member.MemberId + "]");

                            var saveImageOnServer = SaveBase64AsImage(member.MemberId.ToString(), idImageData);

                            if (saveImageOnServer.success && !String.IsNullOrEmpty(saveImageOnServer.msg))
                            {
                                member.VerificationDocumentPath = saveImageOnServer.msg;
                                _dbContext.SaveChanges();
                                _dbContext.Entry(member).Reload();

                                Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 -> Successfully saved ID Doc in Members.VerificationDocumentPath: [" + member.VerificationDocumentPath + "]");
                            }
                            else
                                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 -> Attempted to Save ID Doc on server but FAILED - SaveBase64AsImage Msg: [" + saveImageOnServer.msg + "]");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 -> Attempted to Save ID Doc on server but failed -> Email: [" + userEmail +
                                         "], MemberID: [" + member.MemberId + "], Exception: [" + ex.Message + "]");
                        }
                    }
                    else
                        Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - NO IMAGE SENT - MemberID [" + member.MemberId + "]");

                    #endregion Save User ID Image If Provided


                    // NOW WE HAVE CREATED A NEW NOOCH USER RECORD AND SENT THE REGISTRATION EMAIL (IF THE USER PROVIDED A PW TO CREATE AN ACCOUNT)
                    // NEXT, ATTEMPT TO CREATE A SYNAPSE ACCOUNT FOR THIS USER

                    #region Notify Cliff About New User

                    try
                    {
                        var lastNameUnEncr = CommonHelper.GetDecryptedData(LastName);
                        var ssnTxt = !String.IsNullOrEmpty(ssn) && ssn.Length > 5 ? "XXX - XX - " + ssn.Substring(ssn.Length - 4) : "<em>Not Submitted</em>";
                        var imgIncludedTxt = isIdImageAdded == "1" ? "TRUE" : "FALSE";

                        StringBuilder st = new StringBuilder("<p><strong>This user's Nooch Account information is:</strong></p>" +
                                              "<table border='1' cellpadding='5' style='border-collapse:collapse;'>" +
                                              "<tr><td><strong>Name:</strong></td><td><strong>" + namearray[0] + " " + lastNameUnEncr + "</strong></td></tr>" +
                                              "<tr><td><strong>MemberID:</strong></td><td>" + member.MemberId + "</td></tr>" +
                                              "<tr><td><strong>Nooch_ID:</strong></td><td>" + member.Nooch_ID + "</td></tr>" +
                                              "<tr><td><strong>Email Address:</strong></td><td>" + userEmail + "</td></tr>" +
                                              "<tr><td><strong>Phone #:</strong></td><td>" + CommonHelper.FormatPhoneNumber(userPhone) + "</td></tr>" +
                                              "<tr><td><strong>Address:</strong></td><td>" + address + ", " + cityFromGoogle + ", " + stateAbbrev + ", " + zip + "</td></tr>" +
                                              "<tr><td><strong>SSN:</strong></td><td>" + ssnTxt + "</td></tr>" +
                                              "<tr><td><strong>isIdImageAdded:</strong></td><td>" + imgIncludedTxt +
                                              "<tr><td><strong>Invited By:</strong></td><td>" + inviteCodeMemberName +
                                              "</td></tr></table><br/><br/>- Nooch Bot</body></html>");

                        // Notify Nooch Admin
                        StringBuilder completeEmailTxt = new StringBuilder();
                        string s = "<html><body><h2>New Nooch User Created</h2><p>The following person just created a Nooch account:</p>" +
                                   st.ToString() +
                                   "<br/><br/><small><strong>This email was generated automatically in: [MDA -> RegisterNonNoochUserWithSynapse]</strong></small></body></html>";

                        completeEmailTxt.Append(s);

                        Utility.SendEmail(null, "admin-autonotify@nooch.com", "newUser@nooch.com", null,
                                          "Nooch Alert - NEW USER: " + namearray[0] + " " + lastNameUnEncr,
                                          null, null, null, null, completeEmailTxt.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 Error - Attempted to notify Cliff about new user - Exception: [" + ex + "]");
                    }

                    #endregion Notify Cliff About New User

                    #region Create User with Synapse

                    synapseCreateUserV3Result_int createSynapseUserResult = new synapseCreateUserV3Result_int();
                    try
                    {
                        // Now call Synapse create user service
                        Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - ABOUT TO CALL UTILITY RegisterUserWithSynapseV3() METHOD...");
                        createSynapseUserResult = RegisterUserWithSynapseV3(member.MemberId.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - createSynapseUser FAILED - Exception: [" + ex.Message + "]");
                    }

                    if (createSynapseUserResult != null)
                    {
                        res.ssn_verify_status = "did not check yet";

                        #region Created Synapse User Successfully

                        if (createSynapseUserResult.success == true && !String.IsNullOrEmpty(createSynapseUserResult.oauth.oauth_key))
                        {
                            Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - Synapse User created SUCCESSFULLY - " +
                                        "oauth_consumer_key: [" + createSynapseUserResult.oauth.oauth_key + "]. Now attempting to save in Nooch DB.");

                            res = createSynapseUserResult;

                            if (!String.IsNullOrEmpty(res.reason) && res.reason.IndexOf("Email already registered") > -1)
                            {
                                res.reason = "User already existed, successfully received consumer_key.";
                                Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 SUCCESS -> Reason: [" + res.reason + "], Email: [" + userEmail + "], user_id: [" + res.user_id + "]");
                            }
                            else
                            {
                                // EXPECTED OUTCOME for most users creating a new Synapse Account.
                                Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - User was Created SUCCESSFULLY - Email: [" + userEmail + "], user_id: [" + createSynapseUserResult.user_id + "]");
                            }

                            createUserV3Result_oauth oath = new createUserV3Result_oauth();
                            oath.oauth_key = createSynapseUserResult.oauth.oauth_key; // Already know it's not NULL, so don't need to re-check
                            oath.expires_in = !String.IsNullOrEmpty(createSynapseUserResult.oauth.expires_in) ? createSynapseUserResult.oauth.expires_in : "";

                            res.success = true;
                            res.user_id = !String.IsNullOrEmpty(createSynapseUserResult.user._id.id) ? createSynapseUserResult.user._id.id : "";
                            res.oauth = oath;
                            res.memberIdGenerated = member.MemberId.ToString();
                            res.error_code = createSynapseUserResult.error_code;
                            res.errorMsg = createSynapseUserResult.errorMsg;
                        }

                        #endregion Created Synapse User Successfully

                        else
                        {
                            // CLIFF (9/26/15): I DON'T THINK THIS BLOCK IS NEEDED... THIS METHOD IS ONLY FOR *BRAND NEW* NOOCH USERS COMING FROM A LANDING PAGE...

                            #region Create Synapse User Response -> Reason: 'Email Already Registered'

                            if (createSynapseUserResult.reason == "Email already registered.")
                            {
                                // Case when synapse returned email already registered... chances are we have user id in SynapseCreateUserResults table
                                // Checking Nooch DB

                                string MemberIdFromtransId = GetNonNoochUserTempRegisterIdFromTransId(transId);

                                if (MemberIdFromtransId.Length > 0)
                                {
                                    Guid memGuid = Utility.ConvertToGuid(MemberIdFromtransId);

                                    var synapseRes = _dbContext.SynapseCreateUserResults.Where(memberTemp =>
                                                        memberTemp.MemberId.Value == memGuid &&
                                                        memberTemp.IsDeleted == false).FirstOrDefault();

                                    if (synapseRes != null)
                                    {
                                        _dbContext.Entry(synapseRes).Reload();

                                        res.success = true;
                                        res.reason = "Account already in Nooch DB for that email";
                                        res.user_id = synapseRes.user_id.ToString();
                                        res.oauth.oauth_key = CommonHelper.GetDecryptedData(synapseRes.access_token);
                                        res.oauth.refresh_token = CommonHelper.GetDecryptedData(synapseRes.refresh_token);
                                        res.memberIdGenerated = memGuid.ToString();

                                        return res;
                                    }
                                    else
                                    {
                                        // (7/27/15) Cliff: We should use 'force_create'='no' in the /user/create call to Synapse. Then Synapse will return the user's Oauth key if found.
                                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 Failed - User already registered with Synapse BUT not found in Nooch DB - " +
                                                     "TransID: [" + transId + "] ");
                                        res.reason = "Account already registered, but not found in Nooch DB.";
                                    }
                                }
                                else
                                {
                                    res.reason = createSynapseUserResult.reason;
                                }
                            }

                            #endregion Create Synapse User Response -> Reason: 'Email Already Registered'

                            else
                                res.reason = createSynapseUserResult.reason;
                        }
                    }
                    else
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - createSynapseUser FAILED & Returned NULL");
                        res.reason = !String.IsNullOrEmpty(createSynapseUserResult.reason) ? createSynapseUserResult.reason : "Reg NonNooch User w/ Syn: Error 5927.";
                    }

                    #endregion Create User with Synapse


                    #region Send Email To Referrer (If Applicable)

                    if (!String.IsNullOrEmpty(inviteCode) && inviteCode.ToLower() != "b43a36a6-1da5-47ce-a56c-6210f9ddbd22")
                    {
                        try
                        {
                            Guid invideCodeGuid = Utility.ConvertToGuid(inviteCode);

                            var inviteCodeObj = _dbContext.InviteCodes.Where(inviteTemp => inviteTemp.InviteCodeId == invideCodeGuid).FirstOrDefault();

                            if (inviteCodeObj == null)
                            {
                                Logger.Info("MDA - RegisterNonNoochUserWithSynapseV3 - Could not find Invite Code - [Invite Code ID: " + inviteCode + "]");
                            }
                            else if (inviteCodeObj.count >= inviteCodeObj.totalAllowed)
                            {
                                Logger.Info("MDA - RegisterNonNoochUserWithSynapseV3 - Invite Code limit of [" + inviteCodeObj.totalAllowed +
                                            "] exceeded for Code: [" + inviteCodeObj.code + "]");
                            }
                            else
                            {
                                if (inviteCode.ToLower() != "nocode")
                                {
                                    try
                                    {
                                        // Sending email to user who invited this user (Based on the invite code provided during registration)
                                        SendEmailToInvitor(inviteCodeObj.InviteCodeId, userNameLowerCase, userName);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - EXCEPTION (Inner) trying to send Email To Referrer - [Exception: " + ex + "]");
                                    }
                                }

                                // Now update invite code count
                                inviteCodeObj.count++;
                                _dbContext.SaveChanges();
                                _dbContext.Entry(inviteCodeObj).Reload();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - EXCEPTION (Outer) trying to send Email To Referrer - [Exception: " + ex + "]");
                        }
                    }

                    #endregion Send Email To Referrer (If Applicable)
                }

                #endregion Member Added to Nooch DB Successfully

                else
                {
                    Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - FAILED to add new Member to DB");
                    res.reason = "Failed to save new Nooch Member in DB.";
                }
            }
            else
            {
                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - FAILED - couldn't generate a Nooch ID.");
                res.reason = "Duplicate Nooch_Id generated, contact Nooch support."; // CLIFF (7/28/15): Well actually it wouldn't be a duplicate since it would have to be null...
            }

            return res;
        }


        public string GetNonNoochUserTempRegisterIdFromTransId(string TransId)
        {
            Logger.Info("MDA -> GetNonNoochUserTempRegisterIdFromTransId Fired - TransID: [" + TransId + "]");

            string nonNoochUserId = "";

            try
            {
                Guid TransIDGUId = Utility.ConvertToGuid(TransId);

                var theTransaction = _dbContext.Transactions.Where(pr => pr.TransactionId.Equals(TransIDGUId)).FirstOrDefault();

                if (theTransaction != null)
                {
                    _dbContext.Entry(theTransaction).Reload();
                    // Transaction found! Now check if user came into Nooch via an Email or SMS invite/request.
                    if (theTransaction.PhoneNumberInvited != null &&
                        theTransaction.IsPhoneInvitation == true)
                    {
                        string memberIdFromPhone = CommonHelper.GetMemberIdByContactNumber(CommonHelper.GetDecryptedData(theTransaction.PhoneNumberInvited));

                        if (!String.IsNullOrEmpty(memberIdFromPhone))
                            nonNoochUserId = memberIdFromPhone;
                    }
                    if ((theTransaction.IsPhoneInvitation == null || theTransaction.IsPhoneInvitation == false) &&
                         theTransaction.InvitationSentTo != null)
                    {
                        string memberIdFromEmail = CommonHelper.GetMemberIdByUserName(CommonHelper.GetDecryptedData(theTransaction.InvitationSentTo));

                        if (!String.IsNullOrEmpty(memberIdFromEmail))
                            nonNoochUserId = memberIdFromEmail;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetNonNoochUserTempRegisterIdFromTransId FAILED - TransID: [" + TransId + "], Exception: [" + ex.Message + "]");
            }

            return nonNoochUserId;
        }


        public genericResponse SaveBase64AsImage(string fileNameToBeUsed, string base64String)
        {
            genericResponse res = new genericResponse();
            res.success = false;

            try
            {
                string filnameMade = "";
                byte[] bytes = Convert.FromBase64String(base64String);
                string folderPath = Utility.GetValueFromConfig("SynapsePhotoPath");
                string PhotoUrl = Utility.GetValueFromConfig("SynapseIdPhotoUrl");

                Image image = byteArrayToImage(bytes);
                string fullPathOfSavedImage = "";

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image = Image.FromStream(ms);
                    Bitmap bm = new Bitmap(image);
                    fileNameToBeUsed = fileNameToBeUsed + ".png";
                    filnameMade = fileNameToBeUsed;
                    fullPathOfSavedImage = Path.Combine(folderPath, fileNameToBeUsed);
                    fullPathOfSavedImage = HostingEnvironment.MapPath(fullPathOfSavedImage);

                    // checking if file with same name already exists
                    while (File.Exists(Path.Combine(folderPath, fileNameToBeUsed)))
                    {
                        string[] name = fileNameToBeUsed.Split('.');
                        filnameMade = name[0] + "1" + ".png";
                        fullPathOfSavedImage = Path.Combine(folderPath, filnameMade);
                        fullPathOfSavedImage = HostingEnvironment.MapPath(fullPathOfSavedImage);
                    }

                    bm.Save(fullPathOfSavedImage, System.Drawing.Imaging.ImageFormat.Png);
                }

                fullPathOfSavedImage = PhotoUrl + filnameMade;

                res.msg = fullPathOfSavedImage;
                res.success = true;
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> SaveBase64AsImage FAILED - FileNameToBeUsed: [" + fileNameToBeUsed + "], Exception: [" + ex.Message + "]");
                res.msg = "Exception: " + ex.Message;
            }

            return res;
        }


        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }


        /// <summary>
        /// CC (8/9/16): THIS METHOD SHOULD NOT BE USED ANYMORE - IT IS DEPRECATED W/ SYNAPSE.  USE CommonHelper.sendDocsToSynapseV3() INSTEAD.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="ImageUrl"></param>
        /// <returns></returns>
        public GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
        {
            Logger.Info("MDA -> submitDocumentToSynapseV3 Initialized - [MemberId: " + MemberId +
                        "], ImageURL: [" + ImageUrl + "]");

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();
            res.success = false;

            try
            {
                Guid id = Utility.ConvertToGuid(MemberId);

                #region Get User's Synapse OAuth Consumer Key

                string usersSynapseOauthKey = "";
                var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (usersSynapseDetails == null)
                {
                    Logger.Error("MDA -> submitDocumentToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
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

                var member = CommonHelper.GetMemberDetails(id.ToString()); ;

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

                try
                {
                    submitDocToSynapseV3Class submitDocObj = new submitDocToSynapseV3Class();

                    SynapseV3Input_login login = new SynapseV3Input_login();
                    login.oauth_key = usersSynapseOauthKey;
                    submitDocObj.login = login;

                    submitDocToSynapse_user user = new submitDocToSynapse_user();
                    submitDocToSynapse_user_doc doc = new submitDocToSynapse_user_doc();
                    doc.attachment = "data:image/png;base64," + CommonHelper.ConvertImageURLToBase64(ImageUrl).Replace("\\", "");

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

                    kycInfoResponseFromSynapse resFromSynapse = JsonConvert.DeserializeObject<kycInfoResponseFromSynapse>(content);

                    if (resFromSynapse != null)
                    {
                        if (resFromSynapse.success == true || resFromSynapse.success.ToString().ToLower() == "true")
                        {
                            _dbContext.Entry(usersSynapseDetails).Reload();

                            var permission = resFromSynapse.user.permission != null ? resFromSynapse.user.permission : "NOT FOUND";

                            if (resFromSynapse.user.doc_status != null)
                            {
                                usersSynapseDetails.physical_doc = resFromSynapse.user.doc_status.physical_doc;
                                usersSynapseDetails.virtual_doc = resFromSynapse.user.doc_status.virtual_doc;
                            }

                            // Update Permission in SynapseCreateUserResults Table using value returned by Synapse
                            // If any problem and 'permission' is 'NOT FOUND', leave the DB value as is.
                            usersSynapseDetails.permission = permission != "NOT FOUND" ? permission : usersSynapseDetails.permission;
                            usersSynapseDetails.photos = ImageUrl;

                            int save = 0;

                            try
                            {
                                _dbContext.SaveChanges();
                                save++;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> submitDocumentToSynapseV3 - FAILED to save new Permission and ImageURL in SynapseCreateUserResult Table" +
                                               "New Permission: [" + permission + "], ID Doc URL: [" + ImageUrl + "], Exception: [" + ex + "]");
                                res.message = "Error saving ID doc";
                                save = 0;
                            }

                            // Cliff (5/31/16): THIS IS FAILING TO SAVE FOR SOME REASON ACCORDING TO THE LOGS... CAN'T FIGURE OUT WHY :-(
                            // Malkit (10 June 2016) : This should work now.
                            //     ----^---- GHOST OF FUTURE MALKIT!!  ;-)
                            if (save > 0)
                            {
                                Logger.Info("MDA -> submitDocumentToSynapseV3 - SUCCESSFULLY updated user's Synapse Permission in SynapseCreateUserResult Table" +
                                            "New Permission: [" + permission + "], ID Doc URL: [" + ImageUrl + "]");
                                res.message = "ID doc saved and submitted successfully.";
                                res.success = true;
                            }
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
                    // TO DO: ADD ERROR HANDLING FOR SYNAPSE RESPONSE...

                    res.message = "MDA Exception #3999";
                    Logger.Error("MDA -> submitDocumentToSynapseV3 FAILED - Catch [Exception: " + ex + "]");
                }

                #endregion Call Synapse /user/doc/attachments/add API
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> submitDocumentToSynapseV3 FAILED - Outer Exception - " +
                             "MemberID: [" + MemberId + "], Exception: [" + ex.Message + "]");
            }

            return res;
        }


        public string CreateNonNoochUserAccountAfterRejectMoney(string TransId, string password, string EmailId, string UserName)
        {
            Logger.Info("MDA - CreateNonNoochUserAccountAfterRejectMoney Initiated - [TransId: " + TransId + "], " +
                        "[Email: " + EmailId + "], [Name: " + UserName + "]");

            try
            {
                Guid tid = Utility.ConvertToGuid(TransId);
                password = CommonHelper.GetEncryptedData(password);

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);

                var transactionDetail = new Transaction();
                transactionDetail = _dbContext.Transactions.FirstOrDefault(transTemp =>
                                     transTemp.TransactionId == tid &&
                                    (transTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                                     transTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="));

                if (transactionDetail != null)
                {
                    _dbContext.Entry(transactionDetail).Reload();
                    Logger.Info("MDA - CreateNonNoochUserAccountAfterRejectMoney - Transaction Found - [TransType: " +
                                           CommonHelper.GetDecryptedData(transactionDetail.TransactionType) + "], " + "], [Name: " + UserName + "]");

                    var userNameLowerCase = CommonHelper.GetDecryptedData(EmailId).ToLower();

                    var memdetails = new Member();
                    memdetails = _dbContext.Members.FirstOrDefault(memberTemp =>
                                        (memberTemp.UserName == userNameLowerCase ||
                                         memberTemp.UserNameLowerCase == userNameLowerCase) &&
                                         memberTemp.IsDeleted == false);

                    if (memdetails == null)
                    {
                        #region Username Not Already Used

                        #region Save New Member Record In DB

                        string[] nameAftetSplit = UserName.Split(' ');
                        string firstName = "";
                        string lastName = "";
                        if (nameAftetSplit.Length > 1)
                        {
                            firstName = nameAftetSplit[0];

                            for (int i = 1; i < nameAftetSplit.Length - 1; i++)
                            {
                                lastName += nameAftetSplit[i] + " ";
                            }
                        }
                        else
                        {
                            firstName = nameAftetSplit[0];
                            lastName = " ";
                        }
                        if (lastName != " ")
                        {
                            lastName = lastName.Trim();
                        }
                        if (firstName != " ")
                        {
                            firstName = firstName.Trim();
                        }
                        string noochRandomId = GetRandomNoochId();
                        string newUserPhoneNumber = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        newUserPhoneNumber = newUserPhoneNumber.Replace("(", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace(")", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace(" ", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace("-", "");

                        string inviteCode = transactionDetail.Member.InviteCodeId.ToString();

                        string emailEnc = CommonHelper.GetEncryptedData(userNameLowerCase);

                        var member = new Member
                        {
                            Nooch_ID = noochRandomId,
                            MemberId = Guid.NewGuid(),
                            UserName = emailEnc,
                            FirstName = CommonHelper.GetEncryptedData(firstName),
                            LastName = CommonHelper.GetEncryptedData(lastName),
                            SecondaryEmail = emailEnc,
                            Password = password.Replace(" ", "+"),
                            PinNumber = pinNumber.Replace(" ", "+"),
                            Status = Constants.STATUS_REGISTERED,
                            IsDeleted = false,
                            DateCreated = DateTime.Now,
                            DateModified = DateTime.Now,
                            UserNameLowerCase = emailEnc,
                            //FacebookAccountLogin = facebookAccountLogin,
                            //InviteCodeIdUsed = inviteCodeObj.InviteCodeId,
                            //InviteCodeId=inviteCodeObj.InviteCodeId
                            Type = "Personal",
                            ContactNumber = newUserPhoneNumber,
                            // contact number verified because already coming from SMS url delivered in phone
                            IsVerifiedPhone = true,
                            IsVerifiedWithSynapse = false,
                            Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png"
                        };

                        if (inviteCode.Length > 0)
                        {
                            member.InviteCodeIdUsed = Utility.ConvertToGuid(inviteCode);
                        }

                        int result = 0;

                        try
                        {
                            _dbContext.Members.Add(member);
                            result = _dbContext.SaveChanges();
                            _dbContext.Entry(member).Reload();
                        }
                        catch (Exception ex)
                        {
                            return "Exception " + ex.ToString();
                        }

                        #endregion Save New Member Record In DB


                        if (result > 0)
                        {
                            #region Create Auto Token

                            var tokenId = Guid.NewGuid();
                            var requestId = Guid.Empty;

                            var token = new AuthenticationToken
                            {
                                TokenId = tokenId,
                                MemberId = member.MemberId,
                                IsActivated = false,
                                DateGenerated = DateTime.Now,
                                FriendRequestId = requestId
                            };
                            //  var tokensRepository = new Repository<AuthenticationTokens, NoochDataEntities>(noochConnection);

                            _dbContext.AuthenticationTokens.Add(token);
                            bool status = Convert.ToBoolean(_dbContext.SaveChanges());

                            #endregion Create Auth Token

                            #region Create Notification Settings
                            // for member notification settings

                            //var memberNotificationsRepository = new Repository<MemberNotifications, NoochDataEntities>(noochConnection);
                            var memberNotification = new MemberNotification
                            {
                                NotificationId = Guid.NewGuid(),
                                MemberId = member.MemberId,
                                FriendRequest = true,
                                InviteRequestAccept = true,
                                TransferSent = true,
                                TransferReceived = true,
                                TransferAttemptFailure = true,
                                NoochToBank = true,
                                BankToNooch = true,
                                EmailFriendRequest = true,
                                EmailInviteRequestAccept = true,
                                EmailTransferSent = true,
                                EmailTransferReceived = true,
                                EmailTransferAttemptFailure = true,
                                TransferUnclaimed = true,
                                BankToNoochRequested = true,
                                BankToNoochCompleted = true,
                                NoochToBankRequested = true,
                                NoochToBankCompleted = true,
                                InviteReminder = true,
                                LowBalance = true,
                                ValidationRemainder = true,
                                ProductUpdates = true,
                                NewAndUpdate = true,
                                DateCreated = DateTime.Now
                            };

                            _dbContext.MemberNotifications.Add(memberNotification);
                            _dbContext.SaveChanges();
                            _dbContext.Entry(memberNotification).Reload();


                            #endregion Create Notification Settings

                            #region Create Privacy Settings

                            var memberPrivacySettings = new MemberPrivacySetting
                            {
                                MemberId = member.MemberId,
                                AllowSharing = true,
                                ShowInSearch = true,
                                DateCreated = DateTime.Now
                            };
                            _dbContext.MemberPrivacySettings.Add(memberPrivacySettings);

                            #endregion Create Privacy Settings


                            if (status)
                            {
                                #region Send Registration Email

                                // Send registration email to member with autogenerated token

                                var fromAddress = Utility.GetValueFromConfig("welcomeMail");

                                try
                                {
                                    var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                    "Nooch/Activation?tokenId=" + tokenId);
                                    var tokens = new Dictionary<string, string>
                                    {
                                        {
                                            Constants.PLACEHOLDER_FIRST_NAME,
                                            CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName))
                                        },
                                        {
                                            Constants.PLACEHOLDER_LAST_NAME,
                                            CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName))
                                        },
                                        {Constants.PLACEHOLDER_OTHER_LINK, link}
                                    };

                                    Utility.SendEmail(Constants.TEMPLATE_REGISTRATION, fromAddress, userNameLowerCase, null,
                                        "Confirm your email on Nooch", link,
                                        tokens, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserAccountAfterRejectMoney EXCEPTION - Member activation email NOT " +
                                                           "sent to [" + userNameLowerCase + "], [Exception: " + ex + "]");
                                }

                                #endregion Send Registration Email


                                #region Send Temp PIN Email
                                try
                                {
                                    // emailing temp pin number
                                    var tokens2 = new Dictionary<string, string>
                                    {
                                        {Constants.PLACEHOLDER_FIRST_NAME, userNameLowerCase},
                                        {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                                    };

                                    Utility.SendEmail("pinSetForNewUser", fromAddress, userNameLowerCase, null,
                                        "Your temporary Nooch Pin Number", null,
                                        tokens2, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA - CreateNonNoochUserAccountAfterRejectMoney - Member Temp PIN mail NOT sent to [" +
                                                           userNameLowerCase + "], [Exception: " + ex + "]");
                                }
                                #endregion Send Temp PIN Email


                                #region Send Email To Referrer (If Applicable)

                                if (!String.IsNullOrEmpty(inviteCode))
                                {
                                    try
                                    {
                                        Guid invideCodeGuid = Utility.ConvertToGuid(inviteCode);

                                        var inviteCodeObj = new InviteCode();

                                        inviteCodeObj = _dbContext.InviteCodes.FirstOrDefault(inviteTemp => inviteTemp.InviteCodeId == invideCodeGuid);

                                        if (inviteCodeObj == null)
                                        {
                                            Logger.Error("MDA -> CreateNonNoochUserAccountAfterRejectMoney - Attempted but invite code not found: [" +
                                                         inviteCode + "]");
                                        }
                                        else if (inviteCodeObj.count >= inviteCodeObj.totalAllowed)
                                        {
                                            Logger.Error("MDA -> CreateNonNoochUserAccountAfterRejectMoney - Attempted to notify referrer but Allowable limit of [" +
                                                         inviteCodeObj.totalAllowed + "] already reached for Code: [" + inviteCode + "]");
                                        }
                                        else
                                        {
                                            if (inviteCode.ToLower() != "nocode")
                                            {
                                                try
                                                {
                                                    // Sending email to user who invited this user (Based on the invite code provided during registration)
                                                    string fullName = CommonHelper.UppercaseFirst(firstName) + " " + CommonHelper.UppercaseFirst(lastName);

                                                    SendEmailToInvitor(inviteCodeObj.InviteCodeId, userNameLowerCase, fullName);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA -> CreateNonNoochUserAccountAfterRejectMoney - EXCEPTION (Inner) trying to send Email To Referrer - [Exception: " + ex + "]");
                                                }
                                            }

                                            // updating invite code count
                                            inviteCodeObj.count++;

                                            _dbContext.SaveChanges();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> CreateNonNoochUserAccountAfterRejectMoney - EXCEPTION (Outer) trying to send Email To Referrer - [Exception: " + ex + "]");
                                    }
                                }

                                #endregion Send Email To Referrer (If Applicable)

                                return "Thanks for registering! Check your email to complete activation.";
                            }
                            else
                            {
                                return "Password creation failed.";
                            }
                        }
                        else
                        {
                            return "Error Saving Member record.";
                        }

                        #endregion Username Not Already Used
                    }
                    else
                    {
                        return "User already exists";
                    }
                }
                else
                {
                    return "No TransId found";
                }
            }
            catch (Exception ex)
            {
                return "Failure " + ex.ToString();
            }

        }


        /// <summary>
        /// For verifying a Synapse bank account attached w/ Routing & Account #'s.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="microDepositOne"></param>
        /// <param name="microDepositTwo"></param>
        /// <param name="BankId"></param>
        /// <returns></returns>
        public SynapseBankLoginV3_Response_Int SynapseV3MFABankVerifyWithMicroDeposits(string MemberId, string microDepositOne, string microDepositTwo, string BankId)
        {
            Logger.Info("MDA -> SynapseV3MFABankVerifyWithMicroDeposits Initiated - [MemberId: " + MemberId +
                        "], [BankId (enc): " + BankId + "], Micro1: [" + microDepositOne + "], Micro2: [" + microDepositTwo + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();
            res.Is_success = false;
            res.Is_MFA = false;

            #region Check If All Data Passed

            if (String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(microDepositOne) ||
                String.IsNullOrEmpty(microDepositTwo) ||
                String.IsNullOrEmpty(BankId))
            {
                if (String.IsNullOrEmpty(MemberId))
                {
                    res.errorMsg = "Invalid data - need MemberId";
                }
                else if (String.IsNullOrEmpty(microDepositOne))
                {
                    res.errorMsg = "Invalid data - need micro deposit one.";
                }
                else if (String.IsNullOrEmpty(microDepositTwo))
                {
                    res.errorMsg = "Invalid data - need micro deposit two.";
                }
                else if (String.IsNullOrEmpty(BankId))
                {
                    res.errorMsg = "Invalid data - need BankAccessToken";
                }
                else
                {
                    res.errorMsg = "Invalid data sent";
                }

                Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: [" + res.errorMsg + "], MemberID: [" + MemberId + "]");
                return res;
            }

            #endregion Check If All Data Passed


            // Now get the Member's Nooch account details
            Guid id = Utility.ConvertToGuid(MemberId);

            var noochMember = CommonHelper.GetMemberDetails(id.ToString());

            if (noochMember != null)
            {
                // Check User's SynapseCreateUserResult record from DB stored after synapse create user service call
                var noochMemberResultFromSynapse = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());

                if (noochMemberResultFromSynapse != null)
                {
                    var bankIdEnc = CommonHelper.GetEncryptedData(BankId);

                    var bankAccountDetails = _dbContext.SynapseBanksOfMembers.FirstOrDefault(b => b.oid == BankId &&
                                                                                                  b.MemberId == id &&
                                                                                                  b.IsAddedUsingRoutingNumber == true);
                    if (bankAccountDetails != null)
                    {
                        #region GOT USERS SYNAPSE AUTH TOKEN

                        try
                        {
                            Logger.Info("MDA -> SynapseV3MFABankVerifyWithMicroDeposits - CHECKPOUINT #1");
                            var bankOid = CommonHelper.GetDecryptedData(BankId);

                            SynapseV3Input_login log = new SynapseV3Input_login()
                            {
                                oauth_key = CommonHelper.GetDecryptedData(noochMemberResultFromSynapse.access_token)
                            };

                            SynapseV3Input_user fing = new SynapseV3Input_user()
                            {
                                fingerprint = noochMember.UDID1
                            };

                            SynapseBankVerifyV3WithMicroDesposits_Input_node node = new SynapseBankVerifyV3WithMicroDesposits_Input_node();

                            SynapseNodeId node_id = new SynapseNodeId() { oid = bankOid };
                            node._id = node_id;

                            SynapseBankVerifyV3WithMicroDesposits_Input_node_verify veri = new SynapseBankVerifyV3WithMicroDesposits_Input_node_verify();
                            veri.micro = new string[2];
                            veri.micro[0] = microDepositOne;
                            veri.micro[1] = microDepositTwo;

                            node.verify = veri;

                            SynapseBankVerifyWithMicroDepositsV3_Input bankLoginPars = new SynapseBankVerifyWithMicroDepositsV3_Input();
                            bankLoginPars.login = log;
                            bankLoginPars.user = fing;
                            bankLoginPars.node = node;

                            string UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/verify" : "https://synapsepay.com/api/v3/node/verify";

                            // Calling Synapse /node/verify API (Verify a Bank Account)

                            var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                            http.Accept = "application/json";
                            http.ContentType = "application/json";
                            http.Method = "POST";

                            string parsedContent = JsonConvert.SerializeObject(bankLoginPars);
                            ASCIIEncoding encoding = new ASCIIEncoding();
                            Byte[] bytes = encoding.GetBytes(parsedContent);

                            Stream newStream = http.GetRequestStream();
                            newStream.Write(bytes, 0, bytes.Length);
                            newStream.Close();

                            var response = http.GetResponse();
                            var stream = response.GetResponseStream();
                            var sr = new StreamReader(stream);
                            var content = sr.ReadToEnd();

                            JObject bankLoginRespFromSynapse = JObject.Parse(content);

                            if (bankLoginRespFromSynapse["success"].ToString().ToLower() == "true" &&
                                bankLoginRespFromSynapse["nodes"] != null)
                            {
                                Logger.Info("MDA -> SynapseV3MFABankVerifyWithMicroDeposits SUCCESSFUL for: [" + MemberId + "]");

                                // Mark Any Existing Synapse Bank Login Entries as Deleted
                                var removeExistingSynapseBankLoginResult = CommonHelper.RemoveSynapseBankLoginResults(id.ToString());

                                #region Update Bank Record in DB

                                RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                                if (allNodesParsedResult != null)
                                {
                                    bankAccountDetails.IsDefault = true; // Should already be true since this is a Routing/Account # bank, so only one
                                    bankAccountDetails.allowed = allNodesParsedResult.nodes[0].allowed.ToString();
                                    bankAccountDetails.mfa_verifed = true;
                                    bankAccountDetails.Status = "Verified";
                                    bankAccountDetails.VerifiedOn = DateTime.Now;

                                    try
                                    {
                                        _dbContext.SaveChanges();

                                        res.Is_success = true;
                                        res.Is_MFA = false;
                                        res.errorMsg = "Bank account verified successfully with micro deposits";
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: Failed to update this bank in DB - " +
                                                     "Member [" + MemberId + "], BankID: [" + BankId + "]");
                                        res.errorMsg = "Failed to update this bank in DB - [" + ex.Message + "]";
                                    }
                                }
                                else
                                {
                                    Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: Unable to parse Bank Nodes Array in Synapse Response.");
                                    res.errorMsg = "Unable to parse Bank Nodes Array in Synapse Response";
                                }

                                #endregion Update Bank Record in DB
                            }
                            else
                            {
                                // Synapse response for 'success' was not true
                                Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: Synapse response for 'success' was not true for Member [" + MemberId + "]");
                                res.errorMsg = "Synapse response for success was not true";
                            }

                            return res;
                        }
                        catch (WebException we)
                        {
                            var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                            Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits FAILED. Exception was: [" + we.Message + "]");

                            res.Is_success = false;

                            var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                            JObject jsonfromsynapse = JObject.Parse(resp);

                            Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits - Response was: [" + jsonfromsynapse + "]");

                            JToken token = jsonfromsynapse["error"]["en"];

                            if (token != null)
                            {
                                res.errorMsg = jsonfromsynapse["error"]["en"].ToString();
                            }
                        }

                        #endregion GOT USERS SYNAPSE AUTH TOKEN
                    }
                    else
                    {
                        Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: Member's Bank not found MemberId: [" + MemberId + "]" + " And BankId - Id of synpasebanks of members table: [" + BankId + "]");
                        res.errorMsg = "Bank not found.";
                    }
                }
                else
                {
                    Logger.Error("MDA - SynapseV3MFABankVerifyWithMicroDeposits FAILED -> Unable to find SynapseCreateUserDetails record for Member: [" + MemberId + "]");
                    res.errorMsg = "Could not find Synapse user for given user.";
                }
            }
            else
            {
                Logger.Error("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: Member not found: [" + MemberId + "]");
                res.errorMsg = "Member not found";
            }

            return res;
        }


        public SynapseBankLoginV3_Response_Int SynapseV3MFABankVerify(string MemberId, string BankName, string MfaResponse, string BankId)
        {
            Logger.Info("MDA -> SynapseV3MFABankVerify Fired - MemberID: [" + MemberId + "], BankName: [" + BankName + "], MFA Answer: [" + MfaResponse + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();
            res.Is_success = false;

            #region Check If All Data Passed

            if (String.IsNullOrEmpty(BankName) ||
                String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(MfaResponse) ||
                String.IsNullOrEmpty(BankId))
            {
                if (String.IsNullOrEmpty(BankName))
                    res.errorMsg = "Invalid data - need Bank Name";
                else if (String.IsNullOrEmpty(MemberId))
                    res.errorMsg = "Invalid data - need MemberId";
                else if (String.IsNullOrEmpty(MfaResponse))
                    res.errorMsg = "Invalid data - need MFA answer";
                else if (String.IsNullOrEmpty(BankId))
                    res.errorMsg = "Invalid data - need BankAccessToken";
                else
                    res.errorMsg = "Invalid data sent";

                Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: [" + res.errorMsg + "] for: [" + MemberId + "]");
                res.Is_success = false;
                return res;
            }

            #endregion Check If All Data Passed

            else
            {
                // Now get the Member's Nooch account details
                Guid memGuid = Utility.ConvertToGuid(MemberId);

                var noochMember = CommonHelper.GetMemberDetails(memGuid.ToString());

                if (noochMember != null)
                {
                    // Checking member auth token in DB stored after synapse create user service call

                    var noochMemberResultFromSynapseAuth = CommonHelper.GetSynapseCreateaUserDetails(memGuid.ToString());
                    if (noochMemberResultFromSynapseAuth == null)
                    {
                        Logger.Info("MDA - SynapseV3MFABankVerify -> could not locate Synapse Auth Token for MemberID: [" + MemberId + "]");
                        res.errorMsg = "No Synapse Authentication code found for given user.";
                        return res;
                    }

                    #region GOT USERS SYNAPSE AUTH TOKEN

                    else
                    {
                        SynapseV3Input_login log = new SynapseV3Input_login()
                        {
                            oauth_key = CommonHelper.GetDecryptedData(noochMemberResultFromSynapseAuth.access_token)
                        };

                        SynapseV3Input_user fngrprnt = new SynapseV3Input_user()
                        {
                            fingerprint = noochMember.UDID1
                        };

                        SynapseBankVerifyV3_Input_node node = new SynapseBankVerifyV3_Input_node();

                        SynapseNodeId node_id = new SynapseNodeId() { oid = BankId };

                        SynapseBankVerifyV3_Input_node_verify answer = new SynapseBankVerifyV3_Input_node_verify();
                        answer.mfa = MfaResponse;

                        node._id = node_id;
                        node.verify = answer;

                        SynapseBankVerifyV3_Input bankLoginPars = new SynapseBankVerifyV3_Input();
                        bankLoginPars.user = fngrprnt;
                        bankLoginPars.login = log;
                        bankLoginPars.node = node;

                        string UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/verify"
                                                                                                              : "https://synapsepay.com/api/v3/node/verify";

                        Logger.Info("MDA -> SynapseV3MFABankVerify - /node/verify PAYLOAD IS: Oauth_Key: [" + bankLoginPars.login.oauth_key +
                                    "], Fngrprnt: [" + bankLoginPars.user.fingerprint + "], Node OID: [" + bankLoginPars.node._id.oid +
                                    "], Bank_PW: [" + bankLoginPars.node.verify.mfa + "], URL: [" + UrlToHit + "]");

                        // Calling Synapse V3.0 Bank Login service (Link a Bank Account)
                        try
                        {
                            var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                            http.Accept = "application/json";
                            http.ContentType = "application/json";
                            http.Method = "POST";

                            string parsedContent = JsonConvert.SerializeObject(bankLoginPars);
                            ASCIIEncoding encoding = new ASCIIEncoding();
                            Byte[] bytes = encoding.GetBytes(parsedContent);

                            Stream newStream = http.GetRequestStream();
                            newStream.Write(bytes, 0, bytes.Length);
                            newStream.Close();

                            var response = http.GetResponse();
                            var stream = response.GetResponseStream();
                            var sr = new StreamReader(stream);
                            var content = sr.ReadToEnd();

                            JObject bankLoginRespFromSynapse = JObject.Parse(content);

                            //Logger.Info("MDA -> SynapseV3MFABankVerify SYNAPSE RESPONSE - [..." + bankLoginRespFromSynapse + "...]");

                            if (bankLoginRespFromSynapse["success"].ToString().ToLower() == "true" &&
                                bankLoginRespFromSynapse["nodes"] != null)
                            {
                                Logger.Info("MDA -> SynapseV3MFABankVerify - SUCCESS from Synapse - [" + bankLoginRespFromSynapse["nodes"].Count() + "] Nodes Returned");

                                res.Is_success = true;

                                #region Check If Another MFA Was Returned

                                if (bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"] != null ||
                                    bankLoginRespFromSynapse["nodes"][0]["allowed"] == null)
                                {
                                    // Now we know MFA is required

                                    // Preparing to save results in SynapseBankLoginResults DB table
                                    SynapseBankLoginResult sbr = new SynapseBankLoginResult();
                                    sbr.MemberId = memGuid;
                                    sbr.IsSuccess = true;
                                    sbr.dateCreated = DateTime.Now;
                                    sbr.IsDeleted = false;
                                    sbr.IsMfa = true;
                                    sbr.IsCodeBasedAuth = false; // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                                    sbr.IsQuestionBasedAuth = true;
                                    sbr.mfaQuestion = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();
                                    sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                                    sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString());
                                    sbr.AuthType = "questions";

                                    // Add object to Nooch DB
                                    _dbContext.SynapseBankLoginResults.Add(sbr);
                                    _dbContext.SaveChanges();
                                    _dbContext.Entry(sbr).Reload();


                                    res.Is_MFA = true;
                                    res.errorMsg = "OK";
                                    _id idd = new _id();
                                    idd.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString();
                                    //res.SynapseNodesList.nodes[0]._id.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString(); // Not sure this syntax is right   -- -- Malkit - me neither..will check this when I get any such response from web sevice.

                                    RootBankObject rObj = new RootBankObject();

                                    nodes nod = new nodes();
                                    nod._id = idd;

                                    nodes[] arr = new nodes[1];
                                    arr[0] = new nodes() { _id = idd };

                                    rObj.nodes = arr;

                                    res.SynapseNodesList = rObj;
                                    res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();

                                    Logger.Info("MDA -> SynapseV3MFABankVerify - Got another MFA Question - Question: [" + bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"] + "]");

                                    return res;
                                }

                                #endregion Check If Another MFA Was Returned

                                #region No MFA response returned

                                else
                                {
                                    res.Is_MFA = false;

                                    // Array[] of banks ("nodes") expected here
                                    RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                                    if (allNodesParsedResult != null)
                                    {
                                        res.errorMsg = "OK";
                                        res.SynapseNodesList = allNodesParsedResult;

                                        Logger.Info("MDA -> SynapseV3MFABankVerify (No MFA Again): SUCCESSFUL, returning Bank Array for: [" + MemberId + "]");

                                        short nodeCount = 1;

                                        // saving these banks ('nodes') in DB, later one of these banks will be set as default bank
                                        foreach (nodes v in allNodesParsedResult.nodes)
                                        {
                                            try
                                            {
                                                Logger.Info("MDA -> SynapseV3MFABankVerify - Parsing Bank Array From Synapse - Node # [" + nodeCount +
                                                            "], Bank: [" + v.info.bank_name +
                                                            "], Name on Account: [" + v.info.name_on_account +
                                                            "], Allowed: [" + v.allowed +
                                                            "], Type: [" + v.info.type +
                                                            "], OID: [" + v._id.oid + "]");
                                                nodeCount += 1;

                                                SynapseBanksOfMember bnkAccnt = new SynapseBanksOfMember();

                                                bnkAccnt.MemberId = Utility.ConvertToGuid(MemberId);
                                                bnkAccnt.AddedOn = DateTime.Now;
                                                bnkAccnt.bankAdddate = DateTime.Now.ToShortDateString();
                                                bnkAccnt.IsDefault = false;

                                                // Holdovers from V2
                                                bnkAccnt.account_number_string = !String.IsNullOrEmpty(v.info.account_num) ? CommonHelper.GetEncryptedData(v.info.account_num) : null;
                                                bnkAccnt.routing_number_string = !String.IsNullOrEmpty(v.info.routing_num) ? CommonHelper.GetEncryptedData(v.info.routing_num) : null;
                                                bnkAccnt.bank_name = !String.IsNullOrEmpty(v.info.bank_name) ? CommonHelper.GetEncryptedData(v.info.bank_name) : null;
                                                bnkAccnt.name_on_account = !String.IsNullOrEmpty(v.info.name_on_account) ? CommonHelper.GetEncryptedData(v.info.name_on_account) : null;
                                                bnkAccnt.nickname = !String.IsNullOrEmpty(v.info.nickname) ? CommonHelper.GetEncryptedData(v.info.nickname) : null;
                                                bnkAccnt.is_active = v.is_active;
                                                bnkAccnt.mfa_verifed = true;
                                                bnkAccnt.Status = "Verified"; // Consider verified immediately since this is after successfully answering MFA

                                                // New in V3
                                                bnkAccnt.oid = CommonHelper.GetEncryptedData(v._id.oid.ToString());
                                                bnkAccnt.allowed = !String.IsNullOrEmpty(v.allowed) ? v.allowed : "UNKNOWN";
                                                bnkAccnt.@class = !String.IsNullOrEmpty(v.info._class) ? v.info._class : "UNKNOWN";
                                                bnkAccnt.supp_id = v.extra.supp_id;
                                                bnkAccnt.type_bank = !String.IsNullOrEmpty(v.info.type) ? v.info.type : "UNKNOWN";
                                                bnkAccnt.type_synapse = "ACH-US";
                                                bnkAccnt.IsAddedUsingRoutingNumber = false;

                                                _dbContext.SynapseBanksOfMembers.Add(bnkAccnt);
                                                _dbContext.SaveChanges();
                                                _dbContext.Entry(bnkAccnt).Reload();
                                            }
                                            catch (Exception ex)
                                            {
                                                var error = "MDA -> SynapseV3MFABankVerify (No MFA Again) FAILED - Unable to update DB for: [" + MemberId +
                                                             "], Exception: [" + ex + "]";
                                                Logger.Error(error);
                                                CommonHelper.notifyCliffAboutError(error);
                                                res.errorMsg = "Error while saving this bank in DB - [" + ex.Message + "]";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var error = "MDA -> SynapseV3MFABankVerify (No MFA Again) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]";
                                        Logger.Error(error);
                                        CommonHelper.notifyCliffAboutError(error);
                                        res.errorMsg = "Error occured while parsing banks list.";
                                    }
                                }

                                #endregion No MFA response returned
                            }
                            else
                            {
                                // Synapse response for 'success' was not true
                                Logger.Error("MDA -> SynapseV3MFABankVerify ERROR: Synapse response for 'success' was not true for Member [" + MemberId + "]");
                                res.errorMsg = "Synapse response for success was not true";
                            }
                        }
                        catch (WebException we)
                        {
                            #region MFA Bank Verify Error From Synapse

                            res.Is_success = false;
                            res.Is_MFA = false;

                            var errorCode = ((HttpWebResponse)we.Response).StatusCode;
                            var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                            JObject jsonFromSynapse = JObject.Parse(resp);

                            Logger.Error("MDA -> SynapseV3MFABankVerify FAILED - HTTP ErrorCode: [" + errorCode.ToString() + "], WebException was: [" + we.Message + "]");
                            Logger.Error(jsonFromSynapse.ToString());

                            CommonHelper.notifyCliffAboutError("MDA -> SynapseV3MFABankVerify FAILED - JSON from Synapse: [" + jsonFromSynapse.ToString() + "]");

                            var error_code = jsonFromSynapse["error_code"].ToString();
                            res.errorMsg = jsonFromSynapse["error"]["en"].ToString();

                            // Synapse Error could be:
                            // "Incorrect verification information supplied"
                            if (!String.IsNullOrEmpty(res.errorMsg))
                            {
                                Logger.Error("MDA -> SynapseV3MFABankVerify FAILED. Synapse Error Msg was: [" + res.errorMsg + "]");

                                if (res.errorMsg.ToLower().IndexOf("incorrect verification info") > -1)
                                {
                                    // Answer was incorrect, so send back the same MFA Question info to let the user try again.
                                    res.Is_MFA = true;
                                    res.mfaMessage = "-same-";
                                    res.bankMFA = BankId;
                                    res.errorMsg = "-incorrect-";
                                }
                            }
                            else
                            {
                                res.errorMsg = "Error in Synapse response - #4827";
                            }

                            #endregion MFA Bank Verify Error From Synapse
                        }
                    }

                    #endregion GOT USERS SYNAPSE AUTH TOKEN
                }
                else
                {
                    Logger.Error("MDA -> SynapseV3MFABankVerify ERROR: Member not found: [" + MemberId + "]");
                    res.errorMsg = "Member not found.";
                }

                return res;
            }
        }


        public GenericInternalResponseForSynapseMethods RemoveSynapseV3BankAccount(string MemberId)
        {
            Logger.Info("MDA -> RemoveSynapseV3BankAccount - [MemberId: " + MemberId + "]");

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();
            res.success = false;

            var id = Utility.ConvertToGuid(MemberId);
            var bankAccountsFound = _dbContext.SynapseBanksOfMembers.Where(bank =>
                                       bank.MemberId.Value == id &&
                                       bank.IsDefault == true).ToList();

            if (bankAccountsFound.Count == 0)
            {
                foreach (SynapseBanksOfMember sb in bankAccountsFound)
                {
                    sb.IsDefault = false;
                    _dbContext.SaveChanges();
                }

                #region Get User's Synapse OAuth Consumer Key

                string usersSynapseOauthKey = "";

                var usersSynapseDetails = _dbContext.SynapseCreateUserResults.Where(synapseUser =>
                                    synapseUser.MemberId.Value.Equals(id) &&
                                    synapseUser.IsDeleted == false).FirstOrDefault();

                if (usersSynapseDetails == null)
                {
                    Logger.Info("MDA -> RemoveSynapseV3BankAccount ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                    res.message = "Could not find this member's account info";
                    return res;
                }
                else
                {
                    _dbContext.Entry(usersSynapseDetails).Reload();
                    usersSynapseOauthKey = CommonHelper.GetDecryptedData(usersSynapseDetails.access_token);
                }

                #endregion Get User's Synapse OAuth Consumer Key


                #region Get User's Fingerprint

                string usersFingerprint = "";

                var member = _dbContext.Members.Where(synapseUser =>
                                    synapseUser.MemberId.Equals(id) &&
                                    synapseUser.IsDeleted == false).FirstOrDefault();

                if (member == null)
                {
                    Logger.Info("MDA -> RemoveSynapseV3BankAccount ABORTED: Member not found. [MemberId: " + MemberId + "]");
                    res.message = "Member not found";
                    return res;
                }
                else
                {
                    if (String.IsNullOrEmpty(member.UDID1))
                    {
                        Logger.Info("MDA -> RemoveSynapseV3BankAccount ABORTED: Member's Fingerprint not found. [MemberId: " + MemberId + "]");
                        res.message = "Could not find this member's fingerprint";
                        return res;
                    }
                    else
                    {
                        usersFingerprint = member.UDID1.ToString();
                    }
                }

                #endregion Get User's Fingerprint


                #region Tell Synapse To Remove This Node

                SynapseRemoveBankV3_Input removeBankPars = new SynapseRemoveBankV3_Input();

                SynapseV3Input_login log = new SynapseV3Input_login() { oauth_key = usersSynapseOauthKey };
                removeBankPars.login = log;

                SynapseV3Input_user userr = new SynapseV3Input_user() { fingerprint = usersFingerprint };
                removeBankPars.user = userr;

                SynapseNodeId noId = new SynapseNodeId() { oid = bankAccountsFound[0].oid };

                SynapseRemoveBankV3_Input_node nodem = new SynapseRemoveBankV3_Input_node() { _id = noId };

                removeBankPars.node = nodem;

                string baseAddress = "";
                baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/remove" : "https://synapsepay.com/api/v3/node/remove";


                try
                {
                    var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(removeBankPars);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    JObject removeBankRespFromSynapse = JObject.Parse(content);

                    if (removeBankRespFromSynapse["success"].ToString().ToLower() == "true" ||
                        removeBankRespFromSynapse["message"]["en"].ToString().ToLower() == "node removed")
                    {
                        Logger.Info("MDA -> RemoveSynapseV3BankAccount SUCCESSFUL - [MemberID: " + MemberId + "]");

                        res.success = true;

                        res.message = "Bank account deleted successfully";
                    }
                    else
                    {
                        Logger.Info("MDA -> RemoveSynapseV3BankAccount FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");

                        res.message = "Remove node response was null from Synapse";
                    }
                }
                catch (WebException ex)
                {
                    res.message = "MDA Exception #10536";

                    Logger.Error("MDA -> RemoveSynapseV3BankAccount FAILED - Catch [Exception: " + ex + "]");
                }

                #endregion Tell Synapse To Remove This Node
            }
            else
            {
                res.message = "No active bank account found for this user";
            }

            return res;
        }


        public synapseIdVerificationQuestionsForDisplay getIdVerificationQuestionsV3(string MemberId)
        {
            Logger.Info("MDA -> getIdVerificationQuestionsV3 Initialized - [MemberId: " + MemberId + "]");

            synapseIdVerificationQuestionsForDisplay response = new synapseIdVerificationQuestionsForDisplay();
            response.success = false;
            response.memberId = MemberId;

            Member noochMember = GetMemberDetails(MemberId);
            if (noochMember.IsVerifiedWithSynapse == true)
            {
                Logger.Info("MDA -> getIdVerificationQuestionsV3 ABORTED: Member's ID already verified with Synapse  :-) - [Member's Name: " +
                            CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName)) + " " +
                            CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName)) + "], MemberId: " + MemberId + "]");
                response.msg = "ID already verified successfully";
            }


            var id = Utility.ConvertToGuid(MemberId);

            // NOTE (9/29/15): It's very important to get the questions in the right order
            List<SynapseIdVerificationQuestion> questions =
                _dbContext.SynapseIdVerificationQuestions.Where(m => m.MemberId == id && m.submitted == false)
                    .ToList()
                    .OrderByDescending(m => m.DateCreated).Take(5).ToList<SynapseIdVerificationQuestion>();


            if (questions.Count == 0)
            {
                Logger.Error("MDA -> getIdVerificationQuestionsV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                return null;
            }
            else if (questions.Count > 1)
            {
                #region Questions Found In DB

                // Check to make sure each of the "QuestionSetId" is the same for each question
                if (questions[0].QuestionSetId.ToString() != questions[1].QuestionSetId.ToString() ||
                    questions[0].QuestionSetId.ToString() != questions[2].QuestionSetId.ToString() ||
                    questions[0].QuestionSetId.ToString() != questions[4].QuestionSetId.ToString())
                {
                    Logger.Info("MDA -> getIdVerificationQuestionsV2 ABORTED: Multiple qeustionSetIds found among the questions. [MemberId: " + MemberId + "]");
                    response.msg = "Multiple qeustionSetIds found among the questions";
                    return null;
                }
                else
                {
                    synapseIdVerificationQuestionsForDisplay questsToReturn = new synapseIdVerificationQuestionsForDisplay();

                    response.success = true;
                    response.submitted = questions[0].submitted == true ? true : false;
                    response.qSetId = questions[0].QuestionSetId;
                    response.dateCreated = questions[0].DateCreated.ToString();

                    List<synapseIdVerificationQuestionAnswerSet> entireQuestionList = new List<synapseIdVerificationQuestionAnswerSet>();

                    foreach (SynapseIdVerificationQuestion x in questions)
                    {
                        synapseIdVerificationQuestionAnswerSet oneQuestionAnswerObj = new synapseIdVerificationQuestionAnswerSet();

                        oneQuestionAnswerObj.id = (Int16)x.SynpQuestionId;
                        oneQuestionAnswerObj.question = x.Question;

                        oneQuestionAnswerObj.answers = new List<synapseIdVerificationAnswerChoices>();
                        List<synapseIdVerificationAnswerChoices> allQs = new List<synapseIdVerificationAnswerChoices>();

                        for (int i = 0; i < 5; i++)
                        {
                            synapseIdVerificationAnswerChoices b = new synapseIdVerificationAnswerChoices();
                            if (i == 0)
                            {
                                b.id = Convert.ToInt16(x.Choice1Id);
                                b.answer = x.Choice1Text;

                            }
                            if (i == 1)
                            {
                                b.id = Convert.ToInt16(x.Choice2Id);
                                b.answer = x.Choice2Text;
                            }
                            if (i == 2)
                            {
                                b.id = Convert.ToInt16(x.Choice3Id);
                                b.answer = x.Choice3Text;
                            }
                            if (i == 3)
                            {
                                b.id = Convert.ToInt16(x.Choice4Id);
                                b.answer = x.Choice4Text;
                            }
                            if (i == 4)
                            {
                                b.id = Convert.ToInt16(x.Choice5Id);
                                b.answer = x.Choice5Text;
                            }
                            allQs.Add(b);
                        }
                        oneQuestionAnswerObj.answers = allQs;

                        entireQuestionList.Add(oneQuestionAnswerObj);
                    }

                    response.questionList = entireQuestionList;
                }

                #endregion Questions Found In DB
            }

            return response;
        }


        public submitIdVerificationInt submitIdVerificationAnswersToSynapseV3(string MemberId, string questionSetId, string quest1id, string quest2id, string quest3id, string quest4id, string quest5id, string answer1id, string answer2id, string answer3id, string answer4id, string answer5id)
        {
            Logger.Info("MDA -> submitIdVerificationAnswersToSynapseV3 Initialized - [MemberId: " + MemberId + "], [questionSetId: " + questionSetId + "]");

            submitIdVerificationInt res = new submitIdVerificationInt();
            res.success = false;

            #region Check User For All Required Data

            // Check for MemberId
            if (string.IsNullOrEmpty(MemberId.ToString()))
            {
                Logger.Info("MDA -> submitIdVerificationAnswersToSynapseV3 ABORTED: No MemberID sent");
                res.message = "Missing a MemberId";
                return res;
            }

            // Check for QuestionSetId
            if (string.IsNullOrEmpty(questionSetId.ToString()))
            {
                Logger.Info("MDA -> submitIdVerificationAnswersToSynapseV3 ABORTED: No Question Set ID Sent. [MemberId: " + MemberId + "]");
                res.message = "Missing a Question Set ID";
                return res;
            }

            // Check for 5 total answers
            if (String.IsNullOrEmpty(answer1id) || String.IsNullOrEmpty(answer2id) || String.IsNullOrEmpty(answer3id) || String.IsNullOrEmpty(answer4id) || String.IsNullOrEmpty(answer5id))
            {
                Logger.Info("MDA -> submitIdVerificationAnswersToSynapseV3 ABORTED: Missing at least 1 answer. [MemberId: " + MemberId + "]");
                res.message = "Missing at least 1 answer";
                return res;
            }

            #endregion Check User For All Required Data

            var id = Utility.ConvertToGuid(MemberId);
            var memberEntity = GetMemberDetails(MemberId);

            // Now check if user already has a Synapse User account (would have a record in SynapseCreateUserResults.dbo)
            #region Get User's Synapse OAuth Consumer Key

            string usersSynapseOauthKey = "";


            var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

            if (usersSynapseDetails == null)
            {
                Logger.Info("MDA -> submitIdVerificationAnswersToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                res.message = "Could not find this member's account info";
                return res;
            }
            else
            {
                _dbContext.Entry(usersSynapseDetails).Reload();
                usersSynapseOauthKey = CommonHelper.GetDecryptedData(usersSynapseDetails.access_token);
            }

            #endregion Get User's Synapse OAuth Consumer Key

            synapseIdVerificationAnswersInput input = new synapseIdVerificationAnswersInput();

            input.login = new SynapseV3Input_login() { oauth_key = CommonHelper.GetDecryptedData(usersSynapseDetails.access_token) };

            input.user = new synapseSubmitIdAnswers_answers_input();
            input.user.fingerprint = memberEntity.UDID1;

            input.user.doc = new synapseSubmitIdAnswers_docSet();
            input.user.doc.question_set_id = questionSetId;

            input.user.doc.answers = new synapseSubmitIdAnswers_Input_quest[5];
            // Finally, set the user's answers
            synapseSubmitIdAnswers_Input_quest[] quest = new synapseSubmitIdAnswers_Input_quest[5];
            quest[0] = new synapseSubmitIdAnswers_Input_quest { question_id = Convert.ToInt16(quest1id), answer_id = Convert.ToInt16(answer1id) };
            quest[1] = new synapseSubmitIdAnswers_Input_quest { question_id = Convert.ToInt16(quest2id), answer_id = Convert.ToInt16(answer2id) };
            quest[2] = new synapseSubmitIdAnswers_Input_quest { question_id = Convert.ToInt16(quest3id), answer_id = Convert.ToInt16(answer3id) };
            quest[3] = new synapseSubmitIdAnswers_Input_quest { question_id = Convert.ToInt16(quest4id), answer_id = Convert.ToInt16(answer4id) };
            quest[4] = new synapseSubmitIdAnswers_Input_quest { question_id = Convert.ToInt16(quest5id), answer_id = Convert.ToInt16(answer5id) };
            input.user.doc.answers = quest;


            #region Call Synapse /user/ssn/answer API

            string baseAddress = "";
            baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/doc/verify" : "https://synapsepay.com/api/v3/user/doc/verify";

            try
            {
                var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(input);
                ASCIIEncoding encoding = new ASCIIEncoding();
                Byte[] bytes = encoding.GetBytes(parsedContent);

                Stream newStream = http.GetRequestStream();

                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();

                var response = http.GetResponse();
                var stream = response.GetResponseStream();
                var sr = new StreamReader(stream);
                var content = sr.ReadToEnd();

                var resFromSynapse = JsonConvert.DeserializeObject<kycInfoResponseFromSynapse>(content);

                if (resFromSynapse != null)
                {
                    if (resFromSynapse.success.ToString().ToLower() == "true")
                    {
                        Logger.Info("MDA -> submitIdVerificationAnswersToSynapse SUCCESSFUL - [MemberID: " + MemberId + "]");

                        res.success = true;
                        res.message = "Ok";

                        // Now we have to update the "DateSubmitted" value for EACH question in the DB (should be exactly 5 per 'QuestionSetId')
                        #region Mark All 5 Questions as 'Submitted' in Nooch DB

                        List<SynapseIdVerificationQuestion> listOfQuestionsToUpdate =
                            _dbContext.SynapseIdVerificationQuestions.Where(m => m.QuestionSetId == questionSetId && m.MemberId == id).Distinct().OrderByDescending(o => o.DateCreated)
                                        .ToList();

                        if (listOfQuestionsToUpdate.Count > 0)
                        {
                            short n = 0;
                            foreach (SynapseIdVerificationQuestion q in listOfQuestionsToUpdate)
                            {
                                if (n > 5)
                                {
                                    Logger.Error("MDA -> submitIdVerificationAnswersToSynapse - Unexpected: More than 5 answers (n = " + n + ") found in DB... Could be a problem somewhere");
                                }
                                q.submitted = true;
                                q.DateResponseSent = DateTime.Now;

                                bool b = _dbContext.SaveChanges() > 0;
                                if (b)
                                {
                                    n += 1;
                                }
                            }

                            if (n > 3) // It should be exactly 5, but setting to > 3 in case there's ever only 4 questions from Synapse
                            {
                                Logger.Info("MDA -> submitIdVerificationAnswersToSynapse - All Questions Marked as Submitted in DB for: [Question Set ID: " + questionSetId + "]");
                            }
                            else
                            {
                                Logger.Error("MDA -> submitIdVerificationAnswersToSynapse - Unexpected: < 4 Questions were Marked as Submitted in DB. [Question Set ID: " + questionSetId + "]");
                            }
                        }
                        else
                        {
                            Logger.Error("MDA -> submitIdVerificationAnswersToSynapse FAILED TO UPDATE DB: List of Banks to Update was null - [MemberId: " + MemberId + "]");
                        }

                        #endregion Mark All 5 Questions as 'Submitted' in Nooch DB

                        // Update Member's DB record
                        if (memberEntity != null)
                        {
                            memberEntity.IsVerifiedWithSynapse = true;
                            memberEntity.ValidatedDate = DateTime.Now;
                            memberEntity.DateModified = DateTime.Now;

                            DbContext dbc = CommonHelper.GetDbContextFromEntity(memberEntity);
                            dbc.SaveChanges();

                            // Get existing record from Create Synapse User Results table for this Member

                            var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                            if (synapseRes != null)
                            {
                                synapseRes.permission = resFromSynapse.user.permission;
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        res.message = "Got a response, but verification was not successful";
                        Logger.Error("MDA -> submitIdVerificationAnswersToSynapseV3 FAILED - Got Synapse response, but success was NOT 'true' - [MemberID: " + MemberId + "]");
                    }
                }
                else
                {
                    res.message = "Verification response was null";
                    Logger.Error("MDA -> submitIdVerificationAnswersToSynapseV3 FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");
                }
            }
            catch (WebException we)
            {
                var httpStatusCode = ((HttpWebResponse)we.Response).StatusCode;

                var response = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                JObject errorJsonFromSynapse = JObject.Parse(response);

                Logger.Error("MDA -> submitIdVerificationAnswersToSynapseV3 FAILED - HTTP Status Code:[" + httpStatusCode + "], [Exception: " + errorJsonFromSynapse.ToString() + "]");

                var errorCode = ((HttpWebResponse)we.Response).StatusCode;
                JToken token = errorJsonFromSynapse["error"]["en"];

                if (!String.IsNullOrEmpty(res.message))
                {
                    // Synapse Error Message could be:
                    // "Incorrect oauth_key/fingerprint"
                    res.message = token.ToString();

                    Logger.Error("MDA -> submitIdVerificationAnswersToSynapseV3 FAILED - [Synapse Error Code: " + errorCode +
                                 "], [Error Msg: " + res.message + "], [MemberID: " + MemberId + "]");
                }
                else
                {
                    res.message = "MDA Exception during submitIdVerificationAnswersToSynapseV3";
                }
            }

            #endregion Call Synapse /user/ssn/answer API

            return res;
        }

        #endregion Synapse Related Methods


        public string SaveMemberSSN(string MemberId, string ssn)
        {
            try
            {
                if (String.IsNullOrEmpty(ssn) || ssn.Length != 9)
                    return "Invalid SSN passed.";

                var guid = new Guid(MemberId);
                var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == guid && m.IsDeleted != true);

                if (memberObj != null)
                {
                    memberObj.SSN = CommonHelper.GetEncryptedData(ssn);
                    memberObj.DateModified = DateTime.Now;

                    _dbContext.SaveChanges();

                    return "SSN saved successfully.";
                }
                else return "Member Id not found or Member status deleted.";
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> SaveMemberSSN FAILED - MemberID: [" + MemberId + "], Exception: [" + ex.Message + "]");
                return "MDA Exception: [" + ex.Message + "]";
            }
        }


        public string SaveDOBForMember(string MemberId, string dob)
        {
            try
            {
                if (String.IsNullOrEmpty(MemberId))
                {
                    Logger.Error("MDA -> SaveDOBForMember FAILED - Missing MemberID - MemberID: [" + MemberId + "]");
                    return "Missing MemberID";
                }

                var noochMember = CommonHelper.GetMemberDetails(MemberId);

                if (noochMember != null)
                {
                    DateTime dateTime2;

                    if (!DateTime.TryParse(dob, out dateTime2))
                    {
                        Logger.Error("MDA -> SaveDOBForMember FAILED - Invalid DOB passed - MemberID: [" + MemberId + "], DOB: [" + dob + "]");
                        return "Invalid DOB passed.";
                    }

                    noochMember.DateOfBirth = dateTime2;
                    noochMember.DateModified = DateTime.Now;
                    var dbContext = CommonHelper.GetDbContextFromEntity(noochMember);
                    dbContext.SaveChanges();

                    return "DOB saved successfully.";
                }
                else
                    return "MemberID not found or Member status deleted.";
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> SaveDOBForMember FAILED - MemberID: [" + MemberId + "], Exception: [" + ex.Message + "]");
                return "MDA Exception: [" + ex.Message + "]";
            }
        }


        /// <summary>
        /// For settings a user's Synapse Bank to 'Verified'. Currently only called 
        /// from the Admin Dashboard and from the BankVerification browser page.
        /// </summary>
        /// <param name="bankId"></param>
        /// <returns>True if successful, false if not.</returns>
        public bool VerifySynapseAccount(string bankId)
        {
            Logger.Info("MDA -> VerifySynapseAccount Initiated - Bank ID: [" + bankId + "]");

            int id = Convert.ToInt16(bankId);

            var memberBank = _dbContext.SynapseBanksOfMembers.FirstOrDefault(member => member.Id.Equals(id) &&
                                                                                       (member.Status != "Verified" || member.Status == null));

            if (memberBank == null)
            {
                Logger.Error("MDA -> VerifySynapseAccount FAILED - No Synapse Bank found with ID of [" + bankId + "] and Status != 'Verified'.");
                return false;
            }
            else
            {
                // got account, updating it to set as verified
                memberBank.Status = "Verified";
                memberBank.VerifiedOn = DateTime.Now;

                int i = _dbContext.SaveChanges();
                _dbContext.Entry(memberBank).Reload();

                if (i > 0)
                {
                    #region Send Bank Verified Email

                    try
                    {
                        var memberId = memberBank.MemberId;
                        var BankName = CommonHelper.GetDecryptedData(memberBank.bank_name);
                        var bankNickName = CommonHelper.GetDecryptedData(memberBank.nickname);

                        // Set Bank Logo URL Variable
                        var bankLogoUrl = CommonHelper.getLogoForBank(BankName);

                        var noochMember = _dbContext.Members.Where(memberTemp => memberTemp.MemberId == memberId &&
                                                      memberTemp.IsDeleted == false).FirstOrDefault();

                        Logger.Info("MDA -> VerifySynapseAccount --> Checkpoint 8831 - About to send Bank Verified email");

                        if (noochMember != null)
                        {
                            var toAddress = CommonHelper.GetDecryptedData(noochMember.UserName.ToLower());
                            var fromAddress = Utility.GetValueFromConfig("adminMail");

                            var firstNameForEmail = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName));
                            var fullName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName)) + " " +
                                           CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName));

                            var tokens = new Dictionary<string, string>
                                            {
                                                {Constants.PLACEHOLDER_FIRST_NAME, firstNameForEmail},
                                                {Constants.PLACEHOLDER_BANK_NAME, BankName},
                                                {Constants.PLACEHOLDER_RECIPIENT_FULL_NAME, fullName},
                                                {Constants.PLACEHOLDER_Recepient_Email, bankNickName},
                                                {Constants.PLACEHOLDER_BANK_BALANCE, bankLogoUrl},
                                            };

                            Utility.SendEmail("bankVerified", fromAddress, toAddress, null,
                                              "Your bank account has been verified on Nooch",
                                              null, tokens, null, null, null);

                            Logger.Info("MDA -> VerifySynapseAccount --> Bank VERIFIED Email sent to: [" + toAddress + "]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> VerifySynapseAccount --> Bank Verified Email NOT sent for Synapse BankID: [" + bankId + "], [Exception: " + ex + "]");
                    }

                    #endregion Send Bank Verified Email

                    return true;
                }
                else
                    Logger.Error("MDA -> VerifySynapseAccount FAILED - Bank found, but error on updating DB record in SynapseBanksOfMembers - BankID: [" + bankId + "] and Status != 'Verified'.");
            }

            return false;
        }


        public string GetTokensAndTransferMoneyToNewUser(string TransactionId, string memberId, string TransactionType, string recipMemId)
        {
            Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser Fired - TransType: [" + TransactionType +
                        "], TransID: [" + TransactionId + "], New User Member ID: [" + memberId +
                        "], Recip MemberID: [" + recipMemId + "]");

            // If a REQUEST, TransactionType will be "RequestToNewUser" & MemberIdAfterSynapseAccountCreation is the Request Recipient, 
            //               and should be the SENDER below, and the Request sender is the RecipMemID (if b/t 2 existing users)
            // If an INVITE, TransactionType will be "SentToNewUser &  MemberIdAfterSynapseAccountCreation is the Invite Recipient,
            //               and should be the NEW USER (RECIPIENT) below

            try
            {
                Guid transGuid = Utility.ConvertToGuid(TransactionId);

                var Transaction = _dbContext.Transactions.FirstOrDefault(m => m.TransactionId == transGuid && m.TransactionStatus == "pending");

                if (Transaction != null)
                {
                    _dbContext.Entry(Transaction).Reload();

                    if (!string.IsNullOrEmpty(memberId))
                    {
                        var newUserObj = GetMemberDetails(memberId);
                        string newUsersEmail = CommonHelper.GetDecryptedData(newUserObj.UserName);

                        // Check if this is a TEST transaction
                        bool isTesting = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"));

                        Guid SenderId, RecipientId;

                        if (TransactionType == "RequestToNewUser")
                        {
                            // The "SENDER" is the person PAYING this request (so the "new" user)
                            SenderId = new Guid(memberId);

                            if (!String.IsNullOrEmpty(recipMemId))
                                RecipientId = new Guid(recipMemId);
                            else
                                RecipientId = Transaction.Member.MemberId;
                        }
                        else
                        {
                            RecipientId = new Guid(memberId);

                            if (!String.IsNullOrEmpty(recipMemId))
                                SenderId = new Guid(recipMemId);
                            else
                                SenderId = Transaction.Member.MemberId;
                        }

                        Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - SENDER MemberID: [" + SenderId +
                                    "], RECIPIENT MemberId: [" + RecipientId + "]");

                        var SenderUserAndBankDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(SenderId.ToString());

                        if (SenderUserAndBankDetails.AccountDetailsErrMessage == "OK" &&
                            SenderUserAndBankDetails.UserDetailsErrMessage == "OK")
                        {
                            var RecipientUserAndBankDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(RecipientId.ToString());

                            if (RecipientUserAndBankDetails.AccountDetailsErrMessage == "OK" &&
                                RecipientUserAndBankDetails.UserDetailsErrMessage == "OK")
                            {
                                // We have all details of both users.  Now call Synapse's Order API service
                                #region Call Synapse Order API

                                var sender = GetMemberDetails(SenderId.ToString());
                                string senderEmail = CommonHelper.GetDecryptedData(sender.UserName);
                                string moneySenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName));
                                string moneySenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName));

                                var access_token = SenderUserAndBankDetails.UserDetails.access_token;
                                var senderFingerprint = SenderUserAndBankDetails.UserDetails.user_fingerprints;
                                var senderBankOid = SenderUserAndBankDetails.BankDetails.bank_oid;

                                var recipient = GetMemberDetails(RecipientId.ToString());
                                string recipEmail = CommonHelper.GetDecryptedData(recipient.UserName);
                                string moneyRecipientFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipient.FirstName));
                                string moneyRecipientLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipient.LastName));

                                var recipBankOid = RecipientUserAndBankDetails.BankDetails.bank_oid;

                                var transId = Transaction.TransactionId.ToString();
                                var amount = Transaction.Amount.ToString();
                                string memoForSyn = !String.IsNullOrEmpty(Transaction.Memo) ? Transaction.Memo : "";
                                var log = "";

                                #region Rent Scene Custom Checks

                                // Cliff (6/14/16): Adding this to check: a.) is this a payment to Rent Scene?
                                //                  and b.) what kind of user the recipient is: Client or Vendor, which determines which Node ID to use for Rent Scene

                                // 575ad909950629625ca88262 - Corp Checking - USE FOR ALL NON-PASSTHROUGH PAYMENTS, i.e.: Payments TO Vendors, and Application fees from Clients to RS
                                // 574f45d79506295ff7a81db8 - Passthrough (Linked to Rent Scene's parent account - USE FOR RENT PAYMENTS - ANYTHING OVER $1,000)
                                // 5759005795062906e1359a8e - Passthrough (Linked to Marvis Burn's Nooch account - NEVER USE)
                                if (recipient.cipTag != null && recipient.cipTag.ToLower() == "vendor" &&
                                    (sender.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                                     senderBankOid == "5759005795062906e1359a8e" || senderBankOid == "574f45d79506295ff7a81db8"))
                                {
                                    // Sender is Rent Scene and recipient is a 'Vendor'
                                    senderBankOid = "575ad909950629625ca88262";
                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - RENT SCENE VENDOR Payment Detected - " +
                                                "Substituting Sender_Bank_NodeID to use RS's Corporate Checking account");
                                }
                                else if (Transaction.Amount < 200 &&
                                         (recipient.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                                          recipBankOid == "574f45d79506295ff7a81db8" || recipBankOid == "5759005795062906e1359a8e") &&
                                         (sender.cipTag.ToLower() == "renter" || sender.cipTag.ToLower() == "client")) // CC (8/5/16): Should be "renter" now as of updating to Synapse's KYC 2.0, used to be "Client"
                                {
                                    // Recipient is Rent Scene AND Sender is a Client AND Amount is < $200 (so it's probably an application fee)
                                    // So use RS's Corporate Checking account.
                                    recipBankOid = "575ad909950629625ca88262";
                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - RENT SCENE Payment Detected Under $200 - " +
                                                "Substituting Receiver_Bank_NodeID to use RS's Corporate Checking account");
                                }

                                #endregion Rent Scene Custom Checks

                                TransactionsDataAccess tda = new TransactionsDataAccess();
                                SynapseV3AddTrans_ReusableClass Call_Synapse_Order_API_Result = new SynapseV3AddTrans_ReusableClass();


                                #region Habitat Custom Checks

                                if (sender.MemberId.ToString().ToLower() == "45357cf0-e651-40e7-b825-e1ff48bf44d2" &&
                                    recipient.cipTag == "vendor")
                                {
                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - HABITAT Payment!!");

                                    // Need to create 2 transactions with Synapse: 1 from Habitat to Rent Scene, & 1 from Rent Scene to the Vendor

                                    moneySenderFirstName = "Habitat";
                                    moneySenderLastName = "";

                                    // Insert RENT SCENE values
                                    var recipBankOid2 = "574f45d79506295ff7a81db8";
                                    var recipEmail2 = "payments@rentscene.com";
                                    var moneyRecipientLastName2 = "Rent Scene";

                                    log = "MDA -> GetTokensAndTransferMoneyToNewUser - About to call AddTransSynapseV3Reusable() in TDA - " +
                                          "TransID: [" + transId + "], Amount: [" + amount + "], Sender Name: [" + moneySenderFirstName + " " + moneySenderLastName +
                                          "], Sender BankOID: [" + senderBankOid + "], Recip Name: [" + moneyRecipientFirstName + " " + moneyRecipientLastName +
                                          "], Recip BankOID: [" + recipBankOid2 + "]";
                                    Logger.Info(log);

                                    // 1st payment - from Habitat -> Rent Scene
                                    Call_Synapse_Order_API_Result = tda.AddTransSynapseV3Reusable(access_token,
                                        senderFingerprint, senderBankOid, amount, recipBankOid2, transId, senderEmail, recipEmail2,
                                        CommonHelper.GetRecentOrDefaultIPOfMember(sender.MemberId),
                                        moneySenderLastName, moneyRecipientLastName2, memoForSyn);


                                    if (Call_Synapse_Order_API_Result.success)
                                    {
                                        #region 2nd Synapse Payment (RS to Vendor)

                                        // 1st Payment was successful, now do the 2nd one (from RS -> original recipient)
                                        Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - 1st HABITAT Payment Successful (to RS) - Response From SYNAPSE's /order/add API - " +
                                                    "OrderID: [" + Call_Synapse_Order_API_Result.responseFromSynapse.trans._id.oid + "]");

                                        moneySenderFirstName = "Rent Scene";
                                        moneySenderLastName = "";

                                        var rentSceneMemId = "852987e8-d5fe-47e7-a00b-58a80dd15b49"; // Rent Scene's MemberID
                                        var rentSceneMemGuid = Utility.ConvertToGuid(rentSceneMemId);

                                        var createSynapseUserObj = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == rentSceneMemGuid &&
                                                                                                                           m.IsDeleted == false);
                                        access_token = createSynapseUserObj.access_token;

                                        senderBankOid = "574f45d79506295ff7a81db8";
                                        senderFingerprint = "6d441f70cc6891e7831432baac2e50d7";
                                        var senderIP = CommonHelper.GetRecentOrDefaultIPOfMember(new Guid(rentSceneMemId));
                                        var senderEmail2 = "payments@rentscene.com";

                                        log = "MDA -> GetTokensAndTransferMoneyToNewUser - About to call 2nd HABITAT Payment (to the Vendor) - " +
                                              "TransID: [" + transId + "], Amount: [" + amount + "], Sender Name: [" + moneySenderFirstName + " " + moneySenderLastName +
                                              "], Sender BankOID: [" + senderBankOid + "], Recip Name: [" + moneyRecipientFirstName + " " + moneyRecipientLastName +
                                              "], Recip BankOID: [" + recipBankOid + "]";

                                        Logger.Info(log);

                                        Call_Synapse_Order_API_Result = tda.AddTransSynapseV3Reusable(access_token,
                                            senderFingerprint, senderBankOid, amount, recipBankOid, transId,
                                            senderEmail2, recipEmail, senderIP, moneySenderLastName, moneyRecipientLastName, memoForSyn);

                                        #endregion 2nd Synapse Payment (RS to Vendor)
                                    }
                                    else
                                    {
                                        var error = "MDA -> GetTokensAndTransferMoneyToNewUser - 1st HABITAT Payment (to RS) FAILED - Response From SYNAPSE's /order/add API - " +
                                                    "ErrorMessage: [" + Call_Synapse_Order_API_Result.ErrorMessage + "], Synapse Error: [" + Call_Synapse_Order_API_Result.responseFromSynapse.error.en + "]";
                                        Logger.Error(error);
                                        CommonHelper.notifyCliffAboutError(error);
                                    }
                                }

                                #endregion Habitat Custom Checks

                                else
                                {
                                    // Expected path for all Payments except for Habitat
                                    log = "MDA -> GetTokensAndTransferMoneyToNewUser - About to call AddTransSynapseV3Reusable() in TDA - " +
                                              "TransID: [" + transId + "], Amount: [" + amount + "], Sender Name: [" + moneySenderFirstName + " " + moneySenderLastName +
                                              "], Sender BankOID: [" + senderBankOid + "], Recip Name: [" + moneyRecipientFirstName + " " + moneyRecipientLastName +
                                              "], Recip BankOID: [" + recipBankOid + "]";
                                    Logger.Info(log);

                                    Call_Synapse_Order_API_Result = tda.AddTransSynapseV3Reusable(access_token, senderFingerprint,
                                        senderBankOid, amount, recipBankOid, transId, senderEmail, recipEmail,
                                        CommonHelper.GetRecentOrDefaultIPOfMember(sender.MemberId),
                                        moneySenderLastName, moneyRecipientLastName, memoForSyn);
                                }

                                #endregion Call Synapse Order API

                                if (Call_Synapse_Order_API_Result.success)
                                {
                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - SUCCESS Response From SYNAPSE's /order/add API - " +
                                                "Synapse OrderID: [" + Call_Synapse_Order_API_Result.responseFromSynapse.trans._id.oid + "]");

                                    Transaction.TransactionStatus = "Success";
                                    Transaction.DateAccepted = DateTime.Now;
                                    int save = _dbContext.SaveChanges();

                                    #region Update Tenant Info If A RENT Payment

                                    if (sender.Type == "Tenant")
                                    {
                                        try
                                        {
                                            var landlordObj = _dbContext.Landlords.FirstOrDefault(m => m.MemberId == SenderId && m.IsDeleted == false);

                                            if (landlordObj != null)
                                            {
                                                landlordObj.DateModified = DateTime.Now;
                                                _dbContext.SaveChanges();
                                                _dbContext.Entry(landlordObj).Reload();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser - Failed to update TENANT info in DB - [MemberID: " +
                                                         SenderId + "], Exception: [" + ex.Message + "]");
                                        }
                                    }

                                    #endregion Update Tenant Info If A RENT Payment

                                    if (save > 0)
                                    {
                                        #region EMAIL NOTIFICATIONS

                                        // NOW SEND EMAILS TO BOTH USERS

                                        #region Setup Email Placeholders

                                        Transaction.TransactionDate = DateTime.Now;
                                        var fromAddress = Utility.GetValueFromConfig("transfersMail");

                                        var newUserPhone = "";

                                        if (Transaction.IsPhoneInvitation != null &&
                                            Transaction.IsPhoneInvitation == true &&
                                            !String.IsNullOrEmpty(Transaction.PhoneNumberInvited))
                                        {
                                            newUserPhone = CommonHelper.GetDecryptedData(Transaction.PhoneNumberInvited);
                                        }

                                        bool isForRentScene = false;
                                        if (recipient.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                                        {
                                            isForRentScene = true;
                                            moneyRecipientFirstName = "Rent Scene";
                                            moneyRecipientLastName = "";
                                        }

                                        var newUserNameForEmail = "";

                                        var recipientPic = (!String.IsNullOrEmpty(recipient.Photo) && recipient.Photo.Length > 20)
                                                           ? recipient.Photo.ToString()
                                                           : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";


                                        if (!String.IsNullOrEmpty(moneyRecipientFirstName))
                                        {
                                            newUserNameForEmail = moneyRecipientFirstName;

                                            if (!String.IsNullOrEmpty(moneyRecipientLastName))
                                                newUserNameForEmail = newUserNameForEmail + " " + moneyRecipientLastName;
                                        }
                                        else if (newUsersEmail.Length > 2)
                                            newUserNameForEmail = newUsersEmail;
                                        else if (newUserPhone.Length > 2)
                                            newUserNameForEmail = newUserPhone;

                                        var wholeAmount = Transaction.Amount.ToString("n2");
                                        string[] s3 = wholeAmount.Split('.');

                                        var transDate = Convert.ToDateTime(Transaction.TransactionDate).ToString("MMMM dd, yyyy");

                                        var memo = "";
                                        if (!String.IsNullOrEmpty(Transaction.Memo))
                                        {
                                            if (Transaction.Memo.Length > 3)
                                            {
                                                var firstThreeChars = Transaction.Memo.Substring(0, 3).ToLower();
                                                bool startsWithFor = firstThreeChars.Equals("for");

                                                if (startsWithFor) memo = Transaction.Memo.ToString();
                                                else memo = "For: " + Transaction.Memo.ToString();
                                            }
                                            else memo = "For: " + Transaction.Memo.ToString();
                                        }

                                        #endregion Setup Email Placeholders

                                        if (TransactionType == "SentToNewUser")
                                        {
                                            #region Emails for TRANSFER to NEW USER

                                            // Send email if invitation was from email and SMS if invitation was from SMS
                                            #region Notify Transfer RECIPIENT (SMS or Email)

                                            #region If Transfer Sent Using Phone Number

                                            if (newUserPhone.Length > 3)
                                            {
                                                // Send SMS notification to Nooch Recipient (the NEW user)

                                                string SMSContent = "You accepted $" + wholeAmount + " sent by " + moneySenderFirstName + " " + moneySenderLastName +
                                                                    ". Amount will be credited to your bank account within 2-3 biz days.";
                                                try
                                                {
                                                    Utility.SendSMS(newUserPhone, SMSContent);

                                                    Logger.Info("MDA - GetTokensAndTransferMoneyToNewUser SUCCESS - SMS sent to recipient - Phone: [" +
                                                        CommonHelper.FormatPhoneNumber(newUserPhone) + "] successfully.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser SUCCESS - But Failure sending SMS to recipient " +
                                                        "- Phone: [" + CommonHelper.FormatPhoneNumber(newUserPhone) + "], Exception: [" + ex.ToString() + "]");
                                                }
                                            }

                                            #endregion If Transfer Sent Using Phone Number

                                            #region If Transfer Sent Using Email Address

                                            else if (newUsersEmail.Length > 3)
                                            {
                                                // Email to Nooch Recipient (the NEW user)
                                                var tokens = new Dictionary<string, string>
                                                        {
                                                            {Constants.MEMO, memo},
                                                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, wholeAmount},
                                                            {Constants.PLACEHOLDER_TRANSACTION_DATE, transDate},
                                                            {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, moneySenderFirstName + " " + moneySenderLastName},
                                                        };

                                                try
                                                {
                                                    string subject = isForRentScene
                                                                     ? "Payment from Rent Scene accepted - $" + wholeAmount
                                                                     : "Nooch payment from " + moneySenderFirstName + " " + moneySenderLastName + " accepted - $" + wholeAmount;

                                                    Utility.SendEmail("transferAcceptedToRecipient", fromAddress, newUsersEmail,
                                                                      null, subject, null, tokens, null, null, null);

                                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - transferAcceptedToRecipient Email sent to [" +
                                                                newUsersEmail + "] successfully.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser -> transferAcceptedToRecipient Email NOT sent to [" +
                                                                 newUsersEmail + "], Exception: [" + ex.Message + "]");
                                                }
                                            }

                                            #endregion If Transfer Sent Using Email Address

                                            #endregion Notify Transfer RECIPIENT (SMS or Email)


                                            #region Email to Transfer SENDER

                                            var tokens2 = new Dictionary<string, string>
                                                {
                                                    {Constants.PLACEHOLDER_FIRST_NAME, moneySenderFirstName},
                                                    {Constants.MEMO, memo},
                                                    {Constants.PLACEHOLDER_FRIEND_LAST_NAME, newUserNameForEmail},
                                                    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, wholeAmount},
                                                };

                                            var toAddress = CommonHelper.GetDecryptedData(Transaction.Member.UserName);
                                            try
                                            {
                                                Utility.SendEmail("transferAcceptedToSender", fromAddress, toAddress,
                                                                   null, newUserNameForEmail + " accepted your payment",
                                                                   null, tokens2, null, null, null);

                                                Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - TransferAcceptedToSender - Email sent to [" +
                                                             toAddress + "] successfully.");
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser - TransferAcceptedToSender - Email NOT sent to [" +
                                                             toAddress + "],  [Exception: " + ex.ToString() + "]");
                                            }

                                            #endregion Email to Transfer SENDER

                                            #endregion Emails for TRANSFER to NEW USER
                                        }

                                        if (TransactionType == "RequestToNewUser")
                                        {
                                            #region Emails for REQUEST to NEW USER

                                            // Send email if request was via email and SMS if request was via Email to
                                            // Nooch RECIPIENT (non-Nooch person who just paid the request)
                                            #region Notify Request RECIPIENT (New user who paid the request)

                                            #region If Transfer Sent Using Phone Number

                                            if (newUserPhone.Length > 3)
                                            {
                                                string SMSContent = "You paid a $" + wholeAmount + " request from " + moneySenderFirstName + " " + moneySenderLastName +
                                                                    ". Amount will be deducted to your bank account in 2-4 biz days.";
                                                try
                                                {
                                                    Utility.SendSMS(newUserPhone, SMSContent);

                                                    Logger.Info("MDA - GetTokensAndTransferMoneyToNewUser SUCCESS - Request Paid SMS sent to recipient - " +
                                                                "[Phone: " + CommonHelper.FormatPhoneNumber(newUserPhone) + "] successfully.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser SUCCESS - But failed to send Request Paid SMS to recipient - " +
                                                                 "[Phone: " + CommonHelper.FormatPhoneNumber(newUserPhone) + "],  [Exception: " + ex + "]");
                                                }
                                            }

                                            #endregion If Transfer Sent Using Phone Number

                                            #region If Transfer Sent Using Email Address

                                            else
                                            {
                                                var tokens = new Dictionary<string, string> 
                                                        {
                                                            {Constants.PLACEHOLDER_FIRST_NAME, moneySenderFirstName},
                                                            {"$UserPicture$", recipientPic},
                                                            {Constants.MEMO, memo},
                                                            {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, moneyRecipientFirstName},
                                                            {Constants.PLACEHOLDER_FRIEND_LAST_NAME, moneyRecipientLastName},
                                                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, wholeAmount}
                                                        };

                                                try
                                                {
                                                    string subject = isForRentScene
                                                                     ? "Your Payment to Rent Scene"
                                                                     : "Your payment to " + moneyRecipientFirstName + " " + moneyRecipientLastName;

                                                    Utility.SendEmail("requestPaidToRecipient", fromAddress,
                                                        newUsersEmail, null, subject, null, tokens, null, null, null);

                                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToRecipient Email sent to [" +
                                                                           newUsersEmail + "] successfully.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToRecipient Email NOT sent to [" +
                                                        newUsersEmail + "], Exception: [" + ex.Message + "]");
                                                }
                                            }

                                            #endregion If Transfer Sent Using Email Address

                                            #endregion Notify Request RECIPIENT (New user who paid the request)

                                            #region Email to Request SENDER who is now receiving the money

                                            // Email to Nooch Sender (person that originally sent the Request)
                                            var tokens2 = new Dictionary<string, string>
                                                    {
                                                        {Constants.PLACEHOLDER_FIRST_NAME, moneyRecipientFirstName},
                                                        {Constants.MEMO, memo},
                                                        {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, ""},
                                                        {Constants.PLACEHOLDER_FRIEND_LAST_NAME, moneySenderFirstName + " " + moneySenderLastName},
                                                        {Constants.PLACEHOLDER_TRANSFER_AMOUNT, wholeAmount},
                                                    };

                                            var toAddress = recipEmail;

                                            try
                                            {
                                                Utility.SendEmail("requestPaidToSender", fromAddress,
                                                                   toAddress, null, moneySenderFirstName + " " + moneySenderLastName + " paid your request on Nooch",
                                                                   null, tokens2, null, null, null);

                                                Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToSender - Email sent to [" +
                                                            toAddress + "] successfully.");
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToSender - Email NOT sent to [" +
                                                    toAddress + "], Exception: [" + ex.Message + "]");
                                            }

                                            #endregion Email to Request SENDER who is now receiving the money

                                            #endregion Emails for REQUEST to NEW USER
                                        }

                                        #endregion EMAIL NOTIFICATIONS

                                        return "Success";
                                    }
                                    else
                                    {
                                        var error = "MDA -> GetTokensAndTransferMoneyToNewUser FAILED - Success from Call Synapse Create Order API, but " +
                                                    "unable to save transaction status in DB - Error Message: [" + Call_Synapse_Order_API_Result.ErrorMessage +
                                                    "], Response From Synapse: [" + Call_Synapse_Order_API_Result.responseFromSynapse + "]";
                                        Logger.Error(error);
                                        CommonHelper.notifyCliffAboutError(error);
                                        return "Error updating transaction status in DB";
                                    }
                                }
                                else
                                {
                                    var error = "MDA -> GetTokensAndTransferMoneyToNewUser FAILED - " +
                                                 "Error Message: [" + Call_Synapse_Order_API_Result.ErrorMessage + "], Synapse Response: [" + Call_Synapse_Order_API_Result.responseFromSynapse + "]";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error + "\n\n  Trans Info: \n" + log);
                                    return !String.IsNullOrEmpty(Call_Synapse_Order_API_Result.ErrorMessage)
                                                ? Call_Synapse_Order_API_Result.ErrorMessage
                                                : "Error from syn";
                                }
                            }
                            else
                            {
                                var error = "MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find Synapse User or Bank " +
                                             "Details for Recipient - TransID: [" + TransactionId + "]";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                return "Request payor bank account details not found or syn user id not found";
                            }
                        }
                        else
                        {
                            var error = "MDA - GetTokensAndTransferMoneyToNewUser FAILED -> MemberIdAfterSynapseAccountCreation " +
                                         "was Null or empty - TransID: [" + TransactionId + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                            return "Request payor bank account details not found or syn user id not found";
                        }
                    }
                    else
                    {
                        var error = "MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find Synapse User or Bank " +
                                     "Details for EXISTING user (which is bad...) - TransID: [" + TransactionId + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                        return "Missing 'MemberIdAfterSynapseAccountCreation' [6101]";
                    }
                }
                else
                {
                    Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find this Transaction - TransID: [" + TransactionId + "]");
                    return "Either transaction already paid or transaction not found";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser FAILED - TransId: [" + TransactionId + "],  Outer Exception: [" + ex + "]");
            }

            return "Unkown Failure";
        }


        /// <summary>
        /// For saving changes to an existing user's profile (called from Profile screen of App).
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="password"></param>
        /// <param name="secondaryMail"></param>
        /// <param name="recoveryMail"></param>
        /// <param name="facebookAcctLogin"></param>
        /// <param name="fileContent"></param>
        /// <param name="contentLength"></param>
        /// <param name="fileExtension"></param>
        /// <param name="contactNumber"></param>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <param name="state"></param>
        /// <param name="zipCode"></param>
        /// <param name="country"></param>
        /// <param name="Picture"></param>
        /// <param name="showinSearch"></param>
        /// <param name="address2"></param>
        /// <param name="dob"></param>
        /// <returns></returns>
        public string MySettings(string memberId, string firstName, string lastName, string password,
            string secondaryMail, string recoveryMail, string facebookAcctLogin,
            string fileContent, int contentLength, string fileExtension,
            string contactNumber, string address, string city, string state, string zipCode, string country,
            byte[] Picture, bool showinSearch, string address2, string dob)
        {
            Logger.Info("MDA -> MySettings (Updating User's Profile) - Name: [" + firstName + " " + lastName +
                        "], MemberID: [" + memberId + "]");

            var folderPath = Utility.GetValueFromConfig("PhotoPath");
            var fileName = memberId;
            var guid = Utility.ConvertToGuid(memberId);

            var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == guid && m.IsDeleted != true);

            if (memberObj != null)
            {
                try
                {
                    #region Encrypt All String Data

                    memberObj.FirstName = CommonHelper.GetEncryptedData(firstName.Split(' ')[0]);

                    if (firstName.IndexOf(' ') > 0)
                        memberObj.LastName = CommonHelper.GetEncryptedData(firstName.Split(' ')[1]);

                    if (memberObj.Password != null && memberObj.Password == "")
                        memberObj.Password = CommonHelper.GetEncryptedData(CommonHelper.GetDecryptedData(password).Replace(" ", "+"));

                    if (!String.IsNullOrEmpty(secondaryMail))
                        memberObj.SecondaryEmail = CommonHelper.GetEncryptedData(secondaryMail);

                    if (!String.IsNullOrEmpty(recoveryMail))
                        memberObj.RecoveryEmail = CommonHelper.GetEncryptedData(recoveryMail);

                    if (contentLength > 0)
                        memberObj.Photo = Utility.UploadPhoto(folderPath, fileName, fileExtension, fileContent, contentLength);

                    if (!String.IsNullOrEmpty(facebookAcctLogin))
                        memberObj.FacebookAccountLogin = facebookAcctLogin;

                    if (!String.IsNullOrEmpty(address))
                        memberObj.Address = CommonHelper.GetEncryptedData(address);

                    if (!String.IsNullOrEmpty(address2))
                        memberObj.Address2 = CommonHelper.GetEncryptedData(address2);

                    if (!String.IsNullOrEmpty(city))
                        memberObj.City = CommonHelper.GetEncryptedData(city);

                    if (!String.IsNullOrEmpty(state))
                        memberObj.State = CommonHelper.GetEncryptedData(state);

                    if (!String.IsNullOrEmpty(country))
                        memberObj.Country = country;

                    if (!String.IsNullOrEmpty(zipCode))
                    {
                        memberObj.Zipcode = CommonHelper.GetEncryptedData(zipCode);

                        // Get state from ZIP via Google Maps API
                        var stateResult = CommonHelper.GetCityAndStateFromZip(zipCode.Trim());
                        var stateAbbrev = stateResult != null && stateResult.stateAbbrev != null ? stateResult.stateAbbrev : "";
                        var cityFromGoogle = stateResult != null && !String.IsNullOrEmpty(stateResult.city) ? stateResult.city : "";

                        if (!String.IsNullOrEmpty(stateAbbrev)) memberObj.State = CommonHelper.GetEncryptedData(stateAbbrev);
                        if (!String.IsNullOrEmpty(stateAbbrev)) memberObj.City = CommonHelper.GetEncryptedData(cityFromGoogle);
                    }

                    DateTime dob2;

                    if (!DateTime.TryParse(dob, out dob2))
                        Logger.Error("MDA -> MySettings - Invalid DOB Passed - MemberID: [" + memberId + "], DOB: [" + dob + "]");
                    else
                        memberObj.DateOfBirth = dob2;

                    #endregion Encrypt All String Data

                    memberObj.ShowInSearch = showinSearch;

                    if (Picture != null)
                    {
                        // make  image from bytes
                        string filename = HttpContext.Current.Server.MapPath("~/UploadedPhotos") + "/Photos/" +
                                          memberObj.MemberId.ToString() + ".png";

                        using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                        {
                            fs.Write(Picture, 0, (int)Picture.Length);
                        }

                        memberObj.Photo = Utility.GetValueFromConfig("PhotoUrl") + memberObj.MemberId + ".png";
                    }
                    else
                    {
                        // check if image is already there
                        if (memberObj.Photo == null)
                            memberObj.Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                    }

                    memberObj.DateModified = DateTime.Now;
                    int value = _dbContext.SaveChanges();
                    _dbContext.Entry(memberObj).Reload();

                    // Finally, check and save the Phone Number
                    if (!String.IsNullOrEmpty(contactNumber) &&
                        (String.IsNullOrEmpty(memberObj.ContactNumber) ||
                        CommonHelper.RemovePhoneNumberFormatting(memberObj.ContactNumber) != CommonHelper.RemovePhoneNumberFormatting(contactNumber)))
                    {
                        if (!IsPhoneNumberAlreadyRegistered(contactNumber))
                        {
                            memberObj.ContactNumber = contactNumber;
                            memberObj.IsVerifiedPhone = false;

                            value = _dbContext.SaveChanges();
                            _dbContext.Entry(memberObj).Reload();

                            #region Sending SMS Verificaion

                            try
                            {
                                var fname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                                var MessageBody = "Hi " + fname + ", Nooch here - just making sure this is your number. Please reply 'Go' to verify your phone number.";

                                var SMSresult = Utility.SendSMS(contactNumber, MessageBody);

                                Logger.Info("MDA -> MySettings - SMS Verification sent to [" + contactNumber + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> MySettings - SMS Verification NOT sent to [" +
                                             contactNumber + "], Exception: [" + ex + "]");
                            }

                            #endregion Sending SMS Verificaion
                        }
                        else
                            return "Phone Number already registered with Nooch";
                    }

                    return value > 0 ? "Your details have been updated successfully." : "Failure";
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> MySettings (Updating User's Profile) FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");

                    return ex.InnerException.Message;
                }
            }

            return "Member not found.";
        }


        public string ResetPin(string memberId, string oldPin, string newPin)
        {
            Logger.Info("MDA -> ResetPin - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var memOldPin = oldPin.Replace(" ", "+");
            var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.PinNumber == memOldPin);
            if (noochMember == null) return "Incorrect pin. Please check your current pin.";
            // Update user's PIN number
            noochMember.PinNumber = newPin.Replace(" ", "+");
            noochMember.DateModified = DateTime.Now;
            int r = _dbContext.SaveChanges();

            if (r > 0)
            {
                _dbContext.Entry(noochMember).Reload();
                return "Pin changed successfully.";
            }
            else
            {
                return "Incorrect pin. Please check your current pin.";
            }
        }


        /// <summary>
        /// To get member notification settings
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public MemberNotification GetMemberNotificationSettings(string memberId)
        {
            //Logger.LogDebugMessage("MDA -> GetMemberNotificationSettings Initiated - [MemberId: " + memberId + "]");

            Guid memId = Utility.ConvertToGuid(memberId);

            var memberNotifications = _dbContext.MemberNotifications.FirstOrDefault(m => m.MemberId == memId);

            return memberNotifications;
        }


        // to save email notification settings for the give user
        public string MemberEmailNotificationSettings(string notificationId, string memberId, bool? friendRequest,
            bool? inviteRequestAccept, bool transferSent, bool transferReceived, bool transferAttemptFailure,
            bool transferUnclaimed, bool bankToNoochRequested, bool bankToNoochCompleted, bool? noochToBankRequested,
            bool noochToBankCompleted, bool? inviteReminder, bool? lowBalance, bool? validationRemainder,
            bool? productUpdates, bool? newAndUpdate, bool? mobTransferReceived)
        {
            int i = 0;
            try
            {
                Logger.Info("MDA -> MemberEmailNotificationSettings - MemberId: [" + memberId + "]");

                var id = Utility.ConvertToGuid(notificationId);
                var memId = Utility.ConvertToGuid(memberId);

                i++;

                var memberNotifications =
                    _dbContext.MemberNotifications.FirstOrDefault(m => m.MemberId == memId);
                i++;
                if (id == Guid.Empty && memberNotifications == null)
                {
                    i++;
                    var memberNotification = new MemberNotification
                    {
                        NotificationId = Guid.NewGuid(),

                        MemberId = Utility.ConvertToGuid(memberId),

                        EmailFriendRequest = friendRequest,
                        EmailInviteRequestAccept = inviteRequestAccept,
                        EmailTransferSent = transferSent,
                        EmailTransferReceived = transferReceived,
                        EmailTransferAttemptFailure = transferAttemptFailure,
                        TransferUnclaimed = transferUnclaimed,
                        BankToNoochRequested = bankToNoochRequested,
                        BankToNoochCompleted = bankToNoochCompleted,
                        NoochToBankRequested = noochToBankRequested,
                        NoochToBankCompleted = noochToBankCompleted,
                        InviteReminder = inviteReminder,
                        LowBalance = lowBalance,
                        ValidationRemainder = validationRemainder,
                        ProductUpdates = productUpdates,
                        NewAndUpdate = newAndUpdate,
                        TransferReceived = mobTransferReceived,
                        DateCreated = DateTime.Now
                    };
                    i++;
                    _dbContext.MemberNotifications.Add(memberNotification);
                    return _dbContext.SaveChanges() > 0 ? "Success" : "Failure";
                }

                i++;

                memberNotifications.MemberId = Utility.ConvertToGuid(memberId);
                memberNotifications.EmailFriendRequest = friendRequest;
                memberNotifications.EmailInviteRequestAccept = inviteRequestAccept;
                memberNotifications.EmailTransferSent = transferSent;
                memberNotifications.EmailTransferReceived = transferReceived;
                memberNotifications.EmailTransferAttemptFailure = transferAttemptFailure;
                memberNotifications.TransferUnclaimed = transferUnclaimed;
                memberNotifications.BankToNoochRequested = bankToNoochRequested;
                memberNotifications.BankToNoochCompleted = bankToNoochCompleted;
                memberNotifications.NoochToBankRequested = noochToBankRequested;
                memberNotifications.NoochToBankCompleted = noochToBankCompleted;
                memberNotifications.InviteReminder = inviteReminder;
                memberNotifications.LowBalance = lowBalance;
                memberNotifications.ValidationRemainder = validationRemainder;
                memberNotifications.ProductUpdates = productUpdates;
                memberNotifications.NewAndUpdate = newAndUpdate;
                memberNotifications.TransferReceived = mobTransferReceived;
                memberNotifications.DateModified = DateTime.Now;


                DbContext dbc = CommonHelper.GetDbContextFromEntity(memberNotifications);

                return dbc.SaveChanges() > 0 ? "Success" : "Failure";
            }
            catch (Exception ex)
            {
                return ex.ToString() + i.ToString();
            }
        }


        public string MemberPushNotificationSettings(string notificationId, string memberId, bool friendRequest,
            bool inviteRequestAccept, bool transferReceived, bool transferAttemptFailure, bool noochToBank, bool bankToNooch)
        {
            try
            {
                Logger.Info("MDA -> MemberPushNotificationSettings - MemberId: [" + memberId + "]");

                var id = Utility.ConvertToGuid(notificationId);
                var memId = Utility.ConvertToGuid(memberId);

                var memberNotifications =
                    _dbContext.MemberNotifications.FirstOrDefault(m => m.MemberId == memId);

                if (id == Guid.Empty && memberNotifications == null)
                {
                    var memberNotification = new MemberNotification
                    {
                        NotificationId = Guid.NewGuid(),
                        MemberId = Utility.ConvertToGuid(memberId),
                        FriendRequest = friendRequest,
                        InviteRequestAccept = inviteRequestAccept,
                        TransferReceived = transferReceived,
                        TransferAttemptFailure = transferAttemptFailure,
                        NoochToBank = noochToBank,
                        BankToNooch = bankToNooch,
                        DateCreated = DateTime.Now
                    };
                    _dbContext.MemberNotifications.Add(memberNotification);

                    return _dbContext.SaveChanges() > 0 ? "Success" : "Failure";
                }

                memberNotifications.MemberId = Utility.ConvertToGuid(memberId);
                memberNotifications.FriendRequest = friendRequest;
                memberNotifications.InviteRequestAccept = inviteRequestAccept;
                memberNotifications.TransferReceived = transferReceived;
                memberNotifications.TransferAttemptFailure = transferAttemptFailure;
                memberNotifications.NoochToBank = noochToBank;
                memberNotifications.BankToNooch = bankToNooch;
                memberNotifications.DateModified = DateTime.Now;

                return _dbContext.SaveChanges() > 0 ? "Success" : "Failure";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        /// <summary>
        /// To update Member's Privacy settings
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="showInSearch"></param>
        /// <param name="allowSharing"></param>
        /// <param name="requireImmediately"></param>
        public string MemberPrivacySettings(string memberId, bool showInSearch, bool allowSharing, bool requireImmediately)
        {
            Logger.Info("MDA -> MemberPrivacySettings - MemberId: [" + memberId + "], Req Imm: [" + requireImmediately +
                        "], ShowInSearch: [" + showInSearch + "], Allow Sharing: [" + allowSharing + "]");

            var saveToDB = 0;
            var id = Utility.ConvertToGuid(memberId);

            var memberPrivacySettings = _dbContext.MemberPrivacySettings.FirstOrDefault(m => m.MemberId == id);

            if (memberPrivacySettings != null)
            {
                memberPrivacySettings.ShowInSearch = showInSearch;
                memberPrivacySettings.AllowSharing = allowSharing;
                memberPrivacySettings.RequireImmediately = requireImmediately;

                if (memberPrivacySettings.DateCreated == null)
                    memberPrivacySettings.DateCreated = DateTime.Now;

                memberPrivacySettings.DateModified = DateTime.Now;

                saveToDB = _dbContext.SaveChanges();

                #region Update Members Table

                try
                {
                    Member memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id);

                    if (memberObj != null)
                    {
                        memberObj.ShowInSearch = showInSearch;
                        memberObj.IsRequiredImmediatley = requireImmediately;
                        memberObj.DateModified = DateTime.Now;

                        saveToDB = _dbContext.SaveChanges();
                        _dbContext.Entry(memberObj).Reload();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> MemberPrivacySettings FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }

                #endregion Update Members Table
            }
            else
                Logger.Error("MDA -> MemberPrivacySettings FAILED - MemberPrivacySettings Record Not Found - MemberID: [" + memberId + "]");

            return saveToDB > 0 ? "Flag is updated successfully." : "Failure";
        }


        public string SetShowInSearch(string memberId, bool showInSearch)
        {
            Logger.Info("MDA -> SetShowInSearch - MemberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            #region Update Members Table

            var memberObj = _dbContext.Members.FirstOrDefault(m => m.MemberId == id);

            if (memberObj != null)
            {
                memberObj.ShowInSearch = showInSearch;
                _dbContext.SaveChanges();
                _dbContext.Entry(memberObj).Reload();
            }
            else
                return "Failure";

            #endregion Update Members Table

            #region Update MemberPrivacySettings Table

            var memberPrivacySettings = _dbContext.MemberPrivacySettings.FirstOrDefault(m => m.MemberId == id);

            if (memberPrivacySettings == null)
            {
                var privacySettings = new MemberPrivacySetting
                {
                    MemberId = id,
                    ShowInSearch = showInSearch,
                    DateCreated = DateTime.Now
                };

                _dbContext.MemberPrivacySettings.Add(privacySettings);
                int r = _dbContext.SaveChanges();
                if (r > 0)
                {
                    _dbContext.Entry(memberObj).Reload();
                    return "ShowInSearch flag is added successfully.";
                }

                return "Failure";
            }

            memberPrivacySettings.ShowInSearch = showInSearch;
            memberPrivacySettings.DateModified = DateTime.Now;

            int r2 = _dbContext.SaveChanges();

            if (r2 > 0)
            {
                _dbContext.Entry(memberPrivacySettings).Reload();
                return "ShowInSearch flag is updated successfully.";
            }
            else
                return "Failure";

            #endregion Update MemberPrivacySettings Table
        }


        public string MemberRegistration(byte[] Picture, string UserName, string FirstName, string LastName,
           string PinNumber, string Password, string SecondaryMail, string RecoveryMail, string UUID, string friendRequestId,
           string invitedFriendFacebookId, string facebookAccountLogin, string inviteCode, string sendEmail, string type, string phone, string address, string zip, string ssn, string dob)
        {
            // Check to make sure Username is not already taken
            if (CommonHelper.GetMemberNameByUserName(UserName) == null)
            {
                Logger.Info("MDA -> Member Registration Fired - Name: [" + FirstName + " " + LastName +
                            "], Email: [" + UserName + "]");

                var userNameLowerCase = UserName.Trim().ToLower();

                var noochRandomId = GetRandomNoochId();

                if (!String.IsNullOrEmpty(noochRandomId))
                {
                    #region Check Invitation/Referral Code

                    var inviteCodeObj = new InviteCode();

                    try
                    {
                        inviteCodeObj = _dbContext.InviteCodes.FirstOrDefault(m => m.code == inviteCode);

                        if (inviteCodeObj != null &&
                            inviteCodeObj.count >= inviteCodeObj.totalAllowed) //Removed (inviteCodeObj == null || inviteCodeObj.count >= inviteCodeObj.totalAllowed) this condition, b/c user can reg without Invite code -Surya 6-May-16
                        {
                            Logger.Info("MDA -> MemberRegistration Attempted but Code Not Found - Code: [" + inviteCode + "]");
                            return "Invite code used or does not exist.";
                        }
                        else if (inviteCodeObj != null)
                        {
                            try
                            {
                                inviteCodeObj.count++;
                                _dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> MemberRegistration - Attempted to update Invite Code Repository but got EXCEPTION: [" + ex.Message + "]");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> MemberRegistration - Attempted to lookup & update Invite Code Repository but got EXCEPTION (Outer): [" + ex.Message + "]");
                    }

                    #endregion Check Invitation/Referral Code


                    #region Parse Name

                    // CC (8/30/16): The new Ionic app is sending the entire Name in the 'FirstName' param.
                    //               Better to parse it here than on the mobile device.
                    if (!String.IsNullOrEmpty(FirstName) && String.IsNullOrEmpty(LastName))
                    {
                        string[] namearray = FirstName.Split(' ');
                        FirstName = namearray[0];

                        // Example Name Formats: Most Common: 1.) Charles Smith
                        //                       Possible Variations: 2.) Charles   3.) Charles H. Smith
                        //                       4.) CJ Smith   5.) C.J. Smith   6.)  Charles Andrew Thomas Smith

                        if (namearray.Length > 1)
                        {
                            if (namearray.Length == 2) // For regular First & Last name: Charles Smith
                                LastName = namearray[1];
                            else if (namearray.Length == 3) // For 3 names, could be a middle name or middle initial: Charles H. Smith or Charles Andrew Smith
                                LastName = namearray[2];
                            else // For more than 3 names (some people have 2 or more middle names)
                                LastName = namearray[namearray.Length - 1];
                        }
                    }

                    #endregion Parse Name


                    #region Create New Member Record In DB

                    var emailEncrypted = CommonHelper.GetEncryptedData(UserName);

                    var member = new Member
                    {
                        Nooch_ID = noochRandomId,
                        MemberId = Guid.NewGuid(),
                        UserName = emailEncrypted,
                        FirstName = CommonHelper.GetEncryptedData(FirstName),
                        LastName = CommonHelper.GetEncryptedData(LastName),
                        SecondaryEmail = emailEncrypted,
                        RecoveryEmail = emailEncrypted,
                        Password = Password.Replace(" ", "+"),
                        PinNumber = !String.IsNullOrEmpty(PinNumber) ? CommonHelper.GetEncryptedData(PinNumber.Replace(" ", "+")) : Utility.GetRandomPinNumber(),
                        Status = Constants.STATUS_REGISTERED,
                        Deposit = CommonHelper.GetEncryptedData("1"),
                        IsDeleted = false,
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now,
                        UserNameLowerCase = CommonHelper.GetEncryptedData(userNameLowerCase),
                        FacebookAccountLogin = CommonHelper.GetEncryptedData(facebookAccountLogin.ToLower()),
                        cipTag = "renter",
                        ShowInSearch = true,
                        Country = "USA",
                        IsVerifiedPhone = false,
                        Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png", //Setting defaul here, will update below if Picture data were passed
                        //   InviteCodeIdUsed = inviteCodeObj.InviteCodeId,
                        Type = !String.IsNullOrEmpty(type) ? type : "Personal",
                        IsOnline = true,
                        // CLIFF (8/12/15): The UDID1 will now be specifically for Synapse's Device Fingerprint requirement.
                        //                  It will NOT be for sending push notifications - we will use the 'DeviceToken' value for that, which cannot
                        //                  be set during Member Registration (if coming from the iOS app) b/c the user is not asked for permission for pushes until after signing up.
                        UDID1 = UUID,
                        IsVerifiedWithSynapse = false,
                    };

                    if (inviteCodeObj != null)
                        member.InviteCodeIdUsed = inviteCodeObj.InviteCodeId;

                    if (Picture != null)
                    {
                        // Make  image from bytes
                        string filename = HttpContext.Current.Server.MapPath("~/UploadedPhotos") + "/Photos/" +
                                          member.MemberId + ".png";
                        using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                        {
                            fs.Write(Picture, 0, (int)Picture.Length);
                        }
                        member.Photo = Utility.GetValueFromConfig("PhotoUrl") + member.MemberId + ".png";
                    }

                    // Save member details to table
                    int result = 0;

                    _dbContext.Members.Add(member);
                    result = _dbContext.SaveChanges();
                    try
                    {
                        setReferralCode(member.MemberId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> Member Registration - FAILED to set Referral Code for Member: [" + member.MemberId +
                                     "], Exception: [" + ex + "]");
                        //return "Exception " + ex.ToString();
                    }

                    #endregion Create New Member Record In DB


                    if (result > 0)
                    {
                        #region Create Auth Token

                        var tokenId = Guid.NewGuid();
                        var requestId = Guid.Empty;

                        if (friendRequestId != null)
                        {
                            requestId = Utility.ConvertToGuid(friendRequestId);
                        }
                        // Save the token details into authentication tokens table
                        var token = new AuthenticationToken
                        {
                            TokenId = tokenId,
                            MemberId = member.MemberId,
                            IsActivated = false,
                            DateGenerated = DateTime.Now,
                            FriendRequestId = requestId
                        };
                        // var tokensRepository = new Repository<AuthenticationTokens, NoochDataEntities>(noochConnection);
                        //bool status = tokensRepository.AddEntity(token) > 0;

                        _dbContext.AuthenticationTokens.Add(token);

                        int status = _dbContext.SaveChanges();

                        #endregion Create Auth Token

                        #region Set Notification Settings

                        // for member notification settings

                        var memberNotification = new MemberNotification
                        {
                            NotificationId = Guid.NewGuid(),

                            MemberId = member.MemberId,
                            FriendRequest = true,
                            InviteRequestAccept = true,
                            TransferSent = true,
                            TransferReceived = true,
                            TransferAttemptFailure = true,
                            NoochToBank = true,
                            BankToNooch = true,
                            EmailFriendRequest = true,
                            EmailInviteRequestAccept = true,
                            EmailTransferSent = true,
                            EmailTransferReceived = true,
                            EmailTransferAttemptFailure = true,
                            TransferUnclaimed = true,
                            BankToNoochRequested = true,
                            BankToNoochCompleted = true,
                            NoochToBankRequested = true,
                            NoochToBankCompleted = true,
                            InviteReminder = true,
                            LowBalance = true,
                            ValidationRemainder = true,
                            ProductUpdates = true,
                            NewAndUpdate = true,
                            DateCreated = DateTime.Now
                        };

                        //  memberNotificationsRepository.AddEntity(memberNotification);
                        _dbContext.MemberNotifications.Add(memberNotification);

                        #endregion Set Notification Settings

                        #region Set Privacy Settings

                        var memberPrivacySettings = new MemberPrivacySetting
                        {
                            MemberId = member.MemberId,
                            AllowSharing = true,
                            ShowInSearch = true,
                            DateCreated = DateTime.Now
                        };

                        _dbContext.MemberPrivacySettings.Add(memberPrivacySettings);
                        _dbContext.SaveChanges();

                        #endregion Set Privacy Settings

                        if (status > 0)
                        {
                            #region Send Registration Email

                            var fromAddress = Utility.GetValueFromConfig("welcomeMail");

                            if (sendEmail != "false") // Cliff (10/29/15): Adding this flag so I don't have to keep manually commenting out this block whenever I manually create a user for any reason
                            {
                                try
                                {
                                    // Send registration email to member with autogenerated token 
                                    var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                        "Nooch/Activation?tokenId=" + tokenId);

                                    var tokens = new Dictionary<string, string>
                                            {
                                                {
                                                    Constants.PLACEHOLDER_FIRST_NAME,
                                                    CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName))
                                                },
                                                {
                                                    Constants.PLACEHOLDER_LAST_NAME,
                                                    CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName))
                                                },
                                                {Constants.PLACEHOLDER_OTHER_LINK, link}
                                            };

                                    // Temp code to prevent sending the registration email here... was demoing and added code in Landlord App
                                    // to also create a regular Nooch user here... can most likely get rid of this condition

                                    Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                                        fromAddress, UserName, null, "Confirm your email on Nooch", link,
                                        tokens, null, null, null);

                                    Logger.Info("MDA -> MemberRegistration email sent to [" + UserName + "] successfully");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> MemberRegistration email NOT sent to [" + UserName + "], [Exception: " + ex + "]");
                                }
                            }
                            else
                                Logger.Info("MDA -> MemberRegistration email NOT sent to [" + UserName + "] because 'sendEmail' flag was: [" + sendEmail + "]");

                            #endregion Send Registration Email


                            #region Send Email To Referrer (If Applicable)

                            if (inviteCode.ToLower() != "nocode")
                            {
                                try
                                {
                                    // Sending email to user who invited this user (Based on the invite code provided during registration)
                                    string fullName = CommonHelper.UppercaseFirst(FirstName) + " " + CommonHelper.UppercaseFirst(LastName);

                                    SendEmailToInvitor(inviteCodeObj.InviteCodeId, userNameLowerCase, fullName);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> MemberRegistration - Exception trying to send Email To Referrer - [Exception: " + ex + "]");
                                }
                            }

                            #endregion Send Email To Referrer (If Applicable)


                            #region Notifying New User's FB Friends Already On Nooch (If Applicable)

                            // CLIFF ADDED: 1/1/15
                            if (!String.IsNullOrEmpty(facebookAccountLogin) && facebookAccountLogin.Length > 2)
                            {
                                // Check the user's FB ID using FB's Graph API to see if any friends also use Nooch
                                try
                                {
                                    WebClient wc = new WebClient();

                                    string NoochFbAppAccessToken = Utility.GetValueFromConfig("NoochFBAppAccessToken");
                                    // CAAC0VZAIiwsEBAEceki7EWz7UryKqsrtZC9cP0KDptv9MlZCCHdL0rU6nXpYJGfL6XoCDRYntjGN1NEiNypIYyeZBBkPlSiFwUeTZBXDqRXU1wiF5ggqrHLUxPANyAz8RGqZCkZAwxa3f4eqjjuL0EipDeOjxNQatTWU94inMjYkNwwOxomyYZBy8KoZCYxNYtGyiyeZBWT6ucTnbcFLBVEG7K

                                    // CLIFF (8/6/15): THIS CODE IS NOT WORKING, PROBABLY JUST OUT OF DATE WITH FB'S API
                                    string urlString = "https://graph.facebook.com/v2.5/" + facebookAccountLogin +
                                                       "/friends?key=value&access_token=" + NoochFbAppAccessToken;

                                    string fbGraphResultString = wc.DownloadString(urlString);

                                    FBResponseClass fbGraphResults = JsonConvert.DeserializeObject<FBResponseClass>(fbGraphResultString);

                                    // If FB returned data
                                    if (fbGraphResults != null)
                                    {
                                        #region If FB returned Data

                                        if (fbGraphResults != null)
                                        {
                                            // The Facebook "data" parameter will contain an array of FB Friends who also use Nooch with their 'name' and 'id'
                                            // Now we need to lookup the UserName (email) and send an email to each of those users

                                            // Create an array of FB User objects from the "data" array returned by FB Graph API
                                            FBMemberDataClass[] FbFriends = fbGraphResults.data;

                                            foreach (FBMemberDataClass FbUser in FbFriends)
                                            {
                                                if (!String.IsNullOrEmpty(FbUser.id))
                                                {
                                                    // 1. Using the FB ID, lookup the user's UserName (email address)

                                                    //   var memberSpec = new Specification<Members>();
                                                    var memberSpec = new Member();
                                                    var encryptedFBId = CommonHelper.GetEncryptedData(FbUser.id);

                                                    //memberSpec.Predicate =
                                                    //        memberTemp => !String.IsNullOrEmpty(memberTemp.FacebookAccountLogin) &&
                                                    //                       memberTemp.FacebookAccountLogin.Equals(encryptedFBId) &&
                                                    //                       memberTemp.IsDeleted == false;

                                                    //  membersRepository = new Repository<Members, NoochDataEntities>(noochConnection);
                                                    //  var noochMember = membersRepository.SelectAll(memberSpec).FirstOrDefault();


                                                    var noochMember = _dbContext.Members.FirstOrDefault(memberTemp => !String.IsNullOrEmpty(memberTemp.FacebookAccountLogin) &&
                                                                           memberTemp.FacebookAccountLogin.Equals(encryptedFBId) &&
                                                                           memberTemp.IsDeleted == false);

                                                    if (noochMember != null)
                                                    {
                                                        // 2. Send email to user using the UserName
                                                        var notifSettings = GetMemberNotificationSettings(noochMember.MemberId.ToString());

                                                        if (notifSettings != null)
                                                        {
                                                            string existingUserFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName));
                                                            string existingUserLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName));
                                                            string newUserFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName));
                                                            string newUserLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName));

                                                            string newUserPictureUrl = "";

                                                            if (member.Photo != null && member.Photo != "")
                                                            {
                                                                newUserPictureUrl = member.Photo.ToString();
                                                                //string requesterPic = "https://www.noochme.com/noochservice/UploadedPhotos/Photos/" + Transaction.Members.MemberId.ToString() + ".png";

                                                                string requesterPic = (!String.IsNullOrEmpty(member.Photo) && member.Photo.Length > 20)
                                                                                        ? member.Photo.ToString()
                                                                                        : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                                                            }

                                                            var tokensForFBEmailTemplate = new Dictionary<string, string>
                                                                    {
                                                                        {Constants.PLACEHOLDER_FIRST_NAME, existingUserFirstName},
                                                                        {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, newUserFirstName},
                                                                        {Constants.PLACEHOLDER_FRIEND_LAST_NAME, newUserLastName},
                                                                        {Constants.PLACEHOLDER_OTHER_LINK, newUserPictureUrl}
                                                                    };

                                                            fromAddress = Utility.GetValueFromConfig("adminMail");
                                                            var toAddress = CommonHelper.GetDecryptedData(noochMember.UserName);

                                                            try
                                                            {
                                                                Utility.SendEmail("FBFriendJoinedNoochEmailTemplate", fromAddress, toAddress, null, "Your friend " +
                                                                    newUserFirstName + " " + newUserLastName + " just joined you on Nooch",
                                                                    null, tokensForFBEmailTemplate, null, null, null);

                                                                Logger.Info("MDA -> MemberRegistration: FB Friend email sent to [" +
                                                                                       toAddress + "] successfully - [New User Email: " + UserName + "]");
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Logger.Info("MDA -> MemberRegistration: FB Friend email NOT sent to [" +
                                                                                       toAddress + "], [Exception: " + ex + "]");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        #endregion If FB returned Data
                                    }
                                    else
                                    {
                                        Logger.Error("MDA -> MemberRegistration: FB Graph API returned NULL for [FB ID: " +
                                                                facebookAccountLogin + "], [New User Email: " + UserName + "]");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Info("MDA -> MemberRegistration - Problem with FB Graph API [FB ID: " +
                                        facebookAccountLogin + "], [New User Email: " + UserName + "],  [Exception: " + ex + "]");
                                }
                            }

                            #endregion Notifying New User's FB Friends Already On Nooch (If Applicable)

                            Logger.Info("MDA -> MemberRegistration - New Member with userName [" + UserName + "] registered successfully");

                            return "Thanks for registering! Check your email to complete activation.";
                        }
                        else
                        {
                            Logger.Error("MDA -> MemberRegistration - Unable to update DB with new Member's info - " +
                                         "Name: [" + FirstName + " " + LastName +
                                         "], Email: [" + UserName + "]");

                            return "Failed to save new Member in DB";
                        }
                    }
                }
            }
            else
            {
                Logger.Error("MDA -> MemberRegistration - Duplicate random Nooch ID was generating.");
                return "Duplicate random Nooch ID was generating";
            }

            return "You are already a nooch member.";
        }


        public string RemoveSynapseBankAccount(string memberId, int bankId)
        {
            Logger.Info("MDA -> RemoveSynapseBankAccount - [MemberId: " + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);
            using (var noochConnection = new NOOCHEntities())
            {
                var bankAccountsFound =
                    noochConnection.SynapseBanksOfMembers.FirstOrDefault(
                        m => m.MemberId == id && m.Id == bankId);

                if (bankAccountsFound != null)
                {
                    bankAccountsFound.IsDefault = false;
                    noochConnection.SaveChanges();
                    SynapseDetailsClass sdc = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(memberId);

                    if (!sdc.wereUserDetailsFound) return "Bank account deleted successfully";

                    if (!String.IsNullOrEmpty(sdc.UserDetails.access_token) &&
                        !String.IsNullOrEmpty(sdc.UserDetails.user_fingerprints))
                    {
                        CommonHelper.RemoveBankNodeFromSynapse(sdc.UserDetails.access_token,
                            sdc.UserDetails.user_fingerprints, CommonHelper.GetDecryptedData(bankAccountsFound.oid), memberId);
                    }

                    return "Bank account deleted successfully";
                }
                else
                {
                    return "No active bank account found for this user";
                }
            }
        }


        public string LogOut(string memberId)
        {
            if (LogoutRequest(memberId) == "Success")
            {
                return "Success";
            }
            else
            {
                return string.Empty;
            }
        }


        public string LogoutRequest(string memberId)
        {
            Logger.Info("MDA -> LogoutRequest Initiated - [MemberId: " + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            using (var noochConnection = new NOOCHEntities())
            {
                var memberEntity = noochConnection.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                if (memberEntity != null)
                {
                    memberEntity.InvalidLoginTime = null;
                    memberEntity.InvalidLoginAttemptCount = null;
                    memberEntity.AccessToken = null;
                    memberEntity.IsOnline = false;

                    noochConnection.SaveChanges();
                    return "Success";
                }
                else
                {
                    return "Invalid username.";
                }
            }
        }


        // to create new user..from landing page..create his pasword here
        // CC (5/29/16): Don't think this method is being used anywhere...
        public string CreateNonNoochUserPassword(string TransId, string password)
        {
            Logger.Info("MemberDataAccess - CreateNonNoochUserPassword[ TransId:" + TransId + "].");

            try
            {
                using (var noochConnection = new NOOCHEntities())
                {
                    Guid tid = Utility.ConvertToGuid(TransId);
                    password = CommonHelper.GetEncryptedData(password);
                    string pinNumber = Utility.GetRandomPinNumber();
                    pinNumber = CommonHelper.GetEncryptedData(pinNumber);

                    var transDetail =
                        noochConnection.Transactions.FirstOrDefault(
                            t => t.TransactionId == tid && (t.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" || t.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="));

                    if (transDetail != null)
                    {
                        var memdetails = noochConnection.Members.FirstOrDefault(m => m.UserName == transDetail.InvitationSentTo && m.IsDeleted == false);

                        if (memdetails == null)
                        {
                            #region TransactionFoundCode

                            string noochRandomId = GetRandomNoochId();

                            var member = new Member
                            {
                                Nooch_ID = noochRandomId,
                                MemberId = Guid.NewGuid(),
                                UserName = CommonHelper.GetEncryptedData(transDetail.InvitationSentTo),
                                UserNameLowerCase = CommonHelper.GetEncryptedData(transDetail.InvitationSentTo.ToLower()),
                                FirstName = CommonHelper.GetEncryptedData(""),
                                LastName = CommonHelper.GetEncryptedData(""),
                                SecondaryEmail = CommonHelper.GetEncryptedData(transDetail.InvitationSentTo.ToLower()),
                                RecoveryEmail = CommonHelper.GetEncryptedData(transDetail.InvitationSentTo.ToLower()),
                                Password = password.Replace(" ", "+"),
                                PinNumber = pinNumber.Replace(" ", "+"),
                                Status = Constants.STATUS_REGISTERED,
                                IsDeleted = false,
                                DateCreated = DateTime.Now,
                                DateModified = DateTime.Now,
                                // FacebookAccountLogin = facebookAccountLogin,
                                // InviteCodeIdUsed = inviteCodeObj.InviteCodeId,
                                Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png",
                                Type = "Personal - Browser",
                                IsVerifiedWithSynapse = false,
                                AdminNotes = "Created via landing page"
                            };

                            int result = 0;

                            try
                            {
                                noochConnection.Members.Add(member);
                                result = noochConnection.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return "Exception " + ex.ToString();
                            }

                            var tokenId = Guid.NewGuid();
                            if (result > 0)
                            {
                                //send registration email to member with autogenerated token 
                                var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                    "/Nooch/Activation?tokenId=" + tokenId);

                                var toEmailAddress = CommonHelper.GetDecryptedData(transDetail.InvitationSentTo.Trim());
                                var fromAddress = Utility.GetValueFromConfig("welcomeMail");

                                var tokens = new Dictionary<string, string>
                                {
                                    {
                                        Constants.PLACEHOLDER_FIRST_NAME,
                                        CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName))
                                    },
                                    {
                                        Constants.PLACEHOLDER_LAST_NAME,
                                        CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.LastName))
                                    },
                                    {Constants.PLACEHOLDER_OTHER_LINK, link}
                                };
                                try
                                {
                                    Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                                                      fromAddress, toEmailAddress, null, "Confirm your email on Nooch", link,
                                                      tokens, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserPassword - Member activation mail NOT sent to [" + toEmailAddress + "] Exception: [" + ex + "]");
                                }

                                // emailing temp pin number
                                var tokens2 = new Dictionary<string, string>
                                {
                                    {Constants.PLACEHOLDER_FIRST_NAME, toEmailAddress},
                                    {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                                };
                                try
                                {
                                    Utility.SendEmail("pinSetForNewUser", fromAddress,
                                                       toEmailAddress, null, "Your temporary Nooch Pin Number", null,
                                                       tokens2, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> Member temp PIN mail NOT sent to [" + toEmailAddress + "], Exception: [" + ex + "]");
                                }

                                var requestId = Guid.Empty;

                                var token = new AuthenticationToken
                                {
                                    TokenId = tokenId,
                                    MemberId = member.MemberId,
                                    IsActivated = false,
                                    DateGenerated = DateTime.Now,
                                    FriendRequestId = requestId
                                };

                                noochConnection.AuthenticationTokens.Add(token);
                                bool status = noochConnection.SaveChanges() > 0;

                                // for member notification settings
                                var memberNotification = new MemberNotification
                                {
                                    NotificationId = Guid.NewGuid(),
                                    MemberId = member.MemberId,
                                    FriendRequest = true,
                                    InviteRequestAccept = true,
                                    TransferSent = true,
                                    TransferReceived = true,
                                    TransferAttemptFailure = true,
                                    NoochToBank = true,
                                    BankToNooch = true,
                                    EmailFriendRequest = true,
                                    EmailInviteRequestAccept = true,
                                    EmailTransferSent = true,
                                    EmailTransferReceived = true,
                                    EmailTransferAttemptFailure = true,
                                    TransferUnclaimed = true,
                                    BankToNoochRequested = true,
                                    BankToNoochCompleted = true,
                                    NoochToBankRequested = true,
                                    NoochToBankCompleted = true,
                                    InviteReminder = true,
                                    LowBalance = true,
                                    ValidationRemainder = true,
                                    ProductUpdates = true,
                                    NewAndUpdate = true,
                                    DateCreated = DateTime.Now
                                };
                                noochConnection.MemberNotifications.Add(memberNotification);
                                noochConnection.SaveChanges();

                                var memberPrivacySettings = new MemberPrivacySetting
                                {
                                    MemberId = member.MemberId,
                                    AllowSharing = true,
                                    ShowInSearch = true,
                                    DateCreated = DateTime.Now
                                };
                                noochConnection.MemberPrivacySettings.Add(memberPrivacySettings);
                                noochConnection.SaveChanges();

                                if (status)
                                {
                                    return "Thanks for registering! Check your email to complete activation.";
                                }
                                else
                                {
                                    return "Password creation failed.";
                                }
                            }
                            else
                            {
                                return "Error Saving Member record.";
                            }

                            #endregion
                        }
                        else
                        {
                            return "User already exists";
                        }
                    }
                    else
                    {
                        return "No TransId found";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failure " + ex.ToString();
            }
        }


        public string SetAutoPayStatusForTenant(bool statustoSet, string tenantId)
        {
            try
            {
                Logger.Info("MDA -> SetAutoPayStatusForTenant - TenantId: [" + tenantId + "]");

                using (var noochConnection = new NOOCHEntities())
                {
                    var id = Utility.ConvertToGuid(tenantId);

                    var noochMember = noochConnection.Tenants.FirstOrDefault(t => t.TenantId == id && t.IsDeleted == false);

                    if (noochMember != null)
                    {
                        noochMember.IsAutopayOn = statustoSet;
                        noochConnection.SaveChanges();

                        if (statustoSet == true) return "Autopay turned ON successfully.";
                        else return "Autopay turned OFF successfully.";
                    }
                    else return "No such tenant found.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MemberDataAccess - Operation:SetAutoPayStatusForTenant[ tenantId:" + tenantId + "]. Error reason --> [ " + ex.ToString() + " ] ");
                return "Server error.";
            }
        }




    }

}