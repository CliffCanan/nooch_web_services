using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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


        //public string LoginRequest(string userName, string password, Boolean rememberMeEnabled, decimal lat, decimal lng, string udid, string devicetoken)
        //{
        //    Logger.Info("MDA -> LoginRequest Initiated - [UserName: " + userName + "], [UDID: " + udid + "]");

        //    var userEmail = userName;
        //    var userNameLowerCase = CommonHelper.GetEncryptedData(userName.ToLower());
        //    userName = CommonHelper.GetEncryptedData(userName);

        //    using (var noochConnection = new NoochDataEntities())
        //    {
        //        var memberSpecification = new Specification<Members>
        //        {
        //            Predicate = memberTemp =>
        //                            memberTemp.UserNameLowerCase.Equals(userNameLowerCase) &&
        //                            memberTemp.IsDeleted == false
        //        };

        //        var membersRepository = new Repository<Members, NoochDataEntities>(noochConnection);
        //        var memberEntity =
        //            dbContext.Members.FirstOrDefault(
        //                m => m.IsDeleted == false && m.UserNameLowerCase == userNameLowerCase);

        //        if (memberEntity != null)
        //        {
        //            var memberNotifications = GetMemberNotificationSettingsByUserName(userEmail);

        //            if (memberEntity.Status == "Temporarily_Blocked")
        //            {
        //                Logger.Info("MDA -> LoginRequest FAILED - User is Already TEMPORARILY_BLOCKED - UserName: [" + userName + "]");
        //                return "Temporarily_Blocked";
        //            }
        //            else if (memberEntity.Status == "Suspended")
        //            {
        //                Logger.Info("MDA -> LoginRequest FAILED - User is Already SUSPENDED - UserName: [" + userName + "]");
        //                return "Suspended";
        //            }

        //            else if (memberEntity.Status == "Active" ||
        //                     memberEntity.Status == "Registered" ||
        //                     memberEntity.Status == "NonRegistered" ||
        //                     memberEntity.Type == "Personal - Browser")
        //            {
        //                #region

        //                #region Check If User Is Already Logged In

        //                // Check if user already logged in or not.  If yes, then send Auto Logout email
        //                if (!String.IsNullOrEmpty(memberEntity.AccessToken) &&
        //                    !String.IsNullOrEmpty(memberEntity.UDID1) &&
        //                    memberEntity.IsOnline == true &&
        //                    memberEntity.UDID1.ToLower() != udid.ToLower())
        //                {
        //                    Logger.Info("MDA -> LoginRequest - Sending Automatic Logout Notification - [UserName: " + userEmail +
        //                                           "], [UDID: " + udid +
        //                                           "], [AccessToken: " + memberEntity.AccessToken + "]");

        //                    var fromAddress = Utility.GetValueFromConfig("adminMail");
        //                    var toAddress = userEmail;
        //                    var userFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName));

        //                    string msg = "Hi,\n\nYou have been automatically logged out from your Nooch account because you signed in from a new device.\n" +
        //                                 "If this is a mistake and you feel your account may be compromised, please contact support@nooch.com immediately.  - Team Nooch";

        //                    try
        //                    {
        //                        UtilityDataAccess.SendEmail("", MailPriority.High, fromAddress, toAddress, null,
        //                                                    "Nooch Automatic Logout", null, null, null, null, msg);

        //                        Logger.Info("MDA -> LoginRequest - Automatic Log Out Email sent to [" + toAddress + "] successfully.");

        //                        // Checking if phone exists and isVerified before sending SMS to user
        //                        if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
        //                        {
        //                            try
        //                            {
        //                                //msg = "Hi, You were automatically logged out from your Nooch account b/c you signed in from another device. " +
        //                                // "If this is a mistake, contact support@nooch.com immediately. - Nooch";
        //                                //string result = UtilityDataAccess.SendSMS(memberEntity.ContactNumber, msg, memberEntity.AccessToken, memberEntity.MemberId.ToString());

        //                                //Logger.Info("MDA -> LoginRequest - Automatic Log Out SMS sent to [" + memberEntity.ContactNumber + "] successfully. [SendSMS Result: " + result + "]");
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                Logger.LogErrorMessage("MDA -> LoginRequest - Automatic Log Out SMS NOT sent to [" + memberEntity.ContactNumber + "], " +
        //                                                       "Exception: [" + ex.Message + "]");
        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.LogErrorMessage("MDA -> LoginRequest - Automatic Log Out email NOT sent to [" + toAddress + "]. Problem occured in sending email.");
        //                    }

        //                }

        //                #endregion Check If User Is Already Logged In

        //                // Update UDID of device from which the user has logged in.
        //                if (!String.IsNullOrEmpty(udid))
        //                {
        //                    memberEntity.UDID1 = udid;
        //                }
        //                if (!String.IsNullOrEmpty(devicetoken))
        //                {
        //                    memberEntity.DeviceToken = devicetoken;
        //                }

        //                memberEntity.LastLocationLat = lat;
        //                memberEntity.LastLocationLng = lng;
        //                memberEntity.IsOnline = true;

        //                var currentTimeMinus24Hours = DateTime.Now.AddHours(-24);
        //                int loginRetryCountInDb = memberEntity.InvalidLoginAttemptCount.Equals(null)
        //                                          ? 0
        //                                          : memberEntity.InvalidLoginAttemptCount.Value;

        //                // Check (FPTime || InvalidLoginAttemptTime) > CurrentTime - 24 hrs { if true, delete past records and insert new}                    
        //                bool isInvalidLoginTimeOver = new InvalidAttemptDurationSpecification().IsSatisfiedBy(memberEntity.InvalidLoginTime,
        //                                                                                                      currentTimeMinus24Hours);

        //                if (isInvalidLoginTimeOver)
        //                {
        //                    ChangeStatus(memberEntity, rememberMeEnabled);

        //                    //Reset attempt count
        //                    memberEntity.InvalidLoginTime = null;
        //                    memberEntity.InvalidLoginAttemptCount = null;
        //                    membersRepository.UpdateEntity(memberEntity);

        //                    loginRetryCountInDb = memberEntity.InvalidLoginAttemptCount.Equals(null)
        //                                          ? 0
        //                                          : memberEntity.InvalidLoginAttemptCount.Value;

        //                    if (!memberEntity.Password.Equals(password.Replace(" ", "+")))
        //                    {
        //                        return IncreaseInvalidLoginAttemptCount(membersRepository, memberEntity, loginRetryCountInDb);
        //                    }
        //                }

        //                if (loginRetryCountInDb < 4 && memberEntity.Password.Equals(password.Replace(" ", "+")))
        //                {
        //                    //Reset attempt count
        //                    memberEntity.InvalidLoginTime = null;
        //                    memberEntity.InvalidLoginAttemptCount = null;
        //                    memberEntity.InvalidPinAttemptCount = null;
        //                    memberEntity.InvalidPinAttemptTime = null;

        //                    memberEntity.Status = Constants.STATUS_ACTIVE;

        //                    membersRepository.UpdateEntity(memberEntity);

        //                    return "Success"; // active nooch member  
        //                }

        //                else if (memberEntity.InvalidLoginAttemptCount == null ||
        //                         memberEntity.InvalidLoginAttemptCount == 0)
        //                {
        //                    // This is the first invalid try
        //                    Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - 1st Invalid Attempt - UserName: [" + userName + "]");

        //                    return IncreaseInvalidLoginAttemptCount(membersRepository, memberEntity, loginRetryCountInDb);
        //                }

        //                else if (loginRetryCountInDb == 4)
        //                {
        //                    // Already Suspended
        //                    Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - User Already Suspended - UserName: [" + userName + "]");

        //                    return String.Concat("Your account has been temporarily blocked.  You can login only after 24 hours from this time: ",
        //                                         memberEntity.InvalidLoginTime);
        //                }

        //                else if (loginRetryCountInDb == 3)
        //                {
        //                    // This is 4th try, so suspend the member
        //                    Logger.Info("MDA -> LoginRequest FAILED - User's PW was incorrect - 3rd Invalid Attempt, now suspending user - UserName: [" + userName + "]");

        //                    memberEntity.InvalidLoginTime = DateTime.Now;
        //                    memberEntity.InvalidLoginAttemptCount = loginRetryCountInDb + 1;
        //                    memberEntity.Status = Constants.STATUS_TEMPORARILY_BLOCKED;
        //                    membersRepository.UpdateEntity(memberEntity);

        //                    // email to user after 3 invalid login attemt
        //                    #region SendingEmailToUser

        //                    var tokens = new Dictionary<string, string>
        //                    {
        //                        {
        //                            Constants.PLACEHOLDER_FIRST_NAME,
        //                            CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName))
        //                        }
        //                    };

        //                    try
        //                    {
        //                        var fromAddress = Utility.GetValueFromConfig("adminMail");
        //                        string emailAddress = CommonHelper.GetDecryptedData(memberEntity.UserName);
        //                        Logger.Info(
        //                            "SupendMember - Attempt to send mail for Supend Member[ memberId:" +
        //                            memberEntity.MemberId + "].");
        //                        UtilityDataAccess.SendEmail("userSuspended", MailPriority.High, fromAddress,
        //                            emailAddress, null, "Your Nooch account has been suspended", null, tokens, null,
        //                            null, null);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        Logger.Info("SupendMember - Supend Member status email not send to [" +
        //                                               memberEntity.MemberId +
        //                                               "]. Problem occured in sending Supend Member status mail. ");
        //                    }

        //                    #endregion

        //                    return String.Concat(
        //                            "Your account has been temporarily blocked.  You can login only after 24 hours from this time: ",
        //                            memberEntity.InvalidLoginTime);
        //                }

        //                return IncreaseInvalidLoginAttemptCount(membersRepository, memberEntity, loginRetryCountInDb);

        //                #endregion
        //            }
        //            else
        //            {
        //                return "Invalid user id or password.";
        //            }
        //        }
        //        else
        //        {
        //            return "Invalid user id or password.";
        //        }
        //    }
        //}

        #region Malkit sir code, the code in common helper (difference in just name in 'Ip' P is Caps there) 
        //public string UpdateMemberIpAddressAndDeviceId(string MemberId, string IP, string DeviceId)
        //{
        //    if (String.IsNullOrEmpty(MemberId))
        //    {
        //        return "MemberId not supplied.";
        //    }

        //    Guid memId = Utility.ConvertToGuid(MemberId);


        //    bool ipSavedSuccessfully = false;
        //    bool udidIdSavedSuccessfully = false;

        //    #region Save IP Address

        //    if (!String.IsNullOrEmpty(IP))
        //    {
        //        try
        //        {

        //            var ipAddressesFound = _dbContext.MembersIPAddresses.Where(m => m.MemberId == memId).ToList();

        //            if (ipAddressesFound.Count() > 5)
        //            {
        //                // If there are already 5 entries, update the one added first (the oldest)
        //                var lastIpFound = (from c in _dbContext.MembersIPAddresses where c.MemberId == memId select c)
        //                                  .OrderBy(m => m.ModifiedOn)
        //                                  .Take(1)
        //                                  .FirstOrDefault();

        //                lastIpFound.ModifiedOn = DateTime.Now;
        //                lastIpFound.Ip = IP;



        //                int a = _dbContext.SaveChanges();

        //                if (a > 0)
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (1) - [MemberId: " + MemberId + "]");
        //                    ipSavedSuccessfully = true;
        //                }
        //                else
        //                {
        //                    Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - [MemberId: " + MemberId + "]");
        //                }
        //            }
        //            else
        //            {
        //                // Otherwise, make a new entry
        //                MembersIPAddress mip = new MembersIPAddress();
        //                mip.MemberId = memId;
        //                mip.ModifiedOn = DateTime.Now;
        //                mip.Ip = IP;
        //                _dbContext.MembersIPAddresses.Add(mip);
        //                int b = _dbContext.SaveChanges();

        //                if (b > 0)
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (2) - [MemberId: " + MemberId + "]");
        //                    ipSavedSuccessfully = true;
        //                }
        //                else
        //                {
        //                    Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - [MemberId: " + MemberId + "]");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - [Exception: " + ex + "]");
        //        }
        //    }
        //    else
        //    {
        //        Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - [MemberId: " + MemberId + "]");
        //    }

        //    #endregion Save IP Address


        //    #region Save Device ID

        //    if (!String.IsNullOrEmpty(DeviceId))
        //    {
        //        try
        //        {
        //            // CLIFF (8/12/15): This "Device ID" will be stored in Nooch's DB as "UDID1" and is specifically for Synapse's "Fingerprint" requirement...
        //            //                  NOT for push notifications, which should use the "DeviceToken" in Nooch's DB.  (Confusing, but they are different values)

        //            var member = _dbContext.Members.FirstOrDefault(m => m.MemberId == memId);

        //            if (member != null)
        //            {
        //                member.UDID1 = DeviceId;
        //                member.DateModified = DateTime.Now;
        //            }
        //            int c = _dbContext.SaveChanges();

        //            if (c > 0)
        //            {
        //                Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving Device ID - [MemberId: " + MemberId + "]");
        //                udidIdSavedSuccessfully = true;
        //            }
        //            else
        //            {
        //                Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - [MemberId: " + MemberId + "]");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - [Exception: " + ex + "]");
        //        }
        //    }
        //    else
        //    {
        //        Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - [MemberId: " + MemberId + "]");
        //    }

        //    #endregion Save Device ID

        //    if (ipSavedSuccessfully && udidIdSavedSuccessfully)
        //    {
        //        return "Both IP and DeviceID saved successfully.";
        //    }
        //    else if (ipSavedSuccessfully)
        //    {
        //        return "Only IP address saved successfully, not DeviceID.";
        //    }
        //    else if (udidIdSavedSuccessfully)
        //    {
        //        return "Only DeviceID saved successfully, not IP Address.";
        //    }

        //    return "Neither IP address nor DeviceID were saved.";

        //}
        #endregion
        
        public string getReferralCode(String memberId)
        {
            Logger.Info("MDA -> getReferralCode Initiated - [MemberId: " + memberId + "]");

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
                    {
                        return inviteMember.code;
                    }
                    return "";
                }
                //No referal code
                return "";
            }
            return "Invalid";
        }
        public string setReferralCode(Guid memberId)
        {
            Logger.Info("MDA -> setReferralCode - [MemberID: " + memberId + "]");


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
            Logger.Info("MDA -> MemberActivation Initiated - TokenID: [" + tokenId + "]");


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
                                    {
                                        Logger.Info("MDA -> MemberActivation - Saved changes to TENANT table successfully - " +
                                                               "MemberID: [" + memGuid + "]");
                                    }
                                    else
                                    {
                                        Logger.Error("MDA -> MemberActivation - FAILED to save changes to TENANT table - " +
                                                               "MemberID: [" + memGuid + "]");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> MemberActivation EXCEPTION on checking if this user is a TENANT - " +
                                                       "MemberID: [" + memGuid + "], [Exception: " + ex + "]");
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
            Logger.Info("MDA -> GetMemberIds Initiated - phoneEmailList is: [" + phoneEmailListDto.phoneEmailList + "]");


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
                return null;


            }

        }

        public string GetMemberStats(string MemberId, string query)
        {
            try
            {
                return _dbContext.GetReportsForMember(MemberId, query).SingleOrDefault();
            }
            catch
            {
            }
            return "";
        }


        public List<Member> getInvitedMemberList(string memberId)
        {
            Logger.Info("MDA -> getInvitedMemberList - memberId: [" + memberId + "]");


            var id = Utility.ConvertToGuid(memberId);
            //Get the member details

            var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);
            if (noochMember != null)
            {
                _dbContext.Entry(noochMember).Reload();
                Guid n = Utility.ConvertToGuid(noochMember.InviteCodeId.ToString());

                var allnoochMember =
                    _dbContext.Members.Where(m => m.InviteCodeId == noochMember.InviteCodeId).ToList();
                return allnoochMember;
            }
            else
            {
                return null;
            }

        }


        public string SaveMemberDeviceToken(string MemberId, string DeviceToken)
        {
            if (DeviceToken.Length < 10)
            {
                return "Invalid Device Token passed - too short.";
            }

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
            {
                return "Member ID not found or Member status deleted.";
            }
        }

        private static void ChangeStatus(Member member, Boolean rememberMeEnabled)
        {
            if (member.Status == Constants.STATUS_TEMPORARILY_BLOCKED)
            {
                member.Status = Constants.STATUS_ACTIVE;
            }
            member.RememberMeEnabled = rememberMeEnabled;
        }

        public string LoginRequest(string userName, string password, Boolean rememberMeEnabled, decimal lat, decimal lng, string udid, string devicetoken)
        {
            Logger.Info("MDA -> LoginRequest Initiated - [UserName: " + userName + "], [UDID: " + udid + "]");

            var userEmail = userName;
            var userNameLowerCase = CommonHelper.GetEncryptedData(userName.ToLower());
            userName = CommonHelper.GetEncryptedData(userName);


            var memberEntity =
                _dbContext.Members.FirstOrDefault(m => m.UserNameLowerCase == userNameLowerCase && m.IsDeleted == false);

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
                                Logger.Info("MDA -> LoginRequest - Sending Automatic Logout Notification - [UserName: " + userEmail +
                                                       "], [UDID: " + udid +
                                                       "], [AccessToken: " + memberEntity.AccessToken + "]");

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
                                    if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
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
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> LoginRequest - Automatic Log Out email NOT sent to [" + toAddress + "]. Problem occured in sending email.");
                                }

                            }

                            #endregion Check If User Is Already Logged In

                            // Update UDID of device from which the user has logged in.
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
                                //membersRepository.UpdateEntity(memberEntity);                               
                                _dbContext.SaveChanges();
                                _dbContext.Entry(memberEntity).Reload();

                                loginRetryCountInDb = memberEntity.InvalidLoginAttemptCount.Equals(null)
                                    ? 0
                                    : memberEntity.InvalidLoginAttemptCount.Value;

                                if (!memberEntity.Password.Equals(password.Replace(" ", "+")))
                                {
                                    return CommonHelper.IncreaseInvalidLoginAttemptCount(memberEntity.MemberId.ToString(), loginRetryCountInDb);
                                }
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

                                //membersRepository.UpdateEntity(memberEntity);

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
                                //membersRepository.UpdateEntity(memberEntity);
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
                        {
                            return "Invalid user id or password.";
                        }
                }
            }
            else
            {
                return "Invalid user id or password.";
            }

        }

        public List<LocationSearch> GetLocationSearch(string MemberId, int Radius)
        {
            Logger.Info("MDA -> GetLocationSearch - [MemberId: " + MemberId + "],  [Radius: " + Radius + "]");


            try
            {
                List<GetLocationSearch_Result> list = _dbContext.GetLocationSearch(MemberId, Radius).ToList();
                List<LocationSearch> list1 = new List<LocationSearch>();

                foreach (GetLocationSearch_Result loc in list)
                {

                    var config =
                        new MapperConfiguration(cfg => cfg.CreateMap<GetLocationSearch_Result, LocationSearch>()
                            .BeforeMap((src, dest) => src.FirstName = CommonHelper.GetDecryptedData(src.FirstName))
                            .BeforeMap((src, dest) => src.LastName = CommonHelper.GetDecryptedData(src.LastName))
                            );

                    var mapper = config.CreateMapper();

                    LocationSearch obj = mapper.Map<LocationSearch>(loc);

                    obj.FirstName = CommonHelper.GetDecryptedData(loc.FirstName);
                    obj.LastName = CommonHelper.GetDecryptedData(loc.LastName);
                    decimal miles = obj.Miles;
                    obj.Miles = decimal.Parse(miles > 0 ? miles.ToString("###.####") : "000.0000");
                    obj.Photo = loc.Photo;
                    obj.MemberId = loc.MemberId.ToString();
                    list1.Add(obj);
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
            Logger.Info("MDA -> validateInvitationCode - invitationCode: [" + invitationCode + "]");

            var noochInviteMember = _dbContext.InviteCodes.FirstOrDefault(m => m.code == invitationCode && m.count < m.totalAllowed);
            if (noochInviteMember != null)
            {
                _dbContext.Entry(noochInviteMember).Reload();
            }
            return noochInviteMember != null;

        }


        public bool getTotalReferralCode(String referalCode)
        {
            Logger.Info("MDA -> getReferralCode Initiated - referalCode: [" + referalCode + "]");


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
                if (MemberSerachedWithPhone == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }

            }
            else
            {
                return false;
            }
        }
        // code was repeated again as previous one 
        //public string UpdateMemberIPAddressAndDeviceId(string MemberId, string IP, string DeviceId) 
        //{
        //    if (String.IsNullOrEmpty(MemberId))
        //    {
        //        return "MemberId not supplied.";
        //    }

        //    Guid memId = Utility.ConvertToGuid(MemberId);


        //    bool ipSavedSuccessfully = false;
        //    bool udidIdSavedSuccessfully = false;

        //    #region Save IP Address

        //    if (!String.IsNullOrEmpty(IP))
        //    {
        //        try
        //        {

        //            var ipAddressesFound = _dbContext.MembersIPAddresses.Where(memberTemp => memberTemp.MemberId.Value.Equals(memId)).ToList();

        //            if (ipAddressesFound.Count > 5)
        //            {
        //                // If there are already 5 entries, update the one added first (the oldest)
        //                var lastIpFound = (from c in ipAddressesFound select c)
        //                                  .OrderBy(m => m.ModifiedOn)
        //                                  .Take(1)
        //                                  .SingleOrDefault();

        //                lastIpFound.ModifiedOn = DateTime.Now;
        //                lastIpFound.Ip = IP;


        //                int a = _dbContext.SaveChanges();

        //                if (a > 0)
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (1) - [MemberId: " + MemberId + "]");
        //                    ipSavedSuccessfully = true;
        //                }
        //                else
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - [MemberId: " + MemberId + "]");
        //                }
        //            }
        //            else
        //            {
        //                // Otherwise, make a new entry
        //                MembersIPAddress mip = new MembersIPAddress();
        //                mip.MemberId = memId;
        //                mip.ModifiedOn = DateTime.Now;
        //                mip.Ip = IP;
        //                _dbContext.MembersIPAddresses.Add(mip);
        //                int b = _dbContext.SaveChanges();

        //                if (b > 0)
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (2) - [MemberId: " + MemberId + "]");
        //                    ipSavedSuccessfully = true;
        //                }
        //                else
        //                {
        //                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - [MemberId: " + MemberId + "]");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - [Exception: " + ex + "]");
        //        }
        //    }
        //    else
        //    {
        //        Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - [MemberId: " + MemberId + "]");
        //    }

        //    #endregion Save IP Address


        //    #region Save Device ID

        //    if (!String.IsNullOrEmpty(DeviceId))
        //    {
        //        try
        //        {
        //            // CLIFF (8/12/15): This "Device ID" will be stored in Nooch's DB as "UDID1" and is specifically for Synapse's "Fingerprint" requirement...
        //            //                  NOT for push notifications, which should use the "DeviceToken" in Nooch's DB.  (Confusing, but they are different values)


        //            var member = _dbContext.Members.Where(memberTemp => memberTemp.MemberId == memId).FirstOrDefault();

        //            if (member != null)
        //            {
        //                member.UDID1 = DeviceId;
        //                member.DateModified = DateTime.Now;
        //            }

        //            int c = _dbContext.SaveChanges();

        //            if (c > 0)
        //            {
        //                Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving Device ID - [MemberId: " + MemberId + "]");
        //                udidIdSavedSuccessfully = true;
        //            }
        //            else
        //            {
        //                Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - [MemberId: " + MemberId + "]");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - [Exception: " + ex + "]");
        //        }
        //    }
        //    else
        //    {
        //        Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - [MemberId: " + MemberId + "]");
        //    }

        //    #endregion Save Device ID

        //    if (ipSavedSuccessfully && udidIdSavedSuccessfully)
        //    {
        //        return "Both IP and DeviceID saved successfully.";
        //    }
        //    else if (ipSavedSuccessfully)
        //    {
        //        return "Only IP address saved successfully, not DeviceID.";
        //    }
        //    else if (udidIdSavedSuccessfully)
        //    {
        //        return "Only DeviceID saved successfully, not IP Address.";
        //    }

        //    return "Neither IP address nor DeviceID were saved.";

        //}  
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
                Logger.Error("MDA -> GetMemberDetails FAILED - Member ID: [" + memberId + "], [Exception: " + ex + "]");
            }
            return new Member();
        }
        public SynapseBanksOfMember GetSynapseBankAccountDetails(string memberId)
        {
            Logger.Info("MDA -> GetSynapseBankAccountDetails - MemberId: [" + memberId + "]");
            var id = Utility.ConvertToGuid(memberId);

            var memberAccountDetails = _dbContext.SynapseBanksOfMembers.Where(bank =>
                                (bank.MemberId.Value == id &&
                                 bank.IsDefault == true)).FirstOrDefault();
            if (memberAccountDetails != null)
            {
                _dbContext.Entry(memberAccountDetails).Reload();
            }
            return memberAccountDetails;

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

                        string noochRandomId = GetRandomNoochId();
                        string newUserPhoneNumber = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        newUserPhoneNumber = newUserPhoneNumber.Replace("(", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace(")", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace(" ", "");
                        newUserPhoneNumber = newUserPhoneNumber.Replace("-", "");

                        string inviteCode = transactionDetail.Member.InviteCodeId.ToString();

                        string emailEncrypted = userNameLowerCase;

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
                                var fromAddress = Utility.GetValueFromConfig("welcomeMail");

                                #region Send Registration Email

                                try
                                {
                                    //send registration email to member with autogenerated token
                                    var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                    "/Registration/Activation.aspx?tokenId=" + tokenId);

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

                                    Utility.SendEmail(Constants.TEMPLATE_REGISTRATION,
                                        fromAddress, CommonHelper.GetDecryptedData(userNameLowerCase), null, "Confirm your email on Nooch", link,
                                        tokens, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations EXCEPTION - Member activation email NOT sent to [" +
                                                           CommonHelper.GetDecryptedData(userNameLowerCase) + "], [Exception: " + ex + "]");
                                }

                                #endregion Send Registration Email

                                #region Send Temp PIN Email
                                try
                                {
                                    var tokens2 = new Dictionary<string, string>
                                        {
                                            {Constants.PLACEHOLDER_FIRST_NAME,CommonHelper.GetDecryptedData( userNameLowerCase)},
                                            {Constants.PLACEHOLDER_PINNUMBER, CommonHelper.GetDecryptedData(pinNumber)}
                                        };

                                    Utility.SendEmail("pinSetForNewUser",
                                        fromAddress, CommonHelper.GetDecryptedData(userNameLowerCase), null, "Your temporary Nooch PIN",
                                        null, tokens2, null, null, null);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("MDA -> CreateNonNoochUserPasswordForPhoneInvitations EXCEPTION - Member Temp PIN email NOT sent to [" +
                                                          CommonHelper.GetDecryptedData(userNameLowerCase) + "], [Exception: " + ex + "]");
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
                                        {
                                            Logger.Info("MDA -> CreateNonNoochUserPasswordForPhoneInvitations Attempted but invite code not found");
                                        }
                                        else if (inviteCodeObj.count >= inviteCodeObj.totalAllowed)
                                        {
                                            Logger.Info("MDA -> CreateNonNoochUserPasswordForPhoneInvitations Attempted to notify referrer but Allowable limit of [" +
                                                                   inviteCodeObj.totalAllowed + "] already reached for Code: [" + inviteCode + "]");
                                        }
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
                                                    SendEmailToInvitor(inviteCodeObj.InviteCodeId, CommonHelper.GetDecryptedData(userNameLowerCase), fullName);
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
                    else
                    {
                        return "User already exists";
                    }
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
            Logger.Info("MDA -> SendEmailToInvitor - Initiated - Invite Code Used [" + InviteCodeIdUsed + "], [New User Name: " +
                                   userJoinedName + "], [New User Email: " + userJoinedEmail + "]");

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
                        Utility.SendEmail("EmailToInvitorAfterSignup",
                            "hello@nooch.com", toAddress, null,
                            userJoinedName + " joined Nooch with your invite code", null,
                            tokens, null, null, null);

                        Logger.Info("MDA -> SendEmailToInvitor - Email sent to Referrer [" + toAddress + "], [InviteCode: " +
                                               InviteCodeIdUsed + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> SendEmailToInvitor email NOT sent to Referrer - [" + toAddress + "], [InviteCode: " +
                                               InviteCodeIdUsed + "], [New User's Name: " + userJoinedName + "], [Exception: " + ex + "]");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> SendEmailToInvitor FAILED (Outer) - Email NOT sent to Referrer - [New User's Email: " + userJoinedEmail +
                                       "], [InviteCode:" + InviteCodeIdUsed + "], [Exception: " + ex + "]");
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


                var memberEntity = _dbContext.Members.Where(memberTemp => memberTemp.Nooch_ID.Equals(randomId)).FirstOrDefault();
                if (memberEntity == null)
                {
                    return randomId;
                }

                j += i + 1;
            }

            return null;
        }

        #region synapse related methods

        public synapseCreateUserV3Result_int RegisterUserWithSynapseV3(string memberId)
        {
            Logger.Info("MDA -> RegisterUserWithSynapseV3 Initiated - [Member: " + memberId + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;

            Guid guid = Utility.ConvertToGuid(memberId);

            // Get the member details

            var noochMember = CommonHelper.GetMemberDetails(memberId);

            if (noochMember != null)
            {
                // Checks on user account: is phone verified? Is user status 'Active'?

                if (noochMember.Status != "Active" && noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Landlord" && noochMember.Type != "Tenant")
                {
                    Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED. Member is not Active but '" + noochMember.Status + "");
                    res.errorMsg = "user status is not active but, " + noochMember.Status;
                    return res;
                }

                if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                    noochMember.Type != "Landlord" &&
                    noochMember.Type != "Tenant" &&
                    noochMember.Type != "Personal - Browser" &&
                    noochMember.Status != "NonRegistered")
                {
                    Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED. Member's phone is not Verified for Member: [" + memberId + "]");
                    res.errorMsg = "user phone not verified";
                    return res;
                }

                /******  SYNAPSE V3  ******/
                #region Call Synapse V3 API: /v3/user/create

                synapseCreateUserInput_int payload = new synapseCreateUserInput_int();

                string SynapseClientId = Utility.GetValueFromConfig("SynapseClientId");
                string SynapseClientSecret = Utility.GetValueFromConfig("SynapseClientSecret");

                string fullname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName)) + " " +
                                  CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName));

                createUser_client client = new createUser_client();
                client.client_id = SynapseClientId;
                client.client_secret = SynapseClientSecret;

                payload.client = client;

                createUser_login logins = new createUser_login();
                logins.email = CommonHelper.GetDecryptedData(noochMember.UserName);
                // CLIFF (4/20/16): NOT SURE WHY WE WOULD WANT TO SEND THE USER'S NOOCH PASSWORD.  IT'S NOT REQUIRED BY SYNAPSE AND IS AN UNNECESSARY SECURITY RISK.
               // logins.password = CommonHelper.GetDecryptedData(noochMember.Password);
                
                logins.read_only = false; // CLIFF (10/10/12) - I think we might want to keep this false (which is default) - will ask Synapse to clarify

                payload.logins = new createUser_login[1];
                payload.logins[0] = logins; // REQUIRED BY SYNAPSE

                payload.phone_numbers = new string[] { noochMember.ContactNumber }; // REQUIRED BY SYNAPSE
                payload.legal_names = new string[] { fullname }; // REQUIRED BY SYNAPSE

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
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - Had to create a Fingerprint, but failed on saving new value in DB - Continuing on - [MemberID: "
                                               + memberId + "], [Exception: " + ex + "]");
                    }
                }
                payload.fingerprints = new createUser_fingerprints[1];
                payload.fingerprints[0] = fingerprints;

                payload.ips = new string[] { CommonHelper.GetRecentOrDefaultIPOfMember(guid) };

                createUser_extra extra = new createUser_extra();
                extra.note = "";
                extra.supp_id = noochMember.Nooch_ID;
                extra.is_business = false; // CLIFF (10/10/12): For Landlords, this could potentially be true... but we'll figure that out later

                payload.extra = extra;
                var baseAddress = "";
                baseAddress = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/user/create" : "https://synapsepay.com/api/v3/user/create";


                try
                {
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
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED - [Synapse Error Code: " + res.error_code +
                                               "], [Error Msg: " + res.errorMsg + "], [MemberID: " + memberId + "]");

                        #region Email Already Registered

                        // CLIFF (10/10/15): NOT SURE WHAT SYNAPSE'S RESPONSE/ERROR WILL LOOK LIKE FOR THIS CASE (Docs don't say and can't simulate in Sandbox)
                        if (res.errorMsg == "Email already registered.")
                        {
                            // case when synapse returned email already registered...chances are we have user id in SynapseCreateUserResults table
                            // checking Nooch DB

                            var synapseRes =
                                _dbContext.SynapseCreateUserResults.FirstOrDefault(
                                    m => m.MemberId == guid && m.IsDeleted == false);

                            if (synapseRes != null)
                            {
                                res.success = true;
                                res.errorMsg = "Account already in Nooch DB for that email";
                                res.user_id = synapseRes.user_id.ToString();
                                res.oauth_consumer_key = CommonHelper.GetDecryptedData(synapseRes.access_token);

                                return res;
                            }
                            else
                            {
                                // WHAT ABOUT THIS CASE? THIS HAPPENS IF SYNAPSE HAS A RECORD FOR THIS EMAIL ADDRESS, BUT NOOCH DOESN'T IN OUR DB...
                                // THIS IS EXTREMELY UNLIKELY TO EVER OCCUR, BUT COULD IF NOOCH EVER HAS A DB PROBLEM FOR 1 OR MORE USERS.
                                // SYNAPSE DOES *NOT* HAVE THE force_create OPTION IN V3...

                                Logger.Info("MDA -> RegisterUserWithSynapseV3 FAILED - MemberId: [" + memberId + "]. Synapse Error User Already Registered BUT not found in Nooch DB.");
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
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 FAILED: Synapse Error, but *error_code* was null for [MemberID: " +
                                               memberId + "], [Exception: " + we.InnerException + "]");
                    }

                    return res;

                    #endregion Synapse Create User V3 Exception
                }

                #endregion Call Synapse V3 API: /v3/user/create

                #region Synapse Create User Response was SUCCESSFUL

                if (res.success == true &&
                    !String.IsNullOrEmpty(res.oauth.oauth_key))
                {
                    res.user_id = !String.IsNullOrEmpty(res.user._id.id) ? res.user._id.id : "";
                    res.ssn_verify_status = "did not check yet";

                    // Mark any existing Synapse 'Create User' results for this user as 'Deleted'
                    #region Delete Any Old Synapse Create User Records Already in DB

                    var synapseCreateUserObj = _dbContext.SynapseCreateUserResults.Where(m => m.MemberId == guid && m.IsDeleted == false)
                            .ToList();

                    try
                    {
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
                                               "[MemberID: " + memberId + "], [Exception: " + ex.InnerException + "]");
                    }

                    #endregion Delete Any Old DB Records & Create New Record

                    #region Add New Entry To SynapseCreateUserResults DB Table

                    int addRecordToSynapseCreateUserTable = 0;

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
                        newSynapseUser.user_id = res.user._id.id; // this is no more int... this will be string from now onwards

                        // LETS USE THE EXISTING V2 DB TABLE FOR V3: 'SynapseCreateUserResults'... all the same, PLUS a few additional parameters (none are that important, but we should store them):

                        // Adding data for new fields in Synapse V3
                        // CLIFF(4/20/16): NEED TO ADD:  "user.doc_status.physical_doc", "user.doc_status.virtual_doc", "user.extra.extra_security"
                        newSynapseUser.is_business = res.user.extra.is_business;
                        newSynapseUser.legal_name = res.user.legal_names.Length > 0 ? res.user.legal_names[0] : null;
                        newSynapseUser.permission = res.user.permission ?? null;
                        newSynapseUser.Phone_number = res.user.phone_numbers.Length > 0 ? res.user.phone_numbers[0] : null;
                        newSynapseUser.photos = res.user.photos.Length > 0 ? res.user.photos[0] : null;

                        // Now add the new record to the DB
                        _dbContext.SynapseCreateUserResults.Add(newSynapseUser);
                        addRecordToSynapseCreateUserTable = _dbContext.SaveChanges();
                        _dbContext.Entry(newSynapseUser).Reload();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterUserWithSynapseV3 - FAILED To Save New Record in 'Synapse Create User Results' Table - " +
                                     "[MemberID: " + memberId + "], [Exception: " + ex.InnerException + "]");
                    }

                    #endregion Add New Entry To SynapseCreateUserResults DB Table


                    if (addRecordToSynapseCreateUserTable > 0)
                    {
                        Logger.Info("MDA -> RegisterUserWithSynapseV3 SUCCESS - [errorMsg: " + res.errorMsg + "], [user_id: " + res.user_id + "]");

                        if (noochMember.IsVerifiedWithSynapse == true)
                        {
                            Logger.Info("MDA -> RegisterUserWithSynapseV3 - ** ID Already Verified ** - [MemberID: " + memberId + "]");
                            res.ssn_verify_status = "id already verified";
                        }
                        else if (res.user.permission == "SEND-AND-RECEIVE") // Probobly wouldn't ever be this b/c I don't think Synapse ever returns this for brand new users
                        {
                            try
                            {
                                Logger.Info("MDA -> RegisterUserWithSynapseV3 - ** ID Already Verified (Case 2) ** - [MemberID: " + memberId + "]");

                                noochMember.IsVerifiedWithSynapse = true;
                                _dbContext.SaveChanges();

                                res.ssn_verify_status = "id already verified";
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> RegisterUserWithSynapseV3 - IsVerifiedWithSynapse is false, but Synapse returned Permission of \"SEND-AND-RECEIVE\" - " +
                                                       "[Exception: " + ex + "]");
                            }
                        }
                        else
                        {
                            Logger.Error("MDA -> RegisterUserWithSynapseV3 - ID NOT Already Verified, attempting to send SSN info to SynapseV3 - [MemberID: " + memberId + "]");

                            try
                            {
                                // Now: SEND USER'S SSN INFO TO SYNAPSE
                                submitIdVerificationInt submitSsn = CommonHelper.sendUserSsnInfoToSynapseV3(memberId);
                                res.ssn_verify_status = submitSsn.message;

                                // Next if/else are all just for logging
                                if (submitSsn.success == true)
                                {
                                    if (!String.IsNullOrEmpty(submitSsn.message) &&
                                        submitSsn.message.IndexOf("additional") > -1)
                                    {
                                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified, but have additional questions - [Email: " + logins.email + "], [submitSsn.message: " + submitSsn.message + "]");
                                    }
                                    else
                                    {
                                        Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified completely :-) - [Email: " + logins.email + "], [submitSsn.message: " + submitSsn.message + "]");
                                    }
                                }
                                else
                                {
                                    Logger.Info("MDA -> RegisterUserWithSynapseV3 - SSN Info Verified FAILED :-(  [Email: " + logins.email + "], [submitSsn.message: " + submitSsn.message + "]");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> RegisterUserWithSynapseV3 - Attempted sendUserSsnInfoToSynapseV3 but got Exception: [" + ex.Message + "]");
                            }
                        }
                    }
                    else
                    {
                        // FAILED TO ADD SYNAPSE RECORD TO DB
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
                        res.oauth_consumer_key = CommonHelper.GetDecryptedData(synapseRes.access_token);
                        res.oauth.refresh_token = CommonHelper.GetDecryptedData(synapseRes.refresh_token);

                        return res;
                    }
                    else
                    {
                        Logger.Info("MDA -> RegisterUserWithSynapseV3 FAILED: for [" + memberId + "]. Nooch Error 8868.");

                        res.errorMsg = "Synapse returned succes = false, and user not in Nooch DB";

                        return res;
                    }
                }

                #endregion Synapse Create User Response Success Was False & Reason = Email Already Registered

                else
                {
                    res.success = false;
                    res.errorMsg = !String.IsNullOrEmpty(res.errorMsg) ? res.errorMsg : "Unknown failure :-(";

                    Logger.Info("MDA -> RegisterUserWithSynapseV3 FAILED. [MemberId: " + memberId + "], [errorMsg: " + res.errorMsg + "]");

                    return res;
                }
            }
            else // Nooch member was not found in Members.dbo
            {
                Logger.Info("MDA -> RegisterUserWithSynapseV3 FAILED: for [" + memberId + "]. Error #10455.");

                res.success = false;
                res.errorMsg = "Given Member ID not found or Member is deleted.";

                return res;
            }
        }


        public synapseCreateUserV3Result_int RegisterExistingUserWithSynapseV3(string transId, string memberId, string userEmail, string userPhone, string userName, string pw, string ssn, string dob, string address, string zip, string fngprnt, string ip)
        {
            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 Initiated. [Name: " + userName +
                                   "], Email: [" + userEmail +
                                   "], Phone: [" + userPhone +
                                   "], TransId: [" + transId + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;
            res.reason = "Initial";


            #region Check To Make Sure All Data Was Passed

            // First check critical data necessary for just creating the user
            if (String.IsNullOrEmpty(userName) ||
                String.IsNullOrEmpty(userEmail) ||
                String.IsNullOrEmpty(userPhone))
            {
                string missingData = "";

                if (String.IsNullOrEmpty(userName))
                {
                    missingData = missingData + "Users' Name";
                }
                if (String.IsNullOrEmpty(userEmail))
                {
                    missingData = missingData + " Email";
                }
                if (String.IsNullOrEmpty(userPhone))
                {
                    missingData = missingData + " Phone";
                }

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
                {
                    missingData2 = missingData2 + "SSN";
                }
                if (String.IsNullOrEmpty(dob))
                {
                    missingData2 = missingData2 + " DOB";
                }
                if (String.IsNullOrEmpty(address))
                {
                    missingData2 = missingData2 + "Address";
                }
                if (String.IsNullOrEmpty(zip))
                {
                    missingData2 = missingData2 + " ZIP";
                }
                if (String.IsNullOrEmpty(fngprnt))
                {
                    missingData2 = missingData2 + " Fingerprint";
                }
                if (String.IsNullOrEmpty(ip))
                {
                    missingData2 = missingData2 + " IP";
                }

                Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Missing Non-Critical Data: [" + missingData2.Trim() + "]");
            }

            #endregion Check To Make Sure All Data Was Passed


            #region Check If Given Phone Already Exists

            string memberIdFromPhone = CommonHelper.GetMemberIdByContactNumber(userPhone);
            if (!String.IsNullOrEmpty(memberIdFromPhone))
            {
                res.reason = "Given phone number already registered.";
                return res;
            }

            #endregion Check if given email or phone already exists

            Guid memGuid = Utility.ConvertToGuid(memberId);

            var memberObj = _dbContext.Members.FirstOrDefault(memberTemp => memberTemp.MemberId.Equals(memGuid) &&
                                                                            memberTemp.IsDeleted == false);



            if (memberObj != null)
            {
                _dbContext.Entry(memberObj).Reload();

                #region Update Member's Record in Members.dbo

                // Add member details based on given name, email, phone, & other parameters
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
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - DOB was NULL, reassigning it to 'Jan 20, 1981' - [Name: " + userName + "], [TransId: " + transId + "]");
                    dob = "01/20/1981";
                }
                DateTime dateofbirth;
                if (!DateTime.TryParse(dob, out dateofbirth))
                {
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - DOB was NULL - [Name: " + userName + "], [TransId: " + transId + "]");
                }

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);
                memberObj.SecondaryEmail = memberObj.UserName; // In case the supplied email is different than what the Landlord used to invite, saving the original email here as secondary, and updating UserName in next line
                memberObj.UserName = CommonHelper.GetEncryptedData(userEmail.Trim()); // Username might be different if user changes the email on the payRequest page
                memberObj.UserNameLowerCase = CommonHelper.GetEncryptedData(userEmail.Trim().ToLower());
                memberObj.FirstName = FirstName;
                memberObj.LastName = LastName;
                memberObj.ContactNumber = userPhone;
                memberObj.Address = !String.IsNullOrEmpty(address) ? CommonHelper.GetEncryptedData(address) : null;
                memberObj.Zipcode = !String.IsNullOrEmpty(zip) ? CommonHelper.GetEncryptedData(zip) : null;
                memberObj.SSN = !String.IsNullOrEmpty(ssn) ? CommonHelper.GetEncryptedData(ssn) : null;
                memberObj.DateOfBirth = dateofbirth;
                memberObj.Status = "Active";
                memberObj.UDID1 = !String.IsNullOrEmpty(fngprnt) ? fngprnt : null;
                memberObj.DateModified = DateTime.Now;
                if (!String.IsNullOrEmpty(pw))
                {
                    memberObj.Password = CommonHelper.GetEncryptedData(pw);
                }

                int dbUpdatedSuccessfully = _dbContext.SaveChanges();
                _dbContext.Entry(memberObj).Reload();
                #endregion Update Member's Record in Members.dbo

                // Now add the IP address record to the MembersIPAddress Table
                try
                {
                    CommonHelper.UpdateMemberIPAddressAndDeviceId(memberId, ip, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 EXCEPTION on trying to save new Member's IP Address - " +
                                           "[MemberID: " + memberId + "], [Exception: " + ex + "]");
                }

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
                                               "[MemberID: " + memberId + "], [Exception: " + ex + "]");
                    }
                }

                #endregion Update Tenant Record If For A Tenant

                #region Member Updated In Nooch DB Successfully

                if (dbUpdatedSuccessfully > 0)
                {
                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - Nooch Member UPDATED SUCCESSFULLY IN DB (via Browser Landing Page) - " +
                                           "[UserName: " + userEmail + "], [MemberId: " + memberId + "]");

                    bool didUserAddPw = false;

                    #region Check If PW Was Supplied To Create Full Account

                    if (!String.IsNullOrEmpty(pw))
                    {
                        didUserAddPw = true;

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
                            Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Temp PIN email NOT sent to [" + userEmail + "], [Exception: " + ex + "]");
                        }

                        #endregion Temp PIN Email
                    }

                    #endregion Check If PW Was Supplied To Create Full Account

                    // NOW WE HAVE CREATED A NEW NOOCH USER RECORD AND SENT THE REGISTRATION EMAIL (IF THE USER PROVIDED A PW TO CREATE AN ACCOUNT)
                    // NEXT, ATTEMPT TO CREATE A SYNAPSE ACCOUNT FOR THIS USER
                    #region Create User with Synapse


                    // RegisterUserSynapseResultClassint createSynapseUserResult = new RegisterUserSynapseResultClassint();
                    synapseCreateUserV3Result_int createSynapseUserResult = new synapseCreateUserV3Result_int();
                    try
                    {
                        // Now call Synapse create user service
                        Logger.Info("** MDA -> RegisterExistingUserWithSynapseV3 ABOUT TO CALL CREATE SYNAPSE USER METHOD  **");
                        createSynapseUserResult = RegisterUserWithSynapseV3(memberObj.MemberId.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - createSynapseUser FAILED - [Username: " + userEmail +
                                               "], [Exception: " + ex.Message + "]");
                    }

                    if (createSynapseUserResult != null)
                    {
                        res.ssn_verify_status = "did not check yet";

                        #region Created Synapse User Successfully

                        if (createSynapseUserResult.success == true &&
                            !String.IsNullOrEmpty(createSynapseUserResult.oauth.oauth_key))
                        {
                            Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 - Synapse User created SUCCESSFULLY (LN: 6796) - " +
                                                   "[oauth_consumer_key: " + createSynapseUserResult.oauth_consumer_key + "]. Now attempting to save in Nooch DB.");

                            //   Entry in SynapseCreateUser Has been already done from RegisterUserWithSynapseV3 method  //

                            // Synapse User created successfully - Now make entry in [SynapseCreateUserResults] table
                            //SynapseCreateUserResult nr = new SynapseCreateUserResult();
                            //nr.MemberId = memberObj.MemberId;
                            //nr.DateCreated = DateTime.Now;
                            //nr.access_token = CommonHelper.GetEncryptedData(createSynapseUserResult.oauth_consumer_key);
                            //nr.expires_in = createSynapseUserResult.oauth.expires_in;
                            //nr.reason = createSynapseUserResult.reason;
                            //nr.refresh_token = CommonHelper.GetEncryptedData(createSynapseUserResult.oauth.refresh_token);
                            //nr.success = Convert.ToBoolean(res.success);
                            //nr.username = CommonHelper.GetEncryptedData(createSynapseUserResult.user.logins[0].email);
                            //nr.user_id = createSynapseUserResult.user._id.id;
                            //nr.IsDeleted = false;
                            //nr.ModifiedOn = DateTime.Now;
                            //nr.IsForNonNoochUser = false;
                            //nr.NonNoochUserEmail = userEmail;
                            //nr.TransactionIdFromWhichInvited = Utility.ConvertToGuid(transId);
                            //nr.HasNonNoochUserSignedUp = didUserAddPw;

                            //int addRecordToSynapseCreateUserTable = _dbContext.SaveChanges();

                            //  if (addRecordToSynapseCreateUserTable > 0)                                
                            if (createSynapseUserResult.success == true)                                // asuming createSynapseUserResult.success to be equals addRecordToSynapseCreateUserTable > 0
                            {
                                if (!String.IsNullOrEmpty(res.reason) &&
                                    res.reason.IndexOf("Email already registered") > -1)
                                {
                                    // THIS HAPPENS WHEN THE USER ALREADY EXISTS AND WE USED 'force_create'='no'
                                    res.reason = "User already existed, successfully received consumer_key.";

                                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 SUCCESS -> [Reason: " + res.reason + "], [Email: " + userEmail + "], [user_id: " + res.user_id + "]");
                                }

                                // EXPECTED OUTCOME for most users creating a new Synapse Account.
                                // Synapse doesn't always return a "reason" anymore (they used to but stopped sending it for newly created users apparently)
                                else
                                {
                                    Logger.Info("MDA -> RegisterExistingUserWithSynapseV3 SUCCESS - [Email: " + userEmail + "], [user_id: " + createSynapseUserResult.user_id +
                                                           "]. Now about to attempt to send SSN info to Synapse.");

                                    // Now attempt to verify user's ID by sending SSN, DOB, name, & address to Synapse
                                    /* No need of the following code any more as this has already been done in RegisterUserWithSynapseV3 method
                                   try
                                   {
                                       
                                       submitIdVerificationInt submitSsn = CommonHelper.sendUserSsnInfoToSynapseV3(memberObj.MemberId.ToString());
                                       res.ssn_verify_status = submitSsn.message;

                                       // Next if/else are all just for logging
                                       if (submitSsn.success == true)
                                       {
                                           if (!String.IsNullOrEmpty(submitSsn.message) &&
                                               submitSsn.message.IndexOf("additional") > -1)
                                           {
                                               Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - SSN Info verified, but have additional questions - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                           }
                                           else if (!String.IsNullOrEmpty(submitSsn.message) &&
                                                submitSsn.message.IndexOf("Already") > -1)
                                           {
                                               Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - SSN Info Already Verified - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                           }
                                           else
                                           {
                                               Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - SSN Info verified completely :-) - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                           }
                                       }
                                       else
                                       {
                                           Logger.Info("MDA -> RegisterExistingUserWithSynapseV2 - SSN Info verified FAILED :-(  [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                       }
                                   }
                                   catch (Exception ex)
                                   {
                                       Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - Attempted sendUserSsnInfoToSynapse but got Exception: [" + ex.Message + "]");
                                   }
                                     */
                                }
                                createUserV3Result_oauth oath = new createUserV3Result_oauth();
                                res = createSynapseUserResult;
                                res.success = true;
                                oath.oauth_key = createSynapseUserResult.oauth.oauth_key; // Already know it's not NULL, so don't need to re-check
                                res.user_id = !String.IsNullOrEmpty(createSynapseUserResult.user._id.id) ? createSynapseUserResult.user._id.id : "";
                                oath.expires_in = !String.IsNullOrEmpty(createSynapseUserResult.oauth.expires_in) ? createSynapseUserResult.oauth.expires_in : "";
                                res.oauth = oath;
                                res.memberIdGenerated = memberObj.MemberId.ToString();
                                res.error_code = createSynapseUserResult.error_code;
                                res.errorMsg = createSynapseUserResult.errorMsg;

                            }
                            else
                            {
                                Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - FAILED to add record to SynapseCreateUserResults.dbo - [user_id: " + res.user_id + "]");
                            }
                        }

                        #endregion Created Synapse User Successfully

                        else
                        {
                            Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 FAILED - Synapse Create user service failed (but wasn't null) - " +
                                                   "[Reason: " + res.reason + "], [MemberID: " + memberId + "]");
                            res.reason = createSynapseUserResult.reason;
                        }
                    }
                    else
                    {
                        Logger.Error("MDA -> RegisterExistingUserWithSynapseV3 - createSynapseUser FAILED & Returned NULL");
                        res.success = false;
                        res.reason = !String.IsNullOrEmpty(createSynapseUserResult.reason) ? createSynapseUserResult.reason : "Reg NonNooch User w/ Syn: Error 5927.";
                    }

                    #endregion Create User with Synapse
                }

                #endregion Member Updated In Nooch DB Successfully

                else
                {
                    Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - FAILED to add new Member to DB");

                    res.success = false;
                    res.reason = "Failed to save new Nooch Member in DB.";
                }
            }
            else
            {
                Logger.Error("MDA -> RegisterExistingUserWithSynapseV2 - FAILED - MemberID not found in DB - [MemberID: " + memberId + "]");
                res.success = false;
                res.reason = "MemberID not found in DB.";
            }

            return res;
        }

        public synapseCreateUserV3Result_int RegisterNonNoochUserWithSynapseV3(string transId, string userEmail, string userPhone, string userName, string pw, string ssn, string dob, string address, string zip, string fngprnt, string ip)
        {
            // What's the plan? -- Store new Nooch member, then create Synpase user, then check if user supplied a (is password.Length > 0)
            // then store data in new added field in SynapseCreateUserResults table for later use

            Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 Initiated. [Name: " + userName +
                                   "], Email: [" + userEmail + "], Phone: [" + userPhone +
                                   "], DOB: [" + dob + "], SSN: [" + ssn +
                                   "], Address: [" + address + "], ZIP: [" + zip +
                                   "], IP: [" + ip + "], Fngprnt: [" + fngprnt +
                                   "], TransId: [" + transId + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;
            res.reason = "Initial";

            string NewUsersNoochMemId = "";


            #region Check To Make Sure All Data Was Passed

            // First check critical data necessary for just creating the user
            if (String.IsNullOrEmpty(userName) ||
                String.IsNullOrEmpty(userEmail) ||
                String.IsNullOrEmpty(userPhone))
            {
                string missingData = "";

                if (String.IsNullOrEmpty(userName))
                {
                    missingData = missingData + "Users' Name";
                }
                if (String.IsNullOrEmpty(userEmail))
                {
                    missingData = missingData + " Email";
                }
                if (String.IsNullOrEmpty(userPhone))
                {
                    missingData = missingData + " Phone";
                }

                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 FAILED - Missing Critical Data: [" + missingData.Trim() + "]");

                res.reason = "Missing critical data: " + missingData.Trim();
                return res;
            }

            if (String.IsNullOrEmpty(ssn) || String.IsNullOrEmpty(dob) ||
                String.IsNullOrEmpty(address) || String.IsNullOrEmpty(zip) ||
                String.IsNullOrEmpty(fngprnt) || String.IsNullOrEmpty(ip))
            {
                string missingData2 = "";

                if (String.IsNullOrEmpty(ssn))
                {
                    missingData2 = missingData2 + "SSN";
                }
                if (String.IsNullOrEmpty(dob))
                {
                    missingData2 = missingData2 + " DOB";
                }
                if (String.IsNullOrEmpty(address))
                {
                    missingData2 = missingData2 + "Address";
                }
                if (String.IsNullOrEmpty(zip))
                {
                    missingData2 = missingData2 + " ZIP";
                }
                if (String.IsNullOrEmpty(fngprnt))
                {
                    missingData2 = missingData2 + " Fingerprint";
                }
                if (String.IsNullOrEmpty(ip))
                {
                    missingData2 = missingData2 + " IP";
                }

                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - Missing Non-Critical Data: [" + missingData2.Trim() + "]. Continuing on...");
            }

            #endregion Check To Make Sure All Data Was Passed


            #region Check if given Email and Phone already exist

            string memberIdFromPhone = CommonHelper.GetMemberIdByContactNumber(userPhone);
            if (!String.IsNullOrEmpty(memberIdFromPhone))
            {
                res.reason = "Given phone number already registered.";
                return res;
            }

            string memberIdFromEmail = CommonHelper.GetMemberIdByUserName(userEmail);
            if (!String.IsNullOrEmpty(memberIdFromEmail))
            {
                res.reason = "Given email already registered.";
                return res;
            }

            #endregion Check if given email or phone already exists


            // Set up member details based on given name, email, phone, & other parameters
            string noochRandomId = GetRandomNoochId();

            if (!String.IsNullOrEmpty(noochRandomId))
            {
                #region Get Invite Code ID from transaction

                string inviteCode = "";

                try
                {
                    Guid tid = Utility.ConvertToGuid(transId);

                    var transDetail = _dbContext.Transactions.FirstOrDefault(transIdTemp => transIdTemp.TransactionId == tid);

                    if (transDetail != null)
                    {
                        _dbContext.Entry(transDetail).Reload();
                        inviteCode = transDetail.Member.InviteCodeId.ToString();
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
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - DOB was NULL, reassigning it to 'Jan 20, 1980' - [Name: " + userName + "], [TransId: " + transId + "]");
                    dob = "01/20/1981";
                }
                DateTime dateofbirth;
                if (!DateTime.TryParse(dob, out dateofbirth))
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - DOB was NULL - [Name: " + userName + "], [TransId: " + transId + "]");
                }

                string userNameLowerCase = userEmail.Trim().ToLower();
                string userNameLowerCaseEncr = CommonHelper.GetEncryptedData(userNameLowerCase);

                string pinNumber = Utility.GetRandomPinNumber();
                pinNumber = CommonHelper.GetEncryptedData(pinNumber);

                #endregion Parse And Format Data To Save



                var member = new Member
                {
                    Nooch_ID = noochRandomId,
                    MemberId = Guid.NewGuid(),
                    FirstName = FirstName,
                    LastName = LastName,
                    UserName = userNameLowerCaseEncr,
                    UserNameLowerCase = userNameLowerCaseEncr,
                    SecondaryEmail = userNameLowerCaseEncr,
                    RecoveryEmail = userNameLowerCaseEncr,
                    ContactNumber = userPhone,
                    Address = !String.IsNullOrEmpty(address) ? CommonHelper.GetEncryptedData(address) : null,
                    Zipcode = !String.IsNullOrEmpty(zip) ? CommonHelper.GetEncryptedData(zip) : null,
                    SSN = !String.IsNullOrEmpty(ssn) ? CommonHelper.GetEncryptedData(ssn) : null,
                    DateOfBirth = dateofbirth,
                    Password = !String.IsNullOrEmpty(pw) ? CommonHelper.GetEncryptedData(pw) : CommonHelper.GetEncryptedData("jibb3r;jawn"),
                    PinNumber = pinNumber,
                    Status = Constants.STATUS_NON_REGISTERED,
                    IsDeleted = false,
                    DateCreated = DateTime.Now,
                    Type = "Personal",
                    Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png",
                    UDID1 = !String.IsNullOrEmpty(fngprnt) ? fngprnt : null,
                    IsVerifiedWithSynapse = false
                };



                if (inviteCode.Length > 0)
                {
                    member.InviteCodeIdUsed = Utility.ConvertToGuid(inviteCode);
                }

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
                    throw ex;
                }
                #endregion Create New Nooch Member Record in Members.dbo

                NewUsersNoochMemId = member.MemberId.ToString();

                // LASTLY, ADD THE IP ADDRESS RECORD TO THE MembersIPAddress Table =
                // (waited until after the member was actually added to the Members table above... shouldn't actually matter b/c 
                // the UpdateIPAddress method will just create a new record if one doesn't exist, but just to be safe.)
                try
                {
                    CommonHelper.UpdateMemberIPAddressAndDeviceId(NewUsersNoochMemId, ip, null);
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 EXCEPTION on trying to save new Member's IP Address - " +
                                           "MemberID: [" + NewUsersNoochMemId + "], [Exception: " + ex + "]");
                }

                #region Member Added to Nooch DB Successfully

                if (addNewMemberToDB > 0)
                {
                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - New Nooch Member successfully added to DB (via Browser Landing Page) - " +
                                           "UserName: [" + userEmail + "], MemberID: " + member.MemberId + "]");

                    #region Set Up & Save Nooch Notification Settings

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


                    #endregion Set Up & Save Nooch Notification Settings

                    #region Set up & Save Privacy Settings


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

                    #endregion Set up & Save Privacy Settings

                    // WE ARE ADDING EVERY PERSON TO DB IN ORDER TO HAVE THE INFO TO CREATE A SYNAPSE USER
                    // ...EVEN IF THE NON-NOOCH USER DOES NOT PROVIDE A PW TO 'CREATE' A NOOCH ACCOUNT.
                    // SO NOW THAT THE USER HAS JUST BEEN  CREATED, CHECK IF THEY WANTED A FULL NOOCH ACCOUNT
                    // BY CHECKING IF A PW WAS PROVIDED & THEN SEND NEW USER EMAILS

                    bool didUserAddPw = false;

                    #region Check If PW Was Supplied To Create Full Account

                    if (!String.IsNullOrEmpty(pw))
                    {
                        didUserAddPw = true;
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

                        //var link = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                        //                        "/Registration/Activation.aspx?tokenId=" + tokenId);

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

                    // NOW WE HAVE CREATED A NEW NOOCH USER RECORD AND SENT THE REGISTRATION EMAIL (IF THE USER PROVIDED A PW TO CREATE AN ACCOUNT)
                    // NEXT, ATTEMPT TO CREATE A SYNAPSE ACCOUNT FOR THIS USER
                    #region Create User with Synapse

                    // var synapseCreateUserRepo = new Repository<SynapseCreateUserResults, NoochDataEntities>(noochConnection);

                    synapseCreateUserV3Result_int createSynapseUserResult = new synapseCreateUserV3Result_int();
                    try
                    {
                        // Now call Synapse create user service
                        Logger.Info("** MDA -> RegisterExistingUserWithSynapseV2 ABOUT TO CALL CREATE SYNAPSE USER METHOD  **");
                        createSynapseUserResult = RegisterUserWithSynapseV3(member.MemberId.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - createSynapseUser FAILED - [Exception: " + ex.Message + "]");
                    }

                    if (createSynapseUserResult != null)
                    {
                        res.ssn_verify_status = "did not check yet";

                        #region Created Synapse User Successfully

                        if (createSynapseUserResult.success == true &&
                            !String.IsNullOrEmpty(createSynapseUserResult.oauth.oauth_key))
                        {
                            Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - Synapse User created SUCCESSFULLY (LN: 6009) - " +
                                                   "[oauth_consumer_key: " + createSynapseUserResult.oauth_consumer_key + "]. Now attempting to save in Nooch DB.");
                            // Synapse User created successfully
                            // Now make entry in [SynapseCreateUserResults] table
                            //   Entry in SynapseCreateUser Has been already done from RegisterUserWithSynapseV3 method  //

                            //SynapseCreateUserResults nr = new SynapseCreateUserResults();
                            //nr.MemberId = Utility.ConvertToGuid(NewUsersNoochMemId);
                            //nr.DateCreated = DateTime.Now;
                            //nr.access_token = CommonHelper.GetEncryptedData(createSynapseUserResult.oauth_consumer_key);
                            //nr.expires_in = createSynapseUserResult.expires_in;
                            //nr.reason = createSynapseUserResult.reason;
                            //nr.refresh_token = CommonHelper.GetEncryptedData(createSynapseUserResult.refresh_token);
                            //nr.success = Convert.ToBoolean(res.success);
                            //nr.username = CommonHelper.GetEncryptedData(createSynapseUserResult.username);
                            //nr.user_id = createSynapseUserResult.user_id;
                            //nr.IsDeleted = false;
                            //nr.ModifiedOn = DateTime.Now;
                            //nr.IsForNonNoochUser = false;
                            //nr.NonNoochUserEmail = member.UserName;
                            //nr.TransactionIdFromWhichInvited = Utility.ConvertToGuid(transId);
                            //nr.HasNonNoochUserSignedUp = didUserAddPw;

                            //int addRecordToSynapseCreateUserTable = synapseCreateUserRepo.AddEntity(nr);

                            if (createSynapseUserResult.success == true)
                            {
                                if (!String.IsNullOrEmpty(res.reason) &&
                                    res.reason.IndexOf("Email already registered") > -1)
                                {
                                    // THIS HAPPENS WHEN THE USER ALREADY EXISTS AND WE USED 'force_create'='no'
                                    res.reason = "User already existed, successfully received consumer_key.";

                                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 SUCCESS -> [Reason: " + res.reason + "], [Email: " + userEmail + "], [user_id: " + res.user_id + "]");
                                }

                                // EXPECTED OUTCOME for most users creating a new Synapse Account.
                                // Synapse doesn't always return a "reason" anymore (they used to but stopped sending it for newly created users apparently)
                                else
                                {
                                    Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 SUCCESS - [Email: " + userEmail + "], [user_id: " + createSynapseUserResult.user_id +
                                                           "]. Now about to attempt to send SSN info to Synapse.");

                                    // Now attempt to verify user's ID by sending SSN, DOB, name, & address to Synapse
                                    /* No need of the following code any more as this has already been done in RegisterUserWithSynapseV3 method
                               
                                  try
                                  {
                                      submitIdVerificationInt submitSsn = sendUserSsnInfoToSynapse(member.MemberId.ToString());
                                      res.ssn_verify_status = submitSsn.message;

                                      // Next if/else are all just for logging
                                      if (submitSsn.success == true)
                                      {
                                          if (!String.IsNullOrEmpty(submitSsn.message) &&
                                              submitSsn.message.IndexOf("additional") > -1)
                                          {
                                              Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - SSN Info verified, but have additional questions - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                          }
                                          else if (!String.IsNullOrEmpty(submitSsn.message) &&
                                                   submitSsn.message.IndexOf("Already") > -1)
                                          {
                                              Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - SSN Info Already Verified - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                          }
                                          else
                                          {
                                              Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - SSN Info verified completely :-) - [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                          }
                                      }
                                      else
                                      {
                                          Logger.Info("MDA -> RegisterNonNoochUserWithSynapseV3 - SSN Info verified FAILED :-(  [Email: " + userEmail + "], [submitSsn.message: " + submitSsn.message + "]");
                                      }
                                  }
                                  catch (Exception ex)
                                  {
                                      Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - Attempted sendUserSsnInfoToSynapse but got Exception: [" + ex.Message + "]");
                                  }
                                     */
                                }

                                createUserV3Result_oauth oath = new createUserV3Result_oauth();
                                res = createSynapseUserResult;
                                res.success = true;
                                oath.oauth_key = createSynapseUserResult.oauth.oauth_key; // Already know it's not NULL, so don't need to re-check
                                res.user_id = !String.IsNullOrEmpty(createSynapseUserResult.user._id.id) ? createSynapseUserResult.user._id.id : "";
                                oath.expires_in = !String.IsNullOrEmpty(createSynapseUserResult.oauth.expires_in) ? createSynapseUserResult.oauth.expires_in : "";
                                res.oauth = oath;
                                res.memberIdGenerated = member.MemberId.ToString();
                                res.error_code = createSynapseUserResult.error_code;
                                res.errorMsg = createSynapseUserResult.errorMsg;
                            }
                            else
                            {
                                Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - FAILED to add record to SynapseCreateUserResults.dbo - [user_id: " + res.user_id + "]");
                            }
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
                                        Logger.Info("MDA -> RegisterNonNoochUserWithSynapse[transId: " + transId +
                                                               "] Synapse 401 Error User Already Registered BUT not found in Nooch DB.");
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
                            {
                                res.reason = createSynapseUserResult.reason;
                            }
                        }
                    }
                    else
                    {
                        Logger.Error("MDA -> RegisterNonNoochUserWithSynapseV3 - createSynapseUser FAILED & Returned NULL");

                        res.reason = !String.IsNullOrEmpty(createSynapseUserResult.reason) ? createSynapseUserResult.reason : "Reg NonNooch User w/ Syn: Error 5927.";
                    }

                    #endregion Create User with Synapse


                    #region Send Email To Referrer (If Applicable)

                    if (!String.IsNullOrEmpty(inviteCode))
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
            Logger.Info("MDA -> GetNonNoochUserTempRegisterIdFromTransId Initiated - [TransID: " + TransId + "]");

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
                        {
                            nonNoochUserId = memberIdFromPhone;
                        }
                    }
                    if ((theTransaction.IsPhoneInvitation == null || theTransaction.IsPhoneInvitation == false) &&
                         theTransaction.InvitationSentTo != null)
                    {
                        string memberIdFromEmail = CommonHelper.GetMemberIdByUserName(CommonHelper.GetDecryptedData(theTransaction.InvitationSentTo));

                        if (!String.IsNullOrEmpty(memberIdFromEmail))
                        {
                            nonNoochUserId = memberIdFromEmail;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetNonNoochUserTempRegisterIdFromTransId FAILED - [TransID: " + TransId + "], [Exception: " + ex.Message + "]");
            }
            return nonNoochUserId;
        }


        public GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
        {
            Logger.Info("MDA -> submitDocumentToSynapseV3 Initialized - [MemberId: " + MemberId + "]");

            var id = Utility.ConvertToGuid(MemberId);

            GenericInternalResponseForSynapseMethods res = new GenericInternalResponseForSynapseMethods();


            #region Get User's Synapse OAuth Consumer Key

            string usersSynapseOauthKey = "";


            var usersSynapseDetails = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

            if (usersSynapseDetails == null)
            {
               Logger.Info("MDA -> submitDocumentToSynapseV3 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");

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

            submitDocToSynapseV3Class answers = new submitDocToSynapseV3Class();

            SynapseV3Input_login s_log = new SynapseV3Input_login();
            s_log.oauth_key = usersSynapseOauthKey;
            answers.login = s_log;
            //answers.login.oauth_key = usersSynapseOauthKey;

            submitDocToSynapse_user sdtu = new submitDocToSynapse_user();
            submitDocToSynapse_user_doc doc = new submitDocToSynapse_user_doc();
            //doc.attachment = "data:text/csv;base64," + CommonHelper.ConvertImageURLToBase64(ImageUrl).Replace("\\","");
            doc.attachment = "data:text/csv;base64,SUQsTmFtZSxUb3RhbCAoaW4gJCksRmVlIChpbiAkKSxOb3RlLFRyYW5zYWN0aW9uIFR5cGUsRGF0ZSxTdGF0dXMNCjUxMTksW0RlbW9dIEJlbHogRW50ZXJwcmlzZXMsLTAuMTAsMC4wMCwsQmFuayBBY2NvdW50LDE0MzMxNjMwNTEsU2V0dGxlZA0KNTExOCxbRGVtb10gQmVseiBFbnRlcnByaXNlcywtMS4wMCwwLjAwLCxCYW5rIEFjY291bnQsMTQzMzE2MjkxOSxTZXR0bGVkDQo1MTE3LFtEZW1vXSBCZWx6IEVudGVycHJpc2VzLC0xLjAwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDMzMTYyODI4LFNldHRsZWQNCjUxMTYsW0RlbW9dIEJlbHogRW50ZXJwcmlzZXMsLTEuMDAsMC4wMCwsQmFuayBBY2NvdW50LDE0MzMxNjI2MzQsU2V0dGxlZA0KNTExNSxbRGVtb10gQmVseiBFbnRlcnByaXNlcywtMS4wMCwwLjAwLCxCYW5rIEFjY291bnQsMTQzMzE2MjQ5OCxTZXR0bGVkDQo0ODk1LFtEZW1vXSBMRURJQyBBY2NvdW50LC03LjAwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDMyMjUwNTYyLFNldHRsZWQNCjQ4MTIsS2FyZW4gUGF1bCwtMC4xMCwwLjAwLCxCYW5rIEFjY291bnQsMTQzMTk5NDAzNixTZXR0bGVkDQo0NzgwLFNhbmthZXQgUGF0aGFrLC0wLjEwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDMxODQ5NDgxLFNldHRsZWQNCjQzMTUsU2Fua2FldCBQYXRoYWssLTAuMTAsMC4wMCwsQmFuayBBY2NvdW50LDE0Mjk3NzU5MzcsU2V0dGxlZA0KNDMxNCxTYW5rYWV0IFBhdGhhaywtMC4xMCwwLjAwLCxCYW5rIEFjY291bnQsMTQyOTc3NTQzNCxTZXR0bGVkDQo0MzEzLFNhbmthZXQgUGF0aGFrLC0wLjEwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDI5Nzc1MzY0LFNldHRsZWQNCjQzMTIsU2Fua2FldCBQYXRoYWssLTAuMTAsMC4wMCwsQmFuayBBY2NvdW50LDE0Mjk3NzUyNTAsU2V0dGxlZA0KNDMxMSxTYW5rYWV0IFBhdGhhaywtMC4xMCwwLjAwLCxCYW5rIEFjY291bnQsMTQyOTc3NTAxMyxTZXR0bGVkDQo0MjM1LFtEZW1vXSBCZWx6IEVudGVycHJpc2VzLC0wLjEwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDI5MzMxODA2LFNldHRsZWQNCjQxMzYsU2Fua2FldCBQYXRoYWssLTAuMTAsMC4wMCwsQmFuayBBY2NvdW50LDE0Mjg4OTA4NjMsU2V0dGxlZA0KNDAzMCxTYW5rYWV0IFBhdGhhaywtMC4xMCwwLjAwLCxCYW5rIEFjY291bnQsMTQyODIxNTM5NixTZXR0bGVkDQo0MDE0LFtEZW1vXSBCZWx6IEVudGVycHJpc2VzLC0wLjEwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDI4MTI1MzgwLENhbmNsZWQNCjM4MzIsU2Fua2FldCBQYXRoYWssLTAuMTAsMC4wMCwsQmFuayBBY2NvdW50LDE0MjcxMDc0NzAsU2V0dGxlZA0KMzgyNixTYW5rYWV0IFBhdGhhaywtMC4xMCwwLjAwLCxCYW5rIEFjY291bnQsMTQyNzAzNTM5MixTZXR0bGVkDQozODI1LFNhbmthZXQgUGF0aGFrLC0wLjEwLDAuMDAsLEJhbmsgQWNjb3VudCwxNDI3MDMyOTM3LFNldHRsZWQNCg==";


            sdtu.fingerprint = usersFingerprint;

            sdtu.doc = doc;

            answers.user = sdtu;

            //answers.user.doc.attachment = "data:text/csv;base64," + ConvertImageURLToBase64(ImageUrl).Replace("\\", ""); // NEED TO GET THE ACTUAL DOC... EITHER PASS THE BYTES TO THIS METHOD, OR GET FROM DB
            //answers.user.fingerprint = usersFingerprint;

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
                        //var membersRepository = new Repository<Members, NoochDataEntities>(noochConnection);                           

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
                                    "/Registration/Activation.aspx?tokenId=" + tokenId);
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
                                            Logger.Info("MDA -> CreateNonNoochUserAccountAfterRejectMoney - Attempted but invite code not found: [" +
                                                                    inviteCode + "]");
                                        }
                                        else if (inviteCodeObj.count >= inviteCodeObj.totalAllowed)
                                        {
                                            Logger.Info("MDA -> CreateNonNoochUserAccountAfterRejectMoney - Attempted to notify referrer but Allowable limit of [" +
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


        public SynapseBankLoginV3_Response_Int SynapseV3MFABankVerifyWithMicroDeposits(string MemberId, string BankName, string microDepositOne, string microDepositTwo, string BankId)
        {
            Logger.Info("MDA -> SynapseV3MFABankVerifyWithMicroDeposits Initiated. [MemberId: " + MemberId + "], [BankName: " + BankName + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();

            #region Check If All Data Passed

            if (String.IsNullOrEmpty(BankName) ||
                String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(microDepositOne) ||
                String.IsNullOrEmpty(microDepositTwo) ||
                String.IsNullOrEmpty(BankId))
            {
                if (String.IsNullOrEmpty(BankName))
                {
                    res.errorMsg = "Invalid data - need Bank Name";
                }
                else if (String.IsNullOrEmpty(MemberId))
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

                Logger.Info("MDA -> SynapseV3MFABankVerifyWithMicroDeposits ERROR: " + res.errorMsg + " for: [" + MemberId + "]");
                res.Is_success = false;
                return res;
            }

            #endregion Check If All Data Passed

            else
            {

                // Now get the Member's Nooch account details
                Guid id = Utility.ConvertToGuid(MemberId);


                var noochMember = CommonHelper.GetMemberDetails(id.ToString());

                if (noochMember != null)
                {
                    #region Check For Active Status and Verified Phone

                    // Checks on user account: is Member 'Active'? Is phone verified?
                    if (noochMember.Status != "Active" && noochMember.Status != "NonRegistered" &&
                        noochMember.Type != "Landlord" && noochMember.Type != "Tenant" && noochMember.Type != "Personal - Browser")
                    {
                        Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member is not Active [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "User status is not active but: " + noochMember.Status;
                        return res;
                    }

                    if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                         noochMember.Type != "Landlord" && noochMember.Type != "Tenant" && noochMember.Type != "Personal - Browser" &&
                         noochMember.Status != "NonRegistered")
                    {
                        Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member's phone is not verified [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "User phone is not verified";
                        return res;
                    }
                    #endregion Check For Active Status and Verified Phone

                    // checking member auth token in db stored after synapse create user service call
                    #region CHECKING FOR SYNAPSE AUTH TOKEN

                    var noochMemberResultFromSynapseAuth = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());


                    if (noochMemberResultFromSynapseAuth == null)
                    {
                        Logger.Info("MDA - SynapseV3MFABankVerify -> could not locate Synapse Auth Token for Member: [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "No Synapse Authentication code found for given user.";
                        return res;
                    }

                    #endregion CHECKING FOR SYNAPSE AUTH TOKEN

                    #region GOT USERS SYNAPSE AUTH TOKEN

                    else
                    {
                        // we have authentication token
                        SynapseBankVerifyWithMicroDepositsV3_Input bankLoginPars = new SynapseBankVerifyWithMicroDepositsV3_Input();

                        //user login
                        SynapseV3Input_login log = new SynapseV3Input_login()
                        {
                            oauth_key = CommonHelper.GetDecryptedData(noochMemberResultFromSynapseAuth.access_token)
                        };

                        bankLoginPars.login = log;

                        //user finger print
                        SynapseV3Input_user fing = new SynapseV3Input_user()
                        {
                            fingerprint = noochMember.UDID1
                        };

                        bankLoginPars.user = fing;


                        // node object
                        SynapseBankVerifyV3WithMicroDesposits_Input_node no = new SynapseBankVerifyV3WithMicroDesposits_Input_node();

                        SynapseNodeId node_id = new SynapseNodeId() { oid = BankId };
                        no._id = node_id;

                        SynapseBankVerifyV3WithMicroDesposits_Input_node_verify veri = new SynapseBankVerifyV3WithMicroDesposits_Input_node_verify();
                        veri.micro = new string[2];
                        veri.micro[0] = microDepositOne;
                        veri.micro[1] = microDepositTwo;

                        no.verify = veri;

                        bankLoginPars.node = no;

                        //bankLoginPars.node._id.oid = BankId;
                        //bankLoginPars.node.verify.mfa = MfaResponse;

                        string UrlToHit = "";
                        UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/verify" : "https://synapsepay.com/api/v3/node/verify";


                        // Calling Synapse Bank Login service (Link a Bank Account)

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

                        try
                        {
                            var response = http.GetResponse();
                            var stream = response.GetResponseStream();
                            var sr = new StreamReader(stream);
                            var content = sr.ReadToEnd();

                            //dynamic bankLoginResponseFromSynapse = JsonConvert.DeserializeObject <dynamic>(content);
                            //dynamic bankLoginResponseFromSynapse = JArray.Parse(content);

                            JObject bankLoginRespFromSynapse = JObject.Parse(content);

                            if (bankLoginRespFromSynapse["success"].ToString().ToLower() == "true" &&
                                bankLoginRespFromSynapse["nodes"] != null)
                            {
                                res.Is_success = true;

                                #region Marking Any Existing Synapse Bank Login Entries as Deleted


                                var memberLoginResultsCollection = CommonHelper.GetSynapseBankLoginResulList(id.ToString());

                                foreach (SynapseBankLoginResult v in memberLoginResultsCollection)
                                {
                                    v.IsDeleted = true;
                                    _dbContext.SaveChanges();

                                }

                                #endregion Marking Any Existing Synapse Bank Login Entries as Deleted

                                #region Check if MFA was returned

                                if (bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"] != null ||
                                    bankLoginRespFromSynapse["nodes"][0]["allowed"] == null)
                                {
                                    // Now we know MFA is required

                                    // Preparing to save results in SynapseBankLoginResults DB table
                                    SynapseBankLoginResult sbr = new SynapseBankLoginResult();
                                    sbr.MemberId = id;
                                    sbr.IsSuccess = true;
                                    sbr.dateCreated = DateTime.Now;
                                    sbr.IsDeleted = false;
                                    sbr.IsMfa = true;
                                    sbr.IsCodeBasedAuth = false;  // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                                    sbr.IsQuestionBasedAuth = true;
                                    sbr.mfaQuestion = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();
                                    sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                                    sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString()); // CLIFF (8/22/15): Not sure if this syntax is correct
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
                                    //rObj.
                                    res.SynapseNodesList = rObj;
                                    res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();

                                    Logger.Info("MDA -> SynapseV3MFABankVerify: SUCCESS, Got MFA Again from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    return res;


                                    //res.Is_MFA = true;
                                    //res.errorMsg = "OK";
                                    //res.SynapseNodesList.nodes[0]._id.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString(); // Not sure this syntax is right
                                    //res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();

                                    //Logger.LogDebugMessage("MDA -> SynapseV3MFABankVerify: SUCCESS, Got MFA Again from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    //return res;
                                }

                                #endregion Check if MFA was returned

                                #region No MFA response returned

                                else
                                {
                                    // Now we know MFA is NOT required this time

                                    // Array[] of banks ("nodes") expected here
                                    RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                                    if (allNodesParsedResult != null)
                                    {
                                        res.Is_MFA = false;
                                        res.errorMsg = "OK";
                                        res.SynapseNodesList = allNodesParsedResult;

                                        Logger.Info("MDA -> SynapseV3MFABankVerify (No MFA Again): SUCCESSFUL, returning Bank Array for: [" + MemberId + "]");

                                        // saving these banks ("nodes) in DB, later one of these banks will be set as default bank
                                        foreach (nodes v in allNodesParsedResult.nodes)
                                        {
                                            SynapseBanksOfMember sbm = new SynapseBanksOfMember();

                                            sbm.AddedOn = DateTime.Now;
                                            sbm.IsDefault = false;
                                            Guid memId = Utility.ConvertToGuid(MemberId);
                                            sbm.MemberId = memId;
                                            //sbm.account_class = v.account_class;

                                            sbm.account_number_string = CommonHelper.GetEncryptedData(v.info.account_num);
                                            //sbm.account_type = v.type_synapse;

                                            sbm.bank_name = CommonHelper.GetEncryptedData(v.info.bank_name);
                                            //sbm.bankAdddate = v.date;
                                            //sbm.bankid = v.bankOid.ToString();
                                            sbm.mfa_verifed = true;
                                            sbm.name_on_account = CommonHelper.GetEncryptedData(v.info.name_on_account);
                                            sbm.nickname = CommonHelper.GetEncryptedData(v.info.nickname);
                                            sbm.routing_number_string = CommonHelper.GetEncryptedData(v.info.routing_num);


                                            _dbContext.SynapseBanksOfMembers.Add(sbm);
                                            _dbContext.SaveChanges();
                                            _dbContext.Entry(sbm).Reload();

                                        }
                                    }
                                    else
                                    {
                                        Logger.Info("MDA -> SynapseV3MFABankVerify (No MFA Again) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]");

                                        res.Is_MFA = false;
                                        res.errorMsg = "Error occured while parsing banks list.";
                                        res.Is_success = false;
                                    }

                                    return res;
                                }

                                #endregion No MFA response returned
                            }
                            else
                            {
                                // Synapse response for 'success' was not true
                                Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Synapse response for 'success' was not true for Member [" + MemberId + "]");

                                res.Is_success = false;
                                res.errorMsg = "Synapse response for success was not true";
                                return res;
                            }
                        }
                        catch (WebException we)
                        {
                            var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                            Logger.Error("MDA -> SynapseV3MFABankVerify FAILED. Exception was: ["
                                                   + we.ToString() + "], and errorCode: [" + errorCode.ToString() + "]");

                            res.Is_success = false;
                            res.Is_MFA = false;

                            if (errorCode != null)
                            {
                                var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                                JObject jsonfromsynapse = JObject.Parse(resp);
                                JToken token = jsonfromsynapse["reason"];

                                if (token != null)
                                {
                                    res.errorMsg = jsonfromsynapse["reason"].ToString();
                                }
                            }
                            else
                            {
                                res.errorMsg = "Error #140 returned from Synapse";
                            }

                            return res;
                        }
                    }

                    #endregion GOT USERS SYNAPSE AUTH TOKEN
                }
                else
                {
                    Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member not found: [" + MemberId + "]");

                    res.Is_success = false;
                    res.errorMsg = "Member not found.";
                    return res;
                }

            }
        }

        public SynapseBankLoginV3_Response_Int SynapseV3MFABankVerify(string MemberId, string BankName, string MfaResponse, string BankId)
        {
            Logger.Info("MDA -> SynapseV3MFABankVerify Initiated. [MemberId: " + MemberId + "], [BankName: " + BankName + "], [MFA Response: " + MfaResponse + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();

            #region Check If All Data Passed

            if (String.IsNullOrEmpty(BankName) ||
                String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(MfaResponse) ||
                String.IsNullOrEmpty(BankId))
            {
                if (String.IsNullOrEmpty(BankName))
                {
                    res.errorMsg = "Invalid data - need Bank Name";
                }
                else if (String.IsNullOrEmpty(MemberId))
                {
                    res.errorMsg = "Invalid data - need MemberId";
                }
                else if (String.IsNullOrEmpty(MfaResponse))
                {
                    res.errorMsg = "Invalid data - need MFA answer";
                }
                else if (String.IsNullOrEmpty(BankId))
                {
                    res.errorMsg = "Invalid data - need BankAccessToken";
                }
                else
                {
                    res.errorMsg = "Invalid data sent";
                }

                Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: " + res.errorMsg + " for: [" + MemberId + "]");
                res.Is_success = false;
                return res;
            }

            #endregion Check If All Data Passed

            else
            {

                // Now get the Member's Nooch account details
                Guid id = Utility.ConvertToGuid(MemberId);


                var noochMember = CommonHelper.GetMemberDetails(id.ToString());

                if (noochMember != null)
                {
                    #region Check For Active Status and Verified Phone

                    // Checks on user account: is Member 'Active'? Is phone verified?
                    if (noochMember.Status != "Active" && noochMember.Status != "NonRegistered" &&
                        noochMember.Type != "Landlord" && noochMember.Type != "Tenant" && noochMember.Type != "Personal - Browser")
                    {
                        Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member is not Active [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "User status is not active but: " + noochMember.Status;
                        return res;
                    }

                    if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                         noochMember.Type != "Landlord" && noochMember.Type != "Tenant" && noochMember.Type != "Personal - Browser" &&
                         noochMember.Status != "NonRegistered")
                    {
                        Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member's phone is not verified [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "User phone is not verified";
                        return res;
                    }
                    #endregion Check For Active Status and Verified Phone

                    // checking member auth token in db stored after synapse create user service call
                    #region CHECKING FOR SYNAPSE AUTH TOKEN

                    var noochMemberResultFromSynapseAuth = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());


                    if (noochMemberResultFromSynapseAuth == null)
                    {
                        Logger.Info("MDA - SynapseV3MFABankVerify -> could not locate Synapse Auth Token for Member: [" + MemberId + "]");

                        res.Is_success = false;
                        res.errorMsg = "No Synapse Authentication code found for given user.";
                        return res;
                    }

                    #endregion CHECKING FOR SYNAPSE AUTH TOKEN

                    #region GOT USERS SYNAPSE AUTH TOKEN

                    else
                    {
                        // we have authentication token
                        SynapseBankVerifyV3_Input bankLoginPars = new SynapseBankVerifyV3_Input();

                        //user login
                        SynapseV3Input_login log = new SynapseV3Input_login()
                        {
                            oauth_key = CommonHelper.GetDecryptedData(noochMemberResultFromSynapseAuth.access_token)
                        };

                        bankLoginPars.login = log;

                        //user finger print
                        SynapseV3Input_user fing = new SynapseV3Input_user()
                        {
                            fingerprint = noochMember.UDID1
                        };

                        bankLoginPars.user = fing;


                        // node object
                        SynapseBankVerifyV3_Input_node no = new SynapseBankVerifyV3_Input_node();

                        SynapseNodeId node_id = new SynapseNodeId() { oid = BankId };
                        no._id = node_id;

                        SynapseBankVerifyV3_Input_node_verify veri = new SynapseBankVerifyV3_Input_node_verify();
                        veri.mfa = MfaResponse;

                        no.verify = veri;

                        bankLoginPars.node = no;

                        //bankLoginPars.node._id.oid = BankId;
                        //bankLoginPars.node.verify.mfa = MfaResponse;

                        string UrlToHit = "";
                        UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/verify" : "https://synapsepay.com/api/v3/node/verify";


                        // Calling Synapse Bank Login service (Link a Bank Account)

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

                        try
                        {
                            var response = http.GetResponse();
                            var stream = response.GetResponseStream();
                            var sr = new StreamReader(stream);
                            var content = sr.ReadToEnd();

                            //dynamic bankLoginResponseFromSynapse = JsonConvert.DeserializeObject <dynamic>(content);
                            //dynamic bankLoginResponseFromSynapse = JArray.Parse(content);

                            JObject bankLoginRespFromSynapse = JObject.Parse(content);

                            if (bankLoginRespFromSynapse["success"].ToString().ToLower() == "true" &&
                                bankLoginRespFromSynapse["nodes"] != null)
                            {
                                res.Is_success = true;

                                #region Marking Any Existing Synapse Bank Login Entries as Deleted

                                // why would we do this?
                                //var memberLoginResultsCollection = CommonHelper.GetSynapseBankLoginResulList(id.ToString());

                                //foreach (SynapseBankLoginResult v in memberLoginResultsCollection)
                                //{
                                //    v.IsDeleted = true;
                                //    _dbContext.SaveChanges();

                                //}

                                #endregion Marking Any Existing Synapse Bank Login Entries as Deleted

                                #region Check if MFA was returned

                                if (bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"] != null ||
                                    bankLoginRespFromSynapse["nodes"][0]["allowed"] == null)
                                {
                                    // Now we know MFA is required

                                    // Preparing to save results in SynapseBankLoginResults DB table
                                    SynapseBankLoginResult sbr = new SynapseBankLoginResult();
                                    sbr.MemberId = id;
                                    sbr.IsSuccess = true;
                                    sbr.dateCreated = DateTime.Now;
                                    sbr.IsDeleted = false;
                                    sbr.IsMfa = true;
                                    sbr.IsCodeBasedAuth = false;  // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                                    sbr.IsQuestionBasedAuth = true;
                                    sbr.mfaQuestion = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();
                                    sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                                    sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString()); // CLIFF (8/22/15): Not sure if this syntax is correct
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
                                    //rObj.
                                    res.SynapseNodesList = rObj;
                                    res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();

                                    Logger.Info("MDA -> SynapseV3MFABankVerify: SUCCESS, Got MFA Again from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    return res;


                                    //res.Is_MFA = true;
                                    //res.errorMsg = "OK";
                                    //res.SynapseNodesList.nodes[0]._id.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString(); // Not sure this syntax is right
                                    //res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString();

                                    //Logger.LogDebugMessage("MDA -> SynapseV3MFABankVerify: SUCCESS, Got MFA Again from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    //return res;
                                }

                                #endregion Check if MFA was returned

                                #region No MFA response returned

                                else
                                {
                                    // Now we know MFA is NOT required this time

                                    // Array[] of banks ("nodes") expected here
                                    RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                                    if (allNodesParsedResult != null)
                                    {
                                        res.Is_MFA = false;
                                        res.errorMsg = "OK";
                                        res.SynapseNodesList = allNodesParsedResult;

                                        Logger.Info("MDA -> SynapseV3MFABankVerify (No MFA Again): SUCCESSFUL, returning Bank Array for: [" + MemberId + "]");

                                        // saving these banks ("nodes) in DB, later one of these banks will be set as default bank
                                        foreach (nodes v in allNodesParsedResult.nodes)
                                        {
                                            SynapseBanksOfMember sbm = new SynapseBanksOfMember();

                                            sbm.AddedOn = DateTime.Now;
                                            sbm.IsDefault = false;
                                            Guid memId = Utility.ConvertToGuid(MemberId);
                                            sbm.MemberId = memId;
                                            //   sbm.account_class = v.account_class;

                                            sbm.account_number_string = CommonHelper.GetEncryptedData(v.info.account_num);
                                            //    sbm.account_type = v.type_synapse;

                                            sbm.bank_name = CommonHelper.GetEncryptedData(v.info.bank_name);
                                            //sbm.bankAdddate = v.date;

                                            sbm.oid = CommonHelper.GetEncryptedData(v._id.oid.ToString());
                                            sbm.mfa_verifed = true;
                                            sbm.name_on_account = CommonHelper.GetEncryptedData(v.info.name_on_account);
                                            sbm.nickname = CommonHelper.GetEncryptedData(v.info.nickname);
                                            sbm.routing_number_string = CommonHelper.GetEncryptedData(v.info.routing_num);
                                            sbm.allowed = v.allowed;
                                            // sbm.account_class = v.info._class;   Account class is int and it's value is string
                                            // sbm.account_type = Convert.ToInt32( v.info.type); 
                                            sbm.balance = v.info.balance.amount;
                                            sbm.is_active = v.is_active;
                                            sbm.type_bank = v.info.type;
                                            sbm.type_synapse = v.type;
                                            sbm.supp_id = v.extra.supp_id;

                                            _dbContext.SynapseBanksOfMembers.Add(sbm);
                                            _dbContext.SaveChanges();
                                            _dbContext.Entry(sbm).Reload();


                                        }
                                    }
                                    else
                                    {
                                        Logger.Info("MDA -> SynapseV3MFABankVerify (No MFA Again) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]");

                                        res.Is_MFA = false;
                                        res.errorMsg = "Error occured while parsing banks list.";
                                        res.Is_success = false;
                                    }

                                    return res;
                                }

                                #endregion No MFA response returned
                            }
                            else
                            {
                                // Synapse response for 'success' was not true
                                Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Synapse response for 'success' was not true for Member [" + MemberId + "]");

                                res.Is_success = false;
                                res.errorMsg = "Synapse response for success was not true";
                                return res;
                            }
                        }
                        catch (WebException we)
                        {
                            var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                            Logger.Error("MDA -> SynapseV3MFABankVerify FAILED. Exception was: ["
                                                   + we.ToString() + "], and errorCode: [" + errorCode.ToString() + "]");

                            res.Is_success = false;
                            res.Is_MFA = false;

                            if (errorCode != null)
                            {
                                var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                                JObject jsonfromsynapse = JObject.Parse(resp);
                                JToken token = jsonfromsynapse["reason"];

                                if (token != null)
                                {
                                    res.errorMsg = jsonfromsynapse["reason"].ToString();
                                }
                            }
                            else
                            {
                                res.errorMsg = "Error #140 returned from Synapse";
                            }

                            return res;
                        }
                    }

                    #endregion GOT USERS SYNAPSE AUTH TOKEN
                }
                else
                {
                    Logger.Info("MDA -> SynapseV3MFABankVerify ERROR: Member not found: [" + MemberId + "]");

                    res.Is_success = false;
                    res.errorMsg = "Member not found.";
                    return res;
                }

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
                //removeBankPars.login.oauth_key = usersSynapseOauthKey;

                SynapseV3Input_login log = new SynapseV3Input_login() { oauth_key = usersSynapseOauthKey };
                removeBankPars.login = log;

                //removeBankPars.user.fingerprint = usersFingerprint;
                SynapseV3Input_user userr = new SynapseV3Input_user() { fingerprint = usersFingerprint };
                removeBankPars.user = userr;

                SynapseNodeId noId = new SynapseNodeId() { oid = bankAccountsFound[0].oid };

                SynapseRemoveBankV3_Input_node nodem = new SynapseRemoveBankV3_Input_node() { _id = noId };

                //removeBankPars.node._id.oid = bankAccountsFound[0].ToString();
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

                return res;
            }
            else
            {
                res.message = "No active bank account found for this user";

                return res;
            }
        }

        public synapseIdVerificationQuestionsForDisplay getIdVerificationQuestionsV3(string MemberId)
        {
            Logger.Info("MDA -> getIdVerificationQuestionsV2 Initialized - [MemberId: " + MemberId + "]");

            synapseIdVerificationQuestionsForDisplay response = new synapseIdVerificationQuestionsForDisplay();
            response.success = false;
            response.memberId = MemberId;


            // CLIFF (10/1/15): NEED TO ADD A BLOCK TO CHECK THE MEMBERS TABLE AND SEE IF THIS USER'S IsVerifiedWithSynapse IS ALREADY TRUE
            //                  IF IT IS, THEN DON'T NEED TO RE-ASK THESE QUESTIONS, PASS BACK MESSAGE TO CLIENT AND HANDLE APPROPRIATELY

            Member noochMember = GetMemberDetails(MemberId);
            if (noochMember.IsVerifiedWithSynapse == true)
            {
                Logger.Info("MDA -> getIdVerificationQuestionsV2 ABORTED: Member's ID already verified with Synapse  :-) - [Member's Name: " +
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
                Logger.Info("MDA -> getIdVerificationQuestionsV2 ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");

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
                Logger.Info("MDA -> submitIdVerificationAnswersToSynapse ABORTED: Member's Synapse User Details not found. [MemberId: " + MemberId + "]");
                res.message = "Could not find this member's account info";
                return res;
            }
            else
            {
                _dbContext.Entry(usersSynapseDetails).Reload();
                usersSynapseOauthKey = CommonHelper.GetDecryptedData(usersSynapseDetails.access_token);
            }

            #endregion Get User's Synapse OAuth Consumer Key

            //synapseSubmitIdAnswers_answers_input input = new synapseSubmitIdAnswers_answers_input();
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
            //input.user.fingerprint = "";



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
                    #region Get Member Record To Update




                    #endregion Get Member Record To Update

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
                                    Logger.Info("MDA -> submitIdVerificationAnswersToSynapse - Unexpected: More than 5 answers (n = " + n + ") found in DB... Could be a problem somewhere");
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
                                Logger.Info("MDA -> submitIdVerificationAnswersToSynapse - Unexpected: < 4 Questions were Marked as Submitted in DB. [Question Set ID: " + questionSetId + "]");
                            }
                        }
                        else
                        {
                            Logger.Info("MDA -> submitIdVerificationAnswersToSynapse FAILED TO UPDATE DB: List of Banks to Update was null - [MemberId: " + MemberId + "]");
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
                        Logger.Info("MDA -> submitIdVerificationAnswersToSynapse FAILED - Got Synapse response, but success was NOT 'true' - [MemberID: " + MemberId + "]");
                    }
                }
                else
                {
                    res.message = "Verification response was null";
                    Logger.Info("MDA -> submitIdVerificationAnswersToSynapse FAILED - Synapse response was NULL - [MemberID: " + MemberId + "]");
                }
            }
            catch (WebException we)
            {
                res.message = "MDA Exception #9304";

                var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                Logger.Info("MDA -> submitIdVerificationAnswersToSynapse FAILED - Outer Catch. Member: [" + MemberId + "]. WebEx: [" + errorCode.ToString() + "]");

                if (errorCode.ToString() == "Unauthorized")
                {
                    var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                    JObject jsonfromsynapse = JObject.Parse(resp);
                    JToken token = jsonfromsynapse["reason"];

                    if (token != null)
                    {
                        res.message = jsonfromsynapse["reason"].ToString();
                    }
                }
            }

            #endregion Call Synapse /user/ssn/answer API

            return res;

        }

        #endregion


        public string SaveMemberSSN(string MemberId, string ssn)
        {
            if (ssn.Length != 4)
            {
                return "Invalid SSN passed.";
            }

            //Guid MemId = Utility.ConvertToGuid(MemberId);

            var noochMember = CommonHelper.GetMemberDetails(MemberId);

            if (noochMember != null)
            {

                noochMember.SSN = CommonHelper.GetEncryptedData(ssn);
                noochMember.DateModified = DateTime.Now;
                var dbContext = CommonHelper.GetDbContextFromEntity(noochMember);
                var res= dbContext.SaveChanges();
                //if (res != null)
                //{ 
                //_dbContext.Entry(noochMember).Reload();
                //}
                return "SSN saved successfully.";
            }
            else
            {
                return "Member Id not found or Member status deleted.";
            }
        }


        public string SaveDOBForMember(string MemberId, string dob)
        {
            DateTime dateTime2;

            if (!DateTime.TryParse(dob, out dateTime2))
            {
                return "Invalid DOB passed.";
            }


            var noochMember = CommonHelper.GetMemberDetails(MemberId);

            if (noochMember != null)
            {
                noochMember.DateOfBirth = dateTime2;
                noochMember.DateModified = DateTime.Now;
                var dbContext = CommonHelper.GetDbContextFromEntity(noochMember);
                dbContext.SaveChanges();
             //   _dbContext.Entry(noochMember).Reload();

                return "DOB saved successfully.";
            }
            else
            {
                return "Member Id not found or Member status deleted.";
            }

        }


        /// <summary>
        /// For settings a user's Synapse Bank to 'Verified'. Currently only called 
        /// from the Admin Dashboard and from the BankVerification.aspx.cs browser page.
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

                        #region Set Bank Logo URL Variable

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

                        #endregion Set Bank Logo URL Variable

                        Logger.Info("MDA -> VerifySynapseAccount --> Checkpoint 8818 - BankLogoUrl: [" + bankLogoUrl + "]");

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
                {
                    Logger.Error("MDA -> VerifySynapseAccount FAILED - Bank found, but error on updating DB record in SynapseBanksOfMembers - BankID: [" + bankId + "] and Status != 'Verified'.");
                }
            }

            return false;
        }


        public string GetTokensAndTransferMoneyToNewUser(string TransactionId, string MemberIdAfterSynapseAccountCreation, string TransactionType, string recipMemId)
        {
            Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser Initiated - TransType: [" + TransactionType +
                                   "], TransID: [" + TransactionId + "], New User Member ID: [" + MemberIdAfterSynapseAccountCreation +
                                   "], RecipientMemberID: [" + recipMemId + "]");

            // If a REQUEST, TransactionType will be "RequestToNewUser" & MemberIdAfterSynapseAccountCreation is the Request Recipient, and should be the SENDER below
            //               and the Request sender is the RecipMemID (if b/t 2 existing users)
            // If an INVITE, TransactionType will be "SentToNewUser &  MemberIdAfterSynapseAccountCreation is the Invite Recipient, and should be the NEW USER (RECIPIENT) below

            try
            {


                Guid transGuid = Utility.ConvertToGuid(TransactionId);


                var Transaction =
                    _dbContext.Transactions.FirstOrDefault(m => m.TransactionId == transGuid && m.TransactionStatus == "pending");

                if (Transaction != null)
                {
                    _dbContext.Entry(Transaction).Reload();

                    if (!string.IsNullOrEmpty(MemberIdAfterSynapseAccountCreation))
                    {
                        var newUserObj = GetMemberDetails(MemberIdAfterSynapseAccountCreation);
                        string newUsersEmail = CommonHelper.GetDecryptedData(newUserObj.UserName);

                        // Check if this is a TEST transaction by seeing if the new user's email includes "jones00" in it

                        #region Check If Testing

                        bool isTesting = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")); ;

                        //try
                        //{
                        //    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser -> Transaction.Members.MemberId: [" + Transaction.Member.MemberId + "]");

                        //    if (Transaction.Member.MemberId.ToString() == "00bd3972-d900-429d-8a0d-28a5ac4a75d7...")
                        //    {
                        //        Logger.Info("** MDA -> GetTokensAndTransferMoneyToNewUser - THIS WAS A TEST TRANSACTION to TEAM NOOCH ! **");
                        //        isTesting = true;
                        //    }

                        //    newUsersEmail = newUsersEmail.ToLower();

                        //    if (newUsersEmail.IndexOf("jones00") > -1)
                        //    {
                        //        Logger.Info("**  MDA -> GetTokensAndTransferMoneyToNewUser -> THIS IS A TEST USER **  [UserName: " +
                        //                              newUsersEmail + "]. Continuing On!  **");
                        //        isTesting = true;
                        //    }
                        //}
                        //catch (Exception ex)
                        //{
                        //    Logger.Error("**  MDA -> GetTokensAndTransferMoneyToNewUser -> ERROR while checking if this is a test transaction - " +
                        //                           "Exception: [" + ex + "]. Continuing on...  **");
                        //}

                        #endregion Check If Testing

                        Guid SenderId, RecipientId;

                        if (TransactionType == "RequestToNewUser")
                        {
                            // The "SENDER" is the person PAYING this request (so the "new" user)
                            SenderId = new Guid(MemberIdAfterSynapseAccountCreation);

                            if (!String.IsNullOrEmpty(recipMemId))
                            {
                                RecipientId = new Guid(recipMemId);
                            }
                            else
                            {
                                RecipientId = Transaction.Member.MemberId;
                            }
                        }
                        else
                        {
                            RecipientId = new Guid(MemberIdAfterSynapseAccountCreation);

                            if (!String.IsNullOrEmpty(recipMemId))
                            {
                                SenderId = new Guid(recipMemId);
                            }
                            else
                            {
                                SenderId = Transaction.Member.MemberId;
                            }
                        }

                        Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - SENDER MemberId: [" + SenderId + "]");
                        Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - RECIPIENT MemberId: [" + RecipientId + "]");

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

                                TransactionsDataAccess tda = new TransactionsDataAccess();



                                string facilitator_fee = "0";
                                if (Convert.ToDecimal(Transaction.Amount) > 10)
                                {
                                    facilitator_fee = "-.25";
                                }
                                else if (Convert.ToDecimal(Transaction.Amount) < 10)
                                {
                                    facilitator_fee = "-.10";
                                }
                                var sender = GetMemberDetails(SenderId.ToString());
                                var recipient = GetMemberDetails(RecipientId.ToString());
                                string moneySenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName));
                                string moneySenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName));
                                //string requesterPic = "https://www.noochme.com/noochservice/UploadedPhotos/Photos/" + Transaction.Members.MemberId.ToString() + ".png";

                                string moneyRecipientFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipient.FirstName));
                                string moneyRecipientLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipient.LastName));

                                SynapseV3AddTrans_ReusableClass Call_Synapse_Order_API_Result = tda.AddTransSynapseV3Reusable(SenderUserAndBankDetails.UserDetails.access_token,
                                    SenderUserAndBankDetails.UserDetails.user_fingerprints, SenderUserAndBankDetails.BankDetails.bank_oid,
                                   Transaction.Amount.ToString(), facilitator_fee, RecipientUserAndBankDetails.UserDetails.user_id, RecipientUserAndBankDetails.UserDetails.user_fingerprints,
                                   RecipientUserAndBankDetails.BankDetails.bank_oid, Transaction.TransactionId.ToString(), CommonHelper.GetDecryptedData(sender.UserName), CommonHelper.GetDecryptedData(recipient.UserName), CommonHelper.GetRecentOrDefaultIPOfMember(sender.MemberId),
                                   moneySenderLastName, moneyRecipientLastName
                                    );




                                #endregion Call Synapse Order API

                                if (Call_Synapse_Order_API_Result.success) // || isTesting)
                                {
                                    Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser -> Synapse Order API was SUCCESSFUL");

                                    //if (!isTesting)
                                    //{
                                    // If testing, keep this transaction as 'Pending' so we can more easily re-test with the same transaction.
                                    Transaction.TransactionStatus = "Success";

                                    _dbContext.SaveChanges();
                                    _dbContext.Entry(Transaction).Reload();

                                    //}

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

                                        }
                                    }

                                    #endregion Update Tenant Info If A RENT Payment

                                    #region EMAIL NOTIFICATIONS

                                    // NOW SEND EMAILS TO BOTH USERS

                                    #region Setup Email Placeholders

                                    Transaction.TransactionDate = DateTime.Now;

                                    string newUserPhone = "";

                                    if (Transaction.IsPhoneInvitation != null &&
                                        Transaction.IsPhoneInvitation == true &&
                                        !String.IsNullOrEmpty(Transaction.PhoneNumberInvited))
                                    {
                                        newUserPhone = CommonHelper.GetDecryptedData(Transaction.PhoneNumberInvited);
                                    }

                                    string fromAddress = Utility.GetValueFromConfig("transfersMail");



                                    bool isForRentScene = false;
                                    if (recipient.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Rent Scene's account
                                        recipient.MemberId.ToString().ToLower() == "a35c14e9-ee7b-4fc6-b5d5-f54961f2596a") // Just for testing: "sallyanejones00@nooch.com"
                                    {
                                        isForRentScene = true;
                                        moneyRecipientFirstName = "Rent Scene";
                                        moneyRecipientLastName = "";
                                    }

                                    string newUserNameForEmail = "";

                                    string recipientPic = (!String.IsNullOrEmpty(recipient.Photo) && recipient.Photo.Length > 20)
                                                            ? recipient.Photo.ToString()
                                                            : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";


                                    if (!String.IsNullOrEmpty(moneyRecipientFirstName))
                                    {
                                        newUserNameForEmail = moneyRecipientFirstName;

                                        if (!String.IsNullOrEmpty(moneyRecipientLastName))
                                        {
                                            newUserNameForEmail = newUserNameForEmail + " " + moneyRecipientLastName;
                                        }
                                    }
                                    else if (newUsersEmail.Length > 2)
                                    {
                                        newUserNameForEmail = newUsersEmail;
                                    }
                                    else if (newUserPhone.Length > 2)
                                    {
                                        newUserNameForEmail = newUserPhone;
                                    }

                                    string wholeAmount = Transaction.Amount.ToString("n2");
                                    string[] s3 = wholeAmount.Split('.');

                                    string transDate = Convert.ToDateTime(Transaction.TransactionDate).ToString(("MMMM dd, yyyy"));

                                    string memo = "";
                                    if (!String.IsNullOrEmpty(Transaction.Memo))
                                    {
                                        if (Transaction.Memo.Length > 3)
                                        {
                                            string firstThreeChars = Transaction.Memo.Substring(0, 3).ToLower();
                                            bool startsWithFor = firstThreeChars.Equals("for");

                                            if (startsWithFor)
                                            {
                                                memo = Transaction.Memo.ToString();
                                            }
                                            else
                                            {
                                                memo = "For: " + Transaction.Memo.ToString();
                                            }
                                        }
                                        else
                                        {
                                            memo = "For: " + Transaction.Memo.ToString();
                                        }
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
                                                                ". Amount will be credited to your bank account within 2-4 biz days.";
                                            try
                                            {
                                                Utility.SendSMS(newUserPhone, SMSContent);

                                                Logger.Info("TDA - GetTokensAndTransferMoneyToNewUser SUCCESS - SMS sent to recipient - [Phone: " +
                                                    CommonHelper.FormatPhoneNumber(newUserPhone) + "] successfully.");
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("TDA - GetTokensAndTransferMoneyToNewUser SUCCESS - But Failure sending SMS to recipient " +
                                                    "- [Phone: " + CommonHelper.FormatPhoneNumber(newUserPhone) + "],  [Exception: " + ex.ToString() + "]");
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

                                                Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser -> transferAcceptedToRecipient Email sent to [" +
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

                                            Logger.Info(
                                                "MDA -> GetTokensAndTransferMoneyToNewUser - TransferAcceptedToSender - Email sent to [" +
                                                toAddress + "] successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error(
                                                "MDA -> GetTokensAndTransferMoneyToNewUser - TransferAcceptedToSender - Email NOT sent to [" +
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

                                        var toAddress = CommonHelper.GetDecryptedData(recipient.UserName);

                                        try
                                        {
                                            Utility.SendEmail("requestPaidToSender", fromAddress,
                                                toAddress, null, moneySenderFirstName + " " + moneySenderLastName + " paid your request on Nooch",
                                                null, tokens2, null, null, null);

                                            Logger.Info("TDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToSender - Email sent to [" +
                                                                   toAddress + "] successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("TDA -> GetTokensAndTransferMoneyToNewUser - requestPaidToSender - Email NOT sent to [" +
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
                                    Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Synapse Response was NOT successful");

                                    return "Error from syn";
                                }
                            }
                            else
                            {
                                Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find Synapse User or Bank " +
                                                       "Details for Recipient - [TransID: " + TransactionId + "]");

                                return "Request payor bank account details not found or syn user id not found";
                            }
                        }
                        else
                        {
                            Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> MemberIdAfterSynapseAccountCreation " +
                                                       "was Null or empty - [TransID: " + TransactionId + "]");

                            return "Request payor bank account details not found or syn user id not found";
                        }
                    }
                    else
                    {
                        Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find Synapse User or Bank " +
                                                       "Details for EXISTING user (which is bad...) - [TransID: " + TransactionId + "]");

                        return "Missing 'MemberIdAfterSynapseAccountCreation' [9080]";
                    }
                }
                else
                {
                    Logger.Error("MDA - GetTokensAndTransferMoneyToNewUser FAILED -> Couldn't find this Transaction - " +
                                           "[TransID: " + TransactionId + "]");

                    return "Either transaction already paid or transaction not found";
                }


            }
            catch (Exception ex)
            {
                Logger.Error("MDA -> GetTokensAndTransferMoneyToNewUser FAILED - [TransId: " + TransactionId + "],  [Exception: " + ex + "]");
            }

            return "Unkown Failure";
        }


        public string MySettings(string memberId, string firstName, string lastName, string password,
            string secondaryMail, string recoveryMail, string tertiaryMail, string facebookAcctLogin,
            bool useFacebookPicture, string fileContent, int contentLength, string fileExtension,
            string contactNumber, string address, string city, string state, string zipCode, string country,
            string timeZoneKey, byte[] Picture, bool showinSearchb)
        {
            Logger.Info("MDA -> MySettings (Updating User's Profile) - [MemberID: " + memberId + "]");


            string folderPath = Utility.GetValueFromConfig("PhotoPath");
            string fileName = memberId;
            var id = Utility.ConvertToGuid(memberId);

            var member = CommonHelper.GetMemberDetails(memberId);

            if (member != null)
            {
                try
                {
                    #region Encrypt each piece of data

                    member.FirstName = CommonHelper.GetEncryptedData(firstName.Split(' ')[0]);

                    if (firstName.IndexOf(' ') > 0)
                    {
                        member.LastName = CommonHelper.GetEncryptedData(firstName.Split(' ')[1]);
                    }

                    if (member.Password != null && member.Password == "")
                    {
                        member.Password = CommonHelper.GetEncryptedData(CommonHelper.GetDecryptedData(password)
                                                      .Replace(" ", "+"));
                    }

                    if (!String.IsNullOrEmpty(secondaryMail))
                    {
                        member.SecondaryEmail = CommonHelper.GetEncryptedData(secondaryMail);
                    }

                    if (!String.IsNullOrEmpty(recoveryMail))
                    {
                        member.RecoveryEmail = CommonHelper.GetEncryptedData(recoveryMail);
                    }

                    if (!String.IsNullOrEmpty(tertiaryMail))
                    {
                        member.TertiaryEmail = CommonHelper.GetEncryptedData(tertiaryMail);
                    }

                    if (contentLength > 0)
                    {
                        member.Photo = Utility.UploadPhoto(folderPath, fileName, fileExtension, fileContent, contentLength);
                    }

                    if (!String.IsNullOrEmpty(facebookAcctLogin))
                    {
                        member.FacebookAccountLogin = facebookAcctLogin;
                    }

                    if (!String.IsNullOrEmpty(address))
                    {
                        member.Address = CommonHelper.GetEncryptedData(address);
                    }
                    if (!String.IsNullOrEmpty(city))
                    {
                        member.City = CommonHelper.GetEncryptedData(city);
                    }
                    if (!String.IsNullOrEmpty(state))
                    {
                        member.State = CommonHelper.GetEncryptedData(state);
                    }
                    if (!String.IsNullOrEmpty(zipCode))
                    {
                        member.Zipcode = CommonHelper.GetEncryptedData(zipCode);
                    }
                    if (!String.IsNullOrEmpty(country))
                    {
                        member.Country = country;
                    }
                    if (!string.IsNullOrEmpty(timeZoneKey))
                    {
                        member.TimeZoneKey = CommonHelper.GetEncryptedData(timeZoneKey);
                    }

                    #endregion Encrypt each piece of data

                    //Logger.LogDebugMessage("MDA -> MySettings CHECKPOINT #23 - contactNumber (sent from app): [" + contactNumber + "]");
                    //Logger.LogDebugMessage("MDA -> MySettings CHECKPOINT #23 - member.ContactNumber (from DB): [" + member.ContactNumber + "]");

                    if (!String.IsNullOrEmpty(contactNumber) &&
                        (String.IsNullOrEmpty(member.ContactNumber) ||
                        CommonHelper.RemovePhoneNumberFormatting(member.ContactNumber) != CommonHelper.RemovePhoneNumberFormatting(contactNumber)))
                    {
                        if (!IsPhoneNumberAlreadyRegistered(contactNumber))
                        {
                            member.ContactNumber = contactNumber;
                            member.IsVerifiedPhone = false;

                            #region SendingSMSVerificaion

                            try
                            {
                                string fname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName));
                                string MessageBody = "Hi " + fname + ", This is Nooch - just need to verify this is your phone number. Please reply 'Go' to confirm your phone number.";

                                string SMSresult = Utility.SendSMS(contactNumber, MessageBody);

                                Logger.Info("MySettings --> SMS Verification sent to [" + contactNumber + "] successfully.");
                            }
                            catch (Exception)
                            {
                                Logger.Error("MySettings --> SMS Verification NOT sent to [" +
                                    contactNumber + "]. Problem occurred in sending SMS.");
                            }

                            #endregion
                        }
                        else
                        {
                            return "Phone Number already registered with Nooch";
                        }
                    }

                    if (Picture != null)
                    {
                        // make  image from bytes
                        string filename = HttpContext.Current.Server.MapPath("UploadedPhotos") + "/Photos/" +
                                          member.MemberId.ToString() + ".png";

                        using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                        {
                            fs.Write(Picture, 0, (int)Picture.Length);
                        }

                        member.Photo = Utility.GetValueFromConfig("PhotoUrl") + member.MemberId + ".png";
                    }
                    else
                    {
                        // check if image is already there
                        if (member.Photo == null)
                        {
                            member.Photo = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                        }
                    }

                    if (showinSearchb != null)
                    {
                        member.ShowInSearch = showinSearchb;
                    }

                    member.DateModified = DateTime.Now;
                    // CLIFF (7/30/15): We used to set the Validated Date here automatically when the user completed their profile.
                    //                  But now we also need users to send SSN and DoB, so will set the Validated Date in SaveMemberSSN()
                    // member.ValidatedDate = DateTime.Now;
                    DbContext dbc = CommonHelper.GetDbContextFromEntity(member);
                    int value = dbc.SaveChanges();

                    return value > 0 ? "Your details have been updated successfully." : "Failure";
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> MySettings (Updating User's Profile) FAILED - [MemberID: " + memberId + "], [Exception: " + ex.Message + "]");

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
            // update nooch member pin number
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

        // to get member notification settings
        public MemberNotification GetMemberNotificationSettings(string memberId)
        {
            //Logger.LogDebugMessage("MDA -> GetMemberNotificationSettings Initiated - [MemberId: " + memberId + "]");


            Guid memId = Utility.ConvertToGuid(memberId);


            var memberNotifications =
                _dbContext.MemberNotifications.FirstOrDefault(m => m.MemberId == memId);


            return memberNotifications;

        }

        // to save email notification settings for the give user
        public string MemberEmailNotificationSettings(string notificationId, string memberId, bool? friendRequest,
            bool? inviteRequestAccept, bool transferSent, bool transferReceived, bool transferAttemptFailure,
            bool transferUnclaimed, bool bankToNoochRequested, bool bankToNoochCompleted, bool? noochToBankRequested,
            bool noochToBankCompleted, bool? inviteReminder, bool? lowBalance, bool? validationRemainder,
            bool? productUpdates, bool? newAndUpdate)
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
                            DateCreated = DateTime.Now
                        };
                        i++;
                        _dbContext.MemberNotifications.Add(memberNotification);
                        return _dbContext.SaveChanges() > 0 ? "Success" : "Failure";
                    }
                    i++;

                    memberNotifications.MemberId= Utility.ConvertToGuid(memberId);
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
                    memberNotifications.DateModified = DateTime.Now;

                    return _dbContext.SaveChanges() > 0 ? "Success" : "Failure";
                
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




        public string SetShowInSearch(string memberId, bool showInSearch)
        {
            Logger.Info("MDA -> SetShowInSearch - memberId: [" + memberId + "]");

            

                var id = Utility.ConvertToGuid(memberId);

                // code to update setting in members table 
                
            var memberSettings = _dbContext.Members.FirstOrDefault(m => m.MemberId == id);
                    
                if (memberSettings != null)
                {
                    // member found
                    memberSettings.ShowInSearch = showInSearch;
                    _dbContext.SaveChanges();
                    _dbContext.Entry(memberSettings).Reload();
                    
                }
                else
                {
                    return "Failure";
                }

                
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
                        _dbContext.Entry(memberSettings).Reload();
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
                {
                    return  "Failure";
                }
                
        }
    }
}
