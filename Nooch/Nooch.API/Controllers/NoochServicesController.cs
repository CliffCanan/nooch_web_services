using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Helpers;
using System.Web.Http;
using AutoMapper;
using Nooch.Common;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities;
using Nooch.Common.Entities.MobileAppInputEntities;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Data;
using Nooch.DataAccess;
using System.Collections.ObjectModel;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Common.Resources;
using Nooch.Common.Entities.LandingPagesRelatedEntities;
using System.Drawing;
using ImageProcessor;
using System.IO.Compression;
using Hangfire;
using System.Web.Http.Cors;


namespace Nooch.API.Controllers
{
    // Malkit (23 July 2016)
    // Make sure to not push code to production server with CORS line uncommented 
    // CORS exposes api's for cross site scripting, added these to use on dev server only for the purpose of testing ionic app in browser

    [EnableCors(origins: "*", headers: "*", methods: "*")]

    public class NoochServicesController : ApiController
    {

        private readonly NOOCHEntities _dbContext = null;

        public NoochServicesController()
        {
            _dbContext = new NOOCHEntities();
        }


        [HttpPost]
        [ActionName("ForgotPassword")]
        public AppLogin ForgotPassword(StringInput userName)
        {
            AppLogin res = new AppLogin();
            res.success = false;

            try
            {
                //Logger.Info("Service Cntrlr -> ForgotPassword - UserName: [" + userName.Input + "]");
                res = CommonHelper.ForgotPassword(userName.Input);
            }
            catch (Exception ex)
            {
                Logger.Info("Service Cntrlr -> ForgotPassword FAILED - UserName: [" + userName.Input + "], Exception: [" + ex + "]");
                res.msg = "Server Exception: [" + ex.Message + "]";
            }

            return res;
        }


