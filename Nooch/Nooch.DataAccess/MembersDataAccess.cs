using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Nooch.Common;
using Nooch.Common.Resources;
using Nooch.Data;

namespace Nooch.DataAccess
{
    public class MembersDataAccess
    {
        private readonly NOOCHEntities dbContext = null;

        public MembersDataAccess()
        {
            dbContext = new NOOCHEntities();
        }
        public Member GetMemberByGuid(Guid memberGuid)
        {
            return dbContext.Members.FirstOrDefault(m => m.MemberId == memberGuid);
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
                        
                        var ipAddressesFound = dbContext.MembersIPAddresses.Where(m => m.MemberId == memId).ToList();  

                        if (ipAddressesFound.Count()> 5)
                        {
                            // If there are already 5 entries, update the one added first (the oldest)
                            var lastIpFound = (from c in dbContext.MembersIPAddresses where c.MemberId == memId select c)
                                              .OrderBy(m => m.ModifiedOn)
                                              .Take(1)
                                              .FirstOrDefault();

                            lastIpFound.ModifiedOn = DateTime.Now;
                            lastIpFound.Ip = IP;



                            int a = dbContext.SaveChanges();

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
                            dbContext.MembersIPAddresses.Add(mip);
                            int b = dbContext.SaveChanges();

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
                        
                        var member = dbContext.Members.FirstOrDefault(m => m.MemberId==memId);

                        if (member != null)
                        {
                            member.UDID1 = DeviceId;
                            member.DateModified = DateTime.Now;
                        }
                        int c = dbContext.SaveChanges();

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

    }
}
