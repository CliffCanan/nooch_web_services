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


namespace Nooch.API.Controllers
{
    public class NoochServicesController : ApiController
    {

        private readonly NOOCHEntities _dbContext = null;

        public NoochServicesController()
        {
            _dbContext = new NOOCHEntities();
        }

        //public IEnumerable<string> Get()
        //{
        //    Logger.Info("came in log");

        //    var config = new MapperConfiguration(cfg => cfg.CreateMap<Member, MemberEnity>());
        //    var mapper = config.CreateMapper();

        //   MembersDataAccess mda = new MembersDataAccess();
        //   Guid memGuid = new Guid("ECA52C86-5674-4008-A09B-0003645D2A1A");
        //    var objtoPam = mda.GetMemberByGuid(memGuid);

        //    MemberEnity me = mapper.Map<Member, MemberEnity>(objtoPam);

        //    return new string[] { "value1", "value2" };
        //}


        [HttpPost]
        [ActionName("ForgotPassword")]
        public StringResult ForgotPassword(StringInput userName)
        {
            try
            {
                Logger.Info("Service Controller - ForgotPassword - [userName: " + userName + "]");

                return new StringResult { Result = CommonHelper.ForgotPassword(userName.Input) };
            }
            catch (Exception ex)
            {
                return new StringResult() { Result = "" };
            }
        }