        [HttpPost]
        [ActionName("UdateMemberIPAddress")]
        public StringResult UdateMemberIPAddress(UpdateMemberIpInput member)
        {
            if (CommonHelper.IsValidRequest(member.AccessToken, member.MemberId))
            {
                try
                {
                    var res = CommonHelper.UpdateMemberIPAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId);
                    return new StringResult() { Result = res };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> UpdateMemberIPAddressAndDeviceId FAILED - MemberID: [" + member.MemberId +
                                 "], Exception: [" + ex + "]");
                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("UdateMemberNotificationTokenAndDeviceInfo")]
        public StringResult UdateMemberNotificationTokenAndDeviceInfo(UdateMemberNotificationTokenAndDeviceInfoInput member)
        {
            if (CommonHelper.IsValidRequest(member.AccessToken, member.MemberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    return new StringResult() { Result = CommonHelper.UdateMemberNotificationTokenAndDeviceInfo(member.MemberId, member.NotificationToken, member.DeviceId, member.DeviceOS) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> UdateMemberNotificationTokenAndDeviceInfo FAILED - MemberID: [" + member.MemberId +
                                 "], Exception: [" + ex + "]");
                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetMemberByUdId")]
        public MemberDto GetMemberByUdId(string udId, string accessToken, string memberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - GetPrimaryEmail [udId: " + udId + "]");

                    var memberEntity = CommonHelper.GetMemberByUdId(udId);
                    var member = new MemberDto { UserName = memberEntity.UserName, Status = memberEntity.Status };
                    return member;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetMemberByUdId FAILED - Exception: [" + ex.Message + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// Get a user's pending transactions count.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="AccessToken"></param>
        /// <returns>PendingTransCoutResult [sic] object with values for 4 types of pending transactions: requests sent/received, invites, disputes unresolved.</returns>
        [HttpGet]
        [ActionName("GetMemberPendingTransctionsCount")]
        public PendingTransCountResult GetMemberPendingTransctionsCount(string MemberId, string AccessToken)
        {
            var res = new PendingTransCountResult();

            if (CommonHelper.IsValidRequest(AccessToken, MemberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Cntlr -> GetMemberPendingTransctionsCount - MemberId: [" + MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    res = tda.GetMemberPendingTransCount(MemberId);
                }
                catch (Exception)
                {
                    //throw new Exception("Server Error");
                    return new PendingTransCountResult { pendingRequestsSent = "0", pendingRequestsReceived = "0", pendingInvitationsSent = "0", pendingDisputesNotSolved = "0" };
                }
            }
            else
            {
                //throw new Exception("Invalid OAuth 2 Access");
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetMemberIdByUserName")]
        public StringResult GetMemberIdByUserName(string userName)
        {
            try
            {
                return new StringResult { Result = CommonHelper.GetMemberIdByUserName(userName) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetMemberByUserName FAILED - [userName: " + userName +
                             "], Exception: [" + ex + "]");
            }
            return new StringResult();
        }


        [HttpGet]
        [ActionName("GetMemberUsernameByMemberId")]
        public StringResult GetMemberUsernameByMemberId(string memberId)
        {
            try
            {
                return new StringResult { Result = CommonHelper.GetMemberUsernameByMemberId(memberId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetMemberUsernameByMemberId FAILED - MemberID: [" + memberId +
                             "], Exception: [" + ex + "]");
            }
            return new StringResult();
        }


        [HttpGet]
        [ActionName("GetPhoneNumberByMemberId")]
        public StringResult GetPhoneNumberByMemberId(string memberId)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> GetPhoneNumberByMemberId Fired - MemberID: [" + memberId + "]");
                res.Result = CommonHelper.GetPhoneNumberByMemberId(memberId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetPhoneNumberByMemberId FAILED - Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetMemberIdByPhone")]
        public StringResult GetMemberIdByPhone(string phoneNo, string accessToken)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> GetMemberByPhone Fired - Phone #: [" + phoneNo + "]");
                res.Result = CommonHelper.GetMemberIdByPhone(phoneNo);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetMemberByPhone FAILED - Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        /// <summary>
        /// CC (9/6/16): NOT CURRENTLY USED ANYWHERE.
        /// </summary>
        /// <param name="phoneEmailListDto"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("GetMemberIds")]
        public PhoneEmailListDto GetMemberIds(PhoneEmailListDto phoneEmailListDto)
        {
            try
            {
                //Logger.LogDebugMessage("Service Cntlr - GetMemberIds Fired - userName: [" + phoneEmailListDto + "]");
                var mda = new MembersDataAccess();
                return mda.GetMemberIds(phoneEmailListDto);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetMemberIds FAILED - Exception: [" + ex + "]");
                return new PhoneEmailListDto();
            }
        }


        [HttpGet]
        [ActionName("CheckIfEmailIsRegistered")]
        public checkEmailForMobileApp CheckIfEmailIsRegistered(string email)
        {
            checkEmailForMobileApp res = new checkEmailForMobileApp();
            res.matchFound = false;

            try
            {
                var memberObj = CommonHelper.GetMemberDetailsByUserName(email);

                if (memberObj != null)
                {
                    res.matchFound = true;
                    res.firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                    //res.lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(test.LastName));
                    res.email = email;
                    res.rememberMe = memberObj.RememberMeEnabled ?? true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> CheckIfEmailIsRegistered FAILED - userName: [" + email +
                             "], Exception: [" + ex + "]");
            }

            return res;
        }


        [HttpGet]
        [ActionName("MemberActivation")]
        public BoolResult MemberActivation(string tokenId)
        {
            BoolResult res = new BoolResult();

            try
            {
                Logger.Info("Service Cntlr -> MemberActivation Fired - TokenId: [" + tokenId + "]");
                var mda = new MembersDataAccess();
                res.Result = mda.MemberActivation(tokenId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> MemberActivation FAILED - TokenID: [" + tokenId + "], Exception: [" + ex + "]");
                res.Result = false;
            }

            return res;
        }


        [HttpGet]
        [ActionName("IsMemberActivated")]
        public BoolResult IsMemberActivated(string tokenId)
        {
            BoolResult res = new BoolResult();

            try
            {
                res.Result = CommonHelper.IsMemberActivated(tokenId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> IsMemberActivated FAILED - TokenID: [" + tokenId + "], Exception: [" + ex + "]");
                res.Result = false;
            }

            return res;
        }


        [HttpGet]
        [ActionName("IsNonNoochMemberActivated")]
        public BoolResult IsNonNoochMemberActivated(string emailId)
        {
            BoolResult res = new BoolResult();

            try
            {
                Logger.Info("Service Cntlr -> IsNonNoochMemberActivated Fired - Email ID: [" + emailId + "]");
                res.Result = CommonHelper.IsNonNoochMemberActivated(emailId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> IsNonNoochMemberActivated FAILED - tokenId: [" + emailId + "]. Exception: [" + ex + "]");
                res.Result = false;
            }

            return res;
        }


        [HttpGet]
        [ActionName("IsDuplicateMember")]
        public StringResult IsDuplicateMember(string userName)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr - IsDuplicateMember Fired - userName: [" + userName + "]");
                res.Result = CommonHelper.IsDuplicateMember(userName);
            }
            catch (Exception ex)
            {
                res.Result = ex.ToString();
            }

            return res;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public MemberDto GetEncryptedData(string data)
        {
            try
            {
                string DecodedMessage = Base64Decode(data);

                var aesAlgorithm = new AES();
                string encryptedData = aesAlgorithm.Encrypt(DecodedMessage, string.Empty);
                MemberDto obj = new MemberDto { Status = encryptedData.Replace(" ", "+") };
                return new MemberDto { Status = encryptedData.Replace(" ", "+") };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetEncryptedData FAILED - sourceData: [" + data + "], Exception: [" + ex + "]");
                return new MemberDto();
            }
        }

        public MemberDto GetDecryptedData(string sourceData)
        {
            try
            {
                //Logger.LogDebugMessage("Service Cntlr - GetDecryptedData - sourceData [" + sourceData + "]");

                var aesAlgorithm = new AES();
                string decryptedData = aesAlgorithm.Decrypt(sourceData.Replace(" ", "+"), string.Empty);

                string Base64EncodedData = Base64Encode(decryptedData);

                return new MemberDto { Status = Base64EncodedData };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetDecryptedData FAILED - sourceData: [" + sourceData + "]. Exception: [" + ex + "]");
                return new MemberDto();
            }
        }

        public StringResult GetServerCurrentTime()
        {
            try
            {
                return new StringResult { Result = string.Format("{0:MM/d/yyyy hh:mm:ss tt}", DateTime.Now) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetServerCurrentTime FAILED - Exception: [" + ex + "]");
                return new StringResult { Result = "" };
            }
        }


        [HttpGet]
        [ActionName("GetUserDetailsForApp")]
        public userDetailsForMobileApp GetUserDetailsForApp(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> GetUserDetailsForApp - MemberID: [" + memberId + "]");

                    // Get Member's Details
                    var memberObj = CommonHelper.GetMemberDetails(memberId);

                    // Get Synapse Bank Account Info
                    var synUserDetails = CommonHelper.GetSynapseCreateaUserDetails(memberId);
                    var synBankDetails = CommonHelper.GetSynapseBankDetails(memberId);

                    var res = new userDetailsForMobileApp
                    {
                        memberId = memberObj.MemberId.ToString(),
                        DateCreated = memberObj.DateCreated.Value,
                        status = memberObj.Status,
                        email = CommonHelper.GetDecryptedData(memberObj.UserName),
                        contactNumber = !String.IsNullOrEmpty(memberObj.ContactNumber) && memberObj.ContactNumber.Length > 2
                                            ? CommonHelper.FormatPhoneNumber(memberObj.ContactNumber) : "",
                        firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName)),
                        lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName)),
                        userPicture = memberObj.Photo ?? Path.GetFileName("././img/profile_picture.png"),
                        pin = memberObj.PinNumber,
                        rememberMe = memberObj.RememberMeEnabled ?? false,
                        cip_tag = memberObj.cipTag,

                        fbUserId = memberObj.FacebookUserId != "not connected" ? memberObj.FacebookUserId : null,

                        hasSynapseUserAccount = synUserDetails != null && synUserDetails.access_token != null,
                        hasSynapseBank = synBankDetails != null,
                        hasSubmittedId = !String.IsNullOrEmpty(memberObj.VerificationDocumentPath) ||
                                         (synUserDetails != null && !String.IsNullOrEmpty(synUserDetails.photos))
                                         ? true : false,
                        isBankVerified = synBankDetails != null && synBankDetails.Status == "Verified",
                        bankStatus = synBankDetails != null ? synBankDetails.Status : "Not Attached",
                        synUserPermission = synUserDetails != null ? synUserDetails.permission : "",
                        synBankAllowed = synBankDetails != null ? synBankDetails.allowed : "",

                        isProfileComplete = !string.IsNullOrEmpty(memberObj.Address) &&
                                                !string.IsNullOrEmpty(memberObj.City) &&
                                                !string.IsNullOrEmpty(memberObj.Zipcode) &&
                                                !string.IsNullOrEmpty(memberObj.ContactNumber) &&
                                                !string.IsNullOrEmpty(memberObj.SSN) &&
                                                memberObj.DateOfBirth != null,
                        isRequiredImmediately = memberObj.IsRequiredImmediatley ?? false,
                        showInSearch = memberObj.ShowInSearch ?? false,
                        isVerifiedPhone = memberObj.IsVerifiedPhone == true ? true : false,
                    };

                    return res;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetUserDetailsForApp FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("GetMostFrequentFriends")]
        public List<GetMostFrequentFriends_Result> GetMostFrequentFriends(string MemberId, string accesstoken)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    List<GetMostFrequentFriends_Result> listMostFrequentFriends = new List<GetMostFrequentFriends_Result>();
                    MembersDataAccess mda = new MembersDataAccess();
                    listMostFrequentFriends = mda.GetMostFrequentFriends(MemberId);

                    foreach (var friends in listMostFrequentFriends)
                    {
                        Member m = CommonHelper.GetMemberDetails(friends.RecepientId);
                        friends.FirstName = !String.IsNullOrEmpty(m.FirstName) ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(m.FirstName)) : "";
                        friends.LastName = !String.IsNullOrEmpty(m.LastName) ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(m.LastName)) : "";
                        friends.Photo = m.Photo;
                    }

                    return listMostFrequentFriends;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetMostFrequentFriends FAILED - Exception: [" + ex + "]");
                    throw new Exception("Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("DeleteAttachedBankNode")]
        public string DeleteAttachedBankNode(string memberid)
        {

            try
            {
                Logger.Error("Service Cntlr -> DeleteAttachedBankNode for - MemberID: [" + memberid + "]");
                Guid MemId = Utility.ConvertToGuid(memberid);

                var synBankDetails = _dbContext.SynapseBanksOfMembers.Where(b => b.MemberId == MemId && b.IsDefault == true).FirstOrDefault();
                if (synBankDetails != null)
                {
                    synBankDetails.is_active = false;
                    synBankDetails.IsDefault = false;
                    _dbContext.SaveChanges();
                    return "Deleted";
                }
                else
                {
                    return "Bank not found";
                }


            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> DeleteAttachedBankNode FAILED - MemberID: [" + memberid + "], Exception: [" + ex.InnerException + "]");
                throw new Exception("Server Error");
            }


        }


        [HttpGet]
        [ActionName("GetMemberStats")]
        public StringResult GetMemberStats(string MemberId, string accesstoken, string query)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Cntlr - GetMemberStats - MemberId].");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.GetMemberStats(MemberId, query) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetMemberStats FAILED - [Exception: " + ex + "]");

                    throw new Exception("Invalid OAuth 2 Access");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetMemberStatsGeneric")]
        public StatsForMember GetMemberStatsGeneric(string MemberId, string accesstoken)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    StatsForMember sm = new StatsForMember();
                    //Logger.LogDebugMessage("Service Cntlr - GetMemberStats - MemberId].");
                    var memberDataAccess = new MembersDataAccess();
                    sm.Largest_received_transfer = memberDataAccess.GetMemberStats(MemberId, "Largest_received_transfer");
                    sm.Largest_sent_transfer = memberDataAccess.GetMemberStats(MemberId, "Largest_sent_transfer");
                    sm.Total_Friends_Invited = memberDataAccess.GetMemberStats(MemberId, "Total_Friends_Invited");
                    sm.Total_Friends_Joined = memberDataAccess.GetMemberStats(MemberId, "Total_Friends_Joined");
                    sm.Total_no_of_transfer_Received = memberDataAccess.GetMemberStats(MemberId, "Total_no_of_transfer_Received");
                    sm.Total_no_of_transfer_Sent = memberDataAccess.GetMemberStats(MemberId, "Total_no_of_transfer_Sent");
                    sm.Total_P2P_transfers = memberDataAccess.GetMemberStats(MemberId, "Total_P2P_transfers");
                    sm.Total_Sent = memberDataAccess.GetMemberStats(MemberId, "Total_$_Sent");
                    sm.Total_Received = memberDataAccess.GetMemberStats(MemberId, "Total_$_Received");

                    sm.Total_Posts_To_TW = memberDataAccess.GetMemberStats(MemberId, "Total_Posts_To_TW");
                    return sm;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> GetMemberStats FAILED - [Exception: " + ex + "]");

                    throw new Exception("Invalid OAuth 2 Access");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("getInvitedMemberList")]
        public List<MemberForInvitedMembersList> getInvitedMemberList(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                var mda = new MembersDataAccess();
                List<Member> members = mda.getInvitedMemberList(memberId);

                List<MemberForInvitedMembersList> invitedUsersList = new List<MemberForInvitedMembersList>();

                if (members != null && members.Count > 0)
                {
                    foreach (var user in members)
                    {
                        if (user.MemberId.ToString() != memberId)
                        {
                            MemberForInvitedMembersList m = new MemberForInvitedMembersList
                            {
                                FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(user.FirstName)),
                                LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(user.LastName)),
                                UserName = CommonHelper.GetDecryptedData(user.UserName),
                                MemberId = user.MemberId.ToString(),
                                //NoochId = user.Nooch_ID, // Not needed by the mobile app
                                DateCreated = user.DateCreated,
                                Status = user.Status,
                                Photo = user.Photo,
                            };

                            invitedUsersList.Add(m);
                        }
                    }
                }

                return invitedUsersList;

                // CC (9/2/16): Commenting out this way of doing it which was including the user who made the request in the list to send back to the app.
                //return (from fMember in members
                //        let config = new MapperConfiguration(cfg =>
                //            {
                //                cfg.CreateMap<Member, MemberForInvitedMembersList>()
                //                    .BeforeMap((src, dest) =>
                //                        src.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.FirstName)))
                //                    .BeforeMap((src, dest) =>
                //                        src.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.LastName)))
                //                    .BeforeMap((src, dest) =>
                //                        src.UserName = CommonHelper.GetDecryptedData(src.UserName));
                //            })
                //        let mapper = config.CreateMapper()
                //        select mapper.Map<Member, MemberForInvitedMembersList>(fMember)).ToList();
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("SaveMemberDeviceToken")]
        public StringResult SaveMemberDeviceToken(string memberId, string accessToken, string deviceToken)
        {
            Logger.Info("Service Cntrlr -> SaveMemberDeviceToken Fired - MemberID: [" + memberId + "], DeviceToken: [" + deviceToken + "]");

            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                if (!String.IsNullOrEmpty(deviceToken))
                {
                    try
                    {
                        MembersDataAccess mda = new MembersDataAccess();
                        res.Result = mda.SaveMemberDeviceToken(memberId, deviceToken);

                        return res;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Service Cntlr -> SaveMemberDeviceToken FAILED - MemberId: [" + memberId + "]. Exception: [" + ex + "]");
                        throw new Exception("Server Error.");
                    }
                }
                else
                    res.Result = "No DeviceToken was sent!";

                res.Result = "Failed to save DeviceToken";
                return res;
            }
            else
            {
                Logger.Error("Service Cntlr -> SaveMemberDeviceToken FAILED - MemberID: [" + memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetLocationSearch")]
        public List<LocationSearch> GetLocationSearch(string accessToken, string MemberId, int Radius)
        {
            try
            {
                if (CommonHelper.IsValidRequest(accessToken, MemberId))
                {
                    var mda = new MembersDataAccess();
                    List<LocationSearch> list = mda.GetLocationSearch(MemberId, Radius);
                    return list;
                }
                else
                {
                    throw new Exception("Invalid OAuth 2 Access");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        [HttpPost]
        [ActionName("validateInvitationCode")]
        public Boolean validateInvitationCode(string invitationCode)
        {
            //No access token is added as the new user requires to authenticate invitecode.
            var mda = new MembersDataAccess();
            return mda.validateInvitationCode(invitationCode);
        }


        [HttpPost]
        [ActionName("getReferralCode")]
        public StringResult getReferralCode(string memberId, string accessToken)
        {
            return CommonHelper.IsValidRequest(accessToken, memberId) ? new StringResult { Result = CommonHelper.GetMemberReferralCodeByMemberId(memberId) } : new StringResult { Result = "Invalid OAuth 2 Access" };
        }


        [HttpPost]
        [ActionName("getTotalReferralCode")]
        public StringResult getTotalReferralCode(string referalCode)
        {
            var mda = new MembersDataAccess();
            var isValid = mda.getTotalReferralCode(referalCode);
            return new StringResult { Result = isValid.ToString() };
        }


        [HttpPost]
        [ActionName("ApiSMS")]
        public StringResult ApiSMS(string phoneto, string msg, string accessToken, string memberId)
        {
            if ((msg == "Hi\n You were automatically logged out from Nooch because you signed in from another device.\n - Team Nooch") || CommonHelper.IsValidRequest(accessToken, memberId))
            {
                return new StringResult { Result = Utility.SendSMS(phoneto, msg) };
            }
            throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("sendSMS")]
        public StringResult ApiSMS(string to, string msg)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntrlr - sendSMS Fired - MemberID: [" + to + "]. Exception: [" + msg + "]");

                if (String.IsNullOrEmpty(to))
                {
                    res.Result = "Missing phone number to send to.";
                    return res;
                }
                if (String.IsNullOrEmpty(msg))
                {
                    res.Result = "Missing message to send.";
                    return res;
                }

                res.Result = Utility.SendSMS(to, msg);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - sendSMS FAILED - MemberId: [" + to + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("SendTransactionReminderEmail")]
        public StringResult SendTransactionReminderEmail(string ReminderType, string TransactionId, string accessToken, string MemberId)
        {
            //if (CommonHelper.IsValidRequest(accessToken, MemberId)) {
            try
            {
                Logger.Info("Service Cntrlr - SendTransactionReminderEmail - MemberID: [" + MemberId + "], ReminderType: [" + ReminderType + "]");
                var tda = new TransactionsDataAccess();

                return new StringResult { Result = tda.SendTransactionReminderEmail(ReminderType, TransactionId, MemberId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - SendTransactionReminderEmail FAILED - MemberID: [" + MemberId + "], Exception: [" + ex + "]");
                throw new Exception("Server Error.");
            }
            //}
            //else throw new Exception("Invalid OAuth 2 Access");
        }


        /***********************************/
        /****  REQUEST-RELATED METHODS  ****/
        /***********************************/

        #region Request Methods

        /// <summary>
        /// For an existing user to make a request to existing user who never registered for Noooch but used to either receive or pay some money.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RequestMoneyToExistingButNonRegisteredUser")]
        public StringResult RequestMoneyToExistingButNonRegisteredUser(RequestDto requestInput, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                string requestId = string.Empty;

                try
                {
                    Logger.Info("Service Cntrlr - RequestMoneyToExistingButNonRegisteredUser Initiated - MemberID: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToExistingButNonregisteredUser(requestInput) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntrlr - RequestMoneyToExistingButNonRegisteredUser FAILED - MemberID: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// For an existing user to make a request to a non-existing user.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RequestMoneyToNonNoochUserUsingSynapse")]
        public StringResult RequestMoneyToNonNoochUserUsingSynapse(RequestDto requestInput, string accessToken)
        {
            Logger.Info("Service Controller - RequestMoneyToNonNoochUserUsingSynapse Initiated - MemberId: [" + requestInput.MemberId + "]");

            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                string requestId = string.Empty;

                try
                {
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToNonNoochUserUsingSynapse(requestInput) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RequestMoneyToNonNoochUserUsingSynapse FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new System.ArgumentException("Invalid OAuth 2 Access");
            }
        }



        /// <summary>
        /// For an existing user to make a request to non existing user using phone number.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RequestMoneyToNonNoochUserThroughPhoneUsingSynapse")]
        public StringResult RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(RequestDto requestInput, string accessToken, string PayorPhoneNumber)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                string requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoneyToNonNoochUserThroughPhoneUsingSynapse Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(requestInput, PayorPhoneNumber) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// For an existing user to make a request to another existing user.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RequestMoney")]
        public StringResult RequestMoney(RequestDto requestInput, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                string requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoney Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoney(requestInput) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RequestMoney FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// For sending a payment (send or request) from the browser (orig. made for Rent Scene).
        /// Created by Cliff on 12/17/15.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("RequestMoneyFromBrowser")]
        public requestFromBrowser RequestMoneyFromBrowser(string from, string name, string email, string amount, string memo, string pin, string ip, string cip, bool isRequest)
        {
            Logger.Info("Service Cntrlr - RequestMoneyFromBrowser Fired - From: [" + from +
                        "], Name: [" + name + "], Email: [" + email +
                        "], amount: [" + amount + "], memo: [" + memo +
                        "], pin: [" + pin + "], ip: [" + ip +
                        "], CIP Tag: [" + cip + "], isRequest: [" + isRequest + "]");

            requestFromBrowser res = new requestFromBrowser();
            res.success = false;
            res.isEmailAlreadyReg = false;
            res.msg = "Service Layer - Initial";

            #region Check for Required Data

            bool isMissingData = false;

            if (String.IsNullOrEmpty(from))
            {
                res.msg = "Missing 'from' field!";
                isMissingData = true;
            }
            if (String.IsNullOrEmpty(pin))
            {
                res.msg = "Missing PIN!";
                isMissingData = true;
            }
            if (String.IsNullOrEmpty(name))
            {
                res.msg = "Missing name!";
                isMissingData = true;
            }
            if (String.IsNullOrEmpty(email))
            {
                res.msg = "Missing email!";
                isMissingData = true;
            }
            if (String.IsNullOrEmpty(amount))
            {
                res.msg = "Missing amount!";
                isMissingData = true;
            }
            if (String.IsNullOrEmpty(cip))
            {
                // Should never happen, but if it does just use "renter" as the default.
                cip = "renter";
            }
            if (isMissingData)
            {
                Logger.Error("Service Cntrlr -> RequestMoneyFromBrowser FAILED - Missing required data - Msg: [" + res.msg + "]");
                return res;
            }

            #endregion Check for Required Data


            #region Get MemberID of Sending User

            var memIdToUse = "";
            var addressToUse = "";
            var zipToUse = "";

            if (from.ToLower() == "habitat")
            {
                memIdToUse = CommonHelper.GetMemberIdByUserName("andrew@tryhabitat.com");
                addressToUse = "1856 N. Willington St.";
                zipToUse = "19121";
            }
            else if (from.ToLower() == "nooch")
            {
                memIdToUse = CommonHelper.GetMemberIdByUserName("team@nooch.com");
                addressToUse = "3 Scarlet Oak Dr";
                zipToUse = "19041";
            }
            else if (from.ToLower() == "appjaxx")
            {
                memIdToUse = CommonHelper.GetMemberIdByUserName("josh@appjaxx.com");
                addressToUse = "100 Fairhill Road";
                zipToUse = "19440";
            }

            if (String.IsNullOrEmpty(memIdToUse))
            {
                Logger.Error("Service Cntlr -> RequestMoneyFromBrowser FAILED - unable to get MemberID based on given username - From: [" + from + "]");
                res.msg = "Unable to get MemberID based on given username";

                return res;
            }

            #endregion Get MemberID of Sending User


            try
            {
                string requestId = string.Empty;

                RequestDto requestInput = new RequestDto()
                {
                    AddressLine1 = addressToUse,
                    Amount = Convert.ToDecimal(amount),
                    City = "Philadelphia",
                    Country = "US",
                    Latitude = 39.95332018F,
                    Longitude = -75.1661824F,
                    MemberId = memIdToUse,
                    Memo = memo,
                    MoneySenderEmailId = email,
                    Name = name,
                    PinNumber = pin, // Correct encrypted PIN has already been grabbed in NoochController based on 'from' field
                    SenderId = "",
                    State = "PA",
                    ZipCode = zipToUse,
                    cipTag = cip,
                    from = from,
                    //isTesting = "true"
                };

                var tda = new TransactionsDataAccess();

                StringResult tdaRes = new StringResult { Result = tda.RequestMoneyToNonNoochUserUsingSynapse(requestInput) };

                res.msg = tdaRes.Result;
                res.note = requestId;

                if (tdaRes.Result.IndexOf("made successfully") > -1)
                    res.success = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - RequestMoneyFromBrowser FAILED - Email: [" + email + "], Exception: [" + ex + "]");

                res.msg = ex.Message;

                Utility.ThrowFaultException(ex);
            }

            return res;
        }


        /// <summary>
        /// For sending a payment REQUEST to an EXISTING user on behalf of Rent Scene's Nooch account.
        /// Had to create this only for use on the makePayment.aspx Page because I didn't know how to make a 
        /// POST HTTP request from the makePayment.aspx.cs Code-Behind page... so did a GET to here, and this
        /// will forward along to RequestMoneyToNonNoochUserUsingSynapse().
        /// Created by Cliff on 12/23/15.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("RequestMoneyToExistingUserForRentScene")]
        public requestFromBrowser RequestMoneyToExistingUserForRentScene(string from, string name, string email, string amount, string memo, string pin, string ip, bool isRequest, string memberId, string nameFromServer)
        {
            Logger.Info("Service Cntlr - RequestMoneyToExistingUserForRentScene Fired - From: [" + from + ", Name: [" + name +
                        "], Email: [" + email + "], Amount: [" + amount +
                        "], Memo: [" + memo + "], PIN: [" + pin +
                        "], IP: [" + ip + "], isRequest: [" + isRequest + "]" +
                        "], MemberID: [" + memberId + "], NameFromServer: [" + nameFromServer + "]");

            requestFromBrowser res = new requestFromBrowser();
            res.success = false;
            res.isEmailAlreadyReg = false;
            res.msg = "Service Layer - Initial";

            try
            {
                #region Check for Required Data

                bool isMissingData = false;

                if (String.IsNullOrEmpty(from))
                {
                    res.msg = "Missing FROM!";
                    isMissingData = true;
                }
                if (String.IsNullOrEmpty(pin))
                {
                    res.msg = "Missing PIN!";
                    isMissingData = true;
                }
                if (String.IsNullOrEmpty(name))
                {
                    res.msg = "Missing name!";
                    isMissingData = true;
                }
                if (String.IsNullOrEmpty(email))
                {
                    res.msg = "Missing email!";
                    isMissingData = true;
                }
                if (String.IsNullOrEmpty(amount))
                {
                    res.msg = "Missing amount!";
                    isMissingData = true;
                }

                if (isMissingData)
                {
                    Logger.Error("Service Cntlr -> RequestMoneyToExistingUserForRentScene FAILED - Missing required data - Msg: [" + res.msg + "]");
                    return res;
                }

                #endregion Check for Required Data

                var address = "";
                var city = "Philadelphia";
                var zip = "";
                var memIdOfRequester = "";

                if (from == "habitat")
                {
                    memIdOfRequester = CommonHelper.GetMemberIdByUserName("andrew@tryhabitat.com");
                    address = "1856 N. Willington St.";
                    city = "Philadelphia";
                    zip = "19121";
                }
                else if (from == "appjaxx")
                {
                    address = "100 Fairhill Road";
                    city = "Hatfield";
                    zip = "19440";
                    memIdOfRequester = "8b4b4983-f022-4289-ba6e-48d5affb5484";
                }
                else
                {
                    address = "3 Scarlet Oak Dr";
                    city = "Haverford";
                    zip = "19041";
                    memIdOfRequester = "00bd3972-d900-429d-8a0d-28a5ac4a75d7"; // team@nooch.com
                }

                Logger.Info("Service Cntlr - RequestMoneyToExistingUserForRentScene - Checkpoint - address: [" + address +
                            "], city: [" + city + "], ZIP: [" + zip + "], memIdOfRequest: [" + memIdOfRequester + "]");

                string requestId = string.Empty;

                RequestDto requestInput = new RequestDto()
                {
                    AddressLine1 = address,
                    Amount = Convert.ToDecimal(amount),
                    City = city,
                    Country = "US",
                    Latitude = 39.95332018F,
                    Longitude = -75.1661824F,
                    MemberId = memIdOfRequester,
                    Memo = memo,
                    MoneySenderEmailId = !String.IsNullOrEmpty(memberId) ? email : "",
                    Name = nameFromServer,
                    PinNumber = pin,
                    SenderId = !String.IsNullOrEmpty(memberId) ? memberId : "",
                    State = "PA",
                    TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    ZipCode = zip,
                };

                var tda = new TransactionsDataAccess();
                StringResult tdaRes = new StringResult { Result = tda.RequestMoneyToExistingButNonregisteredUser(requestInput) };

                res.msg = tdaRes.Result;
                res.note = requestId;

                if (tdaRes.Result.IndexOf("made successfully") > -1)
                    res.success = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - RequestMoneyToExistingUserForRentScene FAILED - Email: [" + email + "], Exception: [" + ex + "]");

                res.msg = ex.Message;
                res.note = "Outer exception in Service Layer (RequestMoneyToExistingUserForRentScene)!";
            }

            return res;
        }



        /// <summary>
        /// For an existing user to reject a request from another existing user.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("RejectMoneyRequestForExistingNoochUser")]
        public StringResult RejectMoneyRequestForExistingNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service Cntlr - RejectMoneyRequestForExistingNoochUser - [TransactionId: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyRequestForExistingNoochUser(transactionId);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - RejectMoneyRequestForExistingNoochUser FAILED - TransID: [" + transactionId +
                             "], Exception: [" + ex.Message + "]");

                throw new Exception("Server Error");
            }
        }


        #endregion Request Methods


        #region Cancel Transaction Services

        [HttpGet]
        [ActionName("CancelMoneyRequestForNonNoochUser")]
        public StringResult CancelMoneyRequestForNonNoochUser(string TransactionId, string MemberId)
        {
            TransactionsDataAccess tda = new TransactionsDataAccess();
            return new StringResult { Result = tda.CancelMoneyRequestForNonNoochUser(TransactionId, MemberId) };
        }


        /// <summary>
        /// For Cancelling a REQUEST sent to an EXISTING Nooch user.
        /// Currently called only from the iOS app.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        [HttpGet]
        [ActionName("CancelMoneyRequestForExistingNoochUser")]
        public StringResult CancelMoneyRequestForExistingNoochUser(string TransactionId, string MemberId)
        {
            TransactionsDataAccess tda = new TransactionsDataAccess();
            return new StringResult
            {
                Result = tda.CancelMoneyRequestForExistingNoochUser(TransactionId, MemberId)
            };
        }


        /// <summary>
        /// CLIFF (12/8/15): NOT SURE WHERE THIS IS USED. NOT CALLED BY THE iOS app.  Might be called from cancel landing page.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("CancelMoneyTransferForSender")]
        public StringResult CancelMoneyTransferForSender(string TransactionId, string MemberId)
        {
            TransactionsDataAccess obj = new TransactionsDataAccess();
            return new StringResult { Result = obj.CancelMoneyTransferForSender(TransactionId, MemberId) };
        }


        /// <summary>
        /// For Cancelling an INVITE (Send Money) to a NON-NOOCH user. Called only from the iOS app.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("CancelMoneyTransferToNonMemberForSender")]
        public StringResult CancelMoneyTransferToNonMemberForSender(string TransactionId, string MemberId)
        {
            TransactionsDataAccess tda = new TransactionsDataAccess();
            return new StringResult { Result = tda.CancelMoneyTransferToNonMemberForSender(TransactionId, MemberId) };
        }


        [HttpGet]
        [ActionName("CancelRejectTransaction")]
        public string CancelRejectTransaction(string memberId, string accessToken, string transactionId, string userResponse)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - CancelRejectTransaction Fired - MemberID: [" + memberId + "]");
                    var tda = new TransactionsDataAccess();
                    return tda.CancelRejectTransaction(transactionId, userResponse);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        #endregion Cancel Transaction Services


        [HttpGet]
        [ActionName("SaveSocialMediaPost")]
        public StringResult SaveSocialMediaPost(string MemberId, string accesstoken, string PostTo, string PostContent)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - SaveSocialMediaPost Fired - MemberID: [" + MemberId + "],  Posted To: [" + PostTo + "]");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.SaveMediaPosts(MemberId, PostTo, PostContent) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - SaveSocialMediaPost FAILED - MemberID: [" + MemberId + "], Exception: [" + ex.Message + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /*******************************/
        /**  PROFILE-RELATED METHODS  **/
        /*******************************/
        #region Profile-Related Functions


        /// <summary>
        /// This is for getting details for the PROFILE screen of the mobile app. So it only needs to
        /// return data specifically for that screen and nothing more.
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("GetMyDetails")]
        public MySettingsInput GetMyDetails(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    // Logger.LogDebugMessage("Service Cntlr -> GetMyDetails Initiated - MemberID: [" + memberId + "]");

                    var myDetails = CommonHelper.GetMemberDetails(memberId);
                    Guid MemId = Utility.ConvertToGuid(memberId);
                    var authToken = _dbContext.AuthenticationTokens.FirstOrDefault(m => m.MemberId == MemId &&
                                                                                        m.IsActivated == true);

                    var settings = new MySettingsInput
                    {
                        UserName = !String.IsNullOrEmpty(myDetails.UserName) ? CommonHelper.GetDecryptedData(myDetails.UserName) : "",
                        FirstName = !String.IsNullOrEmpty(myDetails.FirstName) ? CommonHelper.GetDecryptedData(myDetails.FirstName) : "",
                        LastName = !String.IsNullOrEmpty(myDetails.LastName) ? CommonHelper.GetDecryptedData(myDetails.LastName) : "",
                        DateOfBirth = myDetails.DateOfBirth != null ? Convert.ToDateTime(myDetails.DateOfBirth).ToString("MM/dd/yyyy") : "",
                        IsVerifiedPhone = myDetails.IsVerifiedPhone ?? false,
                        IsVerifiedEmail = authToken != null || myDetails.Status == "Active" || myDetails.Status == "NonRegistered" ? true : false,
                        IsSsnAdded = !String.IsNullOrEmpty(myDetails.SSN) && CommonHelper.GetDecryptedData(myDetails.SSN).Length > 8,

                        ContactNumber = !String.IsNullOrEmpty(myDetails.ContactNumber) ? CommonHelper.FormatPhoneNumber(myDetails.ContactNumber) : myDetails.ContactNumber,
                        SecondaryMail = !String.IsNullOrEmpty(myDetails.SecondaryEmail) ? CommonHelper.GetDecryptedData(myDetails.SecondaryEmail) : "",
                        RecoveryMail = !String.IsNullOrEmpty(myDetails.RecoveryEmail) ? CommonHelper.GetDecryptedData(myDetails.RecoveryEmail) : "",
                        ShowInSearch = Convert.ToBoolean(myDetails.ShowInSearch),

                        Address = !String.IsNullOrEmpty(myDetails.Address) ? CommonHelper.GetDecryptedData(myDetails.Address) : "",
                        Address2 = !String.IsNullOrEmpty(myDetails.Address2) ? CommonHelper.GetDecryptedData(myDetails.Address2) : "",
                        City = !String.IsNullOrEmpty(myDetails.City) ? CommonHelper.GetDecryptedData(myDetails.City) : "",
                        State = !String.IsNullOrEmpty(myDetails.State) ? CommonHelper.GetDecryptedData(myDetails.State) : "",
                        Zipcode = !String.IsNullOrEmpty(myDetails.Zipcode) ? CommonHelper.GetDecryptedData(myDetails.Zipcode) : "",
                        //Country = !String.IsNullOrEmpty(myDetails.Country) ? myDetails.Country : "",

                        Photo = myDetails.Photo == null ? Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png" : myDetails.Photo,
                        //FacebookAcctLogin = myDetails.FacebookAccountLogin, // CC: this is already being sent in the GetMemberDetails service
                    };

                    return settings;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - GetMyDetails FAILED - MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                    throw new Exception("Server Error.");
                }
            }
            else throw new Exception("Invalid OAuth 2 Access");
        }


        // made it post type beacuse access token might generate white spaces which can
        // be encoded to plus by web request, which will create problem for validating access token.
        [HttpPost]
        [ActionName("SaveMemberSSN")]
        public StringResult SaveMemberSSN(SaveMemberSSN_Input input)
        {
            Logger.Info("Service Cntlr - SaveMemberSSN Fired - MemberID: [" + input.memberId + "]");

            if (CommonHelper.IsValidRequest(input.accessToken, input.memberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    return new StringResult()
                    {
                        Result = mda.SaveMemberSSN(input.memberId, input.SSN)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - Operation FAILED: SaveMemberSSN - MemberID: [" + input.memberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                Logger.Error("Service Cntlr -> SaveMemberSSN FAILED - MemberID: [" + input.memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// CC (9/1/16): NO REASON THIS SHOULD BE SEPARATE FROM THE REGULAR METHOD FOR SAVING A USER'S PROFILE.
        ///              LET'S DELETE THIS METHOD ENTIRELY ONCE UPDATED AND TESTED SUCCESSFULLY.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("SaveDOBForMember")]
        public StringResult SaveDOBForMember(SaveMemberDOB_Input input)
        {
            if (CommonHelper.IsValidRequest(input.accessToken, input.memberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    return new StringResult()
                    {
                        Result = mda.SaveDOBForMember(input.memberId, input.DOB)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - SaveDOBForMember FAILED - MemberID: [" + input.memberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                Logger.Error("Service Cntlr - SaveDOBForMember FAILED - MemberID: [" + input.memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// This is for SAVING a user's Profile details - called from the Mobile App.
        /// </summary>
        /// <param name="mySettings"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MySettings")]
        public StringResult MySettings(MySettingsInput mySettings, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, mySettings.MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> MySettings Fired - MemberID: [" + mySettings.MemberId + "]");

                    string fileContent = null;
                    int contentLength = 0;
                    string fileExtension = null;

                    if (!String.IsNullOrEmpty(mySettings.Photo))
                        mySettings.Picture = System.Convert.FromBase64String(mySettings.Photo);

                    var mda = new MembersDataAccess();
                    return new StringResult
                    {
                        Result = mda.MySettings(mySettings.MemberId, mySettings.FirstName.ToLower(), mySettings.LastName.ToLower(),
                            mySettings.Password, mySettings.SecondaryMail, mySettings.RecoveryMail, mySettings.FacebookAcctLogin,
                            fileContent, contentLength, fileExtension, mySettings.ContactNumber,
                            mySettings.Address, mySettings.City, mySettings.State, mySettings.Zipcode, mySettings.Country,
                            mySettings.Picture, mySettings.ShowInSearch, mySettings.Address2, mySettings.DateOfBirth)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> MySettings FAILED - MemberID: [" + mySettings.MemberId + "], Exception: [" + ex + "]");
                    return new StringResult { Result = ex.Message };
                }
            }
            else throw new Exception("Invalid OAuth 2 Access");
        }


        #endregion Profile-Related Functions


        /******************************/
        /**  HISORY-RELATED METHODS  **/
        /******************************/
        #region History Related Methods

        [HttpGet]
        [ActionName("GetsingleTransactionDetail")]
        public TransactionDto GetsingleTransactionDetail(string MemberId, string accesstoken, string transactionId)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> GetSingleTransactionDetail Fired - " + "MemberID: [" + MemberId + "], TransID: [" + transactionId + "]");

                    var tda = new TransactionsDataAccess();

                    var trans = tda.GetTransactionById(transactionId);

                    TransactionDto obj = new TransactionDto();
                    obj.TransactionId = trans.TransactionId.ToString();

                    //the sender is same as the current member than display the names of receipients.
                    if (trans.Member.MemberId.ToString() == MemberId)
                    {
                        // Member is Receiver in this transaction
                        obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                        obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                        obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member1.Photo != null ? Path.GetFileName(trans.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));

                        decimal m = (Convert.ToDecimal(trans.Amount) + Convert.ToDecimal(trans.TransactionFee));
                        obj.Amount = Math.Round(m, 2);
                    }
                    else if (trans.Member1.MemberId.ToString() == MemberId)
                    {
                        //the receiver is same as the current member than display the names of sender.
                        obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                        obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo != null ? Path.GetFileName(trans.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                        obj.Amount = trans.Amount;
                    }

                    obj.MemberId = trans.Member.MemberId.ToString();
                    obj.RecepientId = trans.Member1.MemberId.ToString();

                    obj.Name = obj.FirstName + " " + obj.LastName;
                    //obj.TransactionDate = string.Format("{0:dd/MM/yyyy HH:mm:ss}", trans.TransactionDate);
                    obj.TransactionDate = trans.TransactionDate.ToString();

                    obj.TransactionType = CommonHelper.GetDecryptedData(trans.TransactionType);

                    if (obj.TransactionType == "Transfer" && obj.MemberId == MemberId)
                    {
                        obj.TransactionType = "Sent";
                    }
                    if (obj.TransactionType == "Transfer" && obj.RecepientId == MemberId)
                    {
                        obj.TransactionType = "Received";
                    }

                    if (obj.TransactionType == "Disputed")
                    {
                        obj.TransactionType = "Disputed";
                        obj.DisputeId = trans.DisputeTrackingId;
                        string timeZoneDateString = string.Empty;

                        obj.DisputeId = trans.DisputeTrackingId;
                        obj.DisputeReportedDate = trans.DisputeDate.HasValue ? Convert.ToDateTime(trans.DisputeDate).ToShortDateString() : string.Empty;
                        obj.DisputeReviewDate = trans.ReviewDate.HasValue ? Convert.ToDateTime(trans.ReviewDate).ToShortDateString() : string.Empty;
                        obj.DisputeResolvedDate = trans.ResolvedDate.HasValue ? Convert.ToDateTime(trans.ResolvedDate).ToShortDateString() : string.Empty;
                        obj.DisputeStatus = CommonHelper.GetDecryptedData(trans.DisputeStatus);
                        obj.AdminNotes = trans.AdminNotes;
                    }

                    obj.Memo = trans.Memo;
                    obj.Picture = trans.Picture;

                    obj.AddressLine1 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine1 != null) ? trans.GeoLocation.AddressLine1 : string.Empty;
                    obj.AddressLine2 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine2 != null) ? trans.GeoLocation.AddressLine2 : string.Empty;
                    obj.City = (trans.GeoLocation != null && trans.GeoLocation.City != null) ? trans.GeoLocation.City : string.Empty;
                    obj.State = (trans.GeoLocation != null && trans.GeoLocation.State != null) ? trans.GeoLocation.State : string.Empty;
                    obj.Country = (trans.GeoLocation != null && trans.GeoLocation.Country != null) ? trans.GeoLocation.Country : string.Empty;
                    obj.ZipCode = (trans.GeoLocation != null && trans.GeoLocation.ZipCode != null) ? trans.GeoLocation.ZipCode : string.Empty;
                    obj.Latitude = (trans.GeoLocation != null && trans.GeoLocation.Latitude != null) ? (float)trans.GeoLocation.Latitude : default(float);
                    obj.Longitude = (trans.GeoLocation != null && trans.GeoLocation.Longitude != null) ? (float)trans.GeoLocation.Longitude : default(float);
                    obj.TransactionStatus = trans.TransactionStatus;

                    return obj;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - GetsingleTransactionDetail FAILED - MemberID: [" + MemberId + "], Exception: [" + ex + "]");
                    throw (ex);
                }
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("GetTransactionsList")]
        public Collection<TransactionDto> GetTransactionsList(string memberId, string listType, int pSize, int pIndex, string accessToken, string SubListType)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Cntlr - GetTransactionsList Initiated - MemberID: [" + member + "]");

                    var tda = new TransactionsDataAccess();

                    int totalRecordsCount = 0;

                    var transactionListEntities = tda.GetTransactionsList(memberId, listType, pSize, pIndex, SubListType, out totalRecordsCount);

                    if (transactionListEntities != null && transactionListEntities.Count > 0)
                    {
                        var Transactions = new Collection<TransactionDto>();

                        foreach (var trans in transactionListEntities)
                        {
                            try
                            {
                                #region Foreach inside

                                TransactionDto obj = new TransactionDto();
                                obj.MemberId = trans.Member.MemberId.ToString();
                                obj.NoochId = trans.Member.Nooch_ID.ToString();
                                obj.RecepientId = trans.Member1.MemberId.ToString();
                                obj.TransactionId = trans.TransactionId.ToString();
                                if (trans.InvitationSentTo != null)
                                    obj.InvitationSentTo = CommonHelper.GetDecryptedData(trans.InvitationSentTo);


                                //the sender is same as the current member than display the names of receipients.
                                if (trans.Member.MemberId.ToString() == memberId)
                                {
                                    // Member is Receiver in this transaction
                                    obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));

                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"),
                                            trans.Member1.Photo != null
                                                ? Path.GetFileName(trans.Member1.Photo)
                                                : Path.GetFileName("gv_no_photo.jpg"));

                                    decimal trFee = (trans.TransactionFee != null) ? Convert.ToDecimal(trans.TransactionFee) : 0;
                                    obj.Amount = Math.Round((trans.Amount + trFee), 2);
                                }

                                if (trans.Member1.MemberId.ToString() == memberId)
                                {
                                    //the receiver is same as the current member than display the names of sender.
                                    obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo != null ? Path.GetFileName(trans.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                    obj.Amount = Math.Round(trans.Amount, 2);
                                }

                                if (trans.Member.MemberId.ToString() == memberId && trans.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")
                                {
                                    obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member1.Photo != null ? Path.GetFileName(trans.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                    decimal trFee = (trans.TransactionFee != null) ? Convert.ToDecimal(trans.TransactionFee) : 0;
                                    obj.Amount = Math.Round((trans.Amount), 2);
                                }

                                obj.Name = obj.FirstName + " " + obj.LastName;
                                obj.TransactionDate = string.Format("{0:MM/d/yyyy hh:mm:ss tt}", trans.TransactionDate);
                                obj.TransactionType = CommonHelper.GetDecryptedData(trans.TransactionType);

                                if (trans.IsPhoneInvitation != null &&
                                    trans.IsPhoneInvitation == true &&
                                    trans.PhoneNumberInvited != null)
                                {
                                    obj.InvitationSentTo = CommonHelper.GetDecryptedData(trans.PhoneNumberInvited);
                                }

                                if (listType.Equals("SENT") || listType.Equals("RECEIVED") || listType.Equals("DISPUTED") || listType.Equals("ALL"))
                                {
                                    obj.DisputeStatus = !string.IsNullOrEmpty(trans.DisputeStatus) ? CommonHelper.GetDecryptedData(trans.DisputeStatus) : null;
                                    obj.DisputeId = trans.DisputeTrackingId;
                                    obj.DisputeReportedDate = trans.DisputeDate.HasValue ? trans.DisputeDate.ToString() : string.Empty;
                                    obj.DisputeReviewDate = trans.ReviewDate.HasValue ? trans.ReviewDate.ToString() : string.Empty;
                                    obj.DisputeResolvedDate = trans.ResolvedDate.HasValue ? trans.ResolvedDate.ToString() : string.Empty;
                                }

                                obj.Memo = trans.Memo;
                                obj.TotalRecordsCount = totalRecordsCount;
                                obj.TransactionFee = (trans.TransactionFee != null) ? trans.TransactionFee : null;
                                obj.AddressLine1 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine1 != null) ? trans.GeoLocation.AddressLine1 : string.Empty;
                                obj.AddressLine2 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine2 != null) ? trans.GeoLocation.AddressLine2 : string.Empty;
                                obj.City = (trans.GeoLocation != null && trans.GeoLocation.City != null) ? trans.GeoLocation.City : string.Empty;
                                obj.State = (trans.GeoLocation != null && trans.GeoLocation.State != null) ? trans.GeoLocation.State : string.Empty;
                                obj.Country = (trans.GeoLocation != null && trans.GeoLocation.Country != null) ? trans.GeoLocation.Country : string.Empty;
                                obj.ZipCode = (trans.GeoLocation != null && trans.GeoLocation.ZipCode != null) ? trans.GeoLocation.ZipCode : string.Empty;
                                obj.Latitude = (trans.GeoLocation != null && trans.GeoLocation.Latitude != null) ? (float)trans.GeoLocation.Latitude : default(float);
                                obj.Longitude = (trans.GeoLocation != null && trans.GeoLocation.Longitude != null) ? (float)trans.GeoLocation.Longitude : default(float);
                                obj.TransactionStatus = trans.TransactionStatus;
                                obj.Picture = trans.Picture;

                                Transactions.Add(obj);

                                #endregion Foreach inside
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Service Cntlr - GetTransactionsList ERROR - Inner Exception during loop through all transactions - " +
                                             "MemberID: [" + memberId + "], TransID: [" + trans.TransactionId +
                                             "], Amount: [" + trans.Amount.ToString("n2") + "], Exception: [" + ex + "]");
                                continue;
                            }
                        }

                        return Transactions;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - GetTransactionsList FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                    throw ex;
                }
                return new Collection<TransactionDto>();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetLatestReceivedTransaction")]
        public TransactionDto GetLatestReceivedTransaction(string member, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, member))
            {
                var receivedTransaction = new TransactionDto();
                try
                {
                    Logger.Info("Service Cntlr - GetLatestReceivedTransaction - MemberID: [" + member + "]");

                    var tda = new TransactionsDataAccess();
                    var transObj = tda.GetLatestReceivedTransaction(member);

                    if (transObj != null && transObj.Member != null)
                    {
                        var mda = new MembersDataAccess();

                        string addressLine1 = (transObj.GeoLocation != null && transObj.GeoLocation.AddressLine1 != null) ? transObj.GeoLocation.AddressLine1 : string.Empty;
                        string addressLine2 = (transObj.GeoLocation != null && transObj.GeoLocation.AddressLine2 != null) ? transObj.GeoLocation.AddressLine2 : string.Empty;
                        string city = (transObj.GeoLocation != null && transObj.GeoLocation.City != null) ? transObj.GeoLocation.City : string.Empty;
                        string state = (transObj.GeoLocation != null && transObj.GeoLocation.State != null) ? transObj.GeoLocation.State : string.Empty;
                        string country = (transObj.GeoLocation != null && transObj.GeoLocation.Country != null) ? transObj.GeoLocation.Country : string.Empty;
                        string zipCode = (transObj.GeoLocation != null && transObj.GeoLocation.ZipCode != null) ? transObj.GeoLocation.ZipCode : string.Empty;

                        float altitude = (transObj.GeoLocation != null && transObj.GeoLocation.Altitude != null) ? (float)transObj.GeoLocation.Altitude : default(float);
                        float latitude = (transObj.GeoLocation != null && transObj.GeoLocation.Latitude != null) ? (float)transObj.GeoLocation.Latitude : default(float);
                        float longitude = (transObj.GeoLocation != null && transObj.GeoLocation.Longitude != null) ? (float)transObj.GeoLocation.Longitude : default(float);

                        return new TransactionDto
                        {
                            MemberId = transObj.Member.MemberId.ToString(),
                            NoochId = transObj.Member.Nooch_ID.ToString(),
                            RecepientId = transObj.Member1.MemberId.ToString(),
                            TransactionId = transObj.TransactionId.ToString(),
                            Name = CommonHelper.GetDecryptedData(transObj.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transObj.Member.LastName),
                            FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.FirstName)),
                            LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.LastName)),
                            TransactionDate = !string.IsNullOrEmpty(transObj.Member1.TimeZoneKey) ? string.Format("{0:MM/d/yyyy hh:mm:ss tt}", Convert.ToDateTime(mda.GMTTimeZoneConversion(transObj.TransactionDate.ToString(), transObj.Member1.TimeZoneKey))) : string.Format("{0:MM/d/yyyy hh:mm:ss tt}", transObj.TransactionDate),
                            Amount = Math.Round(transObj.Amount, 2),
                            DisputeStatus = !string.IsNullOrEmpty(transObj.DisputeStatus) ? CommonHelper.GetDecryptedData(transObj.DisputeStatus) : null,
                            DisputeId = transObj.DisputeTrackingId,
                            // sender photo - Received from
                            Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transObj.Member.Photo != null ? Path.GetFileName(transObj.Member.Photo) : Path.GetFileName("gv_no_photo.jpg")),
                            DisputeReportedDate = transObj.DisputeDate.HasValue ? !string.IsNullOrEmpty(transObj.Member1.TimeZoneKey) ? mda.GMTTimeZoneConversion(transObj.DisputeDate.ToString(), transObj.Member1.TimeZoneKey) : transObj.DisputeDate.ToString() : string.Empty,
                            DisputeReviewDate = transObj.ReviewDate.HasValue ? !string.IsNullOrEmpty(transObj.Member1.TimeZoneKey) ? mda.GMTTimeZoneConversion(transObj.ReviewDate.ToString(), transObj.Member1.TimeZoneKey) : transObj.ReviewDate.ToString() : string.Empty,
                            DisputeResolvedDate = transObj.ResolvedDate.HasValue ? !string.IsNullOrEmpty(transObj.Member1.TimeZoneKey) ? mda.GMTTimeZoneConversion(transObj.ResolvedDate.ToString(), transObj.Member1.TimeZoneKey) : transObj.ResolvedDate.ToString() : string.Empty,
                            TransactionType = !string.IsNullOrEmpty(transObj.DisputeStatus) ? "Received from" : "Received",

                            AddressLine1 = addressLine1,
                            AddressLine2 = addressLine2,
                            City = city,
                            State = state,
                            Country = country,
                            ZipCode = zipCode,

                            Latitude = latitude,
                            Longitude = longitude
                        };
                    }

                    receivedTransaction = null;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return receivedTransaction;
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetTransactionsSearchList")]
        public List<TransactionDto> GetTransactionsSearchList(string member, string friendName, string category, int pageSize, int pageIndex, string accessToken, string sublist)
        {
            if (CommonHelper.IsValidRequest(accessToken, member))
            {
                try
                {
                    Logger.Info("Service Controller - GetTransactionsList - member: [" + member + "]");

                    var transactionDataAccess = new TransactionsDataAccess();
                    int totalRecordsCount = 0;
                    var transactionListEntities = transactionDataAccess.GetTransactionsSearchList(member, friendName, category, pageSize, pageIndex, sublist);

                    if (transactionListEntities != null && transactionListEntities.Count > 0)
                    {

                        var Transactions = new Collection<TransactionDto>();

                        foreach (var trans in transactionListEntities)
                        {
                            TransactionDto obj = new TransactionDto();
                            obj.MemberId = trans.Member.MemberId.ToString();
                            obj.NoochId = trans.Member.Nooch_ID;
                            obj.RecepientId = trans.Member1.MemberId.ToString();
                            obj.TransactionId = trans.TransactionId.ToString();

                            // If the transaction's sender matches the user, then display the receipient's name.
                            if (trans.Member.MemberId.ToString().ToUpper() == member.ToUpper())
                            {
                                // Member is Receiver in this transaction
                                obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                                obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                                obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member1.Photo != null ? Path.GetFileName(trans.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            }
                            else if (trans.Member1.MemberId.ToString().ToUpper() == member.ToUpper())
                            {
                                obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                                obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                                obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo != null ? Path.GetFileName(trans.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            }
                            obj.Name = obj.FirstName + " " + obj.LastName;
                            obj.TransactionDate = string.Format("{0:MM/d/yyyy hh:mm:ss tt}", trans.TransactionDate);
                            obj.Amount = Math.Round(trans.Amount, 2);
                            obj.TransactionType = CommonHelper.GetDecryptedData(trans.TransactionType);

                            if (category.Equals("SENT") || category.Equals("RECEIVED") || category.Equals("DISPUTED") || category.Equals("ALL"))
                            {
                                obj.DisputeStatus = !string.IsNullOrEmpty(trans.DisputeStatus) ? CommonHelper.GetDecryptedData(trans.DisputeStatus) : null;
                                obj.DisputeId = trans.DisputeTrackingId;
                                obj.DisputeReportedDate = trans.DisputeDate.HasValue ? trans.DisputeDate.ToString() : string.Empty;
                                obj.DisputeReviewDate = trans.ReviewDate.HasValue ? trans.ReviewDate.ToString() : string.Empty;
                                obj.DisputeResolvedDate = trans.ResolvedDate.HasValue ? trans.ResolvedDate.ToString() : string.Empty;
                                obj.Memo = trans.Memo;
                            }

                            obj.TotalRecordsCount = totalRecordsCount;
                            obj.AddressLine1 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine1 != null) ? trans.GeoLocation.AddressLine1 : string.Empty;
                            obj.AddressLine2 = (trans.GeoLocation != null && trans.GeoLocation.AddressLine2 != null) ? trans.GeoLocation.AddressLine2 : string.Empty;
                            obj.City = (trans.GeoLocation != null && trans.GeoLocation.City != null) ? trans.GeoLocation.City : string.Empty;
                            obj.State = (trans.GeoLocation != null && trans.GeoLocation.State != null) ? trans.GeoLocation.State : string.Empty;
                            obj.Country = (trans.GeoLocation != null && trans.GeoLocation.Country != null) ? trans.GeoLocation.Country : string.Empty;
                            obj.ZipCode = (trans.GeoLocation != null && trans.GeoLocation.ZipCode != null) ? trans.GeoLocation.ZipCode : string.Empty;
                            obj.Latitude = (trans.GeoLocation != null && trans.GeoLocation.Latitude != null) ? (float)trans.GeoLocation.Latitude : default(float);
                            obj.Longitude = (trans.GeoLocation != null && trans.GeoLocation.Longitude != null) ? (float)trans.GeoLocation.Longitude : default(float);
                            obj.TransactionStatus = trans.TransactionStatus;

                            if (obj.FirstName.ToLower().Contains(friendName.ToLower()) || obj.LastName.ToLower().Contains(friendName.ToLower()))
                                Transactions.Add(obj);
                        }
                        //return Transactions.Skip((pageIndex - 1) * pageSize).Take(pageIndex * pageSize).ToList();
                        return Transactions.ToList();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception " + ex);
                }
                return new List<TransactionDto>();
            }
            else
            {
                throw new Exception("Invalid oAuth access token.");
            }
        }


        /// <summary>
        /// To get a single transaction's details.
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="category"></param>
        /// <param name="transactionId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("GetTransactionDetail")]
        public TransactionDto GetTransactionDetail(string memberId, string category, string transactionId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> GetTransactionDetail - MemberID: [" + memberId + "]");

                    var tda = new TransactionsDataAccess();
                    var mda = new MembersDataAccess();

                    var transList = tda.GetTransactionDetail(memberId, category, transactionId);

                    if (transList != null)
                    {
                        var trans = new TransactionDto();

                        DateTime transDate = Convert.ToDateTime(transList.TransactionDate);
                        string timeZoneDateString = string.Empty;
                        var transDateTime = new string[3];
                        string transDateString = transDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                        transDateTime = transDateString.Split(' ');

                        #region Dispute Checks

                        DateTime disputeDate;
                        string disputeDateString = string.Empty;
                        var disputeDateTime = new string[3];

                        DateTime disputeReviewDate;
                        string disputeReviewDateString = string.Empty;
                        var disputeReviewDateTime = new string[3];

                        DateTime disputeResolvedDate;
                        string disputeResolvedDateString = string.Empty;
                        var disputeResolvedDateTime = new string[3];

                        if (transList.DisputeDate != null)
                        {
                            disputeDate = Convert.ToDateTime(transList.DisputeDate);
                            disputeDateString = disputeDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                            disputeDateTime = disputeDateString.Split(' ');
                        }

                        if (transList.ReviewDate != null)
                        {
                            disputeReviewDate = Convert.ToDateTime(transList.ReviewDate);
                            disputeReviewDateString = disputeReviewDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                            disputeDateTime = disputeReviewDateString.Split(' ');
                        }

                        if (transList.ResolvedDate != null)
                        {
                            disputeResolvedDate = Convert.ToDateTime(transList.ResolvedDate);
                            disputeResolvedDateString = disputeResolvedDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                            disputeResolvedDateTime = disputeResolvedDateString.Split(' ');
                        }

                        trans.DisputeStatus = !string.IsNullOrEmpty(transList.DisputeStatus) ? CommonHelper.GetDecryptedData(transList.DisputeStatus) : null;
                        trans.DisputeId = transList.DisputeTrackingId;
                        trans.DisputeReportedDate = transList.DisputeDate.HasValue ? disputeDateString : "";
                        trans.DisputeReviewDate = transList.ReviewDate.HasValue ? disputeReviewDateString : "";
                        trans.DisputeResolvedDate = transList.ResolvedDate.HasValue ? disputeResolvedDateString : "";

                        #endregion Dispute Checks


                        trans.TransactionId = transList.TransactionId.ToString();
                        trans.Date = transDateTime[0];
                        trans.Time = transDateTime[1] + " " + transDateTime[2];
                        trans.Amount = Math.Round(transList.Amount, 2);

                        if (transList.GeoLocation != null)
                        {
                            trans.AddressLine1 = transList.GeoLocation.AddressLine1 != null ? transList.GeoLocation.AddressLine1 : string.Empty;
                            trans.AddressLine2 = transList.GeoLocation.AddressLine2 != null ? transList.GeoLocation.AddressLine2 : string.Empty;
                            trans.City = transList.GeoLocation.City != null ? transList.GeoLocation.City : string.Empty;
                            trans.State = transList.GeoLocation.State != null ? transList.GeoLocation.State : string.Empty;
                            trans.Country = transList.GeoLocation.Country != null ? transList.GeoLocation.Country : string.Empty;
                            trans.ZipCode = transList.GeoLocation.ZipCode != null ? transList.GeoLocation.ZipCode : string.Empty;
                            trans.Latitude = transList.GeoLocation.Latitude != null ? (float)transList.GeoLocation.Latitude : default(float);
                            trans.Longitude = transList.GeoLocation.Longitude != null ? (float)transList.GeoLocation.Longitude : default(float);
                        }

                        if (transList.GeoLocation != null)
                            trans.Location = transList.GeoLocation.Latitude + ", " + transList.GeoLocation.Longitude;
                        else trans.Location = "- Nil -";

                        trans.Picture = transList.Picture;


                        if (category.Equals("SENT"))
                        {
                            #region Sent

                            trans.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member1.FirstName));
                            trans.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member1.LastName));
                            trans.MemberId = transList.Member.MemberId.ToString();
                            trans.NoochId = transList.Member1.Nooch_ID.ToString();
                            trans.RecepientId = transList.Member1.MemberId.ToString();
                            trans.Name = CommonHelper.GetDecryptedData(transList.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(transList.Member1.LastName);
                            trans.TransactionDate = !string.IsNullOrEmpty(transList.Member.TimeZoneKey) ? timeZoneDateString : transDateString;
                            trans.TransactionType = "Sent";
                            trans.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transList.Member1.Photo != null ? Path.GetFileName(transList.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));

                            #endregion Sent
                        }
                        if (category.Equals("RECEIVED"))
                        {
                            #region Received

                            trans.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member.FirstName));
                            trans.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member.LastName));
                            trans.MemberId = transList.Member.MemberId.ToString();
                            trans.NoochId = transList.Member.Nooch_ID.ToString();
                            trans.RecepientId = transList.Member1.MemberId.ToString();
                            trans.Name = CommonHelper.GetDecryptedData(transList.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transList.Member.LastName);
                            trans.TransactionDate = !string.IsNullOrEmpty(transList.Member1.TimeZoneKey) ? timeZoneDateString : transDateString;
                            trans.TransactionType = "Received";
                            // sender photo - Received from
                            trans.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transList.Member.Photo != null ? Path.GetFileName(transList.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));

                            #endregion Received
                        }
                        else if (category.Equals("REQUEST"))
                        {
                            #region Received

                            trans.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member.FirstName));
                            trans.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member.LastName));
                            trans.MemberId = transList.Member.MemberId.ToString();
                            trans.NoochId = transList.Member.Nooch_ID.ToString();
                            trans.RecepientId = transList.Member1.MemberId.ToString();
                            trans.Name = CommonHelper.GetDecryptedData(transList.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transList.Member.LastName);
                            trans.TransactionDate = !string.IsNullOrEmpty(transList.Member1.TimeZoneKey) ? timeZoneDateString : transDateString;
                            trans.TransactionType = "Request";
                            // sender photo - Received from
                            trans.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transList.Member.Photo != null ? Path.GetFileName(transList.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));

                            #endregion Received
                        }
                        else if (category.Equals("DISPUTED"))
                        {
                            #region Disputed

                            if (transList.RaisedBy.Equals("Sender"))
                            {
                                trans.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member1.FirstName));
                                trans.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transList.Member1.LastName));
                                trans.NoochId = transList.Member1.Nooch_ID;
                                trans.Name = CommonHelper.GetDecryptedData(transList.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(transList.Member1.LastName);
                                trans.TransactionDate = !string.IsNullOrEmpty(transList.Member.TimeZoneKey) ? timeZoneDateString : transDateString;
                                trans.TransactionType = "Sent to";
                                // recipient photo - Sent to
                                trans.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transList.Member1.Photo != null ? Path.GetFileName(transList.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            }

                            if (transList.RaisedBy.Equals("Receiver"))
                            {
                                trans.NoochId = transList.Member.Nooch_ID.ToString();
                                trans.Name = CommonHelper.GetDecryptedData(transList.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transList.Member.LastName);
                                trans.TransactionDate = !string.IsNullOrEmpty(transList.Member1.TimeZoneKey) ? timeZoneDateString : transDateString;
                                trans.TransactionType = "Received from";
                                // sender photo - Received from
                                trans.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transList.Member.Photo != null ? Path.GetFileName(transList.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            }

                            #endregion Disputed
                        }

                        #region Donation - UNUSED
                        /*
                        else if (transactionListEntities.RaisedBy.Equals("Donation"))
                        {
                            var donationTransaction = new TransactionDto();

                            if (!string.IsNullOrEmpty(transactionListEntities.Members1.TimeZoneKey))
                            {
                                timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Members1.TimeZoneKey);
                                timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");
                                transactionDateTime = timeZoneDateString.Split(' ');
                            }

                            donationTransaction.MemberId = transactionListEntities.Members.MemberId.ToString();
                            donationTransaction.NoochId = transactionListEntities.Members.Nooch_ID.ToString();
                            donationTransaction.RecepientId = transactionListEntities.Members1.MemberId.ToString();
                            donationTransaction.TransactionId = transactionListEntities.TransactionId.ToString();
                            donationTransaction.Name = "Nooch account";
                            donationTransaction.TransactionType = CommonHelper.GetDecryptedData(transactionListEntities.TransactionType);
                            donationTransaction.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Members1.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                            donationTransaction.Date = transactionDateTime[0];
                            donationTransaction.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                            donationTransaction.Amount = Math.Round(transactionListEntities.Amount, 2);

                            if (transactionListEntities.GeoLocations != null)
                            {
                                donationTransaction.AddressLine1 = transactionListEntities.GeoLocations.AddressLine1 != null ? transactionListEntities.GeoLocations.AddressLine1 : string.Empty;
                                donationTransaction.AddressLine2 = transactionListEntities.GeoLocations.AddressLine2 != null ? transactionListEntities.GeoLocations.AddressLine2 : string.Empty;
                                donationTransaction.City = transactionListEntities.GeoLocations.City != null ? transactionListEntities.GeoLocations.City : string.Empty;
                                donationTransaction.State = transactionListEntities.GeoLocations.State != null ? transactionListEntities.GeoLocations.State : string.Empty;
                                donationTransaction.Country = transactionListEntities.GeoLocations.Country != null ? transactionListEntities.GeoLocations.Country : string.Empty;
                                donationTransaction.ZipCode = transactionListEntities.GeoLocations.ZipCode != null ? transactionListEntities.GeoLocations.ZipCode : string.Empty;

                                donationTransaction.Altitude = transactionListEntities.GeoLocations.Altitude != null ? (float)transactionListEntities.GeoLocations.Altitude : default(float);
                                donationTransaction.Latitude = transactionListEntities.GeoLocations.Latitude != null ? (float)transactionListEntities.GeoLocations.Latitude : default(float);
                                donationTransaction.Longitude = transactionListEntities.GeoLocations.Longitude != null ? (float)transactionListEntities.GeoLocations.Longitude : default(float);
                            }

                            if (transactionListEntities.GeoLocations != null)
                                donationTransaction.Location = transactionListEntities.GeoLocations.Latitude + ", " +
                                    transactionListEntities.GeoLocations.Longitude;
                            else
                                donationTransaction.Location = "- Nil -";

                            donationTransaction.Picture = transactionListEntities.Picture;

                            return donationTransaction;
                        }*/
                        #endregion Donation - UNUSED

                        return trans;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return new TransactionDto();
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        /// <summary>
        /// To raise dispute for particular transaction.
        /// </summary>
        [HttpPost]
        [ActionName("RaiseDispute")]
        public DisputeResult RaiseDispute(DisputeDto raiseDisputeInput, string accessToken, string memberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Controller - RaiseDispute - transactionId: [" + raiseDisputeInput.TransactionId + "]");
                    var transactionsDataAccess = new TransactionsDataAccess();
                    var result = transactionsDataAccess.RaiseDispute(raiseDisputeInput.MemberId,
                            raiseDisputeInput.RecepientId, raiseDisputeInput.TransactionId, raiseDisputeInput.ListType,
                            raiseDisputeInput.CcMailIds, raiseDisputeInput.BccMailIds, raiseDisputeInput.Subject, raiseDisputeInput.BodyText);
                    return new DisputeResult
                    {
                        Result = result.Result,
                        DisputeId = result.DisputeId,
                        DisputeDate = result.DisputeDate
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RaiseDispute FAILED - TransID: [" + raiseDisputeInput.TransactionId + "]. Exception: [" + ex + "]");
                    throw ex;
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("sendTransactionInCSV")]
        public StringResult sendTransactionInCSV(string memberId, string toAddress, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    var r = GetTransactionsList(memberId, "ALL", 100000, 1, accessToken, "").Select(i => new { i.TransactionId, SenderId = i.MemberId, RecepientId = i.RecepientId, i.DisputeId, i.Amount, i.TransactionDate, i.TransactionFee, i.TransactionType, i.LocationId, i.DisputeReportedDate, i.DisputeReviewDate, i.DisputeResolvedDate, i.AdminNotes, i.RaisedBy, i.Memo, i.Picture, i.TransactionStatus });
                    var csv = string.Join(", ", r.Select(i => i.ToString()).ToArray()).Replace("{", "").Replace("}", "");

                    var sender = CommonHelper.GetMemberDetails(memberId);

                    var tokens = new Dictionary<string, string>
                    {
					    {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName))}
				    };

                    bool b = Utility.SendEmailCSV(toAddress, csv, "Here's your Nooch activity report", "Hi,<br/> Your complete Nooch transaction history is attached in a .CSV file.<br/><br/>Thanks<br/>-Nooch Team", tokens);
                    Logger.Info("TransactionHistorySent - TransactionHistorySent status mail sent to [" + toAddress + "].");

                    if (b)
                        return new StringResult { Result = "1" };
                    else
                        return new StringResult { Result = "0" };
                }
                catch
                {
                    throw new Exception("0");
                }
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }

        #endregion History Related Methods


        /*********************************/
        /**  METHODS FOR LANDING PAGES  **/
        /*********************************/
        #region Landing Page Functions

        [HttpGet]
        [ActionName("GetMemberDetailsForLandingPage")]
        public MemberDto GetMemberDetailsForLandingPage(string memberId)
        {
            Logger.Info("Service Cntlr -> GetMemberDetailsForLandingPage Fired - MemberID: [" + memberId + "]");

            try
            {
                var memberEntity = CommonHelper.GetMemberDetails(memberId);

                // Get Synapse Bank Account Info
                var synapseBank = CommonHelper.GetSynapseBankDetails(memberId);

                var accountstatus = "";

                // Now check this bank's status.
                // CC (10/7/15): If the user's ID is verified (after sending SSN info to Synapse), then consider the bank Verified as well
                if (synapseBank != null)
                    accountstatus = memberEntity.IsVerifiedWithSynapse == true ? "Verified" : synapseBank.Status;

                // Create MemberDTO Object to return to the app
                var member = new MemberDto
                {
                    MemberId = memberEntity.MemberId.ToString(),
                    UserName = CommonHelper.GetDecryptedData(memberEntity.UserName),
                    Status = memberEntity.Status,
                    FirstName = !String.IsNullOrEmpty(memberEntity.FirstName) ?
                                CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName)) :
                                "",
                    LastName = !String.IsNullOrEmpty(memberEntity.LastName) ?
                               CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.LastName)) :
                               "",
                    DateOfBirth = memberEntity.DateOfBirth == null ? "" : Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MM/dd/yyyy"),
                    Address = !String.IsNullOrEmpty(memberEntity.Address) ?
                              CommonHelper.GetDecryptedData(memberEntity.Address) :
                              null,
                    City = !String.IsNullOrEmpty(memberEntity.City) ?
                           CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.City)) :
                           null,
                    Zip = !String.IsNullOrEmpty(memberEntity.Zipcode) ?
                          CommonHelper.GetDecryptedData(memberEntity.Zipcode) :
                          null,
                    ContactNumber = CommonHelper.FormatPhoneNumber(memberEntity.ContactNumber),
                    IsVerifiedPhone = memberEntity.IsVerifiedPhone != null && Convert.ToBoolean(memberEntity.IsVerifiedPhone),
                    IsSSNAdded = memberEntity.SSN != null,
                    ssnLast4 = !String.IsNullOrEmpty(memberEntity.SSN) ? CommonHelper.GetDecryptedData(memberEntity.SSN) : "",
                    PhotoUrl = memberEntity.Photo ?? Path.GetFileName("gv_no_photo.jpg"),
                    FacebookAccountLogin = memberEntity.FacebookUserId != "not connected" ?
                                           memberEntity.FacebookUserId :
                                           "",
                    IsSynapseBankAdded = synapseBank != null,
                    SynapseBankStatus = accountstatus,
                    IsVerifiedWithSynapse = memberEntity.IsVerifiedWithSynapse,
                    DateCreatedString = memberEntity.DateCreated == null ? "" : Convert.ToDateTime(memberEntity.DateCreated).ToString("MM/dd/yyyy"),
                    DeviceToken = memberEntity.DeviceToken,
                    fngrprnt = !String.IsNullOrEmpty(memberEntity.UDID1) ? memberEntity.UDID1 : null,
                    cip_type = memberEntity.cipTag
                };

                if (memberEntity.Type == "Landlord")
                {
                    var landlordEntity = CommonHelper.GetLandlordDetails(memberId);
                    member.companyName = !String.IsNullOrEmpty(landlordEntity.CompanyName)
                                         ? CommonHelper.GetDecryptedData(landlordEntity.CompanyName)
                                         : "NA";
                }

                return member;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetMemberDetailsForLandingPage FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
            }

            return null;
        }


        [HttpGet]
        [ActionName("GetTransactionDetailByIdForRequestPayPage")]
        public TransactionDto GetTransactionDetailByIdForRequestPayPage(string TransactionId)
        {
            try
            {
                Logger.Info("Service Cntrlr -> GetTransactionDetailByIdForRequestPayPage Fired - TransID: [" + TransactionId + "]");

                var tda = new TransactionsDataAccess();
                Transaction tr = tda.GetTransactionById(TransactionId);

                TransactionDto trans = new TransactionDto();

                // Recipient of the Request (user that will pay)
                // NOTE (Cliff 11/25/15): For requests to non-Nooch users, the Members & Members1 BOTH REFER TO THE EXISTING NOOCH USER WHO SENT THE REQUEST.
                trans.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.FirstName)) + " " +
                             CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.LastName));

                // Requester (user that will receive the money)
                trans.RecepientName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.FirstName)) + " " +
                                      CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.LastName));
                trans.Amount = tr.Amount;
                trans.Memo = tr.Memo;
                trans.SenderPhoto = tr.Member.Photo ?? "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                trans.RecepientPhoto = tr.Member1.Photo ?? "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                trans.MemberId = tr.Member1.MemberId.ToString();
                trans.RecepientId = tr.Member.MemberId.ToString();
                trans.TransactionId = tr.TransactionId.ToString();
                trans.TransactionStatus = tr.TransactionStatus;
                trans.TransactionType = CommonHelper.GetDecryptedData(tr.TransactionType);
                trans.TransactionDate = Convert.ToDateTime(tr.TransactionDate).ToString("d MMM, yyyy");
                trans.IsPhoneInvitation = tr.IsPhoneInvitation ?? false;

                if (tr.InvitationSentTo != null &&
                    tr.IsPhoneInvitation != true &&
                    String.IsNullOrEmpty(tr.PhoneNumberInvited)) // Request via EMAIL
                {
                    trans.InvitationSentTo = CommonHelper.GetDecryptedData(tr.InvitationSentTo);
                }
                else if ((tr.IsPhoneInvitation == true || tr.InvitationSentTo == null) &&
                         !String.IsNullOrEmpty(tr.PhoneNumberInvited)) // Request via PHONE
                {
                    trans.InvitationSentTo = CommonHelper.GetDecryptedData(tr.PhoneNumberInvited);
                }
                else
                    trans.InvitationSentTo = "";

                Logger.Info("Service Cntrlr -> GetTransactionDetailByIdForRequestPayPage - Payer: [" + trans.Name +
                            "], Request Sender: [" + trans.RecepientName + "], InvitationSentTo: [" + trans.InvitationSentTo + "]");

                // CLIFF (10/20/15): For requests sent to existing but 'NonRegistered' users, adding this block to include that user's
                //                   Synapse bank name for displaying the confirmation prompt.
                #region Transactions to Existing But NonRegistered Users

                if (tr.IsPhoneInvitation != true &&
                    (String.IsNullOrEmpty(tr.InvitationSentTo) || tr.InvitationSentTo == "J7xCdPdfLcTvoVrLWCi/zw==") && // A few transactions to existing but NonReg users had encyrpted blank spaces for this value
                    tr.SenderId != tr.RecipientId)
                {
                    Logger.Info("Service Cntrlr -> GetTransactionDetailByIdForRequestPayPage - This request is to an EXISTING user - " +
                                "About to get Synapse Bank info for Payer -> trans.RecepientName: [" + trans.RecepientName + "]");

                    trans.IsExistingButNonRegUser = true;

                    // Get the Payor's Account Info (user that will pay - should be trans.RecepientId set above from Members reference)
                    var mda = new MembersDataAccess();
                    var memberEntity = mda.GetMemberDetails(trans.RecepientId);

                    trans.SsnIsVerified = (memberEntity.IsVerifiedWithSynapse == true) ? "true" : "false";

                    // Get Synapse Bank Account Info
                    var synapseBank = mda.GetSynapseBankAccountDetails(trans.RecepientId);

                    if (synapseBank != null)
                    {
                        if (synapseBank.bank_name == "J7xCdPdfLcTvoVrLWCi/zw==")
                        {
                            // For banks added via routing/account number, the 'bank_name' is a blank encrypted string (never null though), so use the Account Number String instead
                            string accntNum = CommonHelper.GetDecryptedData(synapseBank.account_number_string);

                            trans.BankName = (accntNum.Length < 8) ? accntNum : accntNum.Substring(accntNum.Length - 8);
                            trans.BankId = "manual"; // Using this just as a note... the JS for the landing page will check for this and display the bank name as appropriate
                        }
                        else
                        {
                            trans.BankName = !String.IsNullOrEmpty(synapseBank.bank_name) ? CommonHelper.GetDecryptedData(synapseBank.bank_name) : "";
                            trans.BankId = !String.IsNullOrEmpty(synapseBank.nickname) ? CommonHelper.GetDecryptedData(synapseBank.nickname) : "";
                        }
                    }
                    else
                    {
                        trans.BankName = "no bank found";
                    }
                }

                #endregion Transactions to Existing But NonRegistered Users

                return trans;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr -> GetTransactionDetailByIdForRequestPayPage EXCEPTION - [Trans ID: " + TransactionId + "], [Exception: " + ex + "]");
                return null;
            }
        }


        [HttpGet]
        [ActionName("GetMemberInfoForMicroDepositPage")]
        public SynapseV3VerifyNodeWithMicroDeposits_ServiceInput GetMemberInfoForMicroDepositPage(string memberId)
        {
            SynapseV3VerifyNodeWithMicroDeposits_ServiceInput res = new SynapseV3VerifyNodeWithMicroDeposits_ServiceInput();
            res.success = false;
            res.MemberId = memberId;

            try
            {
                Logger.Info("Service Cntlr - GetMemberInfoForMicroDepositPage Fired - MemberID: [" + memberId + "]");

                var memberObj = CommonHelper.GetMemberDetails(memberId);

                if (memberObj != null)
                {
                    res.userFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                    res.userLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));

                    var synapseBankDetails = CommonHelper.GetSynapseBankDetails(memberId);
                    var memGuid = Utility.ConvertToGuid(memberId);

                    // Added 8/22/16
                    #region Find Any Pending Requests

                    var pendingRequestTrans = _dbContext.Transactions.Where(t => t.TransactionStatus == "Pending" && //t.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" || //t.SenderId == mid &&
                                                                                (t.SenderId == memGuid ||
                                                                                 t.InvitationSentTo == memberObj.UserName ||
                                                                                 t.InvitationSentTo == memberObj.UserNameLowerCase ||
                                                                                 t.InvitationSentTo == memberObj.SecondaryEmail)).ToList();
                    res.PendingTransactionList = new List<PendingTransaction>();

                    if (pendingRequestTrans != null && pendingRequestTrans.Count > 0)
                    {
                        foreach (var transaction in pendingRequestTrans)
                        {
                            PendingTransaction pt = new PendingTransaction();
                            pt.TransactionId = transaction.TransactionId;
                            pt.userName = CommonHelper.GetDecryptedData(transaction.Member1.FirstName) + " " +
                                          CommonHelper.GetDecryptedData(transaction.Member1.LastName);
                            pt.amount = transaction.Amount.ToString("n2");
                            pt.RecipientId = transaction.RecipientId;
                            pt.SenderId = transaction.SenderId;

                            if (pt.SenderId == pt.RecipientId)
                            {
                                pt.InvitationSentTo = transaction.InvitationSentTo;
                                pt.TransactionType = "RequestToNewUser";
                                pt.RecipientId = transaction.SenderId;
                            }

                            res.PendingTransactionList.Add(pt);
                        }

                        res.hasPendingPymnt = res.PendingTransactionList.Count > 0 ? true : false;
                    }

                    #endregion Find any pending request

                    if (synapseBankDetails != null)
                    {
                        res.bankName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(synapseBankDetails.bank_name));
                        res.bankNickName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(synapseBankDetails.nickname));
                        res.bankId = synapseBankDetails.oid;
                        res.NodeId1 = synapseBankDetails.oid;

                        if (synapseBankDetails.Status == "Verified")
                        {
                            res.isAlreadyVerified = true;
                            res.verifiedDate = Convert.ToDateTime(synapseBankDetails.VerifiedOn).ToString("MMM d, yyyy");
                            res.errorMsg = "Bank already verified on [" + Convert.ToDateTime(synapseBankDetails.VerifiedOn).ToString("MM/dd/yyyy") + "]";
                        }
                        else res.success = true;
                    }
                    else res.errorMsg = "Synapse bank details not found";
                }
                else res.errorMsg = "Member not found";
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - GetMemberInfoForMicroDepositPage FAILED - MemberID: [" + memberId +
                             "], Exception: [" + ex + "]");
                res.errorMsg = "Server exception: [" + ex.Message + "]";
            }

            return res;
        }


        [HttpGet]
        [ActionName("CreateNonNoochUserPassword")]
        public StringResult CreateNonNoochUserPassword(string TransId, string password)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> CreateNonNoochUserPassword - TransID: [" + TransId + "]");

                var mda = new MembersDataAccess();
                res.Result = mda.CreateNonNoochUserPassword(TransId, password);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> CreateNonNoochUserPassword FAILED - TransID: [" + TransId + "], Exception: [" + ex + " ]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("CreateNonNoochUserPasswordForPhoneInvitations")]
        public StringResult CreateNonNoochUserPasswordForPhoneInvitations(string TransId, string password, string EmailId)
        {
            try
            {
                Logger.Info("Service Cntlr - CreateNonNoochUserPasswordForPhoneInvitations - TransID: [" + TransId + "]");

                var mda = new MembersDataAccess();
                string result = mda.CreateNonNoochUserPasswordForPhoneInvitations(TransId, password, EmailId);

                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                //UtilityService.ThrowFaultException(ex);
                throw ex;
            }
        }


        [HttpPost]
        [ActionName("CreateNonNoochUserAccountAfterRejectMoney")]
        public StringResult CreateNonNoochUserAccountAfterRejectMoney(string TransId, string password, string EmailId, string UserName)
        {
            try
            {
                Logger.Info("Service Cntlr - CreateNonNoochUserAccountAfterRejectMoney - TransID: [" + TransId + "], UserName: [" + UserName + "]");

                var mda = new MembersDataAccess();
                string result = mda.CreateNonNoochUserAccountAfterRejectMoney(TransId, password, EmailId, UserName);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }

            return new StringResult { Result = "Error in Service Layer!" };
        }


        [HttpGet]
        [ActionName("RejectMoneyCommon")]
        public StringResult RejectMoneyCommon(string TransactionId, string UserType, string TransType)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> RejectMoneyCommon Initiated - TransID: [" + TransactionId + "], " +
                            "TransType: [" + TransType + "], UserType: [" + UserType + "]");

                var tda = new TransactionsDataAccess();
                res.Result = tda.RejectMoneyCommon(TransactionId, UserType, TransType); ;
            }
            catch (Exception ex)
            {
                res.Result = "Error in Service Layer (RejectMoneyCommon)";
                Utility.ThrowFaultException(ex);
            }

            return res;
        }


        #endregion Landing Page Functions


        [HttpGet]
        [ActionName("GetTransactionDetailById")]
        public TransactionDto GetTransactionDetailById(string TransactionId)
        {
            try
            {
                Logger.Info("Service Cntlr -> GetTransactionDetailById - TransID: [" + TransactionId + "]");

                var tda = new TransactionsDataAccess();
                Transaction tr = tda.GetTransactionById(TransactionId);

                TransactionDto trans = new TransactionDto();
                trans.AdminNotes = tr.AdminName;
                trans.Amount = tr.Amount;
                trans.IsPrePaidTransaction = tr.IsPrepaidTransaction;
                trans.FirstName = tr.Member.FirstName;
                trans.LastName = tr.Member.LastName;
                trans.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.FirstName)) + " " +
                             CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.LastName));

                trans.RecepientName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.FirstName)) + " " +
                             CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.LastName));
                trans.SenderPhoto = tr.Member.Photo ?? "";
                trans.RecepientPhoto = tr.Member1.Photo ?? "";
                trans.MemberId = tr.Member.MemberId.ToString();
                trans.Memo = tr.Memo;
                trans.RecepientId = tr.Member1.MemberId.ToString();
                trans.TransactionId = tr.TransactionId.ToString();
                trans.TransactionStatus = tr.TransactionStatus;
                trans.TransactionType = tr.TransactionType;
                trans.TransDate = tr.TransactionDate.Value;
                trans.TransactionFee = tr.TransactionFee.Value;
                trans.InvitationSentTo = (tr.InvitationSentTo != null) ? CommonHelper.GetDecryptedData(tr.InvitationSentTo) : "";
                trans.IsPhoneInvitation = tr.IsPhoneInvitation != null && Convert.ToBoolean(tr.IsPhoneInvitation);

                if (tr.IsPhoneInvitation == true)
                {
                    if (CommonHelper.GetDecryptedData(tr.PhoneNumberInvited).Length == 10)
                        trans.PhoneNumberInvited = CommonHelper.FormatPhoneNumber(CommonHelper.GetDecryptedData(tr.PhoneNumberInvited));
                    else
                        trans.PhoneNumberInvited = "";
                }
                else
                    trans.PhoneNumberInvited = "";

                return trans;
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
                return null;
            }
        }


        /********************************/
        /**  SETTINGS-RELATED METHODS  **/
        /********************************/
        #region Settings-Related Functions


        /// <summary>
        /// To get member notification settings
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("GetMemberNotificationSettings")]
        public MemberNotificationSettingsInput GetMemberNotificationSettings(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> GetMemberNotificationSettings - MemberID: [" + memberId + "]");
                    var notification = new MembersDataAccess().GetMemberNotificationSettings(memberId);

                    if (notification != null)
                    {
                        return new MemberNotificationSettingsInput
                        {
                            NotificationId = notification.NotificationId.ToString(),
                            MemberId = memberId,
                            FriendRequest = notification.FriendRequest ?? false,
                            InviteRequestAccept = notification.InviteRequestAccept ?? false,
                            TransferSent = notification.TransferSent ?? false,
                            TransferReceived = notification.TransferReceived ?? false,
                            TransferAttemptFailure = notification.TransferAttemptFailure ?? false,
                            EmailFriendRequest = notification.EmailFriendRequest ?? false,
                            EmailInviteRequestAccept = notification.EmailInviteRequestAccept ?? false,
                            EmailTransferSent = notification.EmailTransferSent ?? false,
                            EmailTransferReceived = notification.EmailTransferReceived ?? false,
                            EmailTransferAttemptFailure = notification.EmailTransferAttemptFailure ?? false,
                            NoochToBank = notification.NoochToBank ?? false,
                            BankToNooch = notification.BankToNooch ?? false,
                            TransferUnclaimed = notification.TransferUnclaimed ?? false,
                            BankToNoochRequested = notification.BankToNoochRequested ?? false,
                            BankToNoochCompleted = notification.BankToNoochCompleted ?? false,
                            NoochToBankRequested = notification.NoochToBankRequested ?? false,
                            NoochToBankCompleted = notification.NoochToBankCompleted ?? false,
                            InviteReminder = notification.InviteReminder ?? false,
                            LowBalance = notification.LowBalance ?? false,
                            ValidationRemainder = notification.ValidationRemainder ?? false,
                            ProductUpdates = notification.ProductUpdates ?? false,
                            NewAndUpdate = notification.NewAndUpdate ?? false
                        };
                    }
                    else
                        return null;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetMemberNotificationSettings FAILED- MemberID: [" + memberId + "], Exception: [" + ex + " ]");
                    throw new Exception("Server Error.");
                }
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        /// <summary>
        /// To save email notification settings of users
        /// </summary>
        /// <param name="memberNotificationSettings"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MemberEmailNotificationSettings")]
        public StringResult MemberEmailNotificationSettings(MemberNotificationsNewStringTypeSettings memberNotificationSettings, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberNotificationSettings.MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> MemberEmailNotificationSettings - MemberId: [" + memberNotificationSettings.MemberId + "]");

                    return new StringResult
                    {
                        Result = new MembersDataAccess().MemberEmailNotificationSettings("",
                            memberNotificationSettings.MemberId, null, null,
                            (memberNotificationSettings.EmailTransferSent == "1") ? true : false, (memberNotificationSettings.EmailTransferReceived == "1") ? true : false, (memberNotificationSettings.EmailTransferAttemptFailure == "1") ? true : false,
                            (memberNotificationSettings.TransferUnclaimed == "1") ? true : false, (memberNotificationSettings.BankToNoochRequested == "1") ? true : false, (memberNotificationSettings.BankToNoochCompleted == "1") ? true : false,
                            (memberNotificationSettings.NoochToBankRequested == "1") ? true : false, (memberNotificationSettings.NoochToBankCompleted == "1") ? true : false, null,
                            null, null, null, null, (memberNotificationSettings.TransferReceived == "1") ? true : false)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> MemberEmailNotificationSettings FAILED - MemberId: [" + memberNotificationSettings.MemberId + "], Exception: [" + ex + " ]");
                    throw new Exception("Server Error.");
                }

            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpPost]
        [ActionName("MemberPushNotificationSettings")]
        public StringResult MemberPushNotificationSettings(MemberNotificationsNewStringTypeSettings memberNotificationSettings, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberNotificationSettings.MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> MemberPushNotificationSettings - MemberID: [" + memberNotificationSettings.MemberId + "]");
                    return new StringResult
                    {
                        Result = new MembersDataAccess().MemberPushNotificationSettings(memberNotificationSettings.NotificationId,
                            memberNotificationSettings.MemberId, (memberNotificationSettings.FriendRequest == "1") ? true : false, (memberNotificationSettings.InviteRequestAccept == "1") ? true : false,
                            (memberNotificationSettings.TransferReceived == "1") ? true : false, (memberNotificationSettings.TransferAttemptFailure == "1") ? true : false,
                            (memberNotificationSettings.NoochToBank == "1") ? true : false, (memberNotificationSettings.BankToNooch == "1") ? true : false)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> MemberPushNotificationSettings FAILED - MemberId: [" + memberNotificationSettings.MemberId + "], Exception: [" + ex + " ]");
                    throw new Exception("Server Error.");
                }
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("SetShowInSearch")]
        public StringResult SetShowInSearch(string memberId, bool search, string accessToken)
        {
            try
            {
                Logger.Info("Service Cntlr -> SetShowInSearch Fired - MemberID: [" + memberId + "]");
                var mda = new MembersDataAccess();
                return new StringResult { Result = mda.SetShowInSearch(memberId, search) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> SetShowInSearch FAILED - MemberID: [" + memberId + "], Exception: [" + ex + " ]");
                throw new Exception("Server Error.");
            }
        }


        /// <summary>
        /// For Updating a user's Privacy/Security settings for the mobile app (Required
        /// Immediately, Show In Search, Allow Sharing settings).
        /// </summary>
        /// <param name="privacySettings"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MemberPrivacySettings")]
        public StringResult MemberPrivacySettings(PrivacySettings privacySettings, string accessToken)
        {
            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accessToken, privacySettings.MemberId))
            {
                try
                {
                    //Logger.Info("Service Cntlr -> MemberPrivacySettings Fired - MemberID: [" + privacySettings.MemberId + "]");

                    var mda = new MembersDataAccess();
                    res.Result = mda.MemberPrivacySettings(privacySettings.MemberId,
                                 (bool)privacySettings.ShowInSearch,
                                 (bool)privacySettings.AllowSharing,
                                 (bool)privacySettings.RequireImmediately);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> MemberPrivacySettings FAILED - MemberID: [" + privacySettings.MemberId + "], Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else res.Result = "Invalid OAuth 2 Access";

            return res;
        }


        [HttpGet]
        [ActionName("GetMemberPrivacySettings")]
        public PrivacySettings GetMemberPrivacySettings(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                var privacySettings = new PrivacySettings();

                try
                {
                    Logger.Info("Service Cntlr -> GetMemberPrivacySettings Fired - MemberID: [" + memberId + "]");

                    var mda = new MembersDataAccess();
                    var memberPrivacySettings = mda.GetMemberPrivacySettings(memberId);

                    if (memberPrivacySettings != null)
                    {
                        privacySettings.MemberId = memberPrivacySettings.Member.MemberId.ToString();
                        privacySettings.ShowInSearch = memberPrivacySettings.ShowInSearch ?? false;
                        privacySettings.AllowSharing = memberPrivacySettings.AllowSharing ?? false;
                        privacySettings.RequireImmediately = memberPrivacySettings.RequireImmediately ?? false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetMemberPrivacySettings FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }

                return privacySettings;
            }
            else throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("SetAllowSharing")]
        public StringResult SetAllowSharing(string memberId, bool allow, string accessToken)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> SetAllowSharing Initiated - MemberID: [" + memberId + "]");
                var mda = new MembersDataAccess();
                res.Result = mda.SetAllowSharing(memberId, allow);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> SetAllowSharing FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("SaveImmediateRequire")]
        public StringResult SaveImmediateRequire(string memberId, Boolean IsRequiredImmediatley, string accesstoken)
        {
            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accesstoken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> SaveImmediateRequire Fired - MemberID: [" + memberId + "]");

                    var mda = new MembersDataAccess();
                    string s = mda.SaveImmediateRequire(memberId, IsRequiredImmediatley);

                    if (s == "success") res.Result = "success";
                    else res.Result = "Member not found";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> SaveImmediateRequire FAILED - Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else res.Result = "Invalid OAuth 2 Access";

            return res;
        }


        #endregion Settings-Related Functions


        /************************************************/
        /***** ----  SYNAPSE-RELATED SERVICES  ---- *****/
        /************************************************/
        #region Synapse-Related Services

        /// <summary>
        /// Used only by other methods WITHIN NoochServices Controller.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("RegisterUserWithSynapseV3")]
        public synapseCreateUserV3Result_int RegisterUserWithSynapseV3(string memberId)
        {
            synapseCreateUserV3Result_int res = new synapseCreateUserV3Result_int();
            res.success = false;
            res.errorMsg = "Service Cntrlr - Initial";

            try
            {
                MembersDataAccess mda = new MembersDataAccess();
                res = mda.RegisterUserWithSynapseV3(memberId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr -> RegisterUserWithSynapseV3 FAILED - Exception: [" + ex + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("RegisterExistingUserWithSynapseV3")]
        public RegisterUserSynapseResultClassExt RegisterExistingUserWithSynapseV3(RegisterUserWithSynapseV3_Input input)
        {
            try
            {
                Logger.Info("Service Cntlr -> RegisterExistingUserWithSynapseV3 Fired - MemberID: [" + input.memberId +
                            "], Name: [" + input.fullname + "], Email: [" + input.email +
                            "], Is ID Img Sent: [" + input.isIdImageAdded + "], CIP: [" + input.cip +
                            "], FBID: [" + input.fbid + "]");

                MembersDataAccess mda = new MembersDataAccess();
                RegisterUserSynapseResultClassExt nc = new RegisterUserSynapseResultClassExt();

                synapseCreateUserV3Result_int res = mda.RegisterExistingUserWithSynapseV3(input.transId, input.memberId, input.email,
                                                                                          input.phone, input.fullname, input.pw, input.ssn,
                                                                                          input.dob, input.address, input.zip, input.fngprnt,
                                                                                          input.ip, input.cip, input.fbid,
                                                                                          input.isIdImageAdded, input.idImageData);

                nc.success = res.success == true ? "true" : "false";
                nc.access_token = res.oauth.oauth_key;
                nc.expires_in = res.oauth.expires_in;
                nc.refresh_token = res.oauth.refresh_token;
                nc.user_id = res.user_id;
                nc.memberIdGenerated = res.memberIdGenerated;
                nc.ssn_verify_status = res.ssn_verify_status;
                nc.reason = res.reason;
                nc.errorMsg = res.errorMsg;

                Logger.Info("Service Cntlr -> RegisterExistingUserWithSynapseV3 - Returning Payload - Reason: [" + nc.reason +
                            "], Error Msg: [" + nc.errorMsg + "]");

                return nc;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> RegisterExistingUserWithSynapsev3 FAILED - MemberID: [" + input.memberId + "], Name: [" + input.fullname +
                             "], Email of New User: [" + input.email + "], Exception: [" + ex + "]");
                return null;
            }
        }


        [HttpPost]
        [ActionName("RegisterNonNoochUserWithSynapse")]
        public RegisterUserSynapseResultClassExt RegisterNonNoochUserWithSynapse(RegisterUserWithSynapseV3_Input input)
        {
            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";

            try
            {
                if (String.IsNullOrEmpty(input.company))
                    input.company = "nooch";

                Logger.Info("Service Cntrlr -> RegisterNonNoochUserWithSynapse Fired - MemberID: [" + input.memberId +
                            "], Name: [" + input.fullname + "], Email: [" + input.email +
                            "], Is ID Img Sent: [" + input.isIdImageAdded + "], CIP: [" + input.cip +
                            "], FBID: [" + input.fbid + "], Company: [" + input.company + "]");

                MembersDataAccess mda = new MembersDataAccess();

                synapseCreateUserV3Result_int mdaRes = mda.RegisterNonNoochUserWithSynapseV3(input.transId, input.email, input.phone, input.fullname,
                                                                                             input.pw, input.ssn, input.dob, input.address,
                                                                                             input.zip, input.fngprnt, input.ip, input.cip, input.fbid,
                                                                                             input.company, input.isIdImageAdded, input.idImageData);

                res.success = mdaRes.success.ToString().ToLower();
                res.reason = mdaRes.reason;
                res.user_id = mdaRes.user_id;
                res.memberIdGenerated = mdaRes.memberIdGenerated;
                res.ssn_verify_status = mdaRes.ssn_verify_status;
                res.errorMsg = mdaRes.errorMsg;

                if (mdaRes.oauth != null)
                {
                    res.access_token = mdaRes.oauth.oauth_key;
                    res.expires_in = mdaRes.oauth.expires_in;
                    res.refresh_token = mdaRes.oauth.refresh_token;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr -> RegisterNonNoochUserWithSynapse FAILED - New User Name: [" + input.fullname +
                             "], Email: [" + input.email + "], TransID: [" + input.transId +
                             "], Exception: [" + ex + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("submitDocumentToSynapseV3")]
        public synapseV3GenericResponse submitDocumentToSynapseV3(SaveVerificationIdDocument DocumentDetails)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();
            res.isSuccess = false;

            try
            {
                Logger.Info("Service Cntlr - submitDocumentToSynapseV3 Fired - MemberID: [" + DocumentDetails.MemberId + "]");

                var mda = new MembersDataAccess();

                // Make URL from byte array b/c submitDocumentToSynapseV3 expects url of image.
                var ImageUrlMade = "";
                var filename = "";

                //convert base64 string into byte array(sent from mobile app)
                if (DocumentDetails.Photo != null)
                {
                    DocumentDetails.Picture = System.Convert.FromBase64String(DocumentDetails.Photo);
                }

                if (DocumentDetails.Picture != null)
                {
                    // Make image from bytes
                    filename = HttpContext.Current.Server.MapPath("../../UploadedPhotos") + "/Photos/" +
                                                                  DocumentDetails.MemberId + ".png";
                    using (MemoryStream inStream = new MemoryStream(DocumentDetails.Picture))
                    {
                        using (ImageFactory imageFactory = new ImageFactory())
                        {
                            imageFactory.Load(inStream).Quality(25).Save(filename);
                        }
                    }
                    //fs.Write(DocumentDetails.Picture, 0, (int)DocumentDetails.Picture.Length);

                    ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + DocumentDetails.MemberId + ".png";
                }
                else
                {
                    Guid memGuid = new Guid(DocumentDetails.MemberId);

                    Member memberObj = mda.GetMemberByGuid(memGuid);

                    if (!String.IsNullOrEmpty(memberObj.VerificationDocumentPath))
                        ImageUrlMade = memberObj.VerificationDocumentPath;
                    else
                        ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                }

                var mdaResult = mda.submitDocumentToSynapseV3(DocumentDetails.MemberId, ImageUrlMade);

                res.isSuccess = mdaResult.success;
                res.msg = mdaResult.message;

                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> submitDocumentToSynapseV3 FAILED - userName: [" + DocumentDetails.MemberId + "], Exception: [" + ex + "]");

                throw new Exception("Server Error.");
            }
        }


        /// <summary>
        /// For adding a Synapse bank account w/ Routing & Account #'s.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="bankNickName"></param>
        /// <param name="account_num"></param>
        /// <param name="routing_num"></param>
        /// <param name="accounttype"></param>
        /// <param name="accountclass"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("SynapseV3AddNodeWithAccountNumberAndRoutingNumber")]
        public SynapseBankLoginV3_Response_Int SynapseV3AddNodeWithAccountNumberAndRoutingNumber(string MemberId, string bankNickName, string account_num, string routing_num, string accounttype, string accountclass)
        {
            Logger.Info("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber Fired - MemberID: [" + MemberId +
                        "], Bank Nick Name: [" + bankNickName + "], Routing #: [" + routing_num +
                        "], Account #: [" + account_num + "], Type: [" + accounttype + "], Class: [" + accountclass + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();
            res.Is_success = false;
            res.Is_MFA = false;

            #region Check if all required data was passed

            if (String.IsNullOrEmpty(bankNickName) || String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(account_num) || String.IsNullOrEmpty(routing_num) ||
                String.IsNullOrEmpty(accounttype) || String.IsNullOrEmpty(accountclass))
            {
                if (String.IsNullOrEmpty(MemberId))
                    res.errorMsg = "Invalid data - need MemberID.";
                else if (String.IsNullOrEmpty(bankNickName))
                    res.errorMsg = "Invalid data - need bank account nick name.";
                else if (String.IsNullOrEmpty(account_num))
                    res.errorMsg = "Invalid data - need bank account number.";
                else if (String.IsNullOrEmpty(routing_num))
                    res.errorMsg = "Invalid data - need bank routing number.";
                else if (String.IsNullOrEmpty(accounttype))
                    res.errorMsg = "Invalid data - need bank account type.";
                else if (String.IsNullOrEmpty(accountclass))
                    res.errorMsg = "Invalid data - need bank account class.";
                else
                    res.errorMsg = "Invalid data - please try again.";

                Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber ABORTING: Invalid data sent for: [" + MemberId + "].");

                return res;
            }

            #endregion Check if all required data was passed

            try
            {
                #region Get the Member's Details

                Guid id = Utility.ConvertToGuid(MemberId);

                var noochMember = CommonHelper.GetMemberDetails(id.ToString());

                if (noochMember == null)
                {
                    Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - Member NOT FOUND.");
                    res.errorMsg = "Member not found.";
                    return res;
                }

                #endregion Get the Member's Details

                #region Check Member's Status And Phone

                // Checks on user account: is phone verified? Is user's status = 'Active'?
                /*if (noochMember.Status != "Active" &&
                    noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE w/ Account/Routing # Attempted, but Member is Not 'Active' but [" + noochMember.Status + "] for MemberId: [" + MemberId + "]");
                    res.errorMsg = "User status is not active but, " + noochMember.Status;
                    return res;
                }

                if (noochMember.IsVerifiedPhone != true &&
                    noochMember.Status != "NonRegistered" && noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE w/ Account/Routing # Attempted, but Member's Phone is Not Verified. MemberId: [" + MemberId + "]");
                    res.errorMsg = "User phone is not verified";
                    return res;
                }*/

                #endregion Check Member's Status And Phone

                #region Get Synapse Account Credentials

                // Check if the user already has Synapse User credentials (would have a record in SynapseCreateUserResults.dbo)

                var createSynapseUserDetails = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());

                if (createSynapseUserDetails == null) // No Synapse user details were found, so need to create a new Synapse User
                {
                    // Call RegisterUserWithSynapse() to get auth token by registering this user with Synapse
                    // This accounts for all users connecting a bank for the FIRST TIME (Sent to this method from Add-Bank.aspx.cs)
                    synapseCreateUserV3Result_int registerSynapseUserResult = RegisterUserWithSynapseV3(MemberId);

                    if (registerSynapseUserResult.success)
                    {
                        createSynapseUserDetails = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());
                    }
                    else
                    {
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED: Could not create Synapse User Record for: [" + MemberId + "].");
                    }
                }

                // Check again if it's still null (which it shouldn't be because we just created a new Synapse user above if it was null.
                if (createSynapseUserDetails == null)
                {
                    Logger.Error("Service Cntrlr -> SynapseV3 ADD NODE ERROR: No Synapse OAuth code found in Nooch DB for: [" + MemberId + "].");
                    res.errorMsg = "No Authentication code found for given user.";
                    return res;
                }

                #endregion Get Synapse Account Credentials


                // We have Synapse authentication token

                #region Setup Call To SynapseV3 /node/add

                SynapseBankLoginUsingRoutingAndAccountNumberv3_Input bankloginParameters = new SynapseBankLoginUsingRoutingAndAccountNumberv3_Input();

                SynapseV3Input_login login = new SynapseV3Input_login();
                login.oauth_key = CommonHelper.GetDecryptedData(createSynapseUserDetails.access_token);

                SynapseV3Input_user user = new SynapseV3Input_user();
                user.fingerprint = noochMember.UDID1;

                SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node node = new SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node();
                SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo nodeInfo = new SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo();
                SynapseBankLoginV3_Input_extra extra = new SynapseBankLoginV3_Input_extra();

                nodeInfo._class = accountclass;
                nodeInfo.routing_num = routing_num;
                nodeInfo.type = accounttype;
                nodeInfo.nickname = bankNickName;
                nodeInfo.account_num = account_num;

                extra.supp_id = "";

                node.type = "ACH-US";
                node.info = nodeInfo;
                node.extra = extra;

                bankloginParameters.login = login;
                bankloginParameters.user = user;
                bankloginParameters.node = node;

                string UrlToHit = "";
                UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/add" : "https://synapsepay.com/api/v3/node/add";

                #endregion Setup Call To SynapseV3 /node/add


                // Calling Synapse Bank Login service
                #region Call SynapseV3 Add Node API

                try
                {
                    var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                    http.Accept = "application/json";
                    http.ContentType = "application/json";
                    http.Method = "POST";

                    string parsedContent = JsonConvert.SerializeObject(bankloginParameters);
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    Byte[] bytes = encoding.GetBytes(parsedContent);

                    Stream newStream = http.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = http.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();

                    JObject addBankRespFromSynapse = JObject.Parse(content);

                    if (addBankRespFromSynapse["success"].ToString().ToLower() == "true" &&
                        addBankRespFromSynapse["nodes"] != null)
                    {
                        // Malkit (30 May 2016): Commented out code for Marking Any Existing Synapse Bank Login Entries as Deleted b/c this service uses routing/account # only,
                        //                       so for this case the result will go be saved in SynapseBanksOfMembers table instead of synpaseBankLoginResult table along
                        //                       with bank account, routing and some other info. From now we will be storing IsAddedUsingRoutingNumber field as true in
                        //                       SynapseBanksOfMembers to keep track of who came through routing numbers.

                        #region Save New Record In SynapseBanksOfMember

                        try
                        {
                            //Logger.Info("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber -> Synapse RESPONSE: [" + addBankRespFromSynapse.ToString() + "]");

                            if (addBankRespFromSynapse["nodes"][0]["info"] != null)
                            {
                                #region Mark all existing banks as inactive

                                try
                                {
                                    var existingBanks = _dbContext.SynapseBanksOfMembers.Where(bank => bank.MemberId.Value.Equals(id) &&
                                                                                                       bank.IsDefault != false).ToList();

                                    foreach (SynapseBanksOfMember sbank in existingBanks)
                                    {
                                        sbank.IsDefault = false;
                                        _dbContext.SaveChanges();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber - ERROR marking existing banks as not default - " +
                                                 "Exception: [" + ex.Message + "]");
                                }

                                #endregion Mark all existing banks as inactive

                                JToken info = addBankRespFromSynapse["nodes"][0]["info"];

                                // Saving entry in SynapseBanksOfMember Table
                                SynapseBanksOfMember sbom = new SynapseBanksOfMember();
                                sbom.IsAddedUsingRoutingNumber = true;
                                sbom.MemberId = id;
                                sbom.AddedOn = DateTime.Now;
                                sbom.mfa_verifed = false;
                                sbom.account_number_string = CommonHelper.GetEncryptedData(info["account_num"].ToString());
                                sbom.bank_name = CommonHelper.GetEncryptedData(info["bank_long_name"].ToString());
                                sbom.@class = info["class"].ToString();
                                sbom.name_on_account = CommonHelper.GetEncryptedData(info["name_on_account"].ToString());
                                sbom.nickname = CommonHelper.GetEncryptedData(info["nickname"].ToString());
                                sbom.routing_number_string = CommonHelper.GetEncryptedData(info["routing_num"].ToString());
                                sbom.type_bank = info["type"].ToString();
                                sbom.oid = CommonHelper.GetEncryptedData(addBankRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString());
                                sbom.allowed = addBankRespFromSynapse["nodes"][0]["allowed"].ToString();
                                sbom.supp_id = addBankRespFromSynapse["nodes"][0]["extra"]["supp_id"].ToString();

                                // Setting as default.... and 'Not Verified' for now, user must complete micro deposit verification
                                sbom.IsDefault = true;
                                sbom.Status = "Not Verified";

                                try
                                {
                                    _dbContext.SynapseBanksOfMembers.Add(sbom);
                                    _dbContext.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    var error = "Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - Could not save record " +
                                                 "in SynapseBanksOfMembers Table - ABORTING - MemberID: [" + MemberId + "], Exception: [" + ex + "]";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error);
                                    res.errorMsg = "Failed to save entry in BankLoginResults table: [" + ex.Message + "]";
                                }

                                Logger.Info("Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber SUCCESS - Added record to SynapseBanksOfMembers Table - UserName: [" + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                res.Is_success = true;

                                if (noochMember.cipTag != "vendor")
                                {
                                    #region Send Initial Micro-Deposit Email

                                    _dbContext.Entry(sbom).Reload();

                                    var firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName));
                                    var accountNum = "**** - " + account_num.Substring(account_num.Length - 4);
                                    var verifyLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                        "Nooch/MicroDepositsVerification?mid=" + MemberId);

                                    var templateToUse = "MicroDepositNotification";

                                    var tokens = new Dictionary<string, string>
	                                        {
                                                {Constants.PLACEHOLDER_FIRST_NAME, firstName},
                                                {"$BankName$", bankNickName},
	                                            {"$BankRouting$", routing_num},
	                                            {"$BankAccountNum$", accountNum},
                                                {"$BankType$", accounttype},
	                                            {"$BankClass$", accountclass},
	                                            {"$PayLink$", verifyLink}
	                                        };

                                    var fromAddress = Utility.GetValueFromConfig("transfersMail");
                                    var toAddress = CommonHelper.GetDecryptedData(noochMember.UserName);

                                    try
                                    {
                                        Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                          "Bank Account Verification - Important Info",
                                                          null, tokens, null, "bankAddedManually@nooch.com", null);

                                        Logger.Info("Service Cntrlr -> SynapseV3 AddNodeWithAccountNumberAndRoutingNumber - [" + templateToUse + "] - Email sent to [" +
                                                     toAddress + "] successfully");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Service Cntrlr -> SynapseV3 AddNodeWithAccountNumberAndRoutingNumber - EMAIL FAILED: " +
                                                     "[" + templateToUse + "] Email NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                                    }

                                    #endregion Send Initial Micro-Deposit Email

                                    #region Scheduling Micro-Deposit Reminder Email

                                    try
                                    {
                                        // Determine when to send the reminder email
                                        TimeSpan delayToUse = TimeSpan.FromDays(2);
                                        DateTime currentDateTime = DateTime.Now;

                                        if ((int)currentDateTime.DayOfWeek == 5)
                                            delayToUse = TimeSpan.FromDays(5);
                                        else if ((int)currentDateTime.DayOfWeek == 6)
                                            delayToUse = TimeSpan.FromDays(4);
                                        else if ((int)currentDateTime.DayOfWeek == 6)
                                            delayToUse = TimeSpan.FromDays(4);

                                        var x = BackgroundJob.Schedule(() => CommonHelper.SendMincroDepositsVerificationReminderEmail(sbom.MemberId.ToString(), sbom.oid), delayToUse);
                                        if (x != null)
                                            Logger.Info("Service Cntrlr -> SynapseV3 AddNodeWithAccountNumberAndRoutingNumber - Scheduled Micro Deposit Reminder email in Background - [" +
                                                        "] DelayToUser: [" + delayToUse.ToString() + " Days]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber ERROR - Failed to schedule Micro-Deposit Reminder Email - " +
                                                     "MemberID: [" + MemberId + " ], Exception: [" + ex + "]");
                                    }

                                    #endregion Scheduling Micro-Deposit Reminder Email
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = "Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - Unable to save SynapseBankLogin response for MFA Bank in DB - " +
                                        "MemberID: [" + MemberId + "], Exception: [" + ex + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                            res.errorMsg = "Got exception - Failed to save entry in BankLoginResults table";
                        }

                        #endregion Save New Record In SynapseBanksOfMember
                    }
                    else
                    {
                        // Synapse response for 'success' was not true
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber ERROR - Synapse response for 'success' was not true - MemberID: [" + MemberId + "]");
                        res.errorMsg = "Synapse response for success was not true";
                    }

                    return res;
                }
                catch (WebException we)
                {
                    #region Bank Login Catch

                    res.Is_success = false;

                    var errorCode = ((HttpWebResponse)we.Response).StatusCode;
                    var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                    JObject jsonFromSynapse = JObject.Parse(resp);

                    var error = "Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - MemberID: [" + MemberId +
                                "], Synapse Response JSON: [" + jsonFromSynapse.ToString() + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);

                    if (jsonFromSynapse["error"] != null)
                    {
                        var errorMsg = jsonFromSynapse["error"]["en"].ToString();
                        res.errorMsg = errorMsg;
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - Synapse MESSAGE: [" + errorMsg + "]");
                    }
                    else
                    {
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - HTTP ErrorCode: [" + errorCode.ToString() +
                                     "], WebException: [" + we.ToString() + "]");
                        res.errorMsg = we.Message;
                    }

                    return res;

                    #endregion Bank Login Catch
                }

                #endregion Call SynapseV3 Add Node API
            }
            catch (Exception ex)
            {
                var error = "Service Cntrlr -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber FAILED - OUTER EXCEPTION - MemberID: [" + MemberId + "], Exception: [" + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                res.errorMsg = "Service Controller Outer Exception: [" + ex.Message + "]";
            }

            return res;
        }


        /// <summary>
        /// For submitting a user's Bank Login credentials to Synapse V3.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="BnkName"></param>
        /// <param name="BnkUserName"></param>
        /// <param name="BnkPw"></param>
        /// <returns>SynapseV3BankLoginResult_ServiceRes</returns>
        [HttpGet]
        [ActionName("SynapseV3AddNodeBankLogin")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3AddNodeBankLogin(string MemberId, string BnkName, string BnkUserName, string BnkPw)
        {
            Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin Fired - MemberId: [" + MemberId + "], BankName: [" + BnkName +
                        "], BankUserName: [" + BnkUserName + "], Bank PW: [" + BnkPw + "]");

            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();
            res.Is_success = false;

            #region Check if all required data was passed

            if (String.IsNullOrEmpty(BnkName) || String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(BnkUserName) || String.IsNullOrEmpty(BnkPw))
            {
                if (String.IsNullOrEmpty(MemberId))
                    res.errorMsg = "Invalid data - need MemberID.";
                else if (String.IsNullOrEmpty(BnkName))
                    res.errorMsg = "Invalid data - need bank name.";
                else if (String.IsNullOrEmpty(BnkUserName))
                    res.errorMsg = "Invalid data - need bank username.";
                else if (String.IsNullOrEmpty(BnkPw))
                    res.errorMsg = "Invalid data - need bank password.";
                else
                    res.errorMsg = "Invalid data - please try again.";

                Logger.Error("Service Controller -> SynapseV3AddNodeBankLogin ABORTING: Invalid data sent for: [" + MemberId + "].");

                return res;
            }

            #endregion Check if all required data was passed

            try
            {
                #region Get the Member's Details

                Guid memGuid = Utility.ConvertToGuid(MemberId);
                var noochMember = _dbContext.Members.FirstOrDefault(m => m.MemberId == memGuid && m.IsDeleted == false);

                if (noochMember == null)
                {
                    Logger.Error("Service Cntrlr -> SynapseV3 ADD NODE ERROR, but Member NOT FOUND.");

                    res.errorMsg = "Member not found.";
                    return res;
                }

                #endregion Get the Member's Details

                #region Check Member's Status And Phone

                // Checks on user account: is phone verified? Is user's status = 'Active'?

                if (noochMember.Status == "Suspended" ||
                    noochMember.Status == "Temporarily_Blocked" ||
                    noochMember.Status == "Deleted")
                {
                    Logger.Error("Service Cntrlr -> SynapseV3 ADD NODE Attempted, but Member is Not 'Active' but [" + noochMember.Status + "] for MemberId: [" + MemberId + "]");

                    res.errorMsg = "User status is not active but, " + noochMember.Status;
                    return res;
                }

                /*if (noochMember.IsVerifiedPhone != true &&
                    noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Personal - Browser")
                {
                    Logger.Error("Service Cntrlr -> SynapseV3 ADD NODE Attempted, but Member's Phone is Not Verified. MemberId: [" + MemberId + "]");

                    res.errorMsg = "User phone is not verified";
                    return res;
                }*/

                #endregion Check Member's Status And Phone

                #region Get Synapse Account Credentials

                // Check if the user already has Synapse User credentials (would have a record in SynapseCreateUserResults.dbo)

                var createSynapseUserDetails = CommonHelper.GetSynapseCreateaUserDetails(memGuid.ToString());

                if (createSynapseUserDetails == null) // No Synapse user details were found, so need to create a new Synapse User
                {
                    Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin - Unable to find existing Synapse Create User record, attempting to register new one: [" + MemberId + "]");

                    // Call RegisterUserWithSynapse() to get auth token by registering this user with Synapse
                    // This accounts for all users connecting a bank for the FIRST TIME (Sent to this method from AddBank.aspx.cs)
                    synapseCreateUserV3Result_int registerSynapseUserResult = RegisterUserWithSynapseV3(MemberId);

                    if (registerSynapseUserResult.success)
                    {
                        createSynapseUserDetails = CommonHelper.GetSynapseCreateaUserDetails(memGuid.ToString());

                        // Check again if it's still null (which it shouldn't be because we just created a new Synapse user above if it was null.
                        if (createSynapseUserDetails == null)
                        {
                            Logger.Error("Service Cntrlr -> SynapseV3AddNodeBankLogin FAILED - Unable to find existing Synapse Create User record or create a new one: [" + MemberId + "]");

                            res.errorMsg = "No Authentication code found for given user.";
                            return res;
                        }
                    }
                    else
                    {
                        Logger.Info("Service Controller -> SynapseV3 ADD NODE ERROR: Could not create Synapse User Record for: [" + MemberId + "].");
                    }
                }

                #endregion Get Synapse Account Credentials


                // We have Synapse authentication token, now call Synapse /v3/node/add
                #region Setup Call To SynapseV3 /node/add

                SynapseBankLoginv3_Input bankloginParameters = new SynapseBankLoginv3_Input();

                SynapseV3Input_login login = new SynapseV3Input_login();
                login.oauth_key = CommonHelper.GetDecryptedData(createSynapseUserDetails.access_token);

                SynapseV3Input_user user = new SynapseV3Input_user();
                if (!String.IsNullOrEmpty(noochMember.UDID1) && noochMember.UDID1.Length > 6)
                {
                    user.fingerprint = noochMember.UDID1;
                }
                else
                {
                    try
                    {
                        var newFingerprint = Guid.NewGuid().ToString("n").Substring(0, 24).ToLower();
                        user.fingerprint = newFingerprint;

                        noochMember.UDID1 = newFingerprint;
                        int save = _dbContext.SaveChanges();

                        Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin - User had no UDID1, but successfully created & saved a new one: Save: [" + save +
                                    "], New UDID1 (Fngrprnt): [" + newFingerprint + "]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeBankLogin - User had no UDID1, Attempted to create & save a new one, but FAILED - Exception: [" + ex.Message + "]");
                    }
                }
                bankloginParameters.login = login;
                bankloginParameters.user = user;

                SynapseBankLoginV3_Input_node node = new SynapseBankLoginV3_Input_node();
                node.type = "ACH-US";

                SynapseBankLoginV3_Input_bankInfo nodeInfo = new SynapseBankLoginV3_Input_bankInfo();
                nodeInfo.bank_id = BnkUserName;
                nodeInfo.bank_pw = BnkPw;
                nodeInfo.bank_name = BnkName;

                node.info = nodeInfo;

                SynapseBankLoginV3_Input_extra extra = new SynapseBankLoginV3_Input_extra();
                extra.supp_id = "";

                node.extra = extra;
                bankloginParameters.node = node;

                string UrlToHit = "";
                UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/add" : "https://synapsepay.com/api/v3/node/add";

                #endregion Setup Call To SynapseV3 /node/add

                Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin - /node/add PAYLOAD: Oauth_Key: [" + bankloginParameters.login.oauth_key +
                            "], Fngrprnt: [" + bankloginParameters.user.fingerprint + "], Type: [" + bankloginParameters.node.type +
                            "], Bank_ID: [" + bankloginParameters.node.info.bank_id + "], Bank_PW: [" + bankloginParameters.node.info.bank_pw +
                            "], Bank_Name: [" + bankloginParameters.node.info.bank_name + "], Supp_ID: [" + bankloginParameters.node.extra.supp_id + "]");

                // Calling Synapse Bank Login service
                #region Call SynapseV3 Add Node API

                var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHit));
                http.Accept = "application/json";
                http.ContentType = "application/json";
                http.Method = "POST";

                string parsedContent = JsonConvert.SerializeObject(bankloginParameters);
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

                    JObject bankLoginRespFromSynapse = JObject.Parse(content);

                    if (bankLoginRespFromSynapse["success"].ToString().ToLower() == "true" &&
                        bankLoginRespFromSynapse["nodes"] != null)
                    {
                        // Marking Any Existing Synapse Bank Login Entries as Deleted
                        var removeExistingSynapseBankLoginResult = CommonHelper.RemoveSynapseBankLoginResults(memGuid.ToString());

                        #region Save New Record In SynapseBankLoginResults

                        try
                        {
                            SynapseBankLoginResult sbr = new SynapseBankLoginResult();
                            sbr.MemberId = memGuid;
                            sbr.IsSuccess = true;
                            sbr.dateCreated = DateTime.Now;
                            sbr.IsDeleted = false;
                            sbr.IsCodeBasedAuth = false; // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                            sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                            sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString());

                            if (bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"] != null ||
                                bankLoginRespFromSynapse["nodes"][0]["allowed"] == null)
                            {
                                #region MFA was returned

                                // Set values for storing in SynapseBankLoginResults table in DB
                                sbr.IsMfa = true;
                                sbr.IsQuestionBasedAuth = true;
                                sbr.mfaQuestion = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString().Trim();
                                sbr.AuthType = "questions";
                                sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString());

                                res.bankOid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString();
                                res.bankMFA = res.bankOid; // CC (5/8/16): Not sure we still need the 'bankMFA' param... already have bankOid above, and mfaQuestion below
                                res.Is_MFA = true;
                                res.errorMsg = "OK";
                                res.mfaQuestion = sbr.mfaQuestion;

                                res.SynapseNodesList = new SynapseNodesListClass();
                                res.SynapseNodesList.nodes = new List<SynapseIndividualNodeClass>();

                                #endregion MFA was returned
                            }
                            else
                            {
                                sbr.IsMfa = false;
                                sbr.IsQuestionBasedAuth = false;
                                sbr.mfaQuestion = "None";
                                sbr.AuthType = null;

                                res.Is_MFA = false;
                            }

                            // Now Add object to Nooch DB (save whether if it's an MFA node or not)
                            _dbContext.SynapseBankLoginResults.Add(sbr);
                            int i = _dbContext.SaveChanges();

                            if (i > 0)
                            {
                                // Return if MFA, otherwise continue on and parse the banks
                                if (res.Is_MFA)
                                {
                                    Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin SUCCESS - Added record to synapseBankLoginResults Table - Got MFA from Synapse - UserName: [" +
                                                CommonHelper.GetDecryptedData(noochMember.UserName) + "], MFA Question: [" + res.mfaQuestion + "]");
                                    res.Is_success = true;
                                    return res;
                                }

                                //Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin SUCCESS - Added record to synapseBankLoginResults Table - NO MFA found - UserName: [" + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");
                            }
                            else
                            {
                                var error = "Service Cntrlr -> SynapseV3AddNodeBankLogin FAILURE - Could not save record in SynapseBankLoginResults Table - ABORTING - MemberID: [" + MemberId + "]";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                res.errorMsg = "Failed to save entry in BankLoginResults table (inner)";
                                return res;
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = "Service Cntrlr -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - MemberID: [" +
                                          MemberId + "], Exception: [" + ex + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                            res.errorMsg = "Got exception - Failed to save entry in BankLoginResults table";
                            return res;
                        }

                        #endregion Save New Record In SynapseBankLoginResults


                        #region No MFA response returned

                        // Array[] of banks ("nodes") expected here
                        RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                        if (allNodesParsedResult != null)
                        {
                            SynapseNodesListClass nodesList = new SynapseNodesListClass();
                            List<SynapseIndividualNodeClass> bankslistextint = new List<SynapseIndividualNodeClass>();

                            short numOfBanksSavedSuccessfully = 0;

                            foreach (nodes bank in allNodesParsedResult.nodes)
                            {
                                try
                                {
                                    SynapseIndividualNodeClass b = new SynapseIndividualNodeClass();
                                    b.oid = bank._id.oid;
                                    b.account_class = bank.info._class;
                                    b.bank_name = bank.info.bank_name;
                                    b.is_active = bank.is_active;
                                    b.name_on_account = bank.info.name_on_account;
                                    b.nickname = bank.info.nickname;
                                    b.account_num = (bank.info.account_num);

                                    bankslistextint.Add(b);


                                    #region Save Each Bank In Nodes Array From Synapse

                                    // saving these banks ("nodes) in DB, later one of these banks will be set as default bank

                                    SynapseBanksOfMember bnkAccnt = new SynapseBanksOfMember();

                                    bnkAccnt.MemberId = Utility.ConvertToGuid(MemberId);
                                    bnkAccnt.AddedOn = DateTime.Now;
                                    bnkAccnt.IsDefault = false;

                                    // Holdovers from V2
                                    bnkAccnt.account_number_string = !String.IsNullOrEmpty(bank.info.account_num) ? CommonHelper.GetEncryptedData(bank.info.account_num) : null;
                                    bnkAccnt.routing_number_string = !String.IsNullOrEmpty(bank.info.routing_num) ? CommonHelper.GetEncryptedData(bank.info.routing_num) : null;
                                    bnkAccnt.bank_name = !String.IsNullOrEmpty(bank.info.bank_name) ? CommonHelper.GetEncryptedData(bank.info.bank_name) : null;
                                    bnkAccnt.name_on_account = !String.IsNullOrEmpty(bank.info.name_on_account) ? CommonHelper.GetEncryptedData(bank.info.name_on_account) : null;
                                    bnkAccnt.nickname = !String.IsNullOrEmpty(bank.info.nickname) ? CommonHelper.GetEncryptedData(bank.info.nickname) : null;
                                    bnkAccnt.is_active = bank.is_active;
                                    bnkAccnt.mfa_verifed = false;
                                    bnkAccnt.Status = "Not Verified";

                                    // New in V3
                                    bnkAccnt.oid = !String.IsNullOrEmpty(bank._id.oid) ? CommonHelper.GetEncryptedData(bank._id.oid) : null;
                                    bnkAccnt.allowed = !String.IsNullOrEmpty(bank.allowed) ? bank.allowed : "UNKNOWN";
                                    bnkAccnt.@class = !String.IsNullOrEmpty(bank.info._class) ? bank.info._class : "UNKNOWN";
                                    bnkAccnt.supp_id = bank.extra.supp_id;
                                    bnkAccnt.type_bank = !String.IsNullOrEmpty(bank.info.type) ? bank.info.type : "UNKNOWN";
                                    bnkAccnt.type_synapse = "ACH-US";

                                    bnkAccnt.IsAddedUsingRoutingNumber = false;

                                    _dbContext.SynapseBanksOfMembers.Add(bnkAccnt);
                                    int addBankToDB = _dbContext.SaveChanges();
                                    _dbContext.Entry(bnkAccnt).Reload();

                                    // HERE
                                    if (addBankToDB == 1)
                                    {
                                        Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin - SUCCESSFULLY Added Bank to DB - Bank OID: [" + bank._id.oid +
                                                    "], MemberID: [" + MemberId + "]");
                                        numOfBanksSavedSuccessfully += 1;
                                    }
                                    else
                                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeBankLogin - Failed to save new BANK in SynapseBanksOfMembers Table in DB - MemberID: [" + MemberId + "]");
                                }
                                catch (Exception ex)
                                {
                                    var error = "Service Cntrlr -> SynapseV3AddNodeBankLogin EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - MemberID: [" +
                                                   MemberId + "], Exception: [" + ex + "]";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error);
                                    res.errorMsg = "Error occured while saving banks from Synapse.";
                                }

                                    #endregion Save Each Bank In Nodes Array From Synapse
                            }
                            nodesList.nodes = bankslistextint;
                            nodesList.success = true;

                            res.SynapseNodesList = nodesList;
                            Logger.Info("Service Cntrlr -> SynapseV3AddNodeBankLogin - No MFA - Successfully saved [" + numOfBanksSavedSuccessfully + "] banks in DB, " +
                                        "Returning [" + allNodesParsedResult.nodes.Length + "] Banks for: [" + MemberId + "]");


                            if (numOfBanksSavedSuccessfully > 0)
                            {
                                res.errorMsg = "OK";
                                res.Is_success = true;
                            }
                            else
                            {
                                var error = "Service Cntrlr -> SynapseV3AddNode - No banks were saved in DB - MemberID: [" + MemberId + "]";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                res.errorMsg = "No banks saved in DB";
                            }
                        }
                        else
                        {
                            var error = "Service Cntrlr -> SynapseV3 ADD NODE (No MFA) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]";
                            Logger.Info(error);
                            CommonHelper.notifyCliffAboutError(error);
                            res.errorMsg = "Error occured while parsing banks list.";
                        }

                        return res;

                        #endregion No MFA response returned
                    }
                    else
                    {
                        // Synapse response for 'success' was not true
                        var error = "Service Cntrlr -> SynapseV3AddNodeBankLogin ERROR - Synapse response for 'success' was not true - MemberID: [" + MemberId + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                        res.errorMsg = "Synapse response for success was not true";
                    }
                }
                catch (WebException we)
                {
                    #region Bank Login Catch

                    res.Is_success = false;
                    res.Is_MFA = false;

                    var errorCode = ((HttpWebResponse)we.Response).StatusCode;
                    var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                    JObject jsonFromSynapse = JObject.Parse(resp);

                    var error = "Service Cntrlr -> SynapseV3AddNodeBankLogin FAILED - MemberID: [" + MemberId +
                                "], Synapse Response JSON: [" + jsonFromSynapse.ToString() + "]";
                    CommonHelper.notifyCliffAboutError(error);

                    if (jsonFromSynapse["error"] != null)
                    {
                        res.errorMsg = jsonFromSynapse["error"]["en"].ToString();
                        Logger.Error("Service Cntrlr -> SynapseV3AddNodeBankLogin FAILED - Synapse Error Msg: [" + res.errorMsg + "]");
                    }
                    else
                    {
                        Logger.Error(error);
                        res.errorMsg = we.Message;
                    }

                    return res;

                    #endregion Bank Login Catch
                }

                #endregion Call SynapseV3 Add Node API
            }
            catch (Exception ex)
            {
                var error = "Service Cntrlr -> SynapseV3AddNodeBankLogin FAILED - OUTER EXCEPTION - MemberID: [" + MemberId + "], Exception: [" + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                res.errorMsg = "Service Controller Outer Exception: [" + ex.Message + "]";
            }

            return res;
        }


        [HttpPost]
        [ActionName("SynapseV3MFABankVerify")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3MFABankVerify(SynapseV3VerifyNode_ServiceInput input)
        {
            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();
            res.Is_success = false;

            try
            {
                MembersDataAccess mda = new MembersDataAccess();

                SynapseBankLoginV3_Response_Int mdaResult = new SynapseBankLoginV3_Response_Int();
                mdaResult = mda.SynapseV3MFABankVerify(input.MemberId, input.BankName, input.mfaResponse, input.bankId);

                res.Is_success = mdaResult.Is_success;
                res.Is_MFA = mdaResult.Is_MFA;
                res.errorMsg = mdaResult.errorMsg;

                #region Bank List Returned

                if (mdaResult.SynapseNodesList != null && mdaResult.SynapseNodesList.nodes.Length > 0 && !mdaResult.Is_MFA)
                {
                    SynapseNodesListClass nodesList = new SynapseNodesListClass();
                    List<SynapseIndividualNodeClass> bankslistextint = new List<SynapseIndividualNodeClass>();

                    foreach (nodes bank in mdaResult.SynapseNodesList.nodes)
                    {
                        SynapseIndividualNodeClass b = new SynapseIndividualNodeClass();
                        b.account_class = bank.info._class;
                        b.bank_name = bank.info.bank_name;
                        b.oid = bank._id.oid;
                        b.is_active = bank.is_active;
                        b.name_on_account = bank.info.name_on_account;
                        b.nickname = bank.info.nickname;
                        b.account_num = (bank.info.account_num);

                        bankslistextint.Add(b);
                    }
                    nodesList.nodes = bankslistextint;
                    nodesList.success = mdaResult.Is_success;

                    res.SynapseNodesList = nodesList;
                }

                #endregion Bank List Returned

                #region MFA Required

                else if (mdaResult.Is_MFA == true)
                {
                    // Most likely the user submitted an incorrect answer, so they can try again...
                    // (Don't need to pass back the Bank ID or question since the HTML page still has it.)
                    // Also possible: another MFA Question from Synapse just like the Bank Login service (/node/add)
                    if (mdaResult.SynapseNodesList != null && mdaResult.SynapseNodesList.nodes.Length > 0)
                        res.bankOid = mdaResult.SynapseNodesList.nodes[0]._id.oid;

                    if (!String.IsNullOrEmpty(mdaResult.mfaMessage))
                        res.mfaQuestion = mdaResult.mfaMessage;
                }

                #endregion MFA Required
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - SynapseV3MFABankVerify FAILED - Exception: [" + ex.Message + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("SynapseV3MFABankVerifyWithMicroDeposits")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3MFABankVerifyWithMicroDeposits(SynapseV3VerifyNodeWithMicroDeposits_ServiceInput input)
        {
            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();
            res.Is_success = false;

            try
            {
                Logger.Info("Service Cntrlr - SynapseV3MFABankVerifyWithMicroDeposits Fired - MemberID: [" + input.MemberId + "]");

                var OauthObj = CommonHelper.GetSynapseCreateaUserDetails(input.MemberId);
                synapseV3checkUsersOauthKey checkTokenResult = CommonHelper.refreshSynapseV3OauthKey(OauthObj.access_token);

                if (checkTokenResult.success == true)
                {
                    MembersDataAccess mda = new MembersDataAccess();

                    SynapseBankLoginV3_Response_Int mdaResult = new SynapseBankLoginV3_Response_Int();
                    mdaResult = mda.SynapseV3MFABankVerifyWithMicroDeposits(input.MemberId, input.microDespositOne, input.microDespositTwo, input.bankId);

                    res.Is_success = mdaResult.Is_success;
                    res.Is_MFA = mdaResult.Is_MFA;
                    res.errorMsg = mdaResult.errorMsg;
                    res.mfaMessage = mdaResult.mfaMessage;
                }
                else
                {
                    Logger.Error("Service Cntrlr - SynapseV3MFABankVerifyWithMicroDeposits FAIELD on checking user's Synapse oauth token - " +
                                 "checktokenresult.msg: [" + checkTokenResult.msg + "], MemberID: [" + input.MemberId + "]");
                    res.errorMsg = checkTokenResult.msg;
                    return res;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - SynapseV3MFABankVerifyWithMicroDeposits FAILED - MemberID: [" + input.MemberId +
                             "], Exception: [" + ex + "]");
                res.errorMsg = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetSynapseBankAndUserDetails")]
        public SynapseDetailsClass GetSynapseBankAndUserDetails(string memberid)
        {
            SynapseDetailsClass res = new SynapseDetailsClass();

            try
            {
                Logger.Info("Service Cntrlr - GetSynapseBankAndUserDetails Fired - MemberID: [" + memberid + "]");
                res = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(memberid);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - GetSynapseBankAndUserDetails FAILED - MemberID: [" + memberid +
                             "], Exception: [" + ex + "]");
            }

            Logger.Info("Service Cntrlr - GetSynapseBankAndUserDetails - RETURNING - MemberID: [" + memberid +
                        "], User Details Found: [" + res.wereUserDetailsFound +
                        "], Bank Details Found: [" + res.wereBankDetailsFound + "]");
            //"], UserDetails.access_token: [" + res.UserDetails.access_token +
            //"], BankDetails.bankid: [" + res.BankDetails.bankid + "]");

            return res;
        }


        [HttpGet]
        [ActionName("GetUsersBankInfoForMobile")]
        public BankDetailsForMobile GetUsersBankInfoForMobile(string memberid)
        {
            BankDetailsForMobile res = new BankDetailsForMobile();

            try
            {
                //Logger.Info("Service Cntrlr - GetUsersBankInfoForMobile Fired - MemberID: [" + memberid + "]");
                res = CommonHelper.GetSynapseBankDetailsForMobile(memberid);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntrlr - GetUsersBankInfoForMobile FAILED - MemberID: [" + memberid +
                             "], Exception: [" + ex + "]");
            }

            //Logger.Info("Service Cntrlr - GetUsersBankInfoForMobile - RETURNING - MemberID: [" + memberid +
            //            "], User Details Found: [" + res.wereUserDetailsFound +
            //            "], Bank Details Found: [" + res.wereBankDetailsFound + "]");

            return res;
        }


        [HttpGet]
        [ActionName("CheckSynapseBankDetails")]
        public CheckSynapseBankDetails CheckSynapseBankDetails(string BankName)
        {
            string bname = BankName.Trim();

            CheckSynapseBankDetails res = new CheckSynapseBankDetails();
            var bdetails = _dbContext.SynapseSupportedBanks.Where(memberTemp =>
                                       memberTemp.BankName.Equals(bname) &&
                                       memberTemp.IsDeleted == false).FirstOrDefault();

            if (bdetails != null)
            {
                _dbContext.Entry(bdetails).Reload();

                res.IsBankFound = true;
                res.Message = "OK";
                res.IsPinRequired = Convert.ToBoolean(bdetails.IsPinRequired);
                res.mfa_type = bdetails.MFAType;
            }
            else
            {
                res.IsBankFound = false;
                res.Message = "Bank not found";
            }

            Logger.Info("Service Controller -> CheckSynapseBankDetails End - BankName: [" + BankName + "], Message to return: [" + res.Message + "]");

            return res;
        }


        [HttpGet]
        [ActionName("SetSynapseDefaultBank")]
        public SynapseBankSetDefaultResult SetSynapseDefaultBank(string MemberId, string BankName, string BankId)
        {
            SynapseBankSetDefaultResult res = CommonHelper.SetSynapseDefaultBank(MemberId, BankName, BankId);
            return res;
        }


        [HttpGet]
        [ActionName("RemoveSynapseV3BankAccount")]
        public synapseV3GenericResponse RemoveSynapseV3BankAccount(string memberId, string accessToken)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();
            res.isSuccess = false;

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - RemoveSynapseV3BankAccount - MemberId: [" + memberId + "]");

                    var memdataAccess = new MembersDataAccess();

                    var mdaResult = memdataAccess.RemoveSynapseV3BankAccount(memberId);

                    res.isSuccess = mdaResult.success;
                    res.msg = mdaResult.message;

                    return res;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - RemoveSynapseV3BankAccount FAILED - MemberId: [" + memberId + "]. Exception: [" + ex + "]");

                    res.msg = "Service layer catch exception";

                    Utility.ThrowFaultException(ex);
                }
            }
            else
                res.msg = "Invalid OAuth 2 Access";

            return res;
        }


        [HttpGet]
        [ActionName("GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit")]
        public TransactionDto GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit(string TransactionId, string MemberId, string TransactionType, string recipMemId)
        {
            try
            {
                Logger.Info("Service Cntlr - GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit Fired - TransType: [" + TransactionType +
                            "], TransID: [" + TransactionId + "],  MemberID: [" + MemberId + "], RecipientMemberID: [" + recipMemId + "]");

                var tda = new TransactionsDataAccess();
                Transaction tr = tda.GetTransactionById(TransactionId);

                if (tr != null)
                {
                    TransactionDto trans = new TransactionDto();
                    trans.AdminNotes = tr.AdminName;
                    trans.IsPrePaidTransaction = tr.IsPrepaidTransaction;
                    trans.FirstName = tr.Member.FirstName;
                    trans.LastName = tr.Member.LastName;
                    trans.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.FirstName)) + " " +
                                 CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.LastName));
                    trans.RecepientName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.FirstName)) + " " +
                                          CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.LastName));
                    trans.SenderPhoto = tr.Member.Photo ?? "";
                    trans.RecepientPhoto = tr.Member1.Photo ?? "";
                    trans.MemberId = tr.Member.MemberId.ToString();
                    trans.RecepientId = tr.Member1.MemberId.ToString();
                    trans.TransactionId = tr.TransactionId.ToString();
                    trans.TransactionStatus = tr.TransactionStatus;
                    trans.TransactionType = tr.TransactionType;
                    trans.TransDate = tr.TransactionDate.Value;
                    trans.TransactionFee = tr.TransactionFee.Value;
                    trans.InvitationSentTo = (tr.InvitationSentTo != null) ? CommonHelper.GetDecryptedData(tr.InvitationSentTo) : "";
                    trans.Amount = tr.Amount;

                    if (String.IsNullOrEmpty(recipMemId)) recipMemId = "";

                    MembersDataAccess mda = new MembersDataAccess();
                    string mdaRes = mda.GetTokensAndTransferMoneyToNewUser(TransactionId, MemberId, TransactionType, recipMemId);

                    trans.synapseTransResult = mdaRes;

                    return trans;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit FAILED - Exception: [" + ex + "]");
            }

            return null;
        }


        [HttpGet]
        [ActionName("getIdVerificationQuestionsV3")]
        public synapseIdVerificationQuestionsForDisplay getIdVerificationQuestionsV3(string memberid)
        {
            Logger.Info("Service Cntlr -> getIdVerificationQuestionsV3 Fired - MemberId: [" + memberid + "]");
            synapseIdVerificationQuestionsForDisplay res = new synapseIdVerificationQuestionsForDisplay();
            res.memberId = memberid;

            try
            {
                var mda = new MembersDataAccess();
                res = mda.getIdVerificationQuestionsV3(memberid);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> getVerificationQuestionsV2 FAILED - Exception: [" + ex.InnerException + "]");

                res.success = false;
                res.msg = "Service layer exception :-(";
            }

            return res;
        }


        [HttpGet]
        [ActionName("submitIdVerificationAswersV3")]
        public synapseV3GenericResponse submitIdVerificationAswersV3(string MemberId, string questionSetId, string quest1id, string quest2id, string quest3id, string quest4id, string quest5id, string answer1id, string answer2id, string answer3id, string answer4id, string answer5id)
        {
            Logger.Info("Service Cntlr -> submitIdVerificationAswersV2 Fired - MemberID: [" + MemberId + "]");

            synapseV3GenericResponse res = new synapseV3GenericResponse();
            res.isSuccess = false;

            try
            {
                var mda = new MembersDataAccess();
                submitIdVerificationInt mdaResult = mda.submitIdVerificationAnswersToSynapseV3(MemberId, questionSetId, quest1id, quest2id, quest3id, quest4id, quest5id, answer1id, answer2id, answer3id, answer4id, answer5id);

                res.isSuccess = mdaResult.success;
                res.msg = mdaResult.message;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> submitIdVerificationAswersV2 FAILED - Exception: [" + ex + "]");

                res.msg = "Exception in service layer";

                throw new Exception("Server Error.");
            }

            return res;
        }


        [HttpPost]
        [ActionName("RemoveSynapseBankAccount")]
        public StringResult RemoveSynapseBankAccount(RemoveBankAccountInputEntity user)
        {
            if (CommonHelper.IsValidRequest(user.AccessToken, user.MemberID))
            {
                try
                {
                    Logger.Info("Service Cntlr - RemoveSynapseBankAccount - MemberID: [" + user.MemberID + "], Bank ID: [" + user.BankAccountId + "]");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.RemoveSynapseBankAccount(user.MemberID, user.BankAccountId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr Error- RemoveSynapseBankAccount - MemberID: [" + user.MemberID + "], Error: [" + ex + " ].");
                    throw new Exception("Server Error.");
                }
            }
            else
                return new StringResult() { Result = "Invalid Access Token." };
        }


        [HttpPost]
        [ActionName("TransferMoneyUsingSynapse")]
        public StringResult TransferMoneyUsingSynapse(TransactionDto transInput, string accessToken)
        {
            Logger.Info("Service Cntlr -> TransferMoneyUsingSynapse Fired - MemberID: [" + transInput.MemberId +
                        "], RecipientID: [" + transInput.RecepientId +
                        "], Amount: [" + transInput.Amount.ToString("n2") +
                        "], doNotSendEmails: [" + transInput.doNotSendEmails + "]");

            StringResult res = new StringResult();

            if (transInput.isForHabitat == true ||
                transInput.isRentAutoPayment == true ||
                transInput.doNotSendEmails == false || // Proxy to tell if it's an admin sending a test transaction (only time doNotSendEmails would be 'false')
                CommonHelper.IsValidRequest(accessToken, transInput.MemberId))
            {
                var trnsactionId = string.Empty;

                try
                {
                    TransactionEntity transactionEntity = GetTransactionEntity(transInput);

                    var tda = new TransactionsDataAccess();

                    res.Result = tda.TransferMoneyUsingSynapse(transactionEntity);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> TransferMoneyUsingSynapse FAILED - MemberID: [" + transInput.MemberId + "], Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else
            {
                Logger.Error("Service Cntlr -> TransferMoneyUsingSynapse FAILED - AccessToken invalid or not found - " +
                             "MemberID: [" + transInput.MemberId + "]");
                res.Result = "Invalid OAuth 2 Access";
            }

            return res;
        }


        [HttpPost]
        [ActionName("TransferMoneyToNonNoochUserUsingSynapse")]
        public StringResult TransferMoneyToNonNoochUserUsingSynapse(TransactionDto transactionInput, string accessToken, string inviteType, string receiverEmailId)
        {
            if (CommonHelper.IsValidRequest(accessToken, transactionInput.MemberId))
            {
                StringResult res = new StringResult();
                string trnsactionId = string.Empty;

                try
                {
                    var transactionDataAccess = new TransactionsDataAccess();
                    TransactionEntity transactionEntity = GetTransactionEntity(transactionInput);

                    res.Result = transactionDataAccess.TransferMoneyToNonNoochUserUsingSynapse(inviteType, receiverEmailId, transactionEntity, transactionInput.cip);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> TransferMoneyToNonNoochUserUsingSynapse FAILED - Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }

                return res;
            }
            else
            {
                Logger.Error("Service Cntlr -> TransferMoneyToNonNoochUserUsingSynapse FAILED - AccessToken Not Found or Invalid - " +
                             "MemberID: [" + transactionInput.MemberId + "], Receiver Email: [" + receiverEmailId + "]");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("TransferMoneyToNonNoochUserThroughPhoneUsingsynapse")]
        public StringResult TransferMoneyToNonNoochUserThroughPhoneUsingsynapse(TransactionDto transactionInput, string accessToken, string inviteType, string receiverPhoneNumer)
        {
            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accessToken, transactionInput.MemberId))
            {
                string trnsactionId = string.Empty;
                try
                {
                    Logger.Info("Service Cntlr - TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - Sender: [" + transactionInput.MemberId + "], TransID: [" + trnsactionId + "], InviteType: [" + inviteType + "]");

                    var tda = new TransactionsDataAccess();
                    TransactionEntity transactionEntity = GetTransactionEntity(transactionInput);

                    res.Result = tda.TransferMoneyToNonNoochUserThroughPhoneUsingsynapse(inviteType, receiverPhoneNumer, transactionEntity);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else
            {
                Logger.Error("Service Cntlr -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - AccessToken not found or not valid.");
                res.Result = "Invalid OAuth 2 Access";
            }

            return res;
        }


        [HttpPost]
        [ActionName("addRowToSynapseCreateUsersTable")]
        public genericResponse addRowToSynapseCreateUsersTable(addSynapseCreateUserRecord input)
        {
            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial";

            try
            {
                Logger.Info("Service Cntlr -> addRowToSynapseCreateUsersTable Fired - MemberId: [" + input.memberId + "], New OAuth_Key: [" + input.access_token + "]");

                using (var noochConnection = new NOOCHEntities())
                {
                    var id = Utility.ConvertToGuid(input.memberId);

                    var member = noochConnection.Members.FirstOrDefault(m => m.MemberId == id);

                    if (member != null)
                    {
                        try
                        {
                            #region Delete Any Old DB Records & Create New Record

                            // Marking any existing Synapse 'Create User' results for this user as Deleted


                            var synapseRes = noochConnection.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == id && m.IsDeleted == false);

                            if (synapseRes != null)
                            {
                                Logger.Info("Service Cntlr -> addRowToSynapseCreateUsersTable - Old record found, about to delete - MemberID: [" +
                                            input.memberId + "], Old Oauth_Key: [" + synapseRes.access_token + "]");

                                synapseRes.IsDeleted = true;
                                synapseRes.ModifiedOn = DateTime.Now;
                                noochConnection.SaveChanges();
                            }

                            try
                            {
                                // Now make a new entry in SynapseCreateUserResults.dbo
                                SynapseCreateUserResult newSynapseUser = new SynapseCreateUserResult();
                                newSynapseUser.MemberId = id;
                                newSynapseUser.DateCreated = DateTime.Now;
                                newSynapseUser.IsDeleted = false;
                                newSynapseUser.access_token = CommonHelper.GetEncryptedData(input.access_token);
                                newSynapseUser.success = true;
                                newSynapseUser.expires_in = input.expires_in;
                                newSynapseUser.reason = "Manually added by Nooch admin";
                                newSynapseUser.refresh_token = CommonHelper.GetEncryptedData(input.refresh_token);
                                newSynapseUser.username = synapseRes.username != null ? synapseRes.username : null;
                                newSynapseUser.user_id = synapseRes.user_id != null ? synapseRes.user_id : null;
                                newSynapseUser.IsForNonNoochUser = false;
                                noochConnection.SynapseCreateUserResults.Add(newSynapseUser);
                                int addRecordToSynapseCreateUserTable = noochConnection.SaveChanges();

                                if (addRecordToSynapseCreateUserTable > 0)
                                {
                                    Logger.Info("Service Cntlr -> addRowToSynapseCreateUsersTable - New Record Added Successfully - MemberID: [" + input.memberId + "], New Oauth_Key: [" + input.access_token + "]");

                                    res.success = true;
                                    res.msg = "New record added to SynapseCreateUserResults successfully.";
                                }
                                else
                                {
                                    Logger.Error("Service Cntlr -> addRowToSynapseCreateUsersTable FAILED - Error Adding New Record To Database - MemberID: [" + input.memberId + "], New Oauth_Key: [" + input.access_token + "]");
                                    res.msg = "Failed to save new record in DB.";
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Service Cntlr -> addRowToSynapseCreateUsersTable FAILED - Exception on Adding New Record To Database - MemberId: [" +
                                             input.memberId + "], Exception: [" + ex + "]");

                                res.msg = "Service layer exception - inner 1.";
                            }

                            #endregion Delete Any Old DB Records & Create New Record
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Service Cntlr -> addRowToSynapseCreateUsersTable FAILED - MemberID: [" +
                                         input.memberId + "], [Exception: " + ex.Message + "]");

                            res.msg = ex.InnerException.Message;
                        }
                    }
                    else
                        res.msg = "Member not found.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> addRowToSynapseCreateUsersTable FAILED (Outer Exception) - MemberID: [" + input.memberId + "], Exception: [" + ex + "]");
                res.msg = "Service layer outer exception - Msg: [" + ex.Message.ToString() + "]";
            }

            return res;
        }


        /// <summary>
        /// For cancelling a transaction with Synapse V3.0
        /// DON'T THINK THIS IS USED ANYWHERE (CLIFF: 9/22/16)
        /// </summary>
        /// <param name="TransationId">TransID of the payment to be cancelled.</param>
        /// <param name="MemberId">MemberID of the SENDER.</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("CancelTransactionAtSynapse")]
        public CancelTransactionAtSynapseResult CancelTransactionAtSynapse(string TransationId, string MemberId)
        {
            CancelTransactionAtSynapseResult CancelTransaction = new CancelTransactionAtSynapseResult();
            CancelTransaction.IsSuccess = false;

            try
            {
                if (String.IsNullOrEmpty(TransationId))
                {
                    Logger.Error("Service Cntlr -> CancelTransactionAtSynapse CodeBehind - TransID: [" + TransationId + "]");
                    CancelTransaction.errorMsg = "Missing TransationId";
                }

                if (string.IsNullOrEmpty(MemberId))
                {
                    Logger.Error("Service Cntlr -> CancelTransactionAtSynapse CodeBehind - MemberID: [" + MemberId + "]");
                    CancelTransaction.errorMsg = "Missing Id";
                }

                if (String.IsNullOrEmpty(CancelTransaction.errorMsg))
                    CancelTransaction = CommonHelper.CancelTransactionAtSynapse(TransationId, MemberId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> CancelTransactionAtSynapse FAILED - TransID: [" + TransationId + "], Exception: [" + ex + "]");
                CancelTransaction.errorMsg = "Server Error.";
            }

            return CancelTransaction;
        }

        // Web-Related Synapse Services

        [HttpGet]
        [ActionName("Submit2FAPin")]
        public synapseV3GenericResponse Submit2FAPin(string memberId, string pin)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();
            res.isSuccess = false;

            try
            {
                Logger.Info("Service Cntrlr -> Submit2FAPin Fired - MemberID: [" + memberId + "], PIN: [" + pin + "]");

                #region Initial Data Checks

                if (String.IsNullOrEmpty(memberId))
                {
                    res.msg = "Missing MemberID!";
                    return res;
                }
                else if (String.IsNullOrEmpty(pin.Trim()))
                {
                    res.msg = "Missing PIN to submit!";
                    return res;
                }
                memberId = memberId.Trim();
                pin = pin.Trim();

                #endregion Initial Data Checks

                Member memberObj = CommonHelper.GetMemberDetails(memberId);

                if (memberObj != null)
                {
                    // Now get the user's Oauth Key
                    var synapseCreateUserObj = _dbContext.SynapseCreateUserResults.FirstOrDefault(m =>
                                                                  m.MemberId == memberObj.MemberId && m.IsDeleted == false);

                    if (synapseCreateUserObj != null)
                    {
                        _dbContext.Entry(synapseCreateUserObj).Reload();

                        var oauthKey = synapseCreateUserObj.access_token;

                        // Now we have everything to send to SynapseV3SignIn in CommonHelper...
                        synapseV3checkUsersOauthKey signInResult = CommonHelper.SynapseV3SignIn(oauthKey, memberObj, pin);

                        if (signInResult.success && !signInResult.is2FA && signInResult.msg == "Oauth key refreshed successfully")
                        {
                            res.isSuccess = true;
                            res.msg = "PIN validated successfully";
                        }
                    }
                    else
                        res.msg = "Users Synapse record not found in DB";
                }
                else
                    res.msg = "Member not found in DB";
            }
            catch (Exception ex)
            {
                Logger.Info("Service Cntlr -> Submit2FAPin FAILED - Exception - MemberID: [" + memberId +
                            "], PIN: [" + pin + "], Exception: [" + ex.Message + "]");
                res.msg = ex.Message;
            }

            return res;
        }


        /// <summary>
        /// For updating a user's Synapse Bank status to 'Verified'. Currently called from a Member Details
        /// page in the Admin Dashboard and from the BankVerification.aspx.cs browser page.
        /// </summary>
        /// <param name="tokenId"></param>
        [HttpGet]
        [ActionName("VerifySynapseAccount")]
        public BoolResult VerifySynapseAccount(string tokenId)
        {
            Logger.Info("Service Cntlr -> VerifySynapseAccount Fired - Bank TokenID: [" + tokenId + "]");

            if (!String.IsNullOrEmpty(tokenId))
            {
                try
                {
                    var mda = new MembersDataAccess();
                    return new BoolResult { Result = mda.VerifySynapseAccount(tokenId) };
                }
                catch (Exception ex)
                {
                    Logger.Info("Service Controller -> VerifySynapseAccount FAILED. [Exception: " + ex + "]");
                    //UtilityService.ThrowFaultException(ex);
                }
            }
            else
            {
                Logger.Error("Service Cntlr -> VerifySynapseAccount FAILED - TokenID was null or empty! - TokenID: [" + tokenId + "]");
            }

            return new BoolResult();
        }


        [HttpGet]
        [ActionName("GetSynapseBankAccountDetails")]
        public SynapseAccoutDetailsInput GetSynapseBankAccountDetails(string memberId, string accessToken)
        {
            // Logger.LogDebugMessage("Service Cntlr -> GetSynapseBankAccountDetails Initiated - memberId: [" + memberId + "]");

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    var accountCollection = CommonHelper.GetSynapseBankDetails(memberId);

                    if (accountCollection != null)
                    {
                        string appPath = Utility.GetValueFromConfig("ApplicationURL");

                        SynapseAccoutDetailsInput o = new SynapseAccoutDetailsInput();

                        o.BankName = CommonHelper.GetDecryptedData(accountCollection.bank_name);
                        o.BankNickName = CommonHelper.GetDecryptedData(accountCollection.nickname);
                        o.BankImageURL = CommonHelper.getLogoForBank(o.BankName);
                        o.AccountName = CommonHelper.GetDecryptedData(accountCollection.account_number_string);
                        o.AccountStatus = accountCollection.Status;
                        o.MemberId = memberId;

                        return o;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> GetSynapseBankAccountDetails FAILED - MemberId: [" + memberId + "]. Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }

                return new SynapseAccoutDetailsInput();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        #endregion Synapse-Related Services



        [HttpGet]
        [ActionName("SaveMembersFBId")]
        public StringResult SaveMembersFBId(string memberId, string fbid, bool IsConnect)
        {
            try
            {
                //Logger.LogDebugMessage("Service Cntlr -> SaveMembersFBId - MemberID: [" + MemberId + "]");
                return new StringResult { Result = CommonHelper.SaveMemberFBId(memberId, fbid, IsConnect) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> SaveMembersFBId Error - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                return new StringResult { Result = ex.Message };
            }
        }


        [HttpGet]
        [ActionName("ValidatePinNumber")]
        public StringResult ValidatePinNumber(string memberId, string pinNo, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - ValidatePinNumber MemberID: [" + memberId + "]");

                    return new StringResult { Result = CommonHelper.ValidatePinNumber(memberId, pinNo.Replace(" ", "+")) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - ValidatePinNumber FAILED MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }

                return new StringResult();
            }
            else
                throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("ValidatePinNumberForPasswordForgotPage")]
        public StringResult ValidatePinNumberForPasswordForgotPage(string memberId, string pinNo)
        {
            try
            {
                Logger.Info("Service Cntlr - ValidatePinNumberForPasswordForgotPage Fired - MemberID: [" + memberId + "]");
                var mda = new MembersDataAccess();
                return new StringResult { Result = CommonHelper.ValidatePinNumber(memberId, pinNo.Replace(" ", "+")) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr - ValidatePinNumberForPasswordForgotPage FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");

            }
            return new StringResult();
        }


        [HttpGet]
        [ActionName("ValidatePinNumberToEnterForEnterForeground")]
        public StringResult ValidatePinNumberToEnterForEnterForeground(string memberId, string pinNo, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr - ValidatePinNumberToEnterForEnterForeground - MemberID: [" + memberId + "]");

                    return new StringResult { Result = CommonHelper.ValidatePinNumberToEnterForEnterForeground(memberId, pinNo.Replace(" ", "+")) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr - ValidatePinNumberToEnterForEnterForeground FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }

                return new StringResult();
            }
            else throw new Exception("Invalid OAuth 2 Access");
        }


        [HttpGet]
        [ActionName("ResetPin")]
        public StringResult ResetPin(string memberId, string oldPin, string newPin, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> ResetPin - MemberID: [" + memberId + "]");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.ResetPin(memberId, oldPin, newPin) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> ResetPin FAILED - MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                    return new StringResult() { Result = "Server Error." };
                }
            }
            else
                return new StringResult() { Result = "Invalid OAuth 2 Access" };
        }


        [HttpPost]
        [ActionName("MemberRegistration")]
        public StringResult MemberRegistration(MemberRegistrationInputDto MemberDetails)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> MemberRegistration Fired - NEW USER'S INFO: Name: [" + MemberDetails.UserName +
                            "], Email: [" + MemberDetails.UserName + "], Type: [" + MemberDetails.type +
                            "], Invite Code: [" + MemberDetails.inviteCode + "], SendEmail: [" + MemberDetails.sendEmail + "]");

                if (!String.IsNullOrEmpty(MemberDetails.Photo))
                    MemberDetails.Picture = System.Convert.FromBase64String(MemberDetails.Photo);

                var mda = new MembersDataAccess();

                var type = String.IsNullOrEmpty(MemberDetails.type) ? "Personal" : MemberDetails.type;

                res.Result = mda.MemberRegistration(MemberDetails.Picture, MemberDetails.UserName, MemberDetails.FirstName.ToLower(),
                                                    MemberDetails.LastName.ToLower(), MemberDetails.PinNumber, MemberDetails.Password,
                                                    MemberDetails.SecondaryMail, MemberDetails.UdId, MemberDetails.friendRequestId,
                                                    MemberDetails.invitedFriendFacebookId, MemberDetails.facebookAccountLogin,
                                                    MemberDetails.inviteCode, MemberDetails.sendEmail, type, null);
            }
            catch (Exception ex)
            {
                var error = "Service Cntlr -> MemberRegistration FAILED: Name: [" + MemberDetails.UserName + "], Exception: [" + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("MemberRegistrationGet")]
        public genericResponse MemberRegistrationGet(string name, string dob, string ssn, string address, string zip,
             string email, string phone, string fngprnt, string ip, string type, string pw)
        {
            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial - Nooch Service";

            try
            {
                Logger.Info("Service Cntlr -> MemberRegistrationGET Fired - NEW USER'S INFO: Name: [" + name +
                            "], Email: [" + email + "],  Type: [" + type +
                            "], Phone: [" + phone + "], Address: [" + address +
                            "], ZIP: [" + zip + "], DOB: [" + dob +
                            "], SSN: [" + ssn + "], IP: [" + ip +
                            "], Fngrprnt: [" + fngprnt + "], PW: [" + pw + "], ");

                #region Parse Name

                var FirstName = string.Empty;
                var LastName = string.Empty;

                if (!String.IsNullOrEmpty(name))
                {
                    string[] namearray = name.Split(' ');
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

                type = String.IsNullOrEmpty(type) ? "Personal - Browser" : CommonHelper.UppercaseFirst(type.ToLower());

                var password = !String.IsNullOrEmpty(pw) ? CommonHelper.GetEncryptedData(pw)
                                                         : CommonHelper.GetEncryptedData("jibb3r;jawn-alt");


                var mda = new MembersDataAccess();

                var mdaRes = mda.MemberRegistration(null, email, FirstName, LastName, "", password, email,
                                                    fngprnt, "", "", "", "BROWSER", "true", type, phone);

                res.msg = mdaRes;

                if (mdaRes.IndexOf("Thanks for registering") > -1)
                {
                    var memId = CommonHelper.GetMemberIdByUserName(email);

                    #region Set IP Address

                    try
                    {
                        if (!String.IsNullOrEmpty(ip) && ip.Length > 4)
                            CommonHelper.UpdateMemberIPAddressAndDeviceId(memId, ip, fngprnt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Service Cntlr -> MemberRegistrationGET FAILED - MemberID: [" + memId +
                                     "], Exception: [" + ex + "]");
                    }

                    #endregion Set IP Address

                    res.note = memId;
                    res.success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> MemberRegistrationGET FAILED - Name: [" + name + "], Email: [" + email + "], Exception: [" + ex + "]");
                res.msg = "MemberRegistrationGet Exception";
            }

            return res;
        }


        #region Login-Related Methods

        private string GenerateAccessToken()
        {
            byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            byte[] key = Guid.NewGuid().ToByteArray();
            string token = Convert.ToBase64String(time.Concat(key).ToArray());
            return CommonHelper.GetEncryptedData(token);
        }


        // added deviceOS parameter to know which operating system user currently using, this will help to send appropriate push message to device
        [HttpGet]
        [ActionName("LoginRequest")]
        public StringResult LoginRequest(string userName, string pwd, Boolean rememberMeEnabled, decimal lat,
                                         decimal lng, string udid, string devicetoken, string deviceOS)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> LoginRequest - userName: [" + userName + "], UDID: [" + udid +
                            "], DeviceOS: [" + deviceOS + "], DeviceToken: [" + devicetoken + "]");

                var mda = new MembersDataAccess();
                var loginAttemptRes = mda.LoginRequest(userName, pwd, rememberMeEnabled, lat, lng, udid, devicetoken, deviceOS);

                if (loginAttemptRes == "Success")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userName, state);
                    res.Result = state;
                }
                else if (string.IsNullOrEmpty(loginAttemptRes))
                {
                    loginAttemptRes = "Authentication failed.";
                    res.Result = "Invalid Login or Password";
                }
                else if (loginAttemptRes == "Registered")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userName, state);
                    res.Result = state;
                }
                else if (loginAttemptRes == "Temporarily_Blocked")
                    res.Result = "Temporarily_Blocked";
                else if (loginAttemptRes == "Suspended")
                    res.Result = "Suspended";
                else if (loginAttemptRes == "Invalid user id or password.")
                    res.Result = "Invalid user id or password.";
                else if (loginAttemptRes == "The password you have entered is incorrect.")
                    res.Result = "The password you have entered is incorrect.";
                else
                    res.Result = loginAttemptRes;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> LoginRequest FAILED - userName: [" + userName + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        /// <summary>
        /// Login via Facebook service for regular Nooch users.
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="FBId"></param>
        /// <param name="rememberMeEnabled"></param>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <param name="udid"></param>
        /// <param name="accesstoken"></param>
        [HttpGet]
        [ActionName("LoginWithFacebook")]
        public StringResult LoginWithFacebook(string userEmail, string FBId, Boolean rememberMeEnabled, decimal lat,
            decimal lng, string udid, string devicetoken)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> LoginWithFacebook - userEmail: [" + userEmail + "], FB ID: [" + FBId + "]");

                var mda = new MembersDataAccess();
                string cookie = mda.LoginwithFB(userEmail, FBId, rememberMeEnabled, lat, lng, udid, devicetoken);

                if (String.IsNullOrEmpty(cookie))
                {
                    cookie = "Authentication failed.";
                    res.Result = "Invalid Login or Password";
                }
                else if (cookie == "Success")
                {
                    HttpCookie cUname = new HttpCookie("nooch_username", FBId.ToLowerInvariant());
                    HttpCookie cAuth = new HttpCookie("nooch_auth", cookie);
                    cUname.Secure = true;
                    cUname.HttpOnly = true;
                    cAuth.Secure = true;
                    cAuth.HttpOnly = true;
                    HttpContext.Current.Response.SetCookie(cUname);
                    HttpContext.Current.Response.SetCookie(cAuth);

                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userEmail, state);
                    res.Result = state;
                }
                else if (cookie == "Registered")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userEmail, state);
                    res.Result = state;
                }
                else if (cookie == "Temporarily_Blocked")
                    res.Result = "Temporarily_Blocked";
                else if (cookie == "FBID or EmailId not registered with Nooch")
                    res.Result = "FBID or EmailId not registered with Nooch";
                else if (cookie == "Suspended")
                    res.Result = "Suspended";
                else if (cookie == "Invalid user id or password.")
                    res.Result = "Invalid user id or password.";
                else if (cookie == "The password you have entered is incorrect.")
                    res.Result = "The password you have entered is incorrect.";
                else
                    res.Result = cookie;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> LoginWithFacebook FAILED - userEmail: [" + userEmail + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("LoginWithFacebookGeneric")]
        public StringResult LoginWithFacebookGeneric(string userEmail, string FBId, Boolean rememberMeEnabled, decimal lat,
            decimal lng, string udid, string devicetoken)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> LoginWithFacebook - userEmail: [" + userEmail + "], FB ID: [" + FBId + "]");

                var mda = new MembersDataAccess();
                string cookie = mda.LoginwithFBGeneric(userEmail, FBId, rememberMeEnabled, lat, lng, udid, devicetoken);

                if (String.IsNullOrEmpty(cookie))
                {
                    cookie = "Authentication failed.";
                    res.Result = "Invalid Login or Password";
                }
                else if (cookie == "Success")
                {
                    HttpCookie cUname = new HttpCookie("nooch_username", FBId.ToLowerInvariant());
                    HttpCookie cAuth = new HttpCookie("nooch_auth", cookie);
                    cUname.Secure = true;
                    cUname.HttpOnly = true;
                    cAuth.Secure = true;
                    cAuth.HttpOnly = true;
                    HttpContext.Current.Response.SetCookie(cUname);
                    HttpContext.Current.Response.SetCookie(cAuth);

                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userEmail, state);
                    res.Result = state;
                }
                else if (cookie == "Registered")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userEmail, state);
                    res.Result = state;
                }
                else if (cookie == "Temporarily_Blocked")
                    res.Result = "Temporarily_Blocked";
                else if (cookie == "FBID or EmailId not registered with Nooch")
                    res.Result = "FBID or EmailId not registered with Nooch";
                else if (cookie == "Suspended")
                    res.Result = "Suspended";
                else if (cookie == "Invalid user id or password.")
                    res.Result = "Invalid user id or password.";
                else if (cookie == "The password you have entered is incorrect.")
                    res.Result = "The password you have entered is incorrect.";
                else
                    res.Result = cookie;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> LoginWithFacebook FAILED - userEmail: [" + userEmail + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("LogOutRequest")]
        public StringResult LogOutRequest(string accessToken, string memberId)
        {
            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> LogOutRequest - MemberID: [" + memberId + "]");

                    var mda = new MembersDataAccess();
                    string cookie = mda.LogOut(memberId);

                    if (string.IsNullOrEmpty(cookie))
                        res.Result = "LogOut failed.";
                    else
                        res.Result = "Success.";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> LogOutRequest FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else res.Result = "Invalid OAuth 2 Access";

            return res;
        }


        #endregion Login-Related Methods


        [HttpGet]
        [ActionName("UpDateLatLongOfUser")]
        public StringResult UpDateLatLongOfUser(string memberId, string accesstoken, string Lat, string Long)
        {
            Logger.Info("Service Cntlr -> UpDateLatLongOfUser Fired - MemberID: [" + memberId + "]");

            StringResult res = new StringResult();

            //if (CommonHelper.IsValidRequest(accesstoken, memberId))
            //{
            if (!String.IsNullOrEmpty(memberId) && !String.IsNullOrEmpty(Lat) && !String.IsNullOrEmpty(Long))
            {
                try
                {
                    var mda = new MembersDataAccess();
                    var s = mda.UpdateUserLocation(memberId, Lat, Long);

                    if (s == "success")
                        res.Result = "success";
                    else
                        res.Result = "Member not found";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> UpDateLatLongOfUser FAILED - MemberId: [" + memberId + "], Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else
                res.Result = "Missing input data";
            //}
            //else
            //    res.Result = "Invalid OAuth 2 Access";

            return res;
        }


        [HttpGet]
        [ActionName("ResendVerificationLink")]
        public StringResult ResendVerificationLink(string UserName)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> ResendVerificationLink - UserName: [" + UserName + "]");

                var mda = new MembersDataAccess();
                res.Result = mda.ResendVerificationLink(UserName);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> ResendVerificationLink FAILED - UserName: [" + UserName + "], Exception: [" + ex.Message + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("ResendVerificationSMS")]
        public StringResult ResendVerificationSMS(string UserName)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> ResendVerificationSMS Fired - UserName: [" + UserName + "]");

                var mda = new MembersDataAccess();
                res.Result = mda.ResendVerificationSMS(UserName);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> ResendVerificationSMS FAILED - UserName: [" + UserName + "], Exception: [" + ex.Message + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("ResetPassword")]
        public BoolResult ResetPassword(string memberId, string newPassword, string newUser)
        {
            BoolResult res = new BoolResult();

            try
            {
                Logger.Info("Service Cntlr -> ResetPassword Fired - MemberID: [" + memberId + "]");
                var mda = new MembersDataAccess();
                res.Result = mda.ResetPassword(memberId, newPassword, newUser);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> ResetPassword FAILED - MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                res.Result = false;
            }

            return res;
        }


        [HttpGet]
        [ActionName("resetlinkvalidationcheck")]
        public BoolResult resetlinkvalidationcheck(string memberId)
        {
            BoolResult res = new BoolResult();

            try
            {
                Logger.Info("Service Cntlr -> resetlinkvalidationcheck Fired - MemberID: [" + memberId + "]");
                var mda = new MembersDataAccess();
                res.Result = mda.resetlinkvalidationcheck(memberId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> resetlinkvalidationcheck FAILED - MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                res.Result = false;
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetRecentMembers")]
        public Collection<MemberClass> GetRecentMembers(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    //Logger.Info("Service Cntlr -> GetRecentMembers Fired - MemberID: [" + memberId + "]");

                    var tda = new TransactionsDataAccess();
                    var recentTrans = tda.GetRecentMembers(memberId);

                    var recentMembersCollection = new Collection<MemberClass>();

                    if (recentTrans != null && recentTrans.Count > 0)
                    {
                        string adminUserName = Utility.GetValueFromConfig("adminMail");

                        int i = 0;

                        foreach (var trans in recentTrans)
                        {
                            string photo = trans.Member1.Photo != null
                                           ? string.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member1.Photo.Substring(trans.Member1.Photo.IndexOf("Photos") + 14))
                                           : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                            string photoRec = trans.Member.Photo != null
                                              ? string.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo.Substring(trans.Member.Photo.IndexOf("Photos") + 14))
                                              : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";

                            if (trans.Member.MemberId.ToString().Equals(memberId.ToLower())) // Sent Collection
                            {
                                if (trans.Member1.Status == "Active")
                                {
                                    var memberItem = new MemberClass
                                    {
                                        UserName = CommonHelper.GetDecryptedData(trans.Member1.UserName),
                                        FirstName = !String.IsNullOrEmpty(trans.Member1.FirstName)
                                                    ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName))
                                                    : "",
                                        LastName = !String.IsNullOrEmpty(trans.Member1.LastName)
                                                   ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName))
                                                   : "",
                                        MemberId = trans.Member1.MemberId.ToString(),
                                        NoochId = trans.Member1.Nooch_ID,
                                        Status = trans.Member1.Status,
                                        Photo = !String.IsNullOrEmpty(photo) ? photo : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png",
                                        TransferStatus = "Sent"
                                    };
                                    var userName = adminUserName != trans.Member1.UserName
                                                   ? CommonHelper.GetDecryptedData(trans.Member1.UserName)
                                                   : trans.Member1.UserName;

                                    if (recentMembersCollection.All(x => x.MemberId != trans.Member1.MemberId.ToString()) &&
                                        trans.Member1.MemberId.ToString() != memberId && !userName.Equals(adminUserName))
                                    {
                                        if (i == 20)
                                            break;
                                        i++;
                                        recentMembersCollection.Add(memberItem);
                                    }
                                }
                            }
                            else if (trans.Member1.MemberId.ToString().Equals(memberId.ToLower())) // Received Collection
                            {
                                if (trans.Member.Status == "Active")
                                {
                                    var memberItem = new MemberClass()
                                    {
                                        UserName = CommonHelper.GetDecryptedData(trans.Member.UserName),
                                        FirstName = !String.IsNullOrEmpty(trans.Member.FirstName)
                                                    ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName))
                                                    : "",
                                        LastName = !String.IsNullOrEmpty(trans.Member.LastName)
                                                   ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName))
                                                   : "",
                                        MemberId = trans.Member.MemberId.ToString(),
                                        NoochId = trans.Member.Nooch_ID,
                                        Status = trans.Member.Status,
                                        Photo = !String.IsNullOrEmpty(photoRec) ? photoRec : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png",
                                        TransferStatus = "Received"
                                    };
                                    var userName = (adminUserName != trans.Member.UserName)
                                                   ? CommonHelper.GetDecryptedData(trans.Member.UserName)
                                                   : trans.Member.UserName;

                                    if (!recentMembersCollection.Any(x => x.MemberId == trans.Member.MemberId.ToString()) &&
                                        trans.Member.MemberId.ToString() != memberId && !userName.Equals(adminUserName))
                                    {
                                        if (i == 20)
                                            break;
                                        i++;
                                        recentMembersCollection.Add(memberItem);
                                    }
                                }
                            }
                        }

                        Logger.Info("Service Cntlr -> GetRecentMembers - RecentMembersCollection COUNT: [" + recentMembersCollection.Count + "], MemberID: [" + memberId + "]");

                        return recentMembersCollection;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> GetRecentMembers FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                }
                return new Collection<MemberClass>();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("HandleRequestMoney")]
        public StringResult HandleRequestMoney(RequestDto handleRequestInput, string accessToken)
        {
            StringResult res = new StringResult();

            if (CommonHelper.IsValidRequest(accessToken, handleRequestInput.MemberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> HandleRequestMoney - MemberID: [" + handleRequestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    res.Result = tda.HandleRequestMoney(handleRequestInput);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> HandleRequestMoney FAILED - MemberID: [" + handleRequestInput.MemberId + "], Exception: [" + ex + "]");
                    res.Result = ex.Message;
                }
            }
            else
                res.Result = "Invalid OAuth 2 Access";

            return res;
        }



        /// <summary>
        /// Private method for setting up a Transaction Entity before executing a transfer.
        /// </summary>
        /// <param name="transactionInput"></param>
        /// <returns></returns>
        private static TransactionEntity GetTransactionEntity(TransactionDto transactionInput)
        {
            var transactionEntity = new TransactionEntity
            {
                Picture = transactionInput.Picture,
                PinNumber = transactionInput.PinNumber,
                MemberId = transactionInput.MemberId,
                RecipientId = transactionInput.RecepientId,
                Amount = transactionInput.Amount,
                Memo = transactionInput.Memo,
                IsPrePaidTransaction = transactionInput.IsPrePaidTransaction,
                DeviceId = transactionInput.DeviceId,
                BankId = transactionInput.BankId,
                BankAccountId = transactionInput.BankAccountId,
                TransactionType = transactionInput.TransactionType,
                TransactionDateTime = DateTime.Now.ToString(),
                doNotSendEmails = transactionInput.doNotSendEmails,
                TransactionId = Guid.NewGuid().ToString(),
                isForHabitat = transactionInput.isForHabitat,
                isRentAutoPayment = transactionInput.isRentAutoPayment == true
                                    ? true
                                    : false,

                Location = new LocationEntity
                {
                    Latitude = transactionInput.Latitude,
                    Longitude = transactionInput.Longitude,
                    AddressLine1 = transactionInput.AddressLine1,
                    AddressLine2 = transactionInput.AddressLine2,
                    City = transactionInput.City,
                    State = transactionInput.State,
                    Country = transactionInput.Country,
                    ZipCode = transactionInput.ZipCode
                }
            };

            return transactionEntity;
        }


        [HttpGet]
        [ActionName("CheckSDNListing")]
        public StringResult CheckSDNListing(string MemberId)
        {
            var noochMemberN = CommonHelper.GetMemberDetails(MemberId);
            if (noochMemberN != null)
            {
                var b = CommonHelper.IsListedInSDN(CommonHelper.GetDecryptedData(noochMemberN.LastName),
                    noochMemberN.MemberId);
                return new StringResult() { Result = b.ToString() };
            }
            return new StringResult();
        }


        [HttpGet]
        [ActionName("GetStateNameByZipcode")]
        public GoogleGeolocationOutput GetStateNameByZipcode(string zipCode)
        {
            GoogleGeolocationOutput res = new GoogleGeolocationOutput();
            res.IsSuccess = false;

            try
            {
                res = CommonHelper.GetCityAndStateFromZip(zipCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> GetStateNameByZipcode FAILED - ZipCode: [" + zipCode + "], Exception: [" + ex + "]");
                res.ErrorMessage = "Server Error.";
            }

            return res;
        }


        [HttpGet]
        [ActionName("SetAutoPayStatusForTenant")]
        public StringResult SetAutoPayStatusForTenant(bool statustoSet, string tenantId)
        {
            StringResult res = new StringResult();

            try
            {
                Logger.Info("Service Cntlr -> SetAutoPayStatusForTenant Fired - TenantID: [" + tenantId + "]");

                var mda = new MembersDataAccess();
                res.Result = mda.SetAutoPayStatusForTenant(statustoSet, tenantId);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Cntlr -> SetAutoPayStatusForTenant EXCEPTION - TenantID: [" + tenantId + "], Exception: [" + ex + "]");
                res.Result = ex.Message;
            }

            return res;
        }


        [HttpGet]
        [ActionName("sendLandlordLeadEmailTemplate")]
        public StringResult sendLandlordLeadEmailTemplate(string template, string email, string firstName,
            string tenantFName, string tenantLName, string propAddress, string subject)
        {
            Logger.Info("Service Cntlr -> sendEmailTemplate Fired - Template: [" + template +
                        "], Email: [" + email + "], First Name: [" + firstName + "], Subject: {" + subject + "]");

            StringResult res = new StringResult();

            if (String.IsNullOrEmpty(email))
                res.Result = "Missing email address to send to!";
            else if (String.IsNullOrEmpty(template))
                res.Result = "Have an email, but missing a Template to send!";
            else if (String.IsNullOrEmpty(tenantFName))
                res.Result = "Missing a Tenant First Name!";
            else if (String.IsNullOrEmpty(tenantLName))
                res.Result = "Missing a Tenant Last Name!";
            else if (String.IsNullOrEmpty(propAddress))
                res.Result = "Missing a Property Address";
            else
            {
                subject = String.IsNullOrEmpty(subject) || subject.Length < 1 ? " " : subject = CommonHelper.UppercaseFirst(subject);
                firstName = String.IsNullOrEmpty(firstName) || firstName.Length < 1 ? " " : firstName = CommonHelper.UppercaseFirst(firstName);

                try
                {
                    var tokens = new Dictionary<string, string>
                        {
                            {Constants.PLACEHOLDER_FIRST_NAME, firstName}, // Landlord's First Name
                            {"$TenantFName$", tenantFName},
                            {"$TenantLName$", tenantLName},
                            {"$PropAddress$", propAddress}
                        };

                    Utility.SendEmail(template, "landlords@nooch.com", email,
                                      null, subject, null, tokens, null, null, null);

                    res.Result = "Email Template [" + template + "] sent successfully to [" + email + "]";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> sendEmailTemplate FAILED - Exception: [" + ex.Message + "]");
                    res.Result = "Server exception!";
                }
            }

            return res;
        }



        // Method to test if given email or phone number is already registered

        [HttpPost]
        [ActionName("CheckMemberExistenceUsingEmailOrPhone")]
        public CheckMemberExistenceUsingEmailOrPhoneResultClass CheckMemberExistenceUsingEmailOrPhone(CheckMemberExistenceUsingEmailOrPhoneInputClass input)
        {
            if (CommonHelper.IsValidRequest(input.AccessToken, input.MemberId))
            {
                CheckMemberExistenceUsingEmailOrPhoneResultClass res = new CheckMemberExistenceUsingEmailOrPhoneResultClass();
                res.IsSuccess = false;
                res.IsMemberFound = false;

                try
                {
                    if (!String.IsNullOrEmpty(input.StringToCheck) && !String.IsNullOrEmpty(input.CheckType))
                    {
                        if (input.CheckType == "email")
                        {
                            var memberObj = CommonHelper.GetMemberDetailsByUserName(input.StringToCheck);

                            if (memberObj != null)
                            {
                                res.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName)) + " " +
                                           CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));
                                res.MemberId = memberObj.MemberId.ToString();
                                res.UserImage = memberObj.Photo ?? "././img/profile_picture.png";
                                res.IsMemberFound = true;
                                res.IsSuccess = true;
                                res.ErrorMessage = "OK";
                            }
                        }
                        else if (input.CheckType == "phone")
                        {
                            var memberObj = CommonHelper.GetMemberByPhone(input.StringToCheck);

                            if (memberObj != null)
                            {
                                res.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName)) + " " +
                                           CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));
                                res.MemberId = memberObj.MemberId.ToString();
                                res.UserImage = memberObj.Photo ?? Path.GetFileName("gv_no_photo.jpg");
                                res.IsMemberFound = true;
                                res.IsSuccess = true;
                                res.ErrorMessage = "OK";
                            }
                        }
                        else
                            res.ErrorMessage = "Invalid data sent!";
                    }
                    else
                        res.ErrorMessage = "Invalid data sent";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> CheckMemberExistenceUsingEmailOrPhone FAILED - StringToCheck: [" + input.StringToCheck + "], Exception: [" + ex + "]");
                    res.ErrorMessage = ex.Message;
                }

                return res;
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        #region UNUSED METHODS

        [HttpPost]
        [ActionName("PayBackTransaction")]
        public string PayBackTransaction(string memberId, string accessToken, string transactionId, string userResponse, GeoLocation location)
        {
            string res = "";

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Cntlr -> PayBackTransaction Fired - MemberID: [" + memberId + "], TransID: [" + transactionId + "]");
                    var tda = new TransactionsDataAccess();
                    res = tda.PayBackTransaction(transactionId, userResponse, location);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Cntlr -> PayBackTransaction FAILED - MemberID: [" + memberId + "], TransID: [" + transactionId + "], Exception: [" + ex + " ]");
                    res = ex.Message;
                }
            }
            else res = "Invalid OAuth 2 Access";

            return res;
        }

        #endregion UNUSED METHODS
    }
}
