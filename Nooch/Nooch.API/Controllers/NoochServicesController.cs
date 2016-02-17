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

namespace Nooch.API.Controllers
{
    public class NoochServicesController : ApiController
    {

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
        [ActionName("UdateMemberIPAddress")]
        public StringResult UdateMemberIPAddress(UpdateMemberIpInput member)
        {
            if (CommonHelper.IsValidRequest(member.AccessToken, member.MemberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    string res = mda.UpdateMemberIpAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId);
                    return new StringResult() { Result = mda.UpdateMemberIpAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer -> UpdateMemberIPAddressAndDeviceId FAILED - MemberID: [" + member.MemberId +
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
                    Logger.Info("Service layer - GetPrimaryEmail [udId: " + udId + "]");

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
                Logger.Error("Service Layer - GetMemberByUserName FAILED - [userName: " + userName +
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
                Logger.Error("Service Layer - GetMemberUsernameByMemberId FAILED - [memberId: " + memberId +
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
                Logger.Info("Service Layer -> GetPhoneNumberByMemberId Initiated - [MemberID: " + memberId + "]");

                return new StringResult
                {
                    Result = CommonHelper.GetPhoneNumberByMemberId(memberId)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> GetPhoneNumberByMemberId FAILED - [Exception: " + ex + "]");
                return new StringResult();
            }
        }

        [HttpGet]
        [ActionName("GetMemberIdByPhone")]
        public StringResult GetMemberIdByPhone(string phoneNo, string accessToken)
        {
            try
            {

                Logger.Info("Service layer - GetMemberByPhone - phoneNo: [" + phoneNo + "]");

                return new StringResult { Result = CommonHelper.GetMemberIdByPhone(phoneNo) };

            }
            catch (Exception ex)
            {
                Logger.Error("Service layer - GetMemberByPhone - FAILED - [Exception: " + ex + "]");

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
                Logger.Error("Service Layer - GetMemberUsernameByMemberId FAILED - [GetMemberNameByUserName : " + userName +
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
                Logger.Info("Service Layer -> MemberActivation Initiated - [tokenId: " + tokenId + "]");
                var mda = new MembersDataAccess();
                return new BoolResult { Result = mda.MemberActivation(tokenId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> MemberActivation Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
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
                Logger.Error("Service Layer -> IsMemberActivated Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
                return new BoolResult();
            }

        }


        [HttpGet]
        [ActionName("IsNonNoochMemberActivated")]
        public BoolResult IsNonNoochMemberActivated(string emailId)
        {
            try
            {
                Logger.Info("Service Layer - IsNonNoochMemberActivated - Email ID: [" + emailId + "]");

                return new BoolResult { Result = CommonHelper.IsNonNoochMemberActivated(emailId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> IsNonNoochMemberActivated Failed - [tokenId: " + emailId + "]. Exception -> " + ex);
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
                Logger.Info("Service layer - IsDuplicateMember - userName: [" + userName + "]");

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

        public MemberDto GetEncryptedData(string sourceData)
        {
            try
            {
                string DecodedMessage = Base64Decode(sourceData);

                var aesAlgorithm = new AES();
                string encryptedData = aesAlgorithm.Encrypt(DecodedMessage, string.Empty);
                MemberDto obj = new MemberDto { Status = encryptedData.Replace(" ", "+") };
                return new MemberDto { Status = encryptedData.Replace(" ", "+") };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer - GetEncryptedData FAILED - sourceData: [" + sourceData + "]. Exception: [" + ex + "]");
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
                Logger.Error("Service Layer - GetDecryptedData FAILED - sourceData: [" + sourceData + "]. Exception: [" + ex + "]");
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
            Logger.Info("Service Layer -> GetMemberDetails - [MemberId: " + memberId + "]");

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
                    Logger.Error("Service Layer -> GetMemberDetails FAILED - [MemberId: " + memberId + "], [Exception: " + ex.InnerException + "]");
                    throw new Exception("Server Error");
                }
                return new MemberDto();
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
                    Logger.Error("Service Layer -> GetMostFrequentFriends FAILED - [Exception: " + ex + "]");
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
                    Logger.Error("Service Layer -> GetMemberStats FAILED - [Exception: " + ex + "]");

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
            Logger.Info("Service Layer -> SaveMemberDeviceToken Initiated - [MemberId: " + memberId + "], [DeviceToken: " + deviceToken + "]");

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
                        Logger.Error("Service Layer -> SaveMemberDeviceToken FAILED - [MemberId: " + memberId + "]. Exception: [" + ex + "]");

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
                Logger.Error("Service Layer -> SaveMemberDeviceToken FAILED - [MemberId: " + memberId + "]. INVALID OAUTH 2 ACCESS.");
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
                Logger.Info("Service Layer -> LoginRequest - [userName: " + userName + "], [UDID: " + udid + "], [devicetoken: " + devicetoken + "]");

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
                Logger.Error("Service Layer -> LoginRequest FAILED - [userName: " + userName + "], [Exception: " + ex + "]");
                throw new Exception("Server Error");
            }

        }

        [HttpGet]
        [ActionName("SaveMemberDeviceToken")]
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
            var memberDataAccess = new MembersDataAccess();
            return memberDataAccess.validateInvitationCode(invitationCode);
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
            var memberDataAccess = new MembersDataAccess();
            var isValid = memberDataAccess.getTotalReferralCode(referalCode);
            return new StringResult { Result = isValid.ToString() };
        }


        [HttpPost]
        [ActionName("ApiSMS")]
        public StringResult ApiSMS(string phoneto, string msg, string accessToken, string memberId)
        {
            if ((msg == "Hi\n You were automatically logged out because you signed in from another device.\n - Team Nooch") || CommonHelper.IsValidRequest(accessToken, memberId))
            {

                return new StringResult { Result = Utility.SendSMS(phoneto, msg) };
            }
            throw new Exception("Invalid OAuth 2 Access");
        }



        #region Money and Transactions Game goes here

        #region cancel transactions
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

        #endregion



        #region send transaction reminders

        [HttpGet]
        [ActionName("SendTransactionReminderEmail")]
        public StringResult SendTransactionReminderEmail(string ReminderType,
            string TransactionId, string accessToken, string MemberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, MemberId))
            {
                try
                {
                    Logger.Info("Service layer - SendTransactionReminderEmail - memberId: [" + MemberId + "]");
                    var transactionDataAccess = new TransactionsDataAccess();

                    return new StringResult { Result = transactionDataAccess.SendTransactionReminderEmail(ReminderType, TransactionId, MemberId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service layer - SendTransactionReminderEmail FAILED - memberId: [" + MemberId + "]. Exception: [" + ex + "]");
                    throw new Exception("Server Error.");
                }
                
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        #endregion



        /***********************************/
        /****  REQUEST-RELATED METHODS  ****/
        /***********************************/

        #region Request methods
        [HttpPost]
        [ActionName("RequestMoney")]
        StringResult RequestMoney(RequestDto requestInput, out string requestId, string accessToken)
        {
            if (CommonHelper.IsValidRequest(accessToken, requestInput.MemberId))
            {
                requestId = string.Empty;
                try
                {
                    Logger.Info("Service Layer - RequestMoney Initiated - MemberId: [" + requestInput.MemberId + "]");
                    var tda = new TransactionsDataAccess();
                    return new StringResult { Result = tda.RequestMoney(requestInput, out requestId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer - RequestMoney FAILED - MemberId: [" + requestInput.MemberId + "], Exception: [" + ex + "]");
                    throw new Exception("Server Error");

                }
                
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }


        #endregion



        #endregion


        #region SDN related stuff

        [HttpGet]
        [ActionName("CheckSDNListing")]
        StringResult CheckSDNListing(string MemberId)
        {


            var noochMemberN = CommonHelper.GetMemberDetails(MemberId);
            if (noochMemberN != null)
            {
                var b = CommonHelper.IsListedInSDN(CommonHelper.GetDecryptedData(noochMemberN.LastName),
                    noochMemberN.MemberId);
            }


            return new StringResult();
        }

        #endregion


        #region Social media related stuff

        [HttpGet]
        [ActionName("SaveSocialMediaPost")]
        StringResult SaveSocialMediaPost(string MemberId, string accesstoken, string PostTo, string PostContent)
        {
            if (CommonHelper.IsValidRequest(accesstoken, MemberId))
            {
                try
                {
                    Logger.Info("Service Layer - SaveSocialMediaPost - [MemberId: " + MemberId + "],  [Posted To: " + PostTo + "]");
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
        #endregion

    }





}