        [HttpPost]
        [ActionName("UdateMemberIPAddress")]
        public StringResult UdateMemberIPAddress(UpdateMemberIpInput member)
        {
            if (CommonHelper.IsValidRequest(member.AccessToken, member.MemberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    string res = CommonHelper.UpdateMemberIPAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId);
                    return new StringResult() { Result = CommonHelper.UpdateMemberIPAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> UpdateMemberIPAddressAndDeviceId FAILED - MemberID: [" + member.MemberId +
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
                    Logger.Info("Service Controller - GetPrimaryEmail [udId: " + udId + "]");

                    var memberEntity = CommonHelper.GetMemberByUdId(udId);
                    var member = new MemberDto { UserName = memberEntity.UserName, Status = memberEntity.Status };
                    return member;
                }
                catch (Exception ex)
                {
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
            return new MemberDto();
        }

        [HttpGet]
        [ActionName("GetMemberPendingTransctionsCount")]
        public PendingTransCoutResult GetMemberPendingTransctionsCount(string MemberId, string AccessToken)
        {
            if (CommonHelper.IsValidRequest(AccessToken, MemberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service layer -> GetMemberPendingTransctionsCount - MemberId: [" + MemberId + "]");
                    var transactionDataAccess = new TransactionsDataAccess();

                    PendingTransCoutResult trans = transactionDataAccess.GetMemberPendingTransCount(MemberId);
                    return trans;
                }
                catch (Exception ex)
                {
                    //throw new Exception("Server Error");
                    return new PendingTransCoutResult { pendingRequestsSent = "0", pendingRequestsReceived = "0", pendingInvitationsSent = "0", pendingDisputesNotSolved = "0" };
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
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
                Logger.Error("Service Controller - GetMemberByUserName FAILED - [userName: " + userName +
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
                Logger.Error("Service Controller - GetMemberUsernameByMemberId FAILED - [memberId: " + memberId +
                                       "], Exception: [" + ex + "]");
            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("GetPhoneNumberByMemberId")]
        public StringResult GetPhoneNumberByMemberId(string memberId)
        {
            try
            {
                Logger.Info("Service Controller -> GetPhoneNumberByMemberId Initiated - [MemberID: " + memberId + "]");

                return new StringResult
                {
                    Result = CommonHelper.GetPhoneNumberByMemberId(memberId)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> GetPhoneNumberByMemberId FAILED - [Exception: " + ex + "]");
                return new StringResult();
            }
        }

        [HttpGet]
        [ActionName("GetMemberIdByPhone")]
        public StringResult GetMemberIdByPhone(string phoneNo, string accessToken)
        {
            try
            {
                Logger.Info("Service Controller - GetMemberByPhone - phoneNo: [" + phoneNo + "]");

                return new StringResult { Result = CommonHelper.GetMemberIdByPhone(phoneNo) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - GetMemberByPhone - FAILED - [Exception: " + ex + "]");
            }
            return new StringResult();
        }

        [HttpPost]
        [ActionName("GetMemberIds")]
        public PhoneEmailListDto GetMemberIds(PhoneEmailListDto phoneEmailListDto)
        {
            try
            {
                //Logger.LogDebugMessage("Service layer - GetMemberIds - userName: [" + phoneEmailListDto + "]");
                var memberDataAccess = new MembersDataAccess();
                return memberDataAccess.GetMemberIds(phoneEmailListDto);
            }
            catch (Exception ex)
            {
                return new PhoneEmailListDto();
            }
        }

        [HttpGet]
        [ActionName("GetMemberNameByUserName")]
        public StringResult GetMemberNameByUserName(string userName)
        {
            try
            {
                return new StringResult { Result = CommonHelper.GetMemberNameByUserName(userName) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - GetMemberUsernameByMemberId FAILED - [GetMemberNameByUserName : " + userName +
                                       "], Exception: [" + ex + "]");
            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("MemberActivation")]
        public BoolResult MemberActivation(string tokenId)
        {
            try
            {
                Logger.Info("Service Controller -> MemberActivation Initiated - [tokenId: " + tokenId + "]");
                var mda = new MembersDataAccess();
                return new BoolResult { Result = mda.MemberActivation(tokenId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> MemberActivation Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
                return new BoolResult();
            }
        }

        [HttpGet]
        [ActionName("IsMemberActivated")]
        public BoolResult IsMemberActivated(string tokenId)
        {
            try
            {
                return new BoolResult { Result = CommonHelper.IsMemberActivated(tokenId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> IsMemberActivated Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
                return new BoolResult();
            }
        }

        [HttpGet]
        [ActionName("IsNonNoochMemberActivated")]
        public BoolResult IsNonNoochMemberActivated(string emailId)
        {
            try
            {
                Logger.Info("Service Controller - IsNonNoochMemberActivated - Email ID: [" + emailId + "]");

                return new BoolResult { Result = CommonHelper.IsNonNoochMemberActivated(emailId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> IsNonNoochMemberActivated Failed - [tokenId: " + emailId + "]. Exception -> " + ex);
                return new BoolResult();
            }
        }

        [HttpGet]
        [ActionName("IsDuplicateMember")]
        public StringResult IsDuplicateMember(string userName)
        {
            StringResult result = new StringResult();
            try
            {
                Logger.Info("Service Controller - IsDuplicateMember - userName: [" + userName + "]");

                return new StringResult { Result = CommonHelper.IsDuplicateMember(userName) };
            }
            catch (Exception ex)
            {
                result.Result = ex.ToString();
            }
            return result;
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
                Logger.Error("Service Controller - GetEncryptedData FAILED - sourceData: [" + data + "]. Exception: [" + ex + "]");
                return new MemberDto();
            }
        }

        public MemberDto GetDecryptedData(string sourceData)
        {
            try
            {
                //Logger.LogDebugMessage("Service Layer - GetDecryptedData - sourceData [" + sourceData + "]");

                var aesAlgorithm = new AES();
                string decryptedData = aesAlgorithm.Decrypt(sourceData.Replace(" ", "+"), string.Empty);

                string Base64EncodedData = Base64Encode(decryptedData);

                return new MemberDto { Status = Base64EncodedData };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - GetDecryptedData FAILED - sourceData: [" + sourceData + "]. Exception: [" + ex + "]");
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
                return new StringResult { Result = "" };
            }
        }

        public StringResult WeeklyLimitTest(string memId)
        {
            var tda = new TransactionsDataAccess();
            Guid memGuid = Utility.ConvertToGuid(memId);

            return new StringResult { Result = CommonHelper.IsWeeklyTransferLimitExceeded(memGuid, 5).ToString() };
        }

        [HttpGet]
        [ActionName("GetMemberDetails")]
        public MemberDto GetMemberDetails(string memberId, string accessToken)
        {
            Logger.Info("Service Controller -> GetMemberDetails - [MemberId: " + memberId + "]");

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    // Get the Member's Account Info

                    var memberEntity = CommonHelper.GetMemberDetails(memberId);

                    // Get Synapse Bank Account Info
                    var synapseBank = CommonHelper.GetSynapseBankAccountDetails(memberId);

                    string accountstatus = "";
                    if (synapseBank != null)
                    {
                        // Now check this bank's status. 
                        // CLIFF (10/7/15): If the user's ID is verified (after sending SSN info to Synapse), then consider the bank Verified as well
                        if (memberEntity.IsVerifiedWithSynapse == true)
                        {
                            accountstatus = "Verified";
                        }
                        else
                        {
                            accountstatus = synapseBank.Status;
                        }
                    }

                    bool b = (synapseBank != null) ? true : false;

                    // Create Member Object to return to the app
                    var member = new MemberDto
                    {
                        MemberId = memberEntity.MemberId.ToString(),
                        UserName = CommonHelper.GetDecryptedData(memberEntity.UserName),
                        Status = memberEntity.Status,
                        FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.FirstName)),
                        LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberEntity.LastName)),
                        PhotoUrl = memberEntity.Photo ?? Path.GetFileName("gv_no_photo.jpg"),
                        LastLocationLat = memberEntity.LastLocationLat,
                        LastLocationLng = memberEntity.LastLocationLng,
                        IsRequiredImmediatley = memberEntity.IsRequiredImmediatley.ToString(),
                        FacebookAccountLogin = memberEntity.FacebookAccountLogin != null ? CommonHelper.GetDecryptedData(memberEntity.FacebookAccountLogin) : "",
                        //IsKnoxBankAdded = b, // Why is the Knox & Synapse value both equal to the same thing?
                        IsSynapseBankAdded = b,
                        SynapseBankStatus = accountstatus,
                        IsVerifiedPhone = (memberEntity.IsVerifiedPhone != null) && Convert.ToBoolean(memberEntity.IsVerifiedPhone),
                        IsSSNAdded = (memberEntity.SSN != null),
                        DateCreated = memberEntity.DateCreated,
                        DateOfBirth = (memberEntity.DateOfBirth == null) ? "" : Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MM/dd/yyyy"),
                        DeviceToken = memberEntity.DeviceToken
                    };

                    return member;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> GetMemberDetails FAILED - [MemberId: " + memberId + "], [Exception: " + ex.InnerException + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetMostFrequentFriends")]
        public List<GetMostFrequentFriends_Result> GetMostFrequentFriends(string MemberId, string accesstoken)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    MembersDataAccess obj = new MembersDataAccess();
                    return obj.GetMostFrequentFriends(MemberId);
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> GetMostFrequentFriends FAILED - [Exception: " + ex + "]");
                    throw new Exception("Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
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
                    //Logger.LogDebugMessage("Service layer - GetMemberStats - MemberId].");
                    var memberDataAccess = new MembersDataAccess();
                    return new StringResult { Result = memberDataAccess.GetMemberStats(MemberId, query) };
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
                var memberDataAccess = new MembersDataAccess();
                List<Member> members = memberDataAccess.getInvitedMemberList(memberId);
                return (from fMember in members let config = new MapperConfiguration(cfg => { cfg.CreateMap<Member, MemberForInvitedMembersList>().BeforeMap((src, dest) => src.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.FirstName))).BeforeMap((src, dest) => src.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.LastName))).BeforeMap((src, dest) => src.UserName = CommonHelper.GetDecryptedData(src.UserName)); }) let mapper = config.CreateMapper() select mapper.Map<Member, MemberForInvitedMembersList>(fMember)).ToList();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("SaveMemberDeviceToken")]
        public StringResult SaveMemberDeviceToken(string memberId, string accessToken, string deviceToken)
        {
            Logger.Info("Service Controller -> SaveMemberDeviceToken Initiated - [MemberId: " + memberId + "], [DeviceToken: " + deviceToken + "]");

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
                        Logger.Error("Service Controller -> SaveMemberDeviceToken FAILED - [MemberId: " + memberId + "]. Exception: [" + ex + "]");

                        throw new Exception("Server Error.");
                    }
                }
                else
                {
                    res.Result = "No DeviceToken was sent!";
                }

                res.Result = "Failed to save DeviceToken";
                return res;
            }
            else
            {
                Logger.Error("Service Controller -> SaveMemberDeviceToken FAILED - [MemberId: " + memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        private string GenerateAccessToken()
        {
            byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            byte[] key = Guid.NewGuid().ToByteArray();
            string token = Convert.ToBase64String(time.Concat(key).ToArray());
            return CommonHelper.GetEncryptedData(token);
        }


        [HttpGet]
        [ActionName("LoginRequest")]
        public StringResult LoginRequest(string userName, string pwd, Boolean rememberMeEnabled, decimal lat,
            decimal lng, string udid, string devicetoken)
        {
            try
            {
                Logger.Info("Service Controller -> LoginRequest - [userName: " + userName + "], [UDID: " + udid + "], [devicetoken: " + devicetoken + "]");

                var mda = new MembersDataAccess();
                string cookie = mda.LoginRequest(userName, pwd, rememberMeEnabled, lat, lng, udid, devicetoken);

                if (string.IsNullOrEmpty(cookie))
                {
                    cookie = "Authentication failed.";
                    return new StringResult { Result = "Invalid Login or Password" };
                }
                else if (cookie == "Temporarily_Blocked")
                {
                    return new StringResult { Result = "Temporarily_Blocked" };
                }
                else if (cookie == "Suspended")
                {
                    return new StringResult { Result = "Suspended" };
                }
                else if (cookie == "Registered")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userName, state);
                    return new StringResult { Result = state };
                }
                else if (cookie == "Invalid user id or password.")
                {
                    return new StringResult { Result = "Invalid user id or password." };
                }
                else if (cookie == "The password you have entered is incorrect.")
                {
                    return new StringResult { Result = "The password you have entered is incorrect." };
                }
                else if (cookie == "Success")
                {
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userName, state);
                    return new StringResult { Result = state };
                }
                else
                {
                    return new StringResult { Result = cookie };
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> LoginRequest FAILED - [userName: " + userName + "], [Exception: " + ex + "]");
                throw new Exception("Server Error");
            }
        }


        [HttpGet]
        [ActionName("GetLocationSearch")]
        public List<LocationSearch> GetLocationSearch(string MemberId, int Radius, string accessToken)
        {
            try
            {
                if (CommonHelper.IsValidRequest(accessToken, MemberId))
                {
                    var memberDataAccess = new MembersDataAccess();
                    List<LocationSearch> list = memberDataAccess.GetLocationSearch(MemberId, Radius);
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
        [ActionName("SendTransactionReminderEmail")]
        public StringResult SendTransactionReminderEmail(string ReminderType, string TransactionId, string accessToken, string MemberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, MemberId))
            {
                try
                {
                    Logger.Info("Service Controller - SendTransactionReminderEmail - [MemberId: " + MemberId + "], [ReminderType: " + ReminderType + "]");
                    var tda = new TransactionsDataAccess();

                    return new StringResult { Result = tda.SendTransactionReminderEmail(ReminderType, TransactionId, MemberId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - SendTransactionReminderEmail FAILED - memberId: [" + MemberId + "]. Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
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
        public StringResult RequestMoneyToExistingButNonRegisteredUser(RequestDto requestInput, out string requestId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoneyToExistingButNonRegisteredUser Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToExistingButNonregisteredUser(requestInput, out requestId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RequestMoneyToExistingButNonRegisteredUser FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        /// <summary>
        /// For an existing user to make a request to non existing user.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RequestMoneyToNonNoochUserUsingSynapse")]
        public StringResult RequestMoneyToNonNoochUserUsingSynapse(RequestDto requestInput, out string requestId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoneyToNonNoochUserUsingSynapse Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToNonNoochUserUsingSynapse(requestInput, out requestId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RequestMoneyToNonNoochUserUsingSynapse FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
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
        public StringResult RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(RequestDto requestInput, out string requestId, string accessToken, string PayorPhoneNumber)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoneyToNonNoochUserThroughPhoneUsingSynapse Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(requestInput, out requestId, PayorPhoneNumber) };
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
        public StringResult RequestMoney(RequestDto requestInput, out string requestId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                requestId = string.Empty;

                try
                {
                    Logger.Info("Service Controller - RequestMoney Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoney(requestInput, out requestId) };
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
        /// For sending a payment (send or request) on behalf of Rent Scene's Nooch account.
        /// Created by Cliff on 12/17/15.
        /// </summary>
        /// <param name="requestInput"></param>
        /// <param name="requestId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("RequestMoneyForRentScene")]
        public requestFromRentScene RequestMoneyForRentScene(string name, string email, string amount, string memo, string pin, string ip, bool isRequest)
        {
            Logger.Info("Service Controller - RequestMoneyForRentScene Initiated - [Name: " + name +
                        "], Email: [" + email + "], amount: [" + amount +
                        "], memo: [" + memo + "], pin: [" + pin +
                        "], ip: [" + ip + "], isRequest: [" + isRequest + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.isEmailAlreadyReg = false;
            res.msg = "Service Layer - Initial";

            #region Check for Required Data

            bool isMissingData = false;

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
                Logger.Error("Service Controller -> RequestMoneyForRentScene FAILED - Missing required data - Msg is: [" + res.msg + "]");
                return res;
            }

            #endregion Check for Required Data

            #region Check If Recipient Already Has A Nooch Account

            //var mda = new MembersDataAccess();

            var memberObj = CommonHelper.GetMemberDetailsByUserName(email);

            if (memberObj != null)
            {
                // This email address is already registered!
                Logger.Error("Service Controller -> RequestMoneyForRentScene FAILED - User already exists with email: [" + email + "]");

                res.isEmailAlreadyReg = true;
                res.memberId = memberObj.MemberId.ToString();
                res.name = (!String.IsNullOrEmpty(memberObj.FirstName)) ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName))
                                                                        : "";
                res.name = (!String.IsNullOrEmpty(memberObj.LastName)) ? res.name + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName))
                                                                        : res.name;
                res.memberStatus = memberObj.Status;
                res.dateCreated = Convert.ToDateTime(memberObj.DateCreated).ToString("MMM d, yyyy");


                //var ada = new AccountDataAccess();
                var userAndBankInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(memberObj.MemberId.ToString());

                if (userAndBankInfo != null &&
                    userAndBankInfo.wereBankDetailsFound == true &&
                    userAndBankInfo.BankDetails != null)
                {
                    res.isBankAttached = true;
                    res.bankStatus = userAndBankInfo.BankDetails.Status;
                }

                res.msg = "Existing user found!";
                res.note = "";
                res.success = true;
                return res;
            }

            #endregion Check If Recipient Already Has A Nooch Account


            try
            {
                var tda = new TransactionsDataAccess();

                string requestId = string.Empty;

                RequestDto requestInput = new RequestDto()
                {
                    AddressLine1 = "1500 JFK Blvd",
                    Amount = Convert.ToDecimal(amount),
                    City = "Philadelphia",
                    Country = "US",
                    Latitude = 39.95332018F,
                    Longitude = -75.1661824F,
                    MemberId = "852987e8-d5fe-47e7-a00b-58a80dd15b49",
                    Memo = memo,
                    MoneySenderEmailId = email,
                    Name = name,
                    PinNumber = pin,
                    SenderId = "",
                    State = "PA",
                    TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    ZipCode = "19102",
                    //isTesting = "true" // REMOVE FOR PRODUCTION!!
                };

                StringResult tdaRes = new StringResult { Result = tda.RequestMoneyToNonNoochUserUsingSynapse(requestInput, out requestId) };

                res.msg = tdaRes.Result;
                res.note = requestId;

                if (tdaRes.Result.IndexOf("made successfully") > -1)
                {
                    res.success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - RequestMoneyForRentScene FAILED - [Email: " + email + "]. Exception: [" + ex + "]");

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
        public requestFromRentScene RequestMoneyToExistingUserForRentScene(string name, string email, string amount, string memo, string pin, string ip, bool isRequest, string memberId, string nameFromServer)
        {
            Logger.Info("Service Controller - RequestMoneyToExistingUserForRentScene Initiated - [Name: " + name +
                        "], Email: [" + email + "], Amount: [" + amount +
                        "], Memo: [" + memo + "], PIN: [" + pin +
                        "], IP: [" + ip + "], isRequest: [" + isRequest + "]" +
                        "], MemberID: [" + memberId + "], NameFromServer: [" + nameFromServer + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.isEmailAlreadyReg = false;
            res.msg = "Service Layer - Initial";

            #region Check for Required Data

            bool isMissingData = false;

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
                Logger.Error("Service Controller -> RequestMoneyForRentScene FAILED - Missing required data - Msg is: [" + res.msg + "]");
                return res;
            }

            #endregion Check for Required Data

            try
            {
                string requestId = string.Empty;

                RequestDto requestInput = new RequestDto()
                {
                    AddressLine1 = "1500 JFK Blvd",
                    Amount = Convert.ToDecimal(amount),
                    City = "Philadelphia",
                    Country = "US",
                    Latitude = 39.95332018F,
                    Longitude = -75.1661824F,
                    MemberId = "852987e8-d5fe-47e7-a00b-58a80dd15b49",
                    Memo = memo,
                    MoneySenderEmailId = String.IsNullOrEmpty(memberId) ? email
                                                                        : "",
                    Name = nameFromServer,
                    PinNumber = !String.IsNullOrEmpty(pin) ? CommonHelper.GetEncryptedData(pin)
                                                           : "eeR7e/xjfVaQpm1w7jCh8g==", // "0000" as the default
                    SenderId = !String.IsNullOrEmpty(memberId) ? memberId
                                                               : "",
                    State = "PA",
                    TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    ZipCode = "19102",
                    //isTesting = "true" // REMOVE FOR PRODUCTION!!
                };


                var tda = new TransactionsDataAccess();
                StringResult tdaRes = new StringResult { Result = tda.RequestMoneyToExistingButNonregisteredUser(requestInput, out requestId) };

                res.msg = tdaRes.Result;
                res.note = requestId;

                if (tdaRes.Result.IndexOf("made successfully") > -1)
                {
                    res.success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - RequestMoneyToExistingButNonRegisteredUser FAILED - [Email: " + email + "]. Exception: [" + ex + "]");

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
                Logger.Info("Service Controller - RejectMoneyRequestForExistingNoochUser - [TransactionId: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyRequestForExistingNoochUser(transactionId);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - RejectMoneyRequestForExistingNoochUser FAILED - [TransactionId: " + transactionId +
                             "], [Exception: " + ex.Message + "]");

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
            return new StringResult { Result = tda.CancelMoneyRequestForExistingNoochUser(TransactionId, MemberId) };
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
                    Logger.Info("Service Controller - CancelRejectTransaction - [MemberId: " + memberId + "]");
                    var tda = new TransactionsDataAccess();
                    string result = tda.CancelRejectTransaction(transactionId, userResponse);
                    return result;
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
        [ActionName("SaveSocialMediaPost")]
        public StringResult SaveSocialMediaPost(string MemberId, string accesstoken, string PostTo, string PostContent)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    Logger.Info("Service Controller - SaveSocialMediaPost - [MemberId: " + MemberId + "],  [Posted To: " + PostTo + "]");
                    var memberDataAccess = new MembersDataAccess();
                    return new StringResult { Result = memberDataAccess.SaveMediaPosts(MemberId, PostTo, PostContent) };
                }
                catch (Exception ex)
                {
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }



        /**********************************/
        /****  HISORY-RELATED METHODS  ****/
        /**********************************/
        #region History Related Methods

        [HttpGet]
        [ActionName("GetsingleTransactionDetail")]
        public TransactionDto GetsingleTransactionDetail(string MemberId, string accesstoken, string transactionId)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {

                    Logger.Info("Service Controller -> GetSingleTransactionDetail Intiated - " +
                                           "MemberId: [" + MemberId + "], TransID: [" + transactionId + "]");

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
                    Logger.Error("Service Controller - GetsingleTransactionDetail FAILED - MemberId: [" + MemberId + "]. Exception: [" + ex + "]");
                    throw (ex);

                }

            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        [HttpGet]
        [ActionName("GetTransactionsList")]
        public Collection<TransactionDto> GetTransactionsList(string memberId, string listType, int pSize, int pIndex, string accessToken, string SubListType)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Layer - GetTransactionsList Initiated - MemberID: [" + member + "]");

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

                                Transactions.Add(obj);

                                #endregion Foreach inside
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Service Controller - GetTransactionsList ERROR - Inner Exception during loop through all transactions - " +
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
                    Logger.Error("Service Controller - GetTransactionsList FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
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
                    Logger.Info("Service Controller - GetLatestReceivedTransaction - [MemberId: " + member + "]");

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
                    Logger.Info("Service Controller - GetTransactionDetail - [MemberId: " + memberId + "]");

                    var transactionDataAccess = new TransactionsDataAccess();

                    var transactionListEntities = transactionDataAccess.GetTransactionDetail(memberId, category, transactionId);
                    DateTime transactionDate = Convert.ToDateTime(transactionListEntities.TransactionDate);
                    string transactionDateString = transactionDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                    var transactionDateTime = new string[3];

                    DateTime disputeDate;
                    string disputeDateString = string.Empty;
                    var disputeDateTime = new string[3];

                    DateTime disputeReviewDate;
                    string disputeReviewDateString = string.Empty;
                    var disputeReviewDateTime = new string[3];

                    DateTime disputeResolvedDate;
                    string disputeResolvedDateString = string.Empty;
                    var disputeResolvedDateTime = new string[3];

                    var memberDataAccess = new MembersDataAccess();

                    if (transactionListEntities.DisputeDate != null)
                    {
                        disputeDate = Convert.ToDateTime(transactionListEntities.DisputeDate);
                        disputeDateString = disputeDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                        disputeDateTime = disputeDateString.Split(' ');
                    }

                    if (transactionListEntities.ReviewDate != null)
                    {
                        disputeReviewDate = Convert.ToDateTime(transactionListEntities.ReviewDate);
                        disputeReviewDateString = disputeReviewDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                        disputeDateTime = disputeReviewDateString.Split(' ');
                    }

                    if (transactionListEntities.ResolvedDate != null)
                    {
                        disputeResolvedDate = Convert.ToDateTime(transactionListEntities.ResolvedDate);
                        disputeResolvedDateString = disputeResolvedDate.ToString("MM/dd/yyyy hh:mm:ss tt");
                        disputeResolvedDateTime = disputeResolvedDateString.Split(' ');
                    }

                    if (transactionListEntities != null)
                    {
                        if (category.Equals("SENT"))
                        {
                            #region Sent
                            var sentTransactions = new TransactionDto();

                            string timeZoneDateString = string.Empty;

                            transactionDateTime = transactionDateString.Split(' ');

                            if (!string.IsNullOrEmpty(transactionListEntities.Member.TimeZoneKey))
                            {
                                timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Member.TimeZoneKey);

                                timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");

                                transactionDateTime = timeZoneDateString.Split(' ');
                            }

                            sentTransactions.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName));
                            sentTransactions.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName));
                            sentTransactions.MemberId = transactionListEntities.Member.MemberId.ToString();
                            sentTransactions.NoochId = transactionListEntities.Member1.Nooch_ID.ToString();
                            sentTransactions.RecepientId = transactionListEntities.Member1.MemberId.ToString();
                            sentTransactions.TransactionId = transactionListEntities.TransactionId.ToString();
                            sentTransactions.Name = CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName);
                            sentTransactions.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Member.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                            sentTransactions.Date = transactionDateTime[0];
                            sentTransactions.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                            sentTransactions.Amount = Math.Round(transactionListEntities.Amount, 2);
                            sentTransactions.DisputeStatus = !string.IsNullOrEmpty(transactionListEntities.DisputeStatus) ? CommonHelper.GetDecryptedData(transactionListEntities.DisputeStatus) : null;
                            sentTransactions.DisputeId = transactionListEntities.DisputeTrackingId;
                            sentTransactions.TransactionType = "Sent";
                            // recipient photo - Sent to,
                            sentTransactions.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transactionListEntities.Member1.Photo != null ? Path.GetFileName(transactionListEntities.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            sentTransactions.DisputeReportedDate = transactionListEntities.DisputeDate.HasValue ? disputeDateString : string.Empty;
                            sentTransactions.DisputeReviewDate = transactionListEntities.ReviewDate.HasValue ? disputeReviewDateString : string.Empty;
                            sentTransactions.DisputeResolvedDate = transactionListEntities.ResolvedDate.HasValue ? disputeResolvedDateString : string.Empty;

                            if (transactionListEntities.GeoLocation != null)
                            {
                                sentTransactions.AddressLine1 = transactionListEntities.GeoLocation.AddressLine1 != null ? transactionListEntities.GeoLocation.AddressLine1 : string.Empty;
                                sentTransactions.AddressLine2 = transactionListEntities.GeoLocation.AddressLine2 != null ? transactionListEntities.GeoLocation.AddressLine2 : string.Empty;
                                sentTransactions.City = transactionListEntities.GeoLocation.City != null ? transactionListEntities.GeoLocation.City : string.Empty;
                                sentTransactions.State = transactionListEntities.GeoLocation.State != null ? transactionListEntities.GeoLocation.State : string.Empty;
                                sentTransactions.Country = transactionListEntities.GeoLocation.Country != null ? transactionListEntities.GeoLocation.Country : string.Empty;
                                sentTransactions.ZipCode = transactionListEntities.GeoLocation.ZipCode != null ? transactionListEntities.GeoLocation.ZipCode : string.Empty;

                                sentTransactions.Latitude = transactionListEntities.GeoLocation.Latitude != null ? (float)transactionListEntities.GeoLocation.Latitude : default(float);
                                sentTransactions.Longitude = transactionListEntities.GeoLocation.Longitude != null ? (float)transactionListEntities.GeoLocation.Longitude : default(float);
                            }

                            if (transactionListEntities.GeoLocation != null)
                            {
                                sentTransactions.Location = transactionListEntities.GeoLocation.Latitude + ", " +
                                    transactionListEntities.GeoLocation.Longitude;
                            }
                            else
                            {
                                sentTransactions.Location = "- Nil -";
                            }

                            sentTransactions.Picture = transactionListEntities.Picture;

                            return sentTransactions;

                            #endregion Sent
                        }
                        if (category.Equals("RECEIVED"))
                        {
                            #region Received

                            var receivedTransactions = new TransactionDto();

                            transactionDateTime = transactionDateString.Split(' ');

                            string timeZoneDateString = string.Empty;

                            if (!string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey))
                            {
                                timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Member1.TimeZoneKey);

                                timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");

                                transactionDateTime = timeZoneDateString.Split(' ');
                            }

                            receivedTransactions.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName));
                            receivedTransactions.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName));
                            receivedTransactions.MemberId = transactionListEntities.Member.MemberId.ToString();
                            receivedTransactions.NoochId = transactionListEntities.Member.Nooch_ID.ToString();
                            receivedTransactions.RecepientId = transactionListEntities.Member1.MemberId.ToString();
                            receivedTransactions.TransactionId = transactionListEntities.TransactionId.ToString();
                            receivedTransactions.Name = CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName);
                            receivedTransactions.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                            receivedTransactions.Date = transactionDateTime[0];
                            receivedTransactions.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                            receivedTransactions.Amount = Math.Round(transactionListEntities.Amount, 2);
                            receivedTransactions.DisputeStatus = !string.IsNullOrEmpty(transactionListEntities.DisputeStatus) ? CommonHelper.GetDecryptedData(transactionListEntities.DisputeStatus) : null;
                            receivedTransactions.DisputeId = transactionListEntities.DisputeTrackingId;
                            receivedTransactions.TransactionType = "Received";
                            // sender photo - Received from
                            receivedTransactions.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transactionListEntities.Member.Photo != null ? Path.GetFileName(transactionListEntities.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            receivedTransactions.DisputeReportedDate = transactionListEntities.DisputeDate.HasValue ? disputeDateString : string.Empty;
                            receivedTransactions.DisputeReviewDate = transactionListEntities.ReviewDate.HasValue ? disputeReviewDateString : string.Empty;
                            receivedTransactions.DisputeResolvedDate = transactionListEntities.ResolvedDate.HasValue ? disputeResolvedDateString : string.Empty;

                            if (transactionListEntities.GeoLocation != null)
                            {
                                receivedTransactions.AddressLine1 = transactionListEntities.GeoLocation.AddressLine1 != null ? transactionListEntities.GeoLocation.AddressLine1 : string.Empty;
                                receivedTransactions.AddressLine2 = transactionListEntities.GeoLocation.AddressLine2 != null ? transactionListEntities.GeoLocation.AddressLine2 : string.Empty;
                                receivedTransactions.City = transactionListEntities.GeoLocation.City != null ? transactionListEntities.GeoLocation.City : string.Empty;
                                receivedTransactions.State = transactionListEntities.GeoLocation.State != null ? transactionListEntities.GeoLocation.State : string.Empty;
                                receivedTransactions.Country = transactionListEntities.GeoLocation.Country != null ? transactionListEntities.GeoLocation.Country : string.Empty;
                                receivedTransactions.ZipCode = transactionListEntities.GeoLocation.ZipCode != null ? transactionListEntities.GeoLocation.ZipCode : string.Empty;

                                receivedTransactions.Latitude = transactionListEntities.GeoLocation.Latitude != null ? (float)transactionListEntities.GeoLocation.Latitude : default(float);
                                receivedTransactions.Longitude = transactionListEntities.GeoLocation.Longitude != null ? (float)transactionListEntities.GeoLocation.Longitude : default(float);
                            }

                            if (transactionListEntities.GeoLocation != null)
                            {
                                receivedTransactions.Location = transactionListEntities.GeoLocation.Latitude + ", " +
                                    transactionListEntities.GeoLocation.Longitude;
                            }
                            else
                            {
                                receivedTransactions.Location = "- Nil -";
                            }

                            receivedTransactions.Picture = transactionListEntities.Picture;

                            return receivedTransactions;

                            #endregion Received
                        }
                        if (category.Equals("REQUEST"))
                        {
                            #region Received

                            var receivedTransactions = new TransactionDto();

                            transactionDateTime = transactionDateString.Split(' ');

                            string timeZoneDateString = string.Empty;

                            if (!string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey))
                            {
                                timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Member1.TimeZoneKey);

                                timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");

                                transactionDateTime = timeZoneDateString.Split(' ');
                            }

                            receivedTransactions.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName));
                            receivedTransactions.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName));
                            receivedTransactions.MemberId = transactionListEntities.Member.MemberId.ToString();
                            receivedTransactions.NoochId = transactionListEntities.Member.Nooch_ID.ToString();
                            receivedTransactions.RecepientId = transactionListEntities.Member1.MemberId.ToString();
                            receivedTransactions.TransactionId = transactionListEntities.TransactionId.ToString();
                            receivedTransactions.Name = CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName);
                            receivedTransactions.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                            receivedTransactions.Date = transactionDateTime[0];
                            receivedTransactions.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                            receivedTransactions.Amount = Math.Round(transactionListEntities.Amount, 2);
                            receivedTransactions.DisputeStatus = !string.IsNullOrEmpty(transactionListEntities.DisputeStatus) ? CommonHelper.GetDecryptedData(transactionListEntities.DisputeStatus) : null;
                            receivedTransactions.DisputeId = transactionListEntities.DisputeTrackingId;
                            receivedTransactions.TransactionType = "Request";
                            // sender photo - Received from
                            receivedTransactions.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transactionListEntities.Member.Photo != null ? Path.GetFileName(transactionListEntities.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            receivedTransactions.DisputeReportedDate = transactionListEntities.DisputeDate.HasValue ? disputeDateString : string.Empty;
                            receivedTransactions.DisputeReviewDate = transactionListEntities.ReviewDate.HasValue ? disputeReviewDateString : string.Empty;
                            receivedTransactions.DisputeResolvedDate = transactionListEntities.ResolvedDate.HasValue ? disputeResolvedDateString : string.Empty;

                            if (transactionListEntities.GeoLocation != null)
                            {
                                receivedTransactions.AddressLine1 = transactionListEntities.GeoLocation.AddressLine1 != null ? transactionListEntities.GeoLocation.AddressLine1 : string.Empty;
                                receivedTransactions.AddressLine2 = transactionListEntities.GeoLocation.AddressLine2 != null ? transactionListEntities.GeoLocation.AddressLine2 : string.Empty;
                                receivedTransactions.City = transactionListEntities.GeoLocation.City != null ? transactionListEntities.GeoLocation.City : string.Empty;
                                receivedTransactions.State = transactionListEntities.GeoLocation.State != null ? transactionListEntities.GeoLocation.State : string.Empty;
                                receivedTransactions.Country = transactionListEntities.GeoLocation.Country != null ? transactionListEntities.GeoLocation.Country : string.Empty;
                                receivedTransactions.ZipCode = transactionListEntities.GeoLocation.ZipCode != null ? transactionListEntities.GeoLocation.ZipCode : string.Empty;

                                receivedTransactions.Latitude = transactionListEntities.GeoLocation.Latitude != null ? (float)transactionListEntities.GeoLocation.Latitude : default(float);
                                receivedTransactions.Longitude = transactionListEntities.GeoLocation.Longitude != null ? (float)transactionListEntities.GeoLocation.Longitude : default(float);
                            }

                            if (transactionListEntities.GeoLocation != null)
                            {
                                receivedTransactions.Location = transactionListEntities.GeoLocation.Latitude + ", " +
                                    transactionListEntities.GeoLocation.Longitude;
                            }
                            else
                            {
                                receivedTransactions.Location = "- Nil -";
                            }

                            receivedTransactions.Picture = transactionListEntities.Picture;

                            return receivedTransactions;

                            #endregion Received
                        }
                        if (category.Equals("DISPUTED"))
                        {
                            #region Disputed

                            var disputedTransaction = new TransactionDto();

                            if (transactionListEntities.RaisedBy.Equals("Sender"))
                            {
                                transactionDateTime = transactionDateString.Split(' ');

                                string timeZoneDateString = string.Empty;

                                if (!string.IsNullOrEmpty(transactionListEntities.Member.TimeZoneKey))
                                {
                                    timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Member.TimeZoneKey);

                                    timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");

                                    transactionDateTime = timeZoneDateString.Split(' ');
                                }

                                disputedTransaction.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName));
                                disputedTransaction.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName));
                                disputedTransaction.NoochId = transactionListEntities.Member1.Nooch_ID;
                                disputedTransaction.TransactionId = transactionListEntities.TransactionId.ToString();
                                disputedTransaction.Name = CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName);
                                disputedTransaction.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Member.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                                disputedTransaction.Date = transactionDateTime[0];
                                disputedTransaction.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                                disputedTransaction.Amount = Math.Round(transactionListEntities.Amount, 2);
                                disputedTransaction.DisputeStatus = !string.IsNullOrEmpty(transactionListEntities.DisputeStatus) ? CommonHelper.GetDecryptedData(transactionListEntities.DisputeStatus) : null;
                                disputedTransaction.DisputeId = transactionListEntities.DisputeTrackingId;
                                disputedTransaction.TransactionType = "Sent to";
                                // recipient photo - Sent to
                                disputedTransaction.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transactionListEntities.Member1.Photo != null ? Path.GetFileName(transactionListEntities.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                disputedTransaction.DisputeReportedDate = transactionListEntities.DisputeDate.HasValue ? disputeDateString : string.Empty;
                                disputedTransaction.DisputeReviewDate = transactionListEntities.ReviewDate.HasValue ? disputeReviewDateString : string.Empty;
                                disputedTransaction.DisputeResolvedDate = transactionListEntities.ResolvedDate.HasValue ? disputeResolvedDateString : string.Empty;

                                if (transactionListEntities.GeoLocation != null)
                                {
                                    disputedTransaction.AddressLine1 = transactionListEntities.GeoLocation.AddressLine1 != null ? transactionListEntities.GeoLocation.AddressLine1 : string.Empty;
                                    disputedTransaction.AddressLine2 = transactionListEntities.GeoLocation.AddressLine2 != null ? transactionListEntities.GeoLocation.AddressLine2 : string.Empty;
                                    disputedTransaction.City = transactionListEntities.GeoLocation.City != null ? transactionListEntities.GeoLocation.City : string.Empty;
                                    disputedTransaction.State = transactionListEntities.GeoLocation.State != null ? transactionListEntities.GeoLocation.State : string.Empty;
                                    disputedTransaction.Country = transactionListEntities.GeoLocation.Country != null ? transactionListEntities.GeoLocation.Country : string.Empty;
                                    disputedTransaction.ZipCode = transactionListEntities.GeoLocation.ZipCode != null ? transactionListEntities.GeoLocation.ZipCode : string.Empty;

                                    disputedTransaction.Latitude = transactionListEntities.GeoLocation.Latitude != null ? (float)transactionListEntities.GeoLocation.Latitude : default(float);
                                    disputedTransaction.Longitude = transactionListEntities.GeoLocation.Longitude != null ? (float)transactionListEntities.GeoLocation.Longitude : default(float);
                                }

                                if (transactionListEntities.GeoLocation != null)
                                {
                                    disputedTransaction.Location = transactionListEntities.GeoLocation.Latitude + ", " +
                                        transactionListEntities.GeoLocation.Longitude;
                                }
                                else
                                {
                                    disputedTransaction.Location = "- Nil -";
                                }

                            }

