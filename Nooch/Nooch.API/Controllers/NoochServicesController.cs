﻿using System;
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

namespace Nooch.API.Controllers
{
    public class NoochServicesController : ApiController
    {

        private readonly NOOCHEntities _dbContext = new NOOCHEntities();

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
                                    obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
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
                                    obj.FirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                                    obj.LastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));
                                    obj.Photo = String.Concat(Utility.GetValueFromConfig("PhotoUrl"), trans.Member.Photo != null ? Path.GetFileName(trans.Member.Photo) : Path.GetFileName("gv_no_photo.jpg"));
                                    obj.Amount = Math.Round(trans.Amount, 2);
                                }

                                if (trans.Member.MemberId.ToString() == member && trans.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")
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
                    Logger.Info("Service layer - RaiseDispute - transactionId: [" + raiseDisputeInput.TransactionId + "]");
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
                    Logger.Error("Service layer - RaiseDispute FAILED - transactionId: " + raiseDisputeInput.TransactionId + "]. Exception: [" + ex + "]");
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


        [HttpGet]
        [ActionName("GetMemberDetailsForLandingPage")]
        public MemberDto GetMemberDetailsForLandingPage(string memberId)
        {
            Logger.Info("Service Layer -> GetMemberDetailsForLandingPage - [MemberId: " + memberId + "]");

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
                Logger.Error("Service Layer -> GetMemberDetailsForLandingPage FAILED - [MemberId: " + memberId + "], [Exception: " + ex + "]");
            }

            return new MemberDto();
        }

                
        //surya started from here

        [HttpPost]
        [ActionName("CreateNonNoochUserAccountAfterRejectMoney")]
        public StringResult CreateNonNoochUserAccountAfterRejectMoney(string TransId, string password, string EmailId, string UserName)
        {
            try
            {
                Logger.Info("Service Layer - CreateNonNoochUserAccountAfterRejectMoney - [TransId: " + TransId + "], [UserName: " + UserName + "]");

                var mda = new MembersDataAccess();
                string result = mda.CreateNonNoochUserAccountAfterRejectMoney(TransId, password, EmailId, UserName);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "" };
        }


        [HttpGet]
        [ActionName("RejectMoneyforNonNoochUser")]
        public StringResult RejectMoneyforNonNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service Layer - RejectMoneyforNonNoochUser - [TransactionId: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyforNonNoochUser(transactionId);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "" };
        }

        [HttpGet]
        [ActionName("RejectMoneyRequestForNonNoochUser")]
        public StringResult RejectMoneyRequestForNonNoochUser(string transactionId)
        {
            try
            {
                Logger.Info("Service Layer - RejectMoneyforNonNoochUser - [TransactionID: " + transactionId + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyRequestForNonNoochUser(transactionId);

                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "" };
        }


        [HttpGet]
        [ActionName("RejectMoneyCommon")]
        public StringResult RejectMoneyCommon(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            try
            {
                Logger.Info("Service Layer -> RejectMoneyCommon Initiated - Transaction ID: [" + TransactionId + "], " +
                                       "TransType: [" + TransType + "], UserType: [" + UserType + "]");

                var tda = new TransactionsDataAccess();
                string result = tda.RejectMoneyCommon(TransactionId, UserType, LinkSource, TransType);
                return new StringResult { Result = result };
            }
            catch (Exception ex)
            {
                Utility.ThrowFaultException(ex);
            }
            return new StringResult { Result = "" };
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

                synapseCreateUserV3Result_int res = mda.RegisterUserWithSynapseV3(memberId);


                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> RegisterUserWithSynapseV3 FAILED. [Exception: " + ex.ToString() + "]");

                return null;
            }
        }


        public synapseV3GenericResponse submitDocumentToSynapseV3(SaveVerificationIdDocument DocumentDetails)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();

            try
            {
                Logger.Info("Service layer - submitDocumentToSynapseV3 [MemberId: " + DocumentDetails.MemberId + "]");

                var mda = new MembersDataAccess();


                // making url from byte array...coz submitDocumentToSynapseV3 expects url of image.

                string ImageUrlMade = "";

                if (DocumentDetails.Picture != null)
                {
                    // Make  image from bytes
                    string filename = HttpContext.Current.Server.MapPath("UploadedPhotos") + "/Photos/" +
                                      DocumentDetails.MemberId + ".png";
                    using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite))
                    {
                        fs.Write(DocumentDetails.Picture, 0, (int)DocumentDetails.Picture.Length);
                    }
                    ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + DocumentDetails.MemberId + ".png";
                }
                else
                {
                    ImageUrlMade = Utility.GetValueFromConfig("PhotoUrl") + "gv_no_photo.png";
                }


                var mdaResult = mda.submitDocumentToSynapseV3(DocumentDetails.MemberId, ImageUrlMade);

                res.isSuccess = mdaResult.success;
                res.msg = mdaResult.message;

                return res;
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer - submitDocumentToSynapseV3 FAILED - [userName: " + DocumentDetails.MemberId + "]. Exception: [" + ex + "]");

                throw new Exception("Server Error.");
            }


        }



        public SynapseBankLoginV3_Response_Int SynapseV3AddNode(string MemberId, string BnkName, string BnkUserName, string BnkPw)
        {
            Logger.Info("MDA -> SynapseV3AddNode Initiated - MemberId: [" + MemberId + "], BankName: [" + BnkName + "]");

            SynapseBankLoginV3_Response_Int res = new SynapseBankLoginV3_Response_Int();
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

                Logger.Error("MDA -> SynapseV3AddNode ABORTING: Invalid data sent for: [" + MemberId + "].");

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
                        Logger.Info("MDA -> SynapseV3 ADD NODE ERROR, but Member NOT FOUND.");

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
                        Logger.Info("MDA -> SynapseV3 ADD NODE Attempted, but Member is Not 'Active' but [" + noochMember.Status + "] for MemberId: [" + MemberId + "]");

                        res.errorMsg = "User status is not active but, " + noochMember.Status;
                        return res;
                    }

                    if ((noochMember.IsVerifiedPhone == null || noochMember.IsVerifiedPhone == false) &&
                         noochMember.Status != "NonRegistered" && noochMember.Type != "Personal - Browser")
                    {
                        Logger.Info("MDA -> SynapseV3 ADD NODE Attempted, but Member's Phone is Not Verified. MemberId: [" + MemberId + "]");

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

                        if (registerSynapseUserResult.success )
                        {
                            createSynapseUserDetails = CommonHelper.GetSynapseCreateaUserDetails(id.ToString());
                        }
                        else
                        {
                            Logger.Info("MDA -> SynapseV3 ADD NODE ERROR: Could not create Synapse User Record for: [" + MemberId + "].");
                        }
                    }

                    // Check again if it's still null (which it shouldn't be because we just created a new Synapse user above if it was null.
                    if (createSynapseUserDetails == null)
                    {
                        Logger.Error("MDA -> SynapseV3 ADD NODE ERROR: No Synapse OAuth code found in Nooch DB for: [" + MemberId + "].");

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

                    string UrlToHit = "https://sandbox.synapsepay.com/api/v3/node/add";
                    //string UrlToHit = "https://synapsepay.com/api/v3/node/add";

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
                                    // Return if MFA, otherwise continue on and parse the banks
                                    if (res.Is_MFA)
                                    {
                                        Logger.Info("MDA -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - Got MFA from Synapse - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");

                                        res.Is_success = true;
                                        return res;
                                    }

                                    Logger.Info("MDA -> SynapseV3AddNode SUCCESS - Added record to synapseBankLoginResults Table - NO MFA found - [UserName: " + CommonHelper.GetDecryptedData(noochMember.UserName) + "]");
                                }
                                else
                                {
                                    Logger.Error("MDA -> SynapseV3AddNode FAILURE - Could not save record in SynapseBankLoginResults Table - ABORTING - [MemberID: " + MemberId + "]");

                                    res.errorMsg = "Failed to save entry in BankLoginResults table (inner)";
                                    return res;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("MDA -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
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

                                Logger.Info("MDA -> SynapseV3AddNode - No MFA - SUCCESSFUL, Now saving all banks found in Bank Array (n = " +
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

                                        // Holdovers from V2
                                        sbm.account_number_string = !String.IsNullOrEmpty(n.info.account_num) ? CommonHelper.GetEncryptedData(n.info.account_num) : null;
                                        sbm.bank_name = !String.IsNullOrEmpty(n.info.bank_name) ? CommonHelper.GetEncryptedData(n.info.bank_name) : null;
                                        sbm.name_on_account = !String.IsNullOrEmpty(n.info.name_on_account) ? CommonHelper.GetEncryptedData(n.info.name_on_account) : null;
                                        sbm.nickname = !String.IsNullOrEmpty(n.info.nickname) ? CommonHelper.GetEncryptedData(n.info.nickname) : null;
                                        sbm.routing_number_string = !String.IsNullOrEmpty(n.info.routing_num) ? CommonHelper.GetEncryptedData(n.info.routing_num) : null;
                                        sbm.is_active = (n.is_active != null) ? n.is_active : false;
                                        sbm.Status = "Not Verified";
                                        // CLIFF (10/11/15): We were using this "bankid" to identify the bank in other places.  Now we need to use oid (below) instead.
                                        // sbm.bankid = !String.IsNullOrEmpty(n._id.oid) ? n._id.oid : null;
                                        // These 2 values were *int* IN V2, but now both are strings...
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
                                            Logger.Info("MDA -> SynapseV3AddNode -SUCCESSFULLY Added Bank to DB - [MemberID: " + MemberId + "]");

                                            numOfBanksSavedSuccessfully += 1;
                                        }
                                        else
                                        {
                                            Logger.Error("MDA -> SynapseV3AddNode - Failed to save new BANK in SynapseBanksOfMembers Table in DB - [MemberID: " + MemberId + "]");

                                            numOfBanksSavedSuccessfully -= 1;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        res.errorMsg = "Error occured while saving banks from Synapse.";
                                        Logger.Error("MDA -> SynapseV3AddNode EXCEPTION on attempting to save SynapseBankLogin response for MFA Bank in DB - [MemberID: " +
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
                                    Logger.Error("MDA -> SynapseV3AddNode - No banks were saved in DB - [MemberID: " + MemberId + "]");
                                    res.errorMsg = "No banks saved in DB";
                                }

                                #endregion Save Each Bank In Nodes Array From Synapse
                            }
                            else
                            {
                                Logger.Info("MDA -> SynapseV3 ADD NODE (No MFA) ERROR: allbanksParsedResult was NULL for: [" + MemberId + "]");

                                res.Is_MFA = false;
                                res.errorMsg = "Error occured while parsing banks list.";
                            }

                            return res;

                            #endregion No MFA response returned
                        }
                        else
                        {
                            // Synapse response for 'success' was not true
                            Logger.Error("MDA -> SynapseV3AddNode ERROR - Synapse response for 'success' was not true - [MemberID: " + MemberId + "]");
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
                Logger.Error("MDA -> SynapseV3AddNode FAILED - OUTER EXCEPTION - [MemberID: " + MemberId + "], [Exception: " + ex + "]");
                res.errorMsg = "MDA Outer Exception";
            }

            return res;
        }



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
                    res.mfaMessage = mdaResult.mfaMessage;
                    res.bankOid = mdaResult.SynapseNodesList.nodes[0]._id.oid;
                }
            }

            #endregion MFA Required

            return res;
        }

        #endregion
    }
}
