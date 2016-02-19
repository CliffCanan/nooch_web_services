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





        // to reject request for existing user
        [HttpGet]
        [ActionName("RejectMoneyRequestForExistingNoochUser")]
        public StringResult RejectMoneyRequestForExistingNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service layer - RejectMoneyRequestForExistingNoochUser - [TransactionId: " + transactionId + "]");

                var transactionDataAccess = new TransactionsDataAccess();
                string result = transactionDataAccess.RejectMoneyRequestForExistingNoochUser(transactionId);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                throw new Exception("Server Error");
            }
            

        }
        #endregion



        #endregion


        #region SDN related stuff

        [HttpGet]
        [ActionName("CheckSDNListing")]
        public StringResult CheckSDNListing(string MemberId)
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
        public StringResult SaveSocialMediaPost(string MemberId, string accesstoken, string PostTo, string PostContent)
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
                     
                    Logger.Info("Service Layer -> GetSingleTransactionDetail Intiated - " +
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
                    Logger.Error("Service layer - GetsingleTransactionDetail FAILED - MemberId: [" + MemberId + "]. Exception: [" + ex + "]");
                    throw  (ex);
                    
                }
               
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }

        [HttpGet]
        [ActionName("GetTransactionsList")]
        public Collection<TransactionDto> GetTransactionsList(string member, string category, int pageSize, int pageIndex, string accessToken, string SubListType)
        {
            if (CommonHelper.IsValidRequest(accessToken, member))
            {
                try
                {
                    //Logger.LogDebugMessage("Service Layer - GetTransactionsList Initiated - MemberID: [" + member + "]");

                    var tda = new TransactionsDataAccess();

                    int totalRecordsCount = 0;

                    var transactionListEntities = tda.GetTransactionsList(member, category, pageSize, pageIndex, SubListType, out totalRecordsCount);

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
                                if (trans.Member.MemberId.ToString() == member)
                                {
                                    // Member is Receiver in this transaction
                                    obj.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));

                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"),
                                            trans.Member1.Photo != null
                                                ? Path.GetFileName(trans.Member1.Photo)
                                                : Path.GetFileName("gv_no_photo.jpg"));

                                    decimal trFee = (trans.TransactionFee != null) ? Convert.ToDecimal(trans.TransactionFee) : 0;
                                    obj.Amount = Math.Round((trans.Amount + trFee), 2);
                                }

                                if (trans.Member1.MemberId.ToString() == member)
                                {
                                    //the receiver is same as the current member than display the names of sender.
                                    obj.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo != null ? Path.GetFileName(trans.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                    obj.Amount = Math.Round(trans.Amount, 2);
                                }

                                if (trans.Member.MemberId.ToString() == member && trans.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")
                                {
                                    obj.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
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

                                if (category.Equals("SENT") || category.Equals("RECEIVED") || category.Equals("DISPUTED") || category.Equals("ALL"))
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
                                Logger.Error("Service Layer - GetTransactionsList ERROR - Inner Exception during loop through all transactions - " +
                                                       "MemberID: [" + member + "], TransID: [" + trans.TransactionId +
                                                       "], Amount: [" + trans.Amount.ToString("n2") + "], Exception: [" + ex + "]");
                                continue;
                            }
                        }
                        return Transactions;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer - GetTransactionsList FAILED - MemberID: [" + member + "], Exception: [" + ex + "]");
                   throw  ex;
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
                    Logger.Info("Service Layer - GetLatestReceivedTransaction - [MemberId: " + member + "]");

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
                            FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.FirstName)),
                            LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.LastName)),
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
            if (CommonHelper.IsValidRequest(accessToken,member))
            {
                try
                {
                    Logger.Info("Service layer - GetTransactionsList - member: [" + member + "]");

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
                                obj.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                                obj.LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                                obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member1.Photo != null ? Path.GetFileName(trans.Member1.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                            }
                            else if (trans.Member1.MemberId.ToString().ToUpper() == member.ToUpper())
                            {
                                obj.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                                obj.LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
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
                    Logger.Info("Service Layer - GetTransactionDetail - [MemberId: " + memberId + "]");

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

                            sentTransactions.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName));
                            sentTransactions.LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName));
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

                            receivedTransactions.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.FirstName));
                            receivedTransactions.LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member.LastName));
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

                                disputedTransaction.FirstName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.FirstName));
                                disputedTransaction.LastName =CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionListEntities.Member1.LastName));
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
         
        


        #endregion History Related Methods
    }





}
