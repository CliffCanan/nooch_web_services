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


        public string UpdateMemberIpAddressAndDeviceId(string MemberId, string IP, string DeviceId)
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

                    var ipAddressesFound = _dbContext.MembersIPAddresses.Where(m => m.MemberId == memId).ToList();

                    if (ipAddressesFound.Count() > 5)
                    {
                        // If there are already 5 entries, update the one added first (the oldest)
                        var lastIpFound = (from c in _dbContext.MembersIPAddresses where c.MemberId == memId select c)
                                          .OrderBy(m => m.ModifiedOn)
                                          .Take(1)
                                          .FirstOrDefault();

                        lastIpFound.ModifiedOn = DateTime.Now;
                        lastIpFound.Ip = IP;



                        int a = _dbContext.SaveChanges();

                        if (a > 0)
                        {
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (1) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - [MemberId: " + MemberId + "]");
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
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (2) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - [MemberId: " + MemberId + "]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - [MemberId: " + MemberId + "]");
            }

            #endregion Save IP Address


            #region Save Device ID

            if (!String.IsNullOrEmpty(DeviceId))
            {
                try
                {
                    // CLIFF (8/12/15): This "Device ID" will be stored in Nooch's DB as "UDID1" and is specifically for Synapse's "Fingerprint" requirement...
                    //                  NOT for push notifications, which should use the "DeviceToken" in Nooch's DB.  (Confusing, but they are different values)

                    var member = _dbContext.Members.FirstOrDefault(m => m.MemberId == memId);

                    if (member != null)
                    {
                        member.UDID1 = DeviceId;
                        member.DateModified = DateTime.Now;
                    }
                    int c = _dbContext.SaveChanges();

                    if (c > 0)
                    {
                        Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving Device ID - [MemberId: " + MemberId + "]");
                        udidIdSavedSuccessfully = true;
                    }
                    else
                    {
                        Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - [MemberId: " + MemberId + "]");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - [MemberId: " + MemberId + "]");
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

        public string getReferralCode(String memberId)
        {
            Logger.Info("MDA -> getReferralCode Initiated - [MemberId: " + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);

            var noochMember = _dbContext.Members.FirstOrDefault(mm => mm.MemberId == id && mm.IsDeleted == false);
            if (noochMember != null)
            {
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
                authenticationToken.IsActivated = true;
                authenticationToken.DateModified = DateTime.Now;

                Guid memGuid = authenticationToken.MemberId;

                if (_dbContext.SaveChanges() > 0)
                {
                    Logger.Info("MDA -> MemberActivation - Member activated successfully - [" + memGuid + "]");


                    var memberObj = _dbContext.Members.FirstOrDefault(mm => mm.MemberId == memGuid);

                    if (memberObj != null)
                    {
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
                                    Logger.Info("MDA -> MemberActivation - This is a TENANT - About to update Tenants Table " +
                                                          "MemberID: [" + memGuid + "]");

                                    tenantdObj.eMail = memberObj.UserName;
                                    tenantdObj.IsEmailVerified = true;
                                    tenantdObj.DateModified = DateTime.Now;

                                    int saveChangesToTenant = _dbContext.SaveChanges();

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
                    phoneEmailObj.memberId = noochMember.MemberId.ToString();
                }
                else
                {
                    // Then check for an EMAIL match

                    var tempEmailEnc = CommonHelper.GetEncryptedData(phoneEmailObj.emailAddy.ToLower());


                    noochMember = _dbContext.Members.FirstOrDefault(m => m.UserName == tempEmailEnc || m.FacebookAccountLogin == tempEmailEnc && m.IsDeleted == false);

                    if (noochMember != null)
                    {
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
                                        Utility.SendEmail("",  fromAddress, toAddress, null,
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
                                        Utility.SendEmail("userSuspended",  fromAddress,
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
                            .BeforeMap((src, dest) => src.LastName= CommonHelper.GetDecryptedData(src.LastName))
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

                var noochInviteMember = _dbContext.InviteCodes.FirstOrDefault(m=>m.code==invitationCode && m.count<m.totalAllowed);
                return noochInviteMember != null;
            
        }


        public bool getTotalReferralCode(String referalCode)
        {
            Logger.Info("MDA -> getReferralCode Initiated - referalCode: [" + referalCode + "]");

                
                var inviteMember = _dbContext.InviteCodes.FirstOrDefault(m=>m.code==referalCode);

                if (inviteMember != null)
                {
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
                result =  _dbContext.SaveChanges();

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
        public string UpdateMemberIPAddressAndDeviceId(string MemberId, string IP, string DeviceId)
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
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (1) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (1) in DB - [MemberId: " + MemberId + "]");
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
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving IP Address (2) - [MemberId: " + MemberId + "]");
                            ipSavedSuccessfully = true;
                        }
                        else
                        {
                            Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving IP Address (2) in DB - [MemberId: " + MemberId + "]");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving IP Address - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId - No IP Address Passed - [MemberId: " + MemberId + "]");
            }

            #endregion Save IP Address


            #region Save Device ID

            if (!String.IsNullOrEmpty(DeviceId))
            {
                try
                {
                    // CLIFF (8/12/15): This "Device ID" will be stored in Nooch's DB as "UDID1" and is specifically for Synapse's "Fingerprint" requirement...
                    //                  NOT for push notifications, which should use the "DeviceToken" in Nooch's DB.  (Confusing, but they are different values)


                    var member = _dbContext.Members.Where(memberTemp => memberTemp.MemberId == memId).FirstOrDefault();

                    if (member != null)
                    {
                        member.UDID1 = DeviceId;
                        member.DateModified = DateTime.Now;
                    }

                    int c = _dbContext.SaveChanges();

                    if (c > 0)
                    {
                        Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId SUCCESS For Saving Device ID - [MemberId: " + MemberId + "]");
                        udidIdSavedSuccessfully = true;
                    }
                    else
                    {
                        Logger.Info("MDA -> UpdateMemberIPAddressAndDeviceId FAILED Trying To Saving Device ID in DB - [MemberId: " + MemberId + "]");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId FAILED For Saving Device ID - [Exception: " + ex + "]");
                }
            }
            else
            {
                Logger.Error("MDA -> UpdateMemberIPAddressAndDeviceId - No Device ID Passed - [MemberId: " + MemberId + "]");
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
                    var userNameLowerCase = CommonHelper.GetEncryptedData(EmailId.ToLower());
                    


                    var memdetails = _dbContext.Members.Where(memberTemp =>
                                (memberTemp.UserName == userNameLowerCase || memberTemp.UserNameLowerCase== userNameLowerCase) &&
                                memberTemp.IsDeleted == false).FirstOrDefault();

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
                                        fromAddress,CommonHelper.GetDecryptedData(userNameLowerCase), null, "Confirm your email on Nooch", link,
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
                                                          CommonHelper.GetDecryptedData( userNameLowerCase )+ "], [Exception: " + ex + "]");
                                }
                                #endregion Send Temp PIN Email

                                #region Send Email Notification To Referrer (If Applicable)

                                if (!String.IsNullOrEmpty(inviteCode))
                                {
                                    try
                                    {
                                        Guid invideCodeGuid = Utility.ConvertToGuid(inviteCode);


                                        var inviteCodeObj = _dbContext.InviteCodes.Where(inviteTemp => inviteTemp.InviteCodeId == invideCodeGuid).FirstOrDefault();

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

        public synapseCreateUserV3Result_int RegisterUserWithSynapseV3(string memberId, bool isSandbox)
        {
            Logger.Info("MDA -> RegisterUserWithSynapseV3 Initiated - [Member: " + memberId + "]");

            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;

            
                Guid id = Utility.ConvertToGuid(memberId);

                //Get the member details

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
                    logins.password = CommonHelper.GetDecryptedData(noochMember.Password);
                    logins.read_only = true; // CLIFF (10/10/12) - I think we might want to keep this false (which is default) - will ask Synapse to clarify

                    payload.logins = new createUser_login[1];
                    payload.logins[0] = logins;

                    payload.phone_numbers = new string[] { noochMember.ContactNumber };
                    payload.legal_names = new string[] { fullname };

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
                        // Found this trick online for generating "random" (not technically) string... make a GUID and remove the "-" (with "n")
                        string randomTxt = Guid.NewGuid().ToString("n").Substring(0, 24);
                        fingerprints.fingerprint = randomTxt;

                        // Now we need to also save this new value for the user in the DB
                        try
                        {
                            noochMember.UDID1 = randomTxt;
                            _dbContext.SaveChanges();
//                            membersRepository.UpdateEntity(noochMember);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> RegisterUserWithSynapseV3 - Had to create a Fingerprint, but failed on saving new value in DB - Continuing on - [MemberID: "
                                                   + memberId + "], [Exception: " + ex + "]");
                        }
                    }
                    payload.fingerprints = new createUser_fingerprints[1];
                    payload.fingerprints[0] = fingerprints;

                    payload.ips = new string[] { CommonHelper.GetRecentOrDefaultIPOfMember(id) };

                    createUser_extra extra = new createUser_extra();
                    extra.note = "";
                    extra.supp_id = noochMember.Nooch_ID;
                    extra.is_business = false; // CLIFF (10/10/12): For Landlords, this could potentially be true... but we'll figure that out later

                    payload.extra = extra;
                    var baseAddress = "https://synapsepay.com/api/v3/user/create";
                    if (isSandbox)
                    {
                     baseAddress = "https://sandbox.synapsepay.com/api/v3/user/create";
                    }
                    //var baseAddress = "https://synapsepay.com/api/v3/user/create";

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
                                        m => m.MemberId == id && m.IsDeleted == false);

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
                        res.user_id = !String.IsNullOrEmpty(res.user.client.id) ? res.user.client.id : "";
                        res.ssn_verify_status = "did not check yet";

                        // Mark any existing Synapse 'Create User' results for this user as Deleted
                        #region Delete Any Old DB Records & Create New Record

                        
                        var synapseCreateUserObj =
                            _dbContext.SynapseCreateUserResults.Where(m => m.MemberId == id && m.IsDeleted == false)
                                .ToList();
                        // CLIFF (10/10/15): Shouldn't the above line create a LIST instead of just selecting the First? In case there are more than one...
                        //                   That should never happen, but still...
                        // modified this to handle more than one record..... 23-Feb-2016

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


                        #region Add New Entry To SynapseCreateUserResults DB Table

                        int addRecordToSynapseCreateUserTable = 0;

                        try
                        {
                            SynapseCreateUserResult newSynapseUser = new SynapseCreateUserResult();
                            newSynapseUser.MemberId = id;
                            newSynapseUser.DateCreated = DateTime.Now;
                            newSynapseUser.IsDeleted = false;
                            newSynapseUser.access_token = CommonHelper.GetEncryptedData(res.oauth.oauth_key);
                            newSynapseUser.success = Convert.ToBoolean(res.success);
                            newSynapseUser.expires_in = res.oauth.expires_at;
                            newSynapseUser.refresh_token = CommonHelper.GetEncryptedData(res.oauth.refresh_token);
                            newSynapseUser.username = CommonHelper.GetEncryptedData(res.user.logins[0].email);
                            newSynapseUser.user_id = res.user.client.id; // this is no more int... this will be string from now onwards

                            // LETS USE THE EXISTING V2 DB TABLE FOR V3: 'SynapseCreateUserResults'... all the same, PLUS a few additional parameters (none are that important, but we should store them):
                            // NEED TO ADD THE FOLLOWING PARAMETERS TO DATABASE: is_business (bool); legal_name (string); permission (string); phone number (string); photos (string)

                            // Adding data for new fields in Synapse V3
                            newSynapseUser.is_business = res.user.extra.is_business;
                            newSynapseUser.legal_name = res.user.legal_names.Length > 0 ? res.user.legal_names[0] : null;
                            newSynapseUser.permission = res.user.permission ?? null;
                            newSynapseUser.Phone_number = res.user.phone_numbers.Length > 0 ? res.user.phone_numbers[0] : null;
                            newSynapseUser.photos = res.user.photos.Length > 0 ? res.user.photos[0] : null;

                            // Now add the new record to the DB
                            _dbContext.SynapseCreateUserResults.Add(newSynapseUser);

                            addRecordToSynapseCreateUserTable = _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("MDA -> RegisterUserWithSynapseV3 - FAILED To Save New Record in 'Synapse Create User Results' Table - " +
                                                   "[MemberID: " + memberId + "], [Exception: " + ex.InnerException + "]");
                        }

                        #endregion Add New Entry To SynapseCreateUserResults DB Table


                        #endregion Delete Any Old DB Records & Create New Record


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
                            return res;
                        }
                    }

                    #endregion Synapse Create User Response was SUCCESSFUL


                    // CLIFF (10/10/15) - DON'T THINK THIS REGION IS NECESSARY FOR SYNAPSE V3... If the call was ever unsuccessful, it would
                    //                    return one of the HTTP errors and be handled in Catch block above
                    #region Synapse Create User Response Success Was False

                    if (res.success == false)
                    {
                        // Check if we have user id in SynapseCreateUserResults table in Nooch DB

                        var synapseRes = _dbContext.SynapseCreateUserResults.FirstOrDefault(m=>m.MemberId==id && m.IsDeleted==false);

                        if (synapseRes != null)
                        {
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


        public GenericInternalResponseForSynapseMethods submitDocumentToSynapseV3(string MemberId, string ImageUrl)
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

                
            var member = CommonHelper.GetMemberDetails(id.ToString());;

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
                    
                    
                    var noochMember =  CommonHelper.GetMemberDetails(id.ToString());

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

                            string UrlToHit = "https://sandbox.synapsepay.com/api/v3/node/verify";
                            //string UrlToHit = "https://synapsepay.com/api/v3/node/verify";

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
                     dbContext.SaveChanges();

                    return "SSN saved successfully.";
                }
                else
                {
                    return "Member Id not found or Member status deleted.";
                }
            
        }
        
    }
}