                            if (transactionListEntities.RaisedBy.Equals("Receiver"))
                            {
                                transactionDateTime = transactionDateString.Split(' ');

                                string timeZoneDateString = string.Empty;

                                if (!string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey))
                                {
                                    timeZoneDateString = memberDataAccess.GMTTimeZoneConversion(transactionListEntities.TransactionDate.ToString(), transactionListEntities.Member1.TimeZoneKey);

                                    timeZoneDateString = Convert.ToDateTime(timeZoneDateString).ToString("MM/dd/yyyy hh:mm:ss tt");

                                    transactionDateTime = timeZoneDateString.Split(' ');
                                }

                                disputedTransaction.NoochId = transactionListEntities.Member.Nooch_ID.ToString();
                                disputedTransaction.TransactionId = transactionListEntities.TransactionId.ToString();
                                disputedTransaction.Name = CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName) + " " + CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName);
                                disputedTransaction.TransactionDate = !string.IsNullOrEmpty(transactionListEntities.Member1.TimeZoneKey) ? timeZoneDateString : transactionDateString;
                                disputedTransaction.Date = transactionDateTime[0];
                                disputedTransaction.Time = transactionDateTime[1] + " " + transactionDateTime[2];
                                disputedTransaction.Amount = Math.Round(transactionListEntities.Amount, 2);
                                disputedTransaction.DisputeStatus = !string.IsNullOrEmpty(transactionListEntities.DisputeStatus) ? CommonHelper.GetDecryptedData(transactionListEntities.DisputeStatus) : null;
                                disputedTransaction.DisputeId = transactionListEntities.DisputeTrackingId;
                                disputedTransaction.TransactionType = "Received from";
                                // sender photo - Received from
                                disputedTransaction.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), transactionListEntities.Member.Photo != null ? Path.GetFileName(transactionListEntities.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                disputedTransaction.DisputeReportedDate = transactionListEntities.DisputeDate.HasValue ? disputeDateString : string.Empty;
                                disputedTransaction.DisputeReviewDate = transactionListEntities.ReviewDate.HasValue ? disputeReviewDateString : string.Empty;
                                disputedTransaction.DisputeResolvedDate = transactionListEntities.ResolvedDate.HasValue ? disputeResolvedDateString : string.Empty;

                                if (transactionListEntities.GeoLocation != null)
                                {
                                    disputedTransaction.AddressLine1 = transactionListEntities.GeoLocation.AddressLine1 != null ? transactionListEntities.GeoLocation.AddressLine1 : string.Empty;
                                    disputedTransaction.AddressLine2 = transactionListEntities.GeoLocation.AddressLine2 != null ? transactionListEntities.GeoLocation.AddressLine2 : string.Empty;
                                    disputedTransaction.City = transactionListEntities.GeoLocation.City != null ? transactionListEntities.GeoLocation.City : string.Empty;
                                    disputedTransaction.State = transactionListEntities.GeoLocation.State != null ? transactionListEntities.GeoLocation.State : string.Empty;
                                    disputedTransaction.Country = transactionListEntities.GeoLocation.Country != null ? transactionListEntities.GeoLocation.Country : string.Empty;
                                    disputedTransaction.ZipCode = transactionListEntities.GeoLocation.ZipCode != null ? transactionListEntities.GeoLocation.ZipCode : string.Empty;

                                    disputedTransaction.Latitude = transactionListEntities.GeoLocation.Latitude != null ? (float)transactionListEntities.GeoLocation.Latitude : default(float);
                                    disputedTransaction.Longitude = transactionListEntities.GeoLocation.Longitude != null ? (float)transactionListEntities.GeoLocation.Longitude : default(float);
                                }

                                if (transactionListEntities.GeoLocation != null)
                                {
                                    disputedTransaction.Location = transactionListEntities.GeoLocation.Latitude + ", " +
                                        transactionListEntities.GeoLocation.Longitude;
                                }
                                else
                                {
                                    disputedTransaction.Location = "- Nil -";
                                }
                            }

                            disputedTransaction.Picture = disputedTransaction.Picture;

                            return disputedTransaction;

                            #endregion Disputed
                        }

                        #region Donation - UNUSED
                        /*
                        if (transactionListEntities.RaisedBy.Equals("Donation"))
                        {
                            var donationTransaction = new TransactionDto();

                            transactionDateTime = transactionDateString.Split(' ');

                            string timeZoneDateString = string.Empty;

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
                            {
                                donationTransaction.Location = transactionListEntities.GeoLocations.Latitude + ", " +
                                    transactionListEntities.GeoLocations.Longitude;
                            }
                            else
                            {
                                donationTransaction.Location = "- Nil -";
                            }

                            donationTransaction.Picture = transactionListEntities.Picture;

                            return donationTransaction;
                        }*/
                        #endregion Donation - UNUSED
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return new TransactionDto();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        /// Get a user's pending transactions count.
        /// </summary>
        /// <param name="MemberId"></param>
        /// <param name="AccessToken"></param>
        /// <returns>PendingTransCoutResult [sic] object with values for 4 types of pending transactions: requests sent/received, invites, disputes unresolved.</returns>




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
                    Logger.Error("Service Controller - RaiseDispute FAILED - transactionId: " + raiseDisputeInput.TransactionId + "]. Exception: [" + ex + "]");
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
                    {
                        return new StringResult { Result = "1" };
                    }
                    else
                    {
                        return new StringResult { Result = "0" };
                    }
                }
                catch
                {
                    throw new Exception("0");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        #endregion History Related Methods


        /*************************************/
        /****  METHODS FOR LANDING PAGES  ****/
        /*************************************/

        [HttpGet]
        [ActionName("GetMemberDetailsForLandingPage")]
        public MemberDto GetMemberDetailsForLandingPage(string memberId)
        {
            Logger.Info("Service Controller -> GetMemberDetailsForLandingPage - [MemberId: " + memberId + "]");

            try
            {
                // Get the Member's Account Info

                var memberEntity = CommonHelper.GetMemberDetails(memberId);

                // Get Synapse Bank Account Info
                var synapseBank = CommonHelper.GetSynapseBankAccountDetails(memberId);

                string accountstatus = "";

                if (synapseBank != null)
                {
                    // Now check this bank's status. 
                    // CLIFF (10/7/15): If the user's ID is verified (after sending SSN info to Synapse), then consider the bank Verified as well
                    if (memberEntity.IsVerifiedWithSynapse == true)
                    {
                        accountstatus = "Verified";
                    }
                    else
                    {
                        accountstatus = synapseBank.Status;
                    }
                }

                bool b = (synapseBank != null);

                //var config =
                //        new MapperConfiguration(cfg => cfg.CreateMap<Member,MemberDto>()
                //            .BeforeMap((src, dest) => src.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.FirstName)))
                //            .BeforeMap((src, dest) => src.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(src.LastName)))
                //            .BeforeMap((src, dest) => src.UserName = CommonHelper.GetDecryptedData(src.UserName))
                //            .BeforeMap((src, dest) => src.DateOfBirth = dest.DateOfBirth == null ? "" : Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MM/dd/yyyy"))
                //            .BeforeMap((src, dest) => src.Address = dest.Address == null ? "" : CommonHelper.GetDecryptedData(dest.Address))
                //            .BeforeMap((src, dest) => src.City = dest.City == null ? "" : CommonHelper.GetDecryptedData(dest.City))
                //            .BeforeMap((src, dest) => src.Zip = dest.Zipcode == null ? "" : CommonHelper.GetDecryptedData(dest.Zipcode))
                //            .BeforeMap((src, dest) => src.IsVerifiedPhone = dest.IsVerifiedPhone == null && Convert.ToBoolean(dest.IsVerifiedPhone))
                //            .BeforeMap((src, dest) => src.IsSSNAdded = dest.SSN == null && Convert.ToBoolean(dest.SSN))
                //            .BeforeMap((src, dest) => src.PhotoUrl = dest.Photo ?? Path.GetFileName("gv_no_photo.jpg"))
                //                .BeforeMap((src, dest) => src.FacebookAccountLogin = dest.FacebookAccountLogin != null ?
                //                           CommonHelper.GetDecryptedData(dest.FacebookAccountLogin) :
                //                           "")
                //                           .BeforeMap((src, dest) => src.IsSynapseBankAdded = b).BeforeMap((src, dest) => src.SynapseBankStatus = accountstatus)

                //            .BeforeMap((src, dest) => src.DateCreatedString = dest.DateCreated == null ? "" : Convert.ToDateTime(memberEntity.DateCreated).ToString("MM/dd/yyyy"))
                //            );

                //var mapper = config.CreateMapper();

                //MemberDto member = mapper.Map<MemberDto>(memberEntity);


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
                    DateOfBirth = (memberEntity.DateOfBirth == null) ? "" : Convert.ToDateTime(memberEntity.DateOfBirth).ToString("MM/dd/yyyy"),
                    Address = !String.IsNullOrEmpty(memberEntity.Address) ?
                              CommonHelper.GetDecryptedData(memberEntity.Address) :
                              null,
                    City = !String.IsNullOrEmpty(memberEntity.City) ?
                           CommonHelper.GetDecryptedData(memberEntity.City) :
                           null,
                    Zip = !String.IsNullOrEmpty(memberEntity.Zipcode) ?
                          CommonHelper.GetDecryptedData(memberEntity.Zipcode) :
                          null,
                    ContactNumber = memberEntity.ContactNumber,
                    IsVerifiedPhone = memberEntity.IsVerifiedPhone != null && Convert.ToBoolean(memberEntity.IsVerifiedPhone),
                    IsSSNAdded = memberEntity.SSN != null,
                    PhotoUrl = memberEntity.Photo ?? Path.GetFileName("gv_no_photo.jpg"),
                    FacebookAccountLogin = memberEntity.FacebookAccountLogin != null ?
                                           CommonHelper.GetDecryptedData(memberEntity.FacebookAccountLogin) :
                                           "",
                    IsSynapseBankAdded = b,
                    SynapseBankStatus = accountstatus,
                    IsVerifiedWithSynapse = memberEntity.IsVerifiedWithSynapse,
                    DateCreatedString = memberEntity.DateCreated == null ? "" : Convert.ToDateTime(memberEntity.DateCreated).ToString("MM/dd/yyyy"),
                    DeviceToken = memberEntity.DeviceToken,
                };

                if (memberEntity.Type == "Landlord")
                {
                    var landlordEntity = CommonHelper.GetLandlordDetails(memberId);
                    member.companyName = !String.IsNullOrEmpty(landlordEntity.CompanyName)
                                         ? CommonHelper.GetDecryptedData(landlordEntity.CompanyName)
                                         : "NA";

                    if (member.companyName.ToLower() == "realty mark llc")
                    {
                        member.companyName = "Realty Mark LLC";
                    }
                }

                return member;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> GetMemberDetailsForLandingPage FAILED - [MemberId: " + memberId + "], [Exception: " + ex + "]");
            }

            return new MemberDto();
        }


        [HttpGet]
        [ActionName("UpdateMemberProfile")]
        public genericResponse UpdateMemberProfile(string memId, string fname, string lname, string email, string phone, string address, string zip, string dob, string ssn, string fngprnt, string ip, string pw)
        {
            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial";

            try
            {
                Logger.Info("Service Controller -> UpdateMemberProfile Initiated - [MemberId: " + memId + "]");

                Member member = null;

                var id = Utility.ConvertToGuid(memId);

                var mda = new MembersDataAccess();
                member = mda.GetMemberByGuid(id);

                if (member != null)
                {
                    try
                    {
                        #region Encrypt each piece of data

                        member.FirstName = CommonHelper.GetEncryptedData(fname.Split(' ')[0]);

                        if (fname.IndexOf(' ') > 0)
                        {
                            member.LastName = CommonHelper.GetEncryptedData(fname.Split(' ')[1]);
                        }
                        if (!String.IsNullOrEmpty(pw) &&
                            member.Password != null && member.Password == "")
                        {
                            member.Password = CommonHelper.GetEncryptedData(CommonHelper.GetDecryptedData(pw)
                                                          .Replace(" ", "+"));
                        }
                        //if (!String.IsNullOrEmpty())
                        //{
                        //    member.SecondaryEmail = CommonHelper.GetEncryptedData(profileInput.SecondaryMail);
                        //}
                        //if (!String.IsNullOrEmpty(profileInput.RecoveryMail))
                        //{
                        //    member.RecoveryEmail = CommonHelper.GetEncryptedData(profileInput.RecoveryMail);
                        //}
                        //if (!String.IsNullOrEmpty(profileInput.FacebookAcctLogin))
                        //{
                        //    member.FacebookAccountLogin = profileInput.FacebookAcctLogin;
                        //}
                        if (!String.IsNullOrEmpty(address))
                        {
                            member.Address = CommonHelper.GetEncryptedData(address);
                        }
                        //if (!String.IsNullOrEmpty(profileInput.City))
                        //{
                        //    member.City = CommonHelper.GetEncryptedData(profileInput.City);
                        //}
                        //if (!String.IsNullOrEmpty(profileInput.State))
                        //{
                        //    member.State = CommonHelper.GetEncryptedData(profileInput.State);
                        //}
                        if (!String.IsNullOrEmpty(zip))
                        {
                            member.Zipcode = CommonHelper.GetEncryptedData(zip);
                        }
                        //if (!String.IsNullOrEmpty(profileInput.Country))
                        //{
                        //    member.Country = profileInput.Country;
                        //}
                        //if (!string.IsNullOrEmpty(profileInput.TimeZoneKey))
                        //{
                        //    member.TimeZoneKey = CommonHelper.GetEncryptedData(profileInput.TimeZoneKey);
                        //}
                        if (!String.IsNullOrEmpty(dob))
                        {
                            DateTime dobDateTime;

                            if (DateTime.TryParse(dob, out dobDateTime))
                            {
                                member.DateOfBirth = dobDateTime;
                            }
                            else
                            {
                                res.note = res.note + "[Invalid DOB passed.]";
                            }
                        }
                        if (!String.IsNullOrEmpty(ssn))
                        {
                            member.SSN = CommonHelper.GetEncryptedData(ssn);
                        }
                        if (!String.IsNullOrEmpty(fngprnt))
                        {
                            member.UDID1 = fngprnt;
                        }

                        #endregion Encrypt each piece of data

                        //Logger.LogDebugMessage("Service Layer -> UpdateMemberProfile - contactNumber (sent from app): [" + contactNumber + "]");
                        //Logger.LogDebugMessage("Service Layer -> UpdateMemberProfile - member.ContactNumber (from DB): [" + member.ContactNumber + "]");

                        phone = CommonHelper.RemovePhoneNumberFormatting(phone);

                        if (!String.IsNullOrEmpty(phone) &&
                            (String.IsNullOrEmpty(member.ContactNumber) ||
                             CommonHelper.RemovePhoneNumberFormatting(member.ContactNumber) != phone))
                        {
                            if (!mda.IsPhoneNumberAlreadyRegistered(phone))
                            {
                                member.ContactNumber = phone;
                                member.IsVerifiedPhone = false;

                                #region Sending SMS Verificaion

                                try
                                {
                                    fname = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(member.FirstName));
                                    string MessageBody = "Hi " + fname + ", This is Nooch - just need to verify this is your phone number. Please reply 'Go' to confirm your phone number.";
                                    string SMSresult = Utility.SendSMS(phone, MessageBody);

                                    Logger.Info("Service Controller -> UpdateMemberProfile -> SMS Verification sent to [" + phone + "] successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Service Controller -> UpdateMemberProfile -> SMS Verification NOT sent to [" +
                                        phone + "], Exception: [" + ex.Message + "]");
                                }

                                #endregion Sending SMS Verificaion
                            }
                            else
                            {
                                res.note = "Phone number was already registered with Nooch: " + CommonHelper.FormatPhoneNumber(phone);
                            }
                        }

                        if (!String.IsNullOrEmpty(ip))
                        {
                            try
                            {
                                CommonHelper.UpdateMemberIPAddressAndDeviceId(memId, ip, "");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Service Controller -> UpdateMemberProfile - FAILED to save IP Address - MemberID: [" + memId +
                                                       "], IP: [" + ip + "], Exception: [" + ex.Message + "]");
                            }
                        }

                        member.DateModified = DateTime.Now;
                        // CLIFF (7/30/15): We used to set the Validated Date here automatically when the user completed their profile.
                        //                  But now we also need users to send SSN and DoB, so will set the Validated Date in SaveMemberSSN()
                        // member.ValidatedDate = DateTime.Now;

                        Logger.Info("Servie Controller -> UpdateMemberProfile - About to save to DB...");

                        int saveToDb = mda.UpdateMember(member);


                        Logger.Info("Servie Controller -> UpdateMemberProfile - Just saved to DB - 'saveToDb': [" + saveToDb + "]");

                        if (saveToDb > 0)
                        {
                            Logger.Info("Servie Controller -> UpdateMemberProfile - Updated Successfully! - MemberID: [" + memId + "]");

                            res.success = true;
                            res.msg = "Your details have been updated successfully.";
                        }
                        else
                        {
                            Logger.Error("Service Controller -> UpdateMemberProfile FAILED - Error on updating record in DB - [MemberID: " + memId + "]");
                            res.msg = "Error on updating member's record in DB.";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Service Controller -> UpdateMemberProfile FAILED - [MemberID: " + memId + "], [Exception: " + ex.Message + "]");

                        res.msg = ex.InnerException.Message;
                    }
                }
                else
                {
                    Logger.Error("Service Controller -> UpdateMemberProfile FAILED - MemberID Not Found - [MemberID: " + memId + "]");
                    res.msg = "Member not found.";
                }

            }
            catch (Exception ex)
            {
                res.msg = "Service Controller exception -> " + ex.Message.ToString();
                Logger.Error("Service Controller -> UpdateMemberProfile FAILED - MemberId: [" + memId + "], Exception: [" + ex + "]");
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetTransactionDetailByIdForRequestPayPage")]
        public TransactionDto GetTransactionDetailByIdForRequestPayPage(string TransactionId)
        {
            try
            {
                Logger.Info("Service Controller -> GetTransactionDetailByIdForRequestPayPage - [Trans ID: " + TransactionId + "]");

                var tda = new TransactionsDataAccess();
                Transaction tr = tda.GetTransactionById(TransactionId);

                TransactionDto trans = new TransactionDto();
                trans.Amount = tr.Amount;

                trans.Memo = tr.Memo;

                // Recipient of the Request (user that will pay)
                // NOTE (Cliff 11/25/15): For requests to non-Nooch users, the Members & Members1 BOTH REFER TO THE EXISTING NOOCH USER WHO SENT THE REQUEST.
                trans.Name = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.FirstName)) + " " +
                             CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member.LastName));

                // Requester (user that will receive the money)
                trans.RecepientName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.FirstName)) + " " +
                                      CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(tr.Member1.LastName));

                Logger.Info("Service Controller -> GetTransactionDetailByIdForRequestPayPage - [trans.Name is: " + trans.Name + "]");
                Logger.Info("Service Controller -> GetTransactionDetailByIdForRequestPayPage - [trans.RecepientName is: " + trans.RecepientName + "]");

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
                    String.IsNullOrEmpty(tr.PhoneNumberInvited))
                {
                    // Request via EMAIL
                    trans.InvitationSentTo = CommonHelper.GetDecryptedData(tr.InvitationSentTo);
                }
                else if ((tr.IsPhoneInvitation == true || tr.InvitationSentTo == null) &&
                          !String.IsNullOrEmpty(tr.PhoneNumberInvited))
                {
                    // Request via PHONE
                    trans.InvitationSentTo = CommonHelper.GetDecryptedData(tr.PhoneNumberInvited);
                }
                else
                {
                    trans.InvitationSentTo = "";
                }


                // CLIFF (10/20/15): For requests sent to existing but'NonRegistered' users, adding this block to include that user's
                //                   Synapse bank name for displaying the confirmation prompt.
                #region Transactions to Existing But NonRegistered Users

                if (tr.IsPhoneInvitation != true &&
                    ((String.IsNullOrEmpty(tr.InvitationSentTo) || tr.InvitationSentTo == "J7xCdPdfLcTvoVrLWCi/zw==") //|| // A few transactions to existing but NonReg users had encyrpted blank spaces for this value
                    //(trans.TransactionType == "Rent" || tr.TransactionType != "EnOIzpmFFTEaAP16hm9Wsw==")) // "EnOIzpmFFTEaAP16hm9Wsw==" = "Rent"
                     ))
                {
                    Logger.Info("Service Controller -> GetTransactionDetailByIdForRequestPayPage CHECKPOINT 2 - This request is to an existing user - " +
                                           "About to get Synapse Bank info for Payor -> [trans.RecepientName is: " + trans.RecepientName + "]");

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
                Logger.Error("Service Controller -> GetTransactionDetailByIdForRequestPayPage EXCEPTION - [Trans ID: " + TransactionId + "], [Exception: " + ex + "]");

                return null;
            }
        }


        [HttpGet]
        [ActionName("CreateNonNoochUserPasswordForPhoneInvitations")]
        public StringResult CreateNonNoochUserPasswordForPhoneInvitations(string TransId, string password, string EmailId)
        {
            try
            {
                Logger.Info("Service Controller - CreateNonNoochUserPasswordForPhoneInvitations - [TransId: " + TransId + "]");

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


        // Surya's code (Jan-Mar 2016)
        [HttpPost]
        [ActionName("CreateNonNoochUserAccountAfterRejectMoney")]
        public StringResult CreateNonNoochUserAccountAfterRejectMoney(string TransId, string password, string EmailId, string UserName)
        {
            try
            {
                Logger.Info("Service Controller - CreateNonNoochUserAccountAfterRejectMoney - [TransId: " + TransId + "], [UserName: " + UserName + "]");

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
        [ActionName("RejectMoneyforNonNoochUser")]
        public StringResult RejectMoneyforNonNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service Controller - RejectMoneyforNonNoochUser - [TransactionId: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyforNonNoochUser(transactionId);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "Error in Service Layer!" };
        }


        [HttpGet]
        [ActionName("RejectMoneyRequestForNonNoochUser")]
        public StringResult RejectMoneyRequestForNonNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service Controller - RejectMoneyforNonNoochUser - [TransactionID: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyRequestForNonNoochUser(transactionId);

                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "Error in Service Layer (RejectMoneyRequestForNonNoochUser)" };
        }


        [HttpGet]
        [ActionName("RejectMoneyCommon")]
        public StringResult RejectMoneyCommon(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            try
            {
                Logger.Info("Service Controller -> RejectMoneyCommon Initiated - Transaction ID: [" + TransactionId + "], " +
                                       "TransType: [" + TransType + "], UserType: [" + UserType + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyCommon(TransactionId, UserType, LinkSource, TransType);
                //return new StringResult { Result = result };
                StringResult Re = new StringResult();
                Re.Result = result;

                return Re;
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }

            return new StringResult { Result = "Error in Service Layer (RejectMoneyCommon)" };
        }


        // made it post type beacuse access token might generate white spaces which can
        //be encoded to plus by web request, which will create problem for validating access token.
        [HttpPost]
        [ActionName("SaveMemberSSN")]
        public StringResult SaveMemberSSN(SaveMemberSSN_Input input)
        {
            if (CommonHelper.IsValidRequest(input.accessToken, input.memberId))
            {
                try
                {
                    Logger.Info("Service Controller - SaveMemberSSN - [MemberId: " + input.memberId + "]");
                    MembersDataAccess mda = new MembersDataAccess();
                    return new StringResult()
                    {
                        Result = mda.SaveMemberSSN(input.memberId, input.SSN)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - Operation FAILED: SaveMemberSSN - [MemberId: " + input.memberId + "]. Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                Logger.Error("Service Controller - Operation FAILED: SaveMemberSSN - memberId: [" + input.memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("SaveDOBForMember")]
        public StringResult SaveDOBForMember(SaveMemberDOB_Input input)
        {
            if (CommonHelper.IsValidRequest(input.accessToken, input.memberId))
            {
                try
                {
                    Logger.Info("Service Controller - SaveDOBForMember - [MemberId: " + input.memberId + "]");

                    MembersDataAccess mda = new MembersDataAccess();
                    return new StringResult()
                    {
                        Result = mda.SaveDOBForMember(input.memberId, input.DOB)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - SaveDOBForMember FAILED - [MemberId: " + input.memberId + "]. [Exception: " + ex + "]");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                Logger.Error("Service Controller - Operation FAILED: SaveDOBForMember - memberId: [" + input.memberId + "]. INVALID OAUTH 2 ACCESS.");
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("GetTransactionDetailById")]
        public TransactionDto GetTransactionDetailById(string TransactionId)
        {
            try
            {
                Logger.Info("Service Controller -> GetTransactionDetailById - [TransactionId: " + TransactionId + "]");

                var tda = new TransactionsDataAccess();
                Transaction tr = tda.GetTransactionById(TransactionId);
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
                    {
                        trans.PhoneNumberInvited =
                            CommonHelper.FormatPhoneNumber(CommonHelper.GetDecryptedData(tr.PhoneNumberInvited));
                    }
                    else
                    {
                        trans.PhoneNumberInvited = "";
                    }
                }
                else
                {
                    trans.PhoneNumberInvited = "";
                }

                trans.Amount = tr.Amount;
                return trans;
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
                return null;
            }
        }


        /************************************************/
        /***** ----  SYNAPSE-RELATED SERVICES  ---- *****/
        /************************************************/
        #region Synapse-Related Services

        [HttpGet]
        [ActionName("RegisterUserWithSynapseV3")]
        public synapseCreateUserV3Result_int RegisterUserWithSynapseV3(string memberId)
        {
            try
            {
                MembersDataAccess mda = new MembersDataAccess();
                // for testing
                synapseCreateUserV3Result_int res = mda.RegisterUserWithSynapseV3(memberId);
                //for live 
                //synapseCreateUserV3Result_int res = mda.RegisterUserWithSynapseV3(memberId, false);

                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> RegisterUserWithSynapseV3 FAILED. [Exception: " + ex.ToString() + "]");

                return null;
            }
        }


        [HttpGet]
        [ActionName("RegisterExistingUserWithSynapseV3")]
        public RegisterUserSynapseResultClassExt RegisterExistingUserWithSynapseV3(string transId, string memberId, string email, string phone, string fullname, string pw, string ssn, string dob, string address, string zip, string fngprnt, string ip)
        {
            Logger.Info("Service Controller -> RegisterExistingUserWithSynapseV3 Initiated - MemberID: [" + memberId + "], " +
                        "Name: [" + fullname + "], Email: [" + email + "]");

            try
            {
                MembersDataAccess mda = new MembersDataAccess();
                RegisterUserSynapseResultClassExt nc = new RegisterUserSynapseResultClassExt();
                synapseCreateUserV3Result_int res = mda.RegisterExistingUserWithSynapseV3(transId, memberId, email, phone, fullname, pw, ssn, dob, address, zip, fngprnt, ip);
                if (res.success == true)
                {
                    nc.access_token = res.oauth.oauth_key;
                    nc.expires_in = res.oauth.expires_in;
                    nc.reason = res.reason;
                    nc.refresh_token = res.oauth.refresh_token;
                    nc.success = res.success.ToString();
                    nc.user_id = res.user_id;
                    nc.username = res.user.logins[0].email;
                    nc.memberIdGenerated = res.memberIdGenerated;
                    nc.ssn_verify_status = res.ssn_verify_status;
                }
                else
                {
                    nc.reason = res.reason;
                    nc.success = res.success.ToString();
                }
                return nc;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> RegisterExistingUserWithSynapsev3 FAILED - [MemberID: " + memberId + "], [Name: " + fullname +
                             ", [Email of New User: " + email + "], [Exception: " + ex.ToString() + "]");
                return null;
            }
        }


        [HttpGet]
        [ActionName("RegisterNonNoochUserWithSynapse")]
        public RegisterUserSynapseResultClassExt RegisterNonNoochUserWithSynapse(string transId, string email, string phone, string fullname, string pw, string ssn, string dob, string address, string zip, string fngprnt, string ip)
        {
            try
            {
                MembersDataAccess mda = new MembersDataAccess();
                synapseCreateUserV3Result_int res = mda.RegisterNonNoochUserWithSynapseV3(transId, email, phone, fullname, pw, ssn, dob, address, zip, fngprnt, ip);

                RegisterUserSynapseResultClassExt nc = new RegisterUserSynapseResultClassExt();

                if (res.success == true)
                {
                    nc.access_token = res.oauth.oauth_key;
                    nc.expires_in = res.oauth.expires_in;
                    nc.reason = res.reason;
                    nc.refresh_token = res.oauth.refresh_token;
                    nc.success = res.success.ToString();
                    nc.user_id = res.user_id;
                    nc.username = res.user.logins[0].email;
                    nc.memberIdGenerated = res.memberIdGenerated;
                    nc.ssn_verify_status = res.ssn_verify_status;
                }
                else
                {
                    nc.reason = res.reason;
                    nc.success = res.success.ToString();
                }

                return nc;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> RegisterNonNoochUserWithSynapseV3 FAILED. [New Usr Name: " + fullname +
                                       "], Email of New User: [" + email + "], TransactionID: [" + transId +
                                       "], Exception: [" + ex.ToString() + "]");
                return null;
            }
        }


        [HttpPost]
        [ActionName("submitDocumentToSynapseV3")]
        public synapseV3GenericResponse submitDocumentToSynapseV3(SaveVerificationIdDocument DocumentDetails)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();

            try
            {
                Logger.Info("Service Controller - submitDocumentToSynapseV3 [MemberId: " + DocumentDetails.MemberId + "]");

                var mda = new MembersDataAccess();

                // Make URL from byte array...b/c submitDocumentToSynapseV3 expects url of image.
                string ImageUrlMade = "";

                if (DocumentDetails.Picture != null)
                {
                    // Make  image from bytes
                    string filename = HttpContext.Current.Server.MapPath("../../UploadedPhotos") + "/Photos/" +
                                      DocumentDetails.MemberId + ".png";
                    using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                    {
                        fs.Write(DocumentDetails.Picture, 0, (int)DocumentDetails.Picture.Length);
                    }
                    ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + DocumentDetails.MemberId + ".png";
                }
                else
                {
                    Guid memGuid = new Guid(DocumentDetails.MemberId);

                    Member memberObj = mda.GetMemberByGuid(memGuid);

                    if (!String.IsNullOrEmpty(memberObj.VerificationDocumentPath))
                    {
                        ImageUrlMade = memberObj.VerificationDocumentPath;
                    }
                    else
                    {
                        ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                    }
                }

                var mdaResult = mda.submitDocumentToSynapseV3(DocumentDetails.MemberId, ImageUrlMade);

                res.isSuccess = mdaResult.success;
                res.msg = mdaResult.message;

                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - submitDocumentToSynapseV3 FAILED - [userName: " + DocumentDetails.MemberId + "]. Exception: [" + ex + "]");

                throw new Exception("Server Error.");
            }
        }


        [HttpGet]
        [ActionName("SynapseV3AddNodeWithAccountNumberAndRoutingNumber")]
        public SynapseBankLoginV3_Response_Int SynapseV3AddNodeWithAccountNumberAndRoutingNumber(string MemberId, string bankNickName, string account_num, string routing_num, string accounttype, string accountclass)
        {
            Logger.Info("Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber Initiated - MemberId: [" + MemberId + "], bankNickName: [" + bankNickName + "], routing_num: [" + routing_num + "], accounttype: [" + accounttype + "], accountclass: [" + accountclass + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();
            res.Is_success = false;

            #region Check if all required data was passed

            if (String.IsNullOrEmpty(bankNickName) || String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(account_num) || String.IsNullOrEmpty(routing_num) || String.IsNullOrEmpty(accounttype) || String.IsNullOrEmpty(accountclass))
            {
                if (String.IsNullOrEmpty(MemberId))
                {
                    res.errorMsg = "Invalid data - need MemberID.";
                }
                else if (String.IsNullOrEmpty(bankNickName))
                {
                    res.errorMsg = "Invalid data - need bank account nick name.";
                }
                else if (String.IsNullOrEmpty(account_num))
                {
                    res.errorMsg = "Invalid data - need bank account number.";
                }
                else if (String.IsNullOrEmpty(routing_num))
                {
                    res.errorMsg = "Invalid data - need bank routing number.";
                }
                else if (String.IsNullOrEmpty(accounttype))
                {
                    res.errorMsg = "Invalid data - need bank account type.";
                }
                else if (String.IsNullOrEmpty(accountclass))
                {
                    res.errorMsg = "Invalid data - need bank account class.";
                }
                else
                {
                    res.errorMsg = "Invalid data - please try again.";
                }

                Logger.Error("Service Controller -> SynapseV3AddNodeWithAccountNumberAndRoutingNumber ABORTING: Invalid data sent for: [" + MemberId + "].");

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
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE ERROR, but Member NOT FOUND.");

                    res.errorMsg = "Member not found.";
                    return res;
                }

                #endregion Get the Member's Details

                #region Check Mermber's Status And Phone

                // Checks on user account: is phone verified? Is user's status = 'Active'?

                if (noochMember.Status != "Active" &&
                    noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE Attempted, but Member is Not 'Active' but [" + noochMember.Status + "] for MemberId: [" + MemberId + "]");

                    res.errorMsg = "User status is not active but, " + noochMember.Status;
                    return res;
                }

                if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                     noochMember.Status != "NonRegistered" && noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE Attempted, but Member's Phone is Not Verified. MemberId: [" + MemberId + "]");

                    res.errorMsg = "User phone is not verified";
                    return res;
                }

                #endregion Check Mermber's Status And Phone

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
                        Logger.Info("Service Controller -> SynapseV3 ADD NODE ERROR: Could not create Synapse User Record for: [" + MemberId + "].");
                    }
                }

                // Check again if it's still null (which it shouldn't be because we just created a new Synapse user above if it was null.
                if (createSynapseUserDetails == null)
                {
                    Logger.Error("Service Controller -> SynapseV3 ADD NODE ERROR: No Synapse OAuth code found in Nooch DB for: [" + MemberId + "].");

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

                bankloginParameters.login = login;
                bankloginParameters.user = user;

                SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node node = new SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node();
                node.type = "ACH-US";

                SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo nodeInfo = new SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo();
                nodeInfo._class = accountclass;
                nodeInfo.routing_num = routing_num;
                nodeInfo.type = accounttype;
                nodeInfo.nickname = bankNickName;
                nodeInfo.account_num = account_num;


                node.info = nodeInfo;

                SynapseBankLoginV3_Input_extra extra = new SynapseBankLoginV3_Input_extra();
                extra.supp_id = "";

                node.extra = extra;
                bankloginParameters.node = node;

                string UrlToHit = "";
                UrlToHit = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/node/add" : "https://synapsepay.com/api/v3/node/add";


                #endregion Setup Call To SynapseV3 /node/add


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
                        #region Marking Any Existing Synapse Bank Login Entries as Deleted


                        var memberLoginResultsCollection = CommonHelper.GetSynapseBankLoginResulList(id.ToString());

                        foreach (SynapseBankLoginResult v in memberLoginResultsCollection)
                        {
                            v.IsDeleted = true;
                            _dbContext.SaveChanges();
                        }

                        #endregion Marking Any Existing Synapse Bank Login Entries as Deleted


                        RootBankObject rootBankObj = new RootBankObject();
                        rootBankObj.success = true;

                        #region Save New Record In SynapseBankLoginResults

                        try
                        {
                            // Preparing to save results in SynapseBankLoginResults DB table
                            SynapseBankLoginResult sbr = new SynapseBankLoginResult();

                            sbr.MemberId = id;
                            sbr.IsSuccess = true;
                            sbr.dateCreated = DateTime.Now;
                            sbr.IsDeleted = false;
                            sbr.IsCodeBasedAuth = false;  // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                            sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                            sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString()); // CLIFF (8/21/15): Not sure if this syntax is correct


                            if (bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"] != null ||
                                bankLoginRespFromSynapse["nodes"][0]["allowed"] == null)
                            {
                                #region MFA was returned

                                // Set final values for storing in Nooch DB
                                sbr.IsMfa = true;
                                sbr.IsQuestionBasedAuth = true;
                                sbr.mfaQuestion = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString().Trim();
                                sbr.AuthType = "questions";


                                // Set final values for returning this function
                                res.Is_MFA = true;
                                res.errorMsg = "OK";
                                res.mfaMessage = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString().Trim();

                                nodes[] nodesarray = new nodes[1];

                                _id idd = new _id();
                                idd.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString();
                                // Cliff: Not sure this syntax is right.   --Malkit: me neither... will check this when I get any such response from web sevice.

                                nodes nodenew = new nodes();
                                nodenew._id = idd;
                                nodenew.allowed = bankLoginRespFromSynapse["nodes"][0]["allowed"] != null ?
                                                  bankLoginRespFromSynapse["nodes"][0]["allowed"].ToString() :
                                                  null;
                                nodenew.extra = new extra();
                                nodenew.extra.mfa = new extra_mfa();
                                nodenew.extra.mfa.message = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString().Trim();

                                rootBankObj.nodes = nodesarray;

                                res.SynapseNodesList = rootBankObj;

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
                                _dbContext.Entry(sbr).Reload();

                                // Return if MFA, otherwise continue on and parse the banks
                                if (res.Is_MFA)
                                {
                                    Logger.Info("Service Controller -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - Got MFA from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    res.Is_success = true;
                                    return res;
                                }

                                Logger.Info("Service Controller -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - NO MFA found - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");
                            }
                            else
                            {
                                Logger.Error("Service Controller -> SynapseV3AddNode FAILURE - Could not save record in SynapseBankLoginResults Table - ABORTING - [MemberID: " + MemberId + "]");

                                res.errorMsg = "Failed to save entry in BankLoginResults table (inner)";
                                return res;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Service Controller -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
                                                   MemberId + "], [Exception: " + ex + "]");

                            res.errorMsg = "Got exception - Failed to save entry in BankLoginResults table";
                            return res;
                        }

                        #endregion Save New Record In SynapseBankLoginResults


                        #region No MFA response returned

                        // MFA is NOT required this time

                        // Array[] of banks ("nodes") expected here
                        // this old sturcture is unable to parse synapse V3 response  add new AddNodeV3BanksListResult -- to parse
                        // SynapseV3BanksListClassint allNodesParsedResult = JsonConvert.DeserializeObject<SynapseV3BanksListClassint>(content);

                        RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                        if (allNodesParsedResult != null)
                        {
                            res.Is_MFA = false;
                            res.SynapseNodesList = allNodesParsedResult;

                            Logger.Info("Service Controller -> SynapseV3AddNode - No MFA - SUCCESSFUL, Now saving all banks found in Bank Array (n = " +
                                                   allNodesParsedResult.nodes.Length + ") for: [" + MemberId + "]");

                            #region Save Each Bank In Nodes Array From Synapse

                            short numOfBanksSavedSuccessfully = 0;

                            // saving these banks ("nodes) in DB, later one of these banks will be set as default bank
                            foreach (nodes n in allNodesParsedResult.nodes)
                            {
                                try
                                {
                                    SynapseBanksOfMember sbm = new SynapseBanksOfMember();

                                    sbm.AddedOn = DateTime.Now;
                                    sbm.IsDefault = false;
                                    sbm.MemberId = Utility.ConvertToGuid(MemberId);

                                    // Holdovers from v3
                                    sbm.account_number_string = !String.IsNullOrEmpty(n.info.account_num) ? CommonHelper.GetEncryptedData(n.info.account_num) : null;
                                    sbm.bank_name = !String.IsNullOrEmpty(n.info.bank_name) ? CommonHelper.GetEncryptedData(n.info.bank_name) : null;
                                    sbm.name_on_account = !String.IsNullOrEmpty(n.info.name_on_account) ? CommonHelper.GetEncryptedData(n.info.name_on_account) : null;
                                    sbm.nickname = !String.IsNullOrEmpty(n.info.nickname) ? CommonHelper.GetEncryptedData(n.info.nickname) : null;
                                    sbm.routing_number_string = !String.IsNullOrEmpty(n.info.routing_num) ? CommonHelper.GetEncryptedData(n.info.routing_num) : null;
                                    sbm.is_active = (n.is_active != null) ? n.is_active : false;
                                    sbm.Status = "Not Verified";
                                    // CLIFF (10/11/15): We were using this "bankid" to identify the bank in other places.  Now we need to use oid (below) instead.
                                    // sbm.bankid = !String.IsNullOrEmpty(n._id.oid) ? n._id.oid : null;
                                    // These 2 values were *int* IN v3, but now both are strings...
                                    //sbm.account_class = v.account_class;
                                    //sbm.account_type = v.type_synapse;

                                    // Just For Nooch's Internal Use
                                    sbm.mfa_verifed = false;

                                    // New in V3
                                    sbm.oid = !String.IsNullOrEmpty(n._id.oid) ? n._id.oid : null;
                                    sbm.allowed = !String.IsNullOrEmpty(n.allowed) ? n.allowed : "UNKNOWN";
                                    sbm.@class = !String.IsNullOrEmpty(n.info._class) ? n.info._class : "UNKNOWN";
                                    sbm.supp_id = !String.IsNullOrEmpty(n.extra.supp_id) ? n.extra.supp_id : null;
                                    sbm.type_bank = !String.IsNullOrEmpty(n.info.type) ? n.info.type : "UNKNOWN";
                                    sbm.type_synapse = "ACH-US";


                                    _dbContext.SynapseBanksOfMembers.Add(sbm);
                                    int addBankToDB = _dbContext.SaveChanges();

                                    if (addBankToDB == 1)
                                    {
                                        _dbContext.Entry(sbm).Reload();
                                        Logger.Info("Service Controller -> SynapseV3AddNode -SUCCESSFULLY Added Bank to DB - [MemberID: " + MemberId + "]");

                                        numOfBanksSavedSuccessfully += 1;
                                    }
                                    else
                                    {
                                        Logger.Error("Service Controller -> SynapseV3AddNode - Failed to save new BANK in SynapseBanksOfMembers Table in DB - [MemberID: " + MemberId + "]");

                                        numOfBanksSavedSuccessfully -= 1;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    res.errorMsg = "Error occured while saving banks from Synapse.";
                                    Logger.Error("Service Controller -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
                                                   MemberId + "], [Exception: " + ex + "]");
                                }
                            }

                            if (numOfBanksSavedSuccessfully > 0)
                            {
                                res.errorMsg = "OK";
                                res.Is_success = true;
                            }
                            else
                            {
                                Logger.Error("Service Controller -> SynapseV3AddNode - No banks were saved in DB - [MemberID: " + MemberId + "]");
                                res.errorMsg = "No banks saved in DB";
                            }

                            #endregion Save Each Bank In Nodes Array From Synapse
                        }
                        else
                        {
                            Logger.Info("Service Controller -> SynapseV3 ADD NODE (No MFA) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]");

                            res.Is_MFA = false;
                            res.errorMsg = "Error occured while parsing banks list.";
                        }

                        return res;

                        #endregion No MFA response returned
                    }
                    else
                    {
                        // Synapse response for 'success' was not true
                        Logger.Error("Service Controller -> SynapseV3AddNode ERROR - Synapse response for 'success' was not true - [MemberID: " + MemberId + "]");
                        res.errorMsg = "Synapse response for success was not true";
                    }
                }
                catch (WebException we)
                {
                    #region Bank Login Catch

                    var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                    Logger.Info("Service Controller -> SynapseV3 ADD NODE FAILED. WebException was: [" + we.ToString() + "], and errorCode: [" + errorCode.ToString() + "]");

                    res.Is_success = false;
                    res.Is_MFA = false;

                    if (errorCode != null) //.ToString().ToLower() == "badrequest" || errorCode.ToString().ToLower() == "unauthorized")
                    {
                        var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(resp);

                        JToken reason = jsonFromSynapse["reason"];

                        if (reason != null)
                        {
                            Logger.Info("Service Controller -> SynapseBankLoginRequest FAILED. Synapse REASON was: [" + reason + "]");
                            // CLIFF: This was jsonfromsynapse["message"] but I changed to "reason" based on Synapse docs... is that right?
                            // Malkit: No, when I debugged I found response was "message" instead of "reason" I think they are sending 2 different things so instead of modifying this I am adding another case below
                            res.errorMsg = jsonFromSynapse["reason"].ToString();
                        }

                        JToken message = jsonFromSynapse["message"];

                        if (message != null)
                        {
                            Logger.Info("Service Controller -> SynapseBankLoginRequest FAILED. Synapse MESSAGE was: [" + message + "]");
                            res.errorMsg = jsonFromSynapse["message"].ToString();
                        }
                    }
                    else
                    {
                        Logger.Info("Service Controller -> SynapseBankLoginRequest FAILED. Synapse response did not include a 'reason' or 'message'.");
                        res.errorMsg = "Error #6553 - Sorry this is not more helpful :-(";
                    }

                    return res;

                    #endregion Bank Login Catch
                }

                #endregion Call SynapseV3 Add Node API

            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> SynapseV3AddNode FAILED - OUTER EXCEPTION - [MemberID: " + MemberId + "], [Exception: " + ex + "]");
                res.errorMsg = "Service Controller Outer Exception";
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
        [ActionName("SynapseV3AddNode")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3AddNode(string MemberId, string BnkName, string BnkUserName, string BnkPw)
        {
            Logger.Info("Service Controller -> SynapseV3AddNode Initiated - MemberId: [" + MemberId + "], BankName: [" + BnkName + "]");

            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();
            res.Is_success = false;

            #region Check if all required data was passed

            if (String.IsNullOrEmpty(BnkName) || String.IsNullOrEmpty(MemberId) ||
                String.IsNullOrEmpty(BnkUserName) || String.IsNullOrEmpty(BnkPw))
            {
                if (String.IsNullOrEmpty(MemberId))
                {
                    res.errorMsg = "Invalid data - need MemberID.";
                }
                else if (String.IsNullOrEmpty(BnkName))
                {
                    res.errorMsg = "Invalid data - need bank name.";
                }
                else if (String.IsNullOrEmpty(BnkUserName))
                {
                    res.errorMsg = "Invalid data - need bank username.";
                }
                else if (String.IsNullOrEmpty(BnkPw))
                {
                    res.errorMsg = "Invalid data - need bank password.";
                }
                else
                {
                    res.errorMsg = "Invalid data - please try again.";
                }

                Logger.Error("Service Controller -> SynapseV3AddNode ABORTING: Invalid data sent for: [" + MemberId + "].");

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
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE ERROR, but Member NOT FOUND.");

                    res.errorMsg = "Member not found.";
                    return res;
                }

                #endregion Get the Member's Details

                #region Check Mermber's Status And Phone

                // Checks on user account: is phone verified? Is user's status = 'Active'?

                if (noochMember.Status != "Active" &&
                    noochMember.Status != "NonRegistered" &&
                    noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE Attempted, but Member is Not 'Active' but [" + noochMember.Status + "] for MemberId: [" + MemberId + "]");

                    res.errorMsg = "User status is not active but, " + noochMember.Status;
                    return res;
                }

                if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                     noochMember.Status != "NonRegistered" && noochMember.Type != "Personal - Browser")
                {
                    Logger.Info("Service Controller -> SynapseV3 ADD NODE Attempted, but Member's Phone is Not Verified. MemberId: [" + MemberId + "]");

                    res.errorMsg = "User phone is not verified";
                    return res;
                }

                #endregion Check Mermber's Status And Phone

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
                        Logger.Info("Service Controller -> SynapseV3 ADD NODE ERROR: Could not create Synapse User Record for: [" + MemberId + "].");
                    }
                }

                // Check again if it's still null (which it shouldn't be because we just created a new Synapse user above if it was null.
                if (createSynapseUserDetails == null)
                {
                    Logger.Error("Service Controller -> SynapseV3 ADD NODE ERROR: No Synapse OAuth code found in Nooch DB for: [" + MemberId + "].");

                    res.errorMsg = "No Authentication code found for given user.";
                    return res;
                }

                #endregion Get Synapse Account Credentials


                // We have Synapse authentication token

                #region Setup Call To SynapseV3 /node/add

                SynapseBankLoginv3_Input bankloginParameters = new SynapseBankLoginv3_Input();

                SynapseV3Input_login login = new SynapseV3Input_login();
                login.oauth_key = CommonHelper.GetDecryptedData(createSynapseUserDetails.access_token);

                SynapseV3Input_user user = new SynapseV3Input_user();
                user.fingerprint = noochMember.UDID1;

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
                        #region Marking Any Existing Synapse Bank Login Entries as Deleted

                        var removeExistingSynapseBankLoginResult = CommonHelper.RemoveSynapseBankLoginResultsForGivenMemberId(id.ToString());


                        //var memberLoginResultsCollection = CommonHelper.GetSynapseBankLoginResulList(id.ToString());

                        //foreach (SynapseBankLoginResult v in memberLoginResultsCollection)
                        //{


                        //    _dbContext.Set(v.GetType()).Add(v); 


                        //    v.IsDeleted = true;
                        //    _dbContext.SaveChanges();
                        //}

                        #endregion Marking Any Existing Synapse Bank Login Entries as Deleted


                        RootBankObject rootBankObj = new RootBankObject();
                        rootBankObj.success = true;

                        #region Save New Record In SynapseBankLoginResults

                        try
                        {
                            // Preparing to save results in SynapseBankLoginResults DB table
                            SynapseBankLoginResult sbr = new SynapseBankLoginResult();

                            sbr.MemberId = id;
                            sbr.IsSuccess = true;
                            sbr.dateCreated = DateTime.Now;
                            sbr.IsDeleted = false;
                            sbr.IsCodeBasedAuth = false;  // NO MORE CODE-BASED WITH SYNAPSE V3, EVERY MFA IS THE SAME NOW
                            sbr.mfaMessage = null; // For Code-Based MFA - NOT USED ANYMORE, SHOULD REMOVE FROM DB
                            sbr.BankAccessToken = CommonHelper.GetEncryptedData(bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString()); // CLIFF (8/21/15): Not sure if this syntax is correct
                            //res.bankMFA = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString();


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

                                nodes[] nodesarray = new nodes[1];

                                _id idd = new _id();
                                idd.oid = bankLoginRespFromSynapse["nodes"][0]["_id"]["$oid"].ToString();
                                // Cliff: Not sure this syntax is right.
                                // Malkit: me neither... will check this when I get any such response from web sevice.
                                // Cliff (5/8/16): are we sure this is right now?

                                nodes nodenew = new nodes();
                                nodenew._id = idd;
                                nodenew.allowed = bankLoginRespFromSynapse["nodes"][0]["allowed"] != null ?
                                                  bankLoginRespFromSynapse["nodes"][0]["allowed"].ToString() :
                                                  null;
                                nodenew.extra = new extra();
                                nodenew.extra.mfa = new extra_mfa();
                                nodenew.extra.mfa.message = bankLoginRespFromSynapse["nodes"][0]["extra"]["mfa"]["message"].ToString().Trim();

                                // CLIFF (5/8/16): what happens to 'nodenew'? It never goes anywhere or is returned anywhere...

                                rootBankObj.nodes = nodesarray;

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
                                    Logger.Info("Service Controller -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - Got MFA from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                    res.Is_success = true;
                                    return res;
                                }

                                Logger.Info("Service Controller -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - NO MFA found - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");
                            }
                            else
                            {
                                Logger.Error("Service Controller -> SynapseV3AddNode FAILURE - Could not save record in SynapseBankLoginResults Table - ABORTING - [MemberID: " + MemberId + "]");

                                res.errorMsg = "Failed to save entry in BankLoginResults table (inner)";
                                return res;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Service Controller -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
                                          MemberId + "], [Exception: " + ex + "]");

                            res.errorMsg = "Got exception - Failed to save entry in BankLoginResults table";
                            return res;
                        }

                        #endregion Save New Record In SynapseBankLoginResults


                        #region No MFA response returned

                        // MFA is NOT required this time

                        // Array[] of banks ("nodes") expected here
                        // this old sturcture is unable to parse synapse V3 response  add new AddNodeV3BanksListResult -- to parse
                        //SynapseV3BanksListClassint allNodesParsedResult = JsonConvert.DeserializeObject<SynapseV3BanksListClassint>(content);

                        RootBankObject allNodesParsedResult = JsonConvert.DeserializeObject<RootBankObject>(content);

                        if (allNodesParsedResult != null)
                        {
                            res.Is_MFA = false;

                            SynapseNodesListClass nodesList = new SynapseNodesListClass();
                            List<SynapseIndividualNodeClass> bankslistextint = new List<SynapseIndividualNodeClass>();

                            foreach (nodes bank in allNodesParsedResult.nodes)
                            {
                                SynapseIndividualNodeClass b = new SynapseIndividualNodeClass();
                                b.account_class = bank.info._class;
                                b.bank_name = bank.info.bank_name;
                                //b.date = bank.date;
                                b.oid = bank._id.oid;
                                b.is_active = bank.is_active;
                                //b.is_verified = bank.is_verified;
                                //b.mfa_verifed = bank.mfa_verifed;
                                b.name_on_account = bank.info.name_on_account;
                                b.nickname = bank.info.nickname;
                                b.account_num = (bank.info.account_num);


                                bankslistextint.Add(b);
                            }
                            nodesList.nodes = bankslistextint;
                            nodesList.success = true;

                            res.SynapseNodesList = nodesList;
                            Logger.Info("Service Controller -> SynapseV3AddNode - No MFA - SUCCESSFUL, Now saving all banks found in Bank Array (n = " +
                                                   allNodesParsedResult.nodes.Length + ") for: [" + MemberId + "]");

                            #region Save Each Bank In Nodes Array From Synapse

                            short numOfBanksSavedSuccessfully = 0;

                            // saving these banks ("nodes) in DB, later one of these banks will be set as default bank
                            foreach (nodes n in allNodesParsedResult.nodes)
                            {
                                try
                                {
                                    SynapseBanksOfMember sbm = new SynapseBanksOfMember();

                                    sbm.AddedOn = DateTime.Now;
                                    sbm.IsDefault = false;
                                    sbm.MemberId = Utility.ConvertToGuid(MemberId);

                                    // Holdovers from v3
                                    sbm.account_number_string = !String.IsNullOrEmpty(n.info.account_num) ? CommonHelper.GetEncryptedData(n.info.account_num) : null;
                                    sbm.bank_name = !String.IsNullOrEmpty(n.info.bank_name) ? CommonHelper.GetEncryptedData(n.info.bank_name) : null;
                                    sbm.name_on_account = !String.IsNullOrEmpty(n.info.name_on_account) ? CommonHelper.GetEncryptedData(n.info.name_on_account) : null;
                                    sbm.nickname = !String.IsNullOrEmpty(n.info.nickname) ? CommonHelper.GetEncryptedData(n.info.nickname) : null;
                                    sbm.routing_number_string = !String.IsNullOrEmpty(n.info.routing_num) ? CommonHelper.GetEncryptedData(n.info.routing_num) : null;
                                    sbm.is_active = (n.is_active != null) ? n.is_active : false;
                                    sbm.Status = "Not Verified";
                                    // CLIFF (10/11/15): We were using this "bankid" to identify the bank in other places.  Now we need to use oid (below) instead.
                                    // sbm.bankid = !String.IsNullOrEmpty(n._id.oid) ? n._id.oid : null;
                                    // These 2 values were *int* IN v3, but now both are strings...
                                    //sbm.account_class = v.account_class;
                                    //sbm.account_type = v.type_synapse;

                                    // Just For Nooch's Internal Use
                                    sbm.mfa_verifed = false;

                                    // New in V3
                                    sbm.oid = !String.IsNullOrEmpty(n._id.oid) ? CommonHelper.GetEncryptedData(n._id.oid) : null;
                                    sbm.allowed = !String.IsNullOrEmpty(n.allowed) ? n.allowed : "UNKNOWN";
                                    sbm.@class = !String.IsNullOrEmpty(n.info._class) ? n.info._class : "UNKNOWN";
                                    sbm.supp_id = !String.IsNullOrEmpty(n.extra.supp_id) ? n.extra.supp_id : null;
                                    sbm.type_bank = !String.IsNullOrEmpty(n.info.type) ? n.info.type : "UNKNOWN";
                                    sbm.type_synapse = "ACH-US";


                                    _dbContext.SynapseBanksOfMembers.Add(sbm);
                                    int addBankToDB = _dbContext.SaveChanges();
                                    _dbContext.Entry(sbm).Reload();


                                    if (addBankToDB == 1)
                                    {
                                        Logger.Info("Service Controller -> SynapseV3AddNode -SUCCESSFULLY Added Bank to DB - [MemberID: " + MemberId + "]");

                                        numOfBanksSavedSuccessfully += 1;
                                    }
                                    else
                                    {
                                        Logger.Error("Service Controller -> SynapseV3AddNode - Failed to save new BANK in SynapseBanksOfMembers Table in DB - [MemberID: " + MemberId + "]");

                                        numOfBanksSavedSuccessfully -= 1;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    res.errorMsg = "Error occured while saving banks from Synapse.";
                                    Logger.Error("Service Controller -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
                                                   MemberId + "], [Exception: " + ex + "]");
                                }
                            }

                            if (numOfBanksSavedSuccessfully > 0)
                            {
                                res.errorMsg = "OK";
                                res.Is_success = true;
                            }
                            else
                            {
                                Logger.Error("Service Controller -> SynapseV3AddNode - No banks were saved in DB - [MemberID: " + MemberId + "]");
                                res.errorMsg = "No banks saved in DB";
                            }

                            #endregion Save Each Bank In Nodes Array From Synapse
                        }
                        else
                        {
                            Logger.Info("Service Controller -> SynapseV3 ADD NODE (No MFA) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]");

                            res.Is_MFA = false;
                            res.errorMsg = "Error occured while parsing banks list.";
                        }

                        return res;

                        #endregion No MFA response returned
                    }
                    else
                    {
                        // Synapse response for 'success' was not true
                        Logger.Error("Service Controller -> SynapseV3AddNode ERROR - Synapse response for 'success' was not true - [MemberID: " + MemberId + "]");
                        res.errorMsg = "Synapse response for success was not true";
                    }
                }
                catch (WebException we)
                {
                    #region Bank Login Catch

                    var errorCode = ((HttpWebResponse)we.Response).StatusCode;

                    Logger.Info("MDA -> SynapseV3 ADD NODE FAILED. WebException was: [" + we.ToString() + "], and errorCode: [" + errorCode.ToString() + "]");

                    res.Is_success = false;
                    res.Is_MFA = false;

                    if (errorCode != null) //.ToString().ToLower() == "badrequest" || errorCode.ToString().ToLower() == "unauthorized")
                    {
                        var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(resp);

                        JToken reason = jsonFromSynapse["reason"];

                        if (reason != null)
                        {
                            Logger.Info("MDA -> SynapseBankLoginRequest FAILED. Synapse REASON was: [" + reason + "]");
                            // CLIFF: This was jsonfromsynapse["message"] but I changed to "reason" based on Synapse docs... is that right?
                            // Malkit: No, when I debugged I found response was "message" instead of "reason" I think they are sending 2 different things so instead of modifying this I am adding another case below
                            res.errorMsg = jsonFromSynapse["reason"].ToString();
                        }

                        JToken message = jsonFromSynapse["message"];

                        if (message != null)
                        {
                            Logger.Info("MDA -> SynapseBankLoginRequest FAILED. Synapse MESSAGE was: [" + message + "]");
                            res.errorMsg = jsonFromSynapse["message"].ToString();
                        }
                    }
                    else
                    {
                        Logger.Info("MDA -> SynapseBankLoginRequest FAILED. Synapse response did not include a 'reason' or 'message'.");
                        res.errorMsg = "Error #6553 - Sorry this is not more helpful :-(";
                    }

                    return res;

                    #endregion Bank Login Catch
                }

                #endregion Call SynapseV3 Add Node API

            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> SynapseV3AddNode FAILED - OUTER EXCEPTION - [MemberID: " + MemberId + "], [Exception: " + ex + "]");
                res.errorMsg = "Service Controller Outer Exception";
            }

            return res;
        }


        [HttpPost]
        [ActionName("SynapseV3MFABankVerify")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3MFABankVerify(SynapseV3VerifyNode_ServiceInput input)
        {
            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();

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
                    //b.date = bank.date;
                    b.oid = bank._id.oid;
                    b.is_active = bank.is_active;
                    //b.is_verified = bank.is_verified;
                    //b.mfa_verifed = bank.mfa_verifed;
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
                //res.Bank_Access_Token = mdaResult.Bank_Access_Token;

                if (!String.IsNullOrEmpty(mdaResult.mfaMessage))
                {
                    res.mfaQuestion = mdaResult.mfaMessage;
                    res.bankOid = mdaResult.SynapseNodesList.nodes[0]._id.oid;
                }
            }

            #endregion MFA Required

            return res;
        }


        [HttpPost]
        [ActionName("SynapseV3MFABankVerifyWithMicroDeposits")]
        public SynapseV3BankLoginResult_ServiceRes SynapseV3MFABankVerifyWithMicroDeposits(SynapseV3VerifyNodeWithMicroDeposits_ServiceInput input)
        {
            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();

            MembersDataAccess mda = new MembersDataAccess();

            SynapseBankLoginV3_Response_Int mdaResult = new SynapseBankLoginV3_Response_Int();
            mdaResult = mda.SynapseV3MFABankVerifyWithMicroDeposits(input.MemberId, input.BankName, input.microDespositOne, input.microDespositTwo, input.bankId);

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
                    //b.date = bank.date;
                    b.oid = bank._id.oid;
                    b.is_active = bank.is_active;
                    //b.is_verified = bank.is_verified;
                    //b.mfa_verifed = bank.mfa_verifed;
                    b.name_on_account = bank.info.name_on_account;
                    b.nickname = bank.info.nickname;

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
                //res.Bank_Access_Token = mdaResult.Bank_Access_Token;

                if (!String.IsNullOrEmpty(mdaResult.mfaMessage))
                {
                    res.mfaQuestion = mdaResult.mfaMessage;
                    res.bankOid = mdaResult.SynapseNodesList.nodes[0]._id.oid;
                }
            }

            #endregion MFA Required

            return res;
        }


        [HttpGet]
        [ActionName("GetSynapseBankAndUserDetails")]
        public SynapseDetailsClass GetSynapseBankAndUserDetails(string memberid)
        {
            SynapseDetailsClass res = new SynapseDetailsClass();

            try
            {
                Logger.Info("Service Controller - GetSynapseBankAndUserDetails Initiated - MemberId: [" + memberid + "]");


                var adaResult = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(memberid);

                res.wereBankDetailsFound = adaResult.wereBankDetailsFound;
                res.wereUserDetailsFound = adaResult.wereUserDetailsFound;

                Logger.Info("Service Controller - GetSynapseBankAndUserDetails Checkpoint #1!");

                res.AccountDetailsErrMessage = adaResult.AccountDetailsErrMessage;
                res.UserDetailsErrMessage = adaResult.UserDetailsErrMessage;

                res = adaResult;

                Logger.Info("Service Controller - GetSynapseBankAndUserDetails Checkpoint #2!");

                //res.UserDetails.access_token = adaResult.UserDetails.access_token;

                //Logger.LogDebugMessage("Service Layer - GetSynapseBankAndUserDetails Checkpoint #3!");

                //res.UserDetails.MemberId = adaResult.UserDetails.MemberId.ToString();
                //res.UserDetails.user_id = adaResult.UserDetails.user_id;
                //res.BankDetails.bankid = adaResult.BankDetails.bankid;
                //res.BankDetails.email = adaResult.BankDetails.email;
                //res.BankDetails.Status = adaResult.BankDetails.Status;
                //res.BankDetails.AddedOn = adaResult.BankDetails.AddedOn;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller - GetSynapseBankAndUserDetails FAILED - MemberId: [" + memberid +
                             "], Exception: [" + ex + "]");
            }

            Logger.Info("Service Controller - GetSynapseBankAndUserDetails FINISHED, ABOUT TO RETURN - MemberId: [" + memberid +
                        "], res.wereUserDetailsFound: [" + res.wereUserDetailsFound +
                        "], res.wereBankDetailsFound: [" + res.wereBankDetailsFound + "]");
            //"], UserDetails.access_token: [" + res.UserDetails.access_token +
            //"], BankDetails.bankid: [" + res.BankDetails.bankid + "]");

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

            Logger.Error("Service Controller -> CheckSynapseBankDetails End - [BankName: " + BankName + "],  [Message to return: " + res.Message + "]");

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
                    Logger.Info("Service Controller - RemoveSynapseV3BankAccount - MemberId: [" + memberId + "]");

                    var memdataAccess = new MembersDataAccess();

                    var mdaResult = memdataAccess.RemoveSynapseV3BankAccount(memberId);

                    res.isSuccess = mdaResult.success;
                    res.msg = mdaResult.message;

                    return res;
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - RemoveSynapseV3BankAccount FAILED - MemberId: [" + memberId + "]. Exception: [" + ex + "]");

                    res.msg = "Service layer catch exception";

                    Utility.ThrowFaultException(ex);
                }
            }
            else
            {
                res.msg = "Invalid OAuth 2 Access";
            }

            return res;
        }


        [HttpGet]
        [ActionName("GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit")]
        public TransactionDto GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit(string TransactionId, string MemberIdAfterSynapseAccountCreation, string TransactionType, string recipMemId)
        {
            try
            {
                Logger.Info("Service Controller - GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit Initiated - TransType: [" + TransactionType +
                                       "], TransId: [" + TransactionId + "],  MemberIdAfterSynapseAccountCreation: [" + MemberIdAfterSynapseAccountCreation +
                                       "], RecipientMemberID: [" + recipMemId + "]");

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

                    MembersDataAccess mda = new MembersDataAccess();

                    if (String.IsNullOrEmpty(recipMemId))
                    {
                        recipMemId = "";
                    }

                    string mdaRes = mda.GetTokensAndTransferMoneyToNewUser(TransactionId, MemberIdAfterSynapseAccountCreation, TransactionType, recipMemId);

                    trans.synapseTransResult = mdaRes;

                    return trans;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit FAILED - [Exception: " + ex + "]");
            }

            return null;
        }


        [HttpGet]
        [ActionName("getIdVerificationQuestionsV3")]
        public synapseIdVerificationQuestionsForDisplay getIdVerificationQuestionsV3(string memberid)
        {
            Logger.Info("Service Controller -> getIdVerificationQuestionsV3 Initiated - [MemberId: " + memberid + "]");
            synapseIdVerificationQuestionsForDisplay res = new synapseIdVerificationQuestionsForDisplay();
            res.memberId = memberid;

            try
            {
                var mda = new MembersDataAccess();
                res = mda.getIdVerificationQuestionsV3(memberid);
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> getVerificationQuestionsV2 FAILED. [Exception: " + ex.InnerException + "]");

                res.success = false;
                res.msg = "Service layer exception :-(";
            }

            return res;
        }


        [HttpGet]
        [ActionName("submitIdVerificationAswersV3")]
        public synapseV3GenericResponse submitIdVerificationAswersV3(string MemberId, string questionSetId, string quest1id, string quest2id, string quest3id, string quest4id, string quest5id, string answer1id, string answer2id, string answer3id, string answer4id, string answer5id)
        {
            Logger.Info("Service Controller -> submitIdVerificationAswersV2 Initiated - [MemberId: " + MemberId + "]");

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
                Logger.Error("Service Controller -> submitIdVerificationAswersV2 FAILED. [Exception: " + ex + "]");

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
                    Logger.Info("Service Controller - RemoveSynapseBankAccount - [MemberId: " + user.MemberID + "], [Bank ID: " + user.BankAccountId + "]");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.RemoveSynapseBankAccount(user.MemberID, user.BankAccountId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller Error- RemoveSynapseBankAccount - memberId: [" + user.MemberID + "] Error : [" + ex + " ].");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                return new StringResult() { Result = "Invalid Access Token." };
            }
        }

        #endregion Synapse-Related Services


        // Web related Services

        /// <summary>
        /// For updating a user's Synapse Bank status to 'Verified'. Currently called from a Member Details
        /// page in the Admin Dashboard and from the BankVerification.aspx.cs browser page.
        /// </summary>
        /// <param name="tokenId"></param>
        [HttpGet]
        [ActionName("VerifySynapseAccount")]
        public BoolResult VerifySynapseAccount(string tokenId)
        {
            Logger.Info("Service Controller -> VerifySynapseAccount Initiated - Bank TokenID: [" + tokenId + "]");

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
                Logger.Error("Service Controller -> VerifySynapseAccount FAILED - TokenID was null or empty! - TokenID: [" + tokenId + "]");
            }

            return new BoolResult();
        }

        [HttpGet]
        [ActionName("GetSynapseBankAccountDetails")]
        public SynapseAccoutDetailsInput GetSynapseBankAccountDetails(string memberId, string accessToken)
        {
            // Logger.LogDebugMessage("Service layer -> GetSynapseBankAccountDetails Initiated - memberId: [" + memberId + "]");

            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    var accountCollection = CommonHelper.GetSynapseBankAccountDetails(memberId);

                    if (accountCollection != null)
                    {
                        string appPath = Utility.GetValueFromConfig("ApplicationURL");

                        SynapseAccoutDetailsInput o = new SynapseAccoutDetailsInput();

                        o.BankName = CommonHelper.GetDecryptedData(accountCollection.bank_name);
                        o.BankNickName = CommonHelper.GetDecryptedData(accountCollection.nickname);
                        switch (o.BankName)
                        {
                            case "Ally":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/ally.png");
                                }
                                break;
                            case "Bank of America":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/bankofamerica.png");
                                }
                                break;
                            case "Wells Fargo":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/WellsFargo.png");
                                }
                                break;
                            case "Chase":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/chase.png");
                                }
                                break;
                            case "Citibank":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/citibank.png");
                                }
                                break;
                            case "TD Bank":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/td.png");
                                }
                                break;
                            case "Capital One 360":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/capone360.png");
                                }
                                break;
                            case "US Bank":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/usbank.png");
                                }
                                break;
                            case "PNC":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/pnc.png");
                                }
                                break;
                            case "SunTrust":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/suntrust.png");
                                }
                                break;
                            case "USAA":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/usaa.png");
                                }
                                break;

                            case "First Tennessee":
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/firsttennessee.png");
                                }
                                break;
                            default:
                                {
                                    o.BankImageURL = String.Concat(appPath, "Assets/Images/bankPictures/no.png");
                                }
                                break;
                        }
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


        [HttpGet]
        [ActionName("SaveMembersFBId")]
        public StringResult SaveMembersFBId(string MemberId, string MemberfaceBookId, string IsConnect)
        {
            try
            {
                //Logger.LogDebugMessage("Service layer -> SaveMembersFBId - MemberId: [" + MemberId + "]");

                return new StringResult { Result = CommonHelper.SaveMemberFBId(MemberId, MemberfaceBookId, IsConnect) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> SaveMembersFBId Error - MemberId: [" + MemberId + "] Error -> " + ex);
            }
            return new StringResult();
        }


        [HttpGet]
        [ActionName("GetMyDetails")]
        public MySettingsInput GetMyDetails(string memberId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Layer -> GetMyDetails Initiated - MemberId: [" + memberId + "]");

                    var myDetails = CommonHelper.GetMemberDetails(memberId);

                    //Check address, city, cell phone to check whether the profile is a valid profile or not
                    bool isvalidprofile = !string.IsNullOrEmpty(myDetails.Address) &&
                                          !string.IsNullOrEmpty(myDetails.City) &&
                                          !string.IsNullOrEmpty(myDetails.Zipcode) &&
                                          !string.IsNullOrEmpty(myDetails.ContactNumber) &&
                                          myDetails.IsVerifiedPhone == true &&
                                          !string.IsNullOrEmpty(myDetails.SSN) &&
                                          myDetails.DateOfBirth != null;

                    var settings = new MySettingsInput
                    {
                        UserName = myDetails.UserName,
                        FirstName = myDetails.FirstName,
                        LastName = myDetails.LastName,
                        Password = myDetails.Password,
                        ContactNumber = myDetails.ContactNumber,
                        SecondaryMail = myDetails.SecondaryEmail,
                        RecoveryMail = myDetails.RecoveryEmail,
                        ShowInSearch = Convert.ToBoolean(myDetails.ShowInSearch),
                        Address = myDetails.Address,
                        City = myDetails.City,
                        State = myDetails.State,
                        Zipcode = myDetails.Zipcode,
                        IsVerifiedPhone = myDetails.IsVerifiedPhone ?? false,
                        IsValidProfile = isvalidprofile,

                        //PinNumber = myDetails.PinNumber,  // Cliff: don't need to send this to the app
                        //AllowPushNotifications = Convert.ToBoolean(myDetails.AllowPushNotifications), // Don't need to send this to the app
                        //Photo = (myDetails.Photo == null) ? Utility.GetValueFromConfig("PhotoUrl") : myDetails.Photo, //CLIFF: this is already being sent in the GetMemberDetails service
                        //FacebookAcctLogin = myDetails.FacebookAccountLogin, //CLIFF: this is already being sent in the GetMemberDetails service
                        //UseFacebookPicture = Convert.ToBoolean(myDetails.UseFacebookPicture),
                        //Country = myDetails.Country,
                        //ClearTransactionHistory = Convert.ToBoolean(myDetails.ClearTransactionHistory),
                        //TimeZoneKey = CommonHelper.GetDecryptedData(myDetails.TimeZoneKey),
                        //IsBankVerified = bankVerified
                    };

                    return settings;
                }
                catch (Exception ex)
                {
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("MySettings")]
        public StringResult MySettings(MySettingsInput mySettings, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, mySettings.MemberId))
            {
                try
                {
                    Logger.Info("Service Controller -> MySettings Initiated - [MemberId: " + mySettings.MemberId + "]");

                    var mda = new MembersDataAccess();
                    string fileContent = null;
                    int contentLength = 0;
                    string fileExtension = null;

                    if (mySettings.AttachmentFile != null)
                    {
                        fileContent = mySettings.AttachmentFile.FileContent;
                        contentLength = mySettings.AttachmentFile.FileContent.Length;
                        fileExtension = mySettings.AttachmentFile.FileExtension;
                    }

                    return new StringResult
                    {
                        Result = mda.MySettings(mySettings.MemberId, mySettings.FirstName.ToLower(), mySettings.LastName.ToLower(),
                            mySettings.Password, mySettings.SecondaryMail, mySettings.RecoveryMail, mySettings.TertiaryMail,
                            mySettings.FacebookAcctLogin, mySettings.UseFacebookPicture, fileContent, contentLength, fileExtension,
                            mySettings.ContactNumber, mySettings.Address, mySettings.City, mySettings.State,
                            mySettings.Zipcode, mySettings.Country, mySettings.TimeZoneKey, mySettings.Picture, mySettings.ShowInSearch)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller -> MySettings FAILED - [MemberId: " + mySettings.MemberId + "], [Exception: " + ex + "]");
                    return new StringResult();
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
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
                    Logger.Info("Service Controller - ValidatePinNumber [memberId: " + memberId + "]");

                    return new StringResult { Result = CommonHelper.ValidatePinNumber(memberId, pinNo.Replace(" ", "+")) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - ValidatePinNumber FAILED [memberId: " + memberId + "]. Exception: [" + ex + "]");

                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("ValidatePinNumberToEnterForEnterForeground")]
      public  StringResult ValidatePinNumberToEnterForEnterForeground(string memberId, string pinNo, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Controller - ValidatePinNumberToEnterForEnterForeground [memberId: " + memberId + "]");

                    return new StringResult { Result = CommonHelper.ValidatePinNumberToEnterForEnterForeground(memberId, pinNo.Replace(" ", "+")) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller - ValidatePinNumberToEnterForEnterForeground FAILED [memberId: " + memberId + "]. Exception: [" + ex + "]");

                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("ResetPin")]
        public StringResult ResetPin(string memberId, string oldPin, string newPin, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Controller - ResetPin - MemberId: [" + memberId + "]");
                    var mda = new MembersDataAccess();
                    return new StringResult { Result = mda.ResetPin(memberId, oldPin, newPin) };
                }
                catch (Exception ex)
                {
                    Logger.Info("Service Controller - ResetPin - MemberId: [" + memberId + "]");
                    return new StringResult() { Result = "Server Error." };
                }
            }
            else
            {
                return new StringResult() { Result = "Invalid oAuth Access Token." };
            }
        }


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
                    Logger.Info("Service Controller - GetMemberNotificationSettings - memberId: [" + memberId + "]");
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
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller Error- GetMemberNotificationSettings - memberId: [" + memberId + "] Error : [" + ex + " ].");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
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
                    Logger.Info("Service Controller - MemberEmailNotificationSettings - memberId: [" + memberNotificationSettings.MemberId + "]");

                    return new StringResult
                    {
                        Result = new MembersDataAccess().MemberEmailNotificationSettings("",
                            memberNotificationSettings.MemberId, null, null,
                            (memberNotificationSettings.EmailTransferSent == "1") ? true : false, (memberNotificationSettings.EmailTransferReceived == "1") ? true : false, (memberNotificationSettings.EmailTransferAttemptFailure == "1") ? true : false,
                            (memberNotificationSettings.TransferUnclaimed == "1") ? true : false, (memberNotificationSettings.BankToNoochRequested == "1") ? true : false, (memberNotificationSettings.BankToNoochCompleted == "1") ? true : false,
                            (memberNotificationSettings.NoochToBankRequested == "1") ? true : false, (memberNotificationSettings.NoochToBankCompleted == "1") ? true : false, null,
                            null, null, null, null)
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Controller Error- MemberEmailNotificationSettings - memberId: [" + memberNotificationSettings.MemberId + "] Error : [" + ex + " ].");
                    throw new Exception("Server Error.");
                }

            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpPost]
        [ActionName("MemberPushNotificationSettings")]
        public StringResult MemberPushNotificationSettings(MemberNotificationsNewStringTypeSettings memberNotificationSettings, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberNotificationSettings.MemberId))
            {
                try
                {
                    Logger.Info("Service Controller - MemberPushNotificationSettings - [MemberId: " + memberNotificationSettings.MemberId + "]");
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
                    Logger.Error("Service Controller Error- MemberPushNotificationSettings - memberId: [" + memberNotificationSettings.MemberId + "] Error : [" + ex + " ].");
                    throw new Exception("Server Error.");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        [HttpGet]
        [ActionName("SetShowInSearch")]
        public StringResult SetShowInSearch(string memberId, bool search, string accessToken)
        {
            try
            {
                Logger.Info("Service Controller - SetShowInSearch Initiated - MemberId: [" + memberId + "]");
                var mda = new MembersDataAccess();
                return new StringResult { Result = mda.SetShowInSearch(memberId, search) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller Error- SetShowInSearch - memberId: [" + memberId + "] Error : [" + ex + " ].");
                throw new Exception("Server Error.");
            }
        }


        [HttpPost]
        [ActionName("MemberRegistration")]
        public StringResult MemberRegistration(MemberRegistrationInputDto MemberDetails)
        {
            try
            {
                Logger.Info("Service Controller - MemberRegistration Initiated - NEW USER'S INFO: Name: [" + MemberDetails.UserName +
                                       "], Email: [" + MemberDetails.UserName + "],  Type: [" + MemberDetails.type +
                                       "],  Invite Code: [" + MemberDetails.inviteCode + "], SendEmail: [" + MemberDetails.sendEmail + "], ");

                var mda = new MembersDataAccess();

                string type = String.IsNullOrEmpty(MemberDetails.type) ? "Personal" : MemberDetails.type;

                return new StringResult
                {
                    Result =
                        mda.MemberRegistration(MemberDetails.Picture, MemberDetails.UserName, MemberDetails.FirstName.ToLower(),
                                               MemberDetails.LastName.ToLower(), MemberDetails.PinNumber, MemberDetails.Password,
                                               MemberDetails.SecondaryMail, MemberDetails.RecoveryMail, MemberDetails.UdId,
                                               MemberDetails.friendRequestId, MemberDetails.invitedFriendFacebookId,
                                               MemberDetails.facebookAccountLogin, MemberDetails.inviteCode, MemberDetails.sendEmail, type,null,null,null,null,null)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Controller -> MemberRegistration FAILED: [Name: " + MemberDetails.UserName + "], Exception: [" + ex + "]");
                Utility.ThrowFaultException(ex);
            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("sendLandlordLeadEmailTemplate")]
        public StringResult sendLandlordLeadEmailTemplate(string template, string email, string firstName,
            string tenantFName, string tenantLName, string propAddress, string subject)
        {
            Logger.Info("Service Layer - sendEmailTemplate Initiated - Template: [" + template +
                                   "], Email: [" + email + "], First Name: [" + firstName + "], Subject: {" + subject + "]");

            StringResult res = new StringResult();

            if (String.IsNullOrEmpty(email))
            {
                res.Result = "Missing email address to send to!";
            }
            else if (String.IsNullOrEmpty(template))
            {
                res.Result = "Have an email, but missing a Template to send!";
            }
            else if (String.IsNullOrEmpty(tenantFName))
            {
                res.Result = "Missing a Tenant First Name!";
            }
            else if (String.IsNullOrEmpty(tenantLName))
            {
                res.Result = "Missing a Tenant Last Name!";
            }
            else if (String.IsNullOrEmpty(propAddress))
            {
                res.Result = "Missing a Property Address";
            }
            else
            {
                if (String.IsNullOrEmpty(subject) || subject.Length < 1)
                {
                    subject = " ";
                }
                else
                {
                    subject = CommonHelper.UppercaseFirst(subject);
                }

                if (String.IsNullOrEmpty(firstName) || firstName.Length < 1)
                {
                    firstName = " ";
                }
                else
                {
                    firstName = CommonHelper.UppercaseFirst(firstName);
                }

                try
                {
                    var tokens = new Dictionary<string, string>
                        {
                            {Constants.PLACEHOLDER_FIRST_NAME, firstName}, // Landlord's First Name
                            {"$TenantFName$", tenantFName},
                            {"$TenantLName$", tenantLName},
                            {"$PropAddress$", propAddress}
                        };

                    Utility.SendEmail(template,  "landlords@rentscene.com", email,
                                                null, subject, null, tokens, null, null, null);

                    res.Result = "Email Template [" + template + "] sent successfully to [" + email + "]";
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer - sendEmailTemplate FAILED - Exception: [" + ex.Message + "]");

                    res.Result = "Server exception!";

                }
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
            try
            {
                Logger.Info("Service Layer -> LoginWithFacebook [userEmail: " + userEmail + "],  [FB ID: " + FBId + "]");

                var mda = new MembersDataAccess();
                string cookie = mda.LoginwithFB(userEmail, FBId, rememberMeEnabled, lat, lng, udid, devicetoken);

                if (string.IsNullOrEmpty(cookie))
                {
                    cookie = "Authentication failed.";
                    return new StringResult { Result = "Invalid Login or Password" };
                }
                else if (cookie == "Temporarily_Blocked")
                {
                    return new StringResult { Result = "Temporarily_Blocked" };
                }
                else if (cookie == "FBID or EmailId not registered with Nooch")
                {
                    return new StringResult { Result = "FBID or EmailId not registered with Nooch" };
                }
                else if (cookie == "Suspended")
                {
                    return new StringResult { Result = "Suspended" };
                }
                else if (cookie == "Registered")
                {
                    
                    string state = GenerateAccessToken();
                    CommonHelper.UpdateAccessToken(userEmail, state);
                    return new StringResult { Result = state };
                    
                }
                else if (cookie == "Invalid user id or password.")
                {
                    return new StringResult { Result = "Invalid user id or password." };
                }
                else if (cookie == "The password you have entered is incorrect.")
                {
                    return new StringResult { Result = "The password you have entered is incorrect." };
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
                    return new StringResult { Result = state };
                }
                else
                {
                    return new StringResult { Result = cookie };
                }

            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> LoginWithFacebook FAILED - [userEmail: " + userEmail + "], [Exception: " + ex + "]");
                
            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("LogOutRequest")]
        public StringResult LogOutRequest(string accessToken, string memberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service Layer -> LogOutRequest - [MemberId: " + memberId + "]");
                    var memberDataAccess = new MembersDataAccess();
                    string cookie = memberDataAccess.LogOut(memberId);
                    if (string.IsNullOrEmpty(cookie))
                    {
                        cookie = "LogOut failed.";
                        return new StringResult { Result = "LogOut failed." };
                    }
                    else
                    {
                        return new StringResult { Result = "Success." };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer -> LogOutRequest FAILED - [MemberId: " + memberId + "], [Exception: " + ex + "]");
                    
                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
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
                Logger.Info("Service Layer - MemberRegistrationGET Initiated - NEW USER'S INFO: Name: [" + name +
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
                        if (namearray.Length == 2)
                        {
                            // For regular First & Last name: Charles Smith
                            LastName = namearray[1];
                        }
                        else if (namearray.Length == 3)
                        {
                            // For 3 names, could be a middle name or middle initial: Charles H. Smith or Charles Andrew Smith
                            LastName = namearray[2];
                        }
                        else
                        {
                            // For more than 3 names (some people have 2 or more middle names)
                            LastName = namearray[namearray.Length - 1];
                        }
                    }
                }

                #endregion Parse Name

                type = String.IsNullOrEmpty(type) ? "Personal - Browser" : CommonHelper.UppercaseFirst(type.ToLower());

                var password = !String.IsNullOrEmpty(pw) ? CommonHelper.GetEncryptedData(pw)
                                                         : CommonHelper.GetEncryptedData("jibb3r;jawn-alt");


                var mda = new MembersDataAccess();

                var mdaRes = mda.MemberRegistration(null, email, FirstName, LastName, "", password, email, email,
                                                    fngprnt, "", "", "", "BROWSER", "true", type,
                                                    phone, address, zip, ssn, dob);

                res.msg = mdaRes;

                if (mdaRes.IndexOf("Thanks for registering") > -1)
                {
                    var memId = CommonHelper.GetMemberIdByUserName(email);

                    #region Set IP Address

                    try
                    {
                        if (!String.IsNullOrEmpty(ip) && ip.Length > 4)
                        {
                            var Result = CommonHelper.UpdateMemberIPAddressAndDeviceId(memId, ip, fngprnt);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Service Layer -> MemberRegistrationGET FAILED - MemberID: [" + memId +
                                               "], Exception: [" + ex + "]");
                    }

                    #endregion Set IP Address

                    res.note = memId;
                    res.success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> MemberRegistrationGET FAILED - Name: [" + name + "], Email: [" + email + "], Exception: [" + ex + "]");
                res.msg = "MemberRegistrationGet Exception";
            }

            return res;
        }

    }
}
