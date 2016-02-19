﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Nooch.Common;
using Nooch.Common.Entities.MobileAppOutputEnities;
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
        //                Logger.LogDebugMessage("MDA -> LoginRequest FAILED - User is Already TEMPORARILY_BLOCKED - UserName: [" + userName + "]");
        //                return "Temporarily_Blocked";
        //            }
        //            else if (memberEntity.Status == "Suspended")
        //            {
        //                Logger.LogDebugMessage("MDA -> LoginRequest FAILED - User is Already SUSPENDED - UserName: [" + userName + "]");
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
        //                    Logger.LogDebugMessage("MDA -> LoginRequest - Sending Automatic Logout Notification - [UserName: " + userEmail +
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

        //                        Logger.LogDebugMessage("MDA -> LoginRequest - Automatic Log Out Email sent to [" + toAddress + "] successfully.");

        //                        // Checking if phone exists and isVerified before sending SMS to user
        //                        if (memberEntity.ContactNumber != null && memberEntity.IsVerifiedPhone == true)
        //                        {
        //                            try
        //                            {
        //                                //msg = "Hi, You were automatically logged out from your Nooch account b/c you signed in from another device. " +
        //                                // "If this is a mistake, contact support@nooch.com immediately. - Nooch";
        //                                //string result = UtilityDataAccess.SendSMS(memberEntity.ContactNumber, msg, memberEntity.AccessToken, memberEntity.MemberId.ToString());

        //                                //Logger.LogDebugMessage("MDA -> LoginRequest - Automatic Log Out SMS sent to [" + memberEntity.ContactNumber + "] successfully. [SendSMS Result: " + result + "]");
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
        //                    Logger.LogDebugMessage("MDA -> LoginRequest FAILED - User's PW was incorrect - 1st Invalid Attempt - UserName: [" + userName + "]");

        //                    return IncreaseInvalidLoginAttemptCount(membersRepository, memberEntity, loginRetryCountInDb);
        //                }

        //                else if (loginRetryCountInDb == 4)
        //                {
        //                    // Already Suspended
        //                    Logger.LogDebugMessage("MDA -> LoginRequest FAILED - User's PW was incorrect - User Already Suspended - UserName: [" + userName + "]");

        //                    return String.Concat("Your account has been temporarily blocked.  You can login only after 24 hours from this time: ",
        //                                         memberEntity.InvalidLoginTime);
        //                }

        //                else if (loginRetryCountInDb == 3)
        //                {
        //                    // This is 4th try, so suspend the member
        //                    Logger.LogDebugMessage("MDA -> LoginRequest FAILED - User's PW was incorrect - 3rd Invalid Attempt, now suspending user - UserName: [" + userName + "]");

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
        //                        Logger.LogDebugMessage(
        //                            "SupendMember - Attempt to send mail for Supend Member[ memberId:" +
        //                            memberEntity.MemberId + "].");
        //                        UtilityDataAccess.SendEmail("userSuspended", MailPriority.High, fromAddress,
        //                            emailAddress, null, "Your Nooch account has been suspended", null, tokens, null,
        //                            null, null);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        Logger.LogDebugMessage("SupendMember - Supend Member status email not send to [" +
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
                //Logger.LogDebugMessage("Service layer -> GetMostFrequentFriends - MemberId: [" + MemberId + "]");

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

                                                //Logger.LogDebugMessage("MDA -> LoginRequest - Automatic Log Out SMS sent to [" + memberEntity.ContactNumber + "] successfully. [SendSMS Result: " + result + "]");
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

        
    }
}
