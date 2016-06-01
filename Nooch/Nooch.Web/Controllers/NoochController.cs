using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Nooch.Common;
using Nooch.Common.Entities.LandingPagesRelatedEntities;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Web.Common;
using Nooch.Data;
using Nooch.Common.Entities;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.DataAccess;
using Nooch.Common.Entities.LandingPagesRelatedEntities.RejectMoney;
using System.Net;
using System.Web.UI;
using Nooch.Common.Cryptography.Algorithms;
using Nooch.Common.Entities.MobileAppInputEntities;

namespace Nooch.Web.Controllers
{
    public class NoochController : Controller
    {
        public ActionResult Index()
        {
            Logger.Info("test message");
            return View();
        }


        public ActionResult BankVerification(string tokenId)
        {
            BankVerification bankVerification = new BankVerification();

            string strUserAgent = Request.UserAgent.ToLower();

            if (strUserAgent != null)
            {
                if (Request.Browser.IsMobileDevice || strUserAgent.Contains("iphone") || strUserAgent.Contains("mobile") || strUserAgent.Contains("iOS"))
                {
                    bankVerification.openAppText = true;
                }
            }

            tokenId = tokenId.Trim();
            tokenId = CommonHelper.GetDecryptedData(tokenId);
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "/VerifySynapseAccount?tokenID=" + tokenId.Trim();

            Logger.Info("Bank Verification Browser Page -> Bank Account Verification Page Loaded - [tokenId: " + tokenId + "]");

            BoolResult result = ResponseConverter<BoolResult>.ConvertToCustomEntity(serviceUrl);

            if (result.Result)
            {
                Logger.Info("Bank Verification Browser Page -> Bank Account Verified Successfully! - Result from Nooch Services was: [" + result.Result + "]");

                bankVerification.Div1 = true;
                bankVerification.Div2 = false;
            }
            else
            {
                Logger.Error("Bank Verification Browser Page -> Bank Account Verification FAILED - Result was: [" + result.Result + "]");

                bankVerification.Div1 = false;
                bankVerification.Div2 = true;
            }
            ViewData["OnLoadData"] = bankVerification;

            return View();
        }


        #region Cancel Request Page

        /// <summary>
        /// Called on Page Load of /CancelRequest page.
        /// </summary>
        /// <returns></returns>
        public ActionResult CancelRequest()
        {
            ResultCancelRequest rcr = new ResultCancelRequest();

            if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]) &&
                !String.IsNullOrEmpty(Request.QueryString["MemberId"]) &&
                !String.IsNullOrEmpty(Request.QueryString["UserType"]))
            {
                rcr = GetTransDetails(Request.QueryString["TransactionId"]);
            }
            else
            {
                Logger.Error("CancelRequest Code Behind -> CancelRequest() FAILED - Missing parameters in URL string - URL: [" + Request.RawUrl + "]");

                rcr.success = false;
                rcr.showPaymentInfo = false;
                rcr.resultMsg = "This looks like an invalid transaction - sorry about that!  Please try again or contact Nooch support for more information.";
            }

            ViewData["OnLoaddata"] = rcr;

            return View();
        }


        /// <summary>
        /// Just for CANELLING a payment - called by the CancelRequest() method when the CancelRequest page first loads.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        public ResultCancelRequest GetTransDetails(string TransactionId)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            rcr.success = false;

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction != null)
            {
                rcr.showPaymentInfo = true; // Show payment info (recipient name/pic, amount)

                if (transaction.TransactionStatus == "Pending")
                {
                    ResultCancelRequest cancelResult = CancelMoneyRequest(transaction.TransactionId, Request.QueryString["MemberId"], Request.QueryString["UserType"]);
                    rcr = cancelResult;
                    // CLIFF (5/15/16): the transaction's status was actually just updated to 'Cancelled' in CancelMoneyRequest()
                    // but this flag is just for telling the CancelMoney.js to display a SweetAlert if it is NOT initially 'Pending' on page load.
                    rcr.initStatus = "pending";
                }
                else if (transaction.TransactionStatus == "Rejected")
                {
                    Logger.Info("CancelRequest Code Behind -> GetTransDetails - This payment has already been Rejected - [TransID: " + TransactionId + "]");
                    rcr.resultMsg = "Looks like this payment has already been rejected.";
                    rcr.initStatus = "rejected";
                }
                else if (transaction.TransactionStatus == "Cancelled")
                {
                    Logger.Info("CancelRequest Code Behind -> GetTransDetails - This payment has already been Cancelled - [TransID: " + TransactionId + "]");
                    rcr.resultMsg = "This payment has already been cancelled.";
                    rcr.initStatus = "cancelled";
                }
            }
            else
            {
                Logger.Error("CancelRequest Code Behind -> GetTransDetails FAILED - [TransID: " + TransactionId + "]");

                rcr.resultMsg = "We were not able to find this transaction. Please try again by reloading this page, or contact Nooch support for further assistance.";
            }

            #region Set Name and Photo

            if (transaction.IsPhoneInvitation && transaction.PhoneNumberInvited.Length > 0)
            {
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                rcr.nameLabel = transaction.PhoneNumberInvited;
            }
            else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            {
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                rcr.nameLabel = transaction.InvitationSentTo;
            }
            else
            {
                rcr.senderImage = !String.IsNullOrEmpty(transaction.SenderPhoto) ? transaction.SenderPhoto : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                rcr.nameLabel = transaction.Name;
            }

            #endregion Set Name and Photo

            rcr.AmountLabel = transaction.Amount.ToString("n2");

            // Reject money page related stuff
            if (!String.IsNullOrEmpty(transaction.TransactionType))
                rcr.TransType = transaction.TransactionType;

            if (!String.IsNullOrEmpty(transaction.TransactionId))
                rcr.TransId = transaction.TransactionId;

            return rcr;
        }


        protected ResultCancelRequest CancelMoneyRequest(string TransactionId, string MemberId, string userType)
        {
            ResultCancelRequest res = new ResultCancelRequest();
            res.success = false;

            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            #region Call CancelMoneyRequest Service

            if (userType == "mx5bTcAYyiOf9I5Py9TiLw==")
            {
                // Service to cancel a REQUEST to an EXISTING Nooch user
                serviceMethod = "/CancelMoneyRequestForExistingNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;
            }
            else if (userType == "U6De3haw2r4mSgweNpdgXQ==")
            {
                // Service to cancel a REQUEST to NON-NOOCH user
                serviceMethod = "/CancelMoneyRequestForNonNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;
            }

            var serviceResult = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (serviceResult.Result == "Transaction Cancelled Successfully.")
            {
                Logger.Info("CancelRequest Code Behind -> Request Successfully Cancelled - [TransID: " + TransactionId + "], [MemberID: " + MemberId + "]");

                res.showPaymentInfo = true;
                res.success = true;
                res.resultMsg = "No problem, we change our minds sometimes too.  This request is cancelled.  But you can always send another request...";
            }
            else
            {
                Logger.Error("CancelRequest Code Behind -> CancelMoneyRequest FAILED - [TransID: " + TransactionId + "], [MemberID: " + MemberId + "], [UserType: " + userType + "]");
                res.resultMsg = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
            }

            #endregion Call CancelMoneyRequest Service

            return res;
        }

        #endregion Cancel Request Page


        #region Add Bank Page

        public ActionResult AddBank(string MemberId)
        {
            if (!String.IsNullOrEmpty(MemberId))
                return View();
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ActionName("CheckBankDetails")]
        public ActionResult CheckBankDetails(string bankname)
        {
            // Get bank details
            BankNameCheckStatus res = new BankNameCheckStatus();
            res.IsSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = "/CheckSynapseBankDetails?BankName=" + bankname;
                Logger.Info("**Add_Bank** CodeBehind -> CheckBankDetails - Service Method to call: [" + String.Concat(serviceUrl, serviceMethod) + "]");

                CheckSynapseBankDetails bankInfoFromServer = ResponseConverter<CheckSynapseBankDetails>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (bankInfoFromServer.IsBankFound == true)
                {
                    res.IsSuccess = true;
                    res.MFAType = bankInfoFromServer.mfa_type;
                    res.IsPinRequired = bankInfoFromServer.IsPinRequired;
                    res.Message = "OK";
                }
                else
                {
                    // bank not found error
                    res.MFAType = bankInfoFromServer.mfa_type;
                    res.IsPinRequired = bankInfoFromServer.IsPinRequired;
                    res.Message = bankInfoFromServer.Message;
                }
            }
            catch (Exception we)
            {
                res.Message = "CheckBankDetails Web Exception - local";
                Logger.Error("**Add_Bank** CodeBehind -> CheckBankDetails FAILED - [Bank Name: " + bankname +
                                   "], [Exception Msg: " + we.Message + "], [Exception Inner: " + we.InnerException + "]");
            }
            return Json(res);
        }


        public BankLoginResult RegisterUserWithSynapse(string memberid)
        {
            Logger.Info("Add Bank CodeBehind -> RegisterUserWithSynapse Initiated - [MemberID: " + memberid + "]");

            BankLoginResult res = new BankLoginResult();
            res.IsSuccess = false;
            res.ssn_verify_status = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RegisterUserWithSynapseV3?memberId=" + memberid;

                synapseCreateUserV3Result_int transaction = ResponseConverter<synapseCreateUserV3Result_int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (transaction.success == false)
                {
                    Logger.Error("Add Bank CodeBehind -> RegisterUserWithSynapse FAILED - Success was False, errorMsg: [" + transaction.errorMsg + "]");
                    res.Message = transaction.errorMsg;
                }
                else if (transaction.success == true)
                {
                    res.IsSuccess = true;

                    res.Message = (!String.IsNullOrEmpty(transaction.errorMsg) && transaction.errorMsg.IndexOf("Missing ") > -1) ? transaction.errorMsg : "OK";
                }

                res.ssn_verify_status = transaction.ssn_verify_status;
            }
            catch (Exception we)
            {
                res.Message = "RegisterUser Web Exception - local";
                Logger.Error("Add Bank CodeBehind -> RegisterUserWithSynapse FAILED - [MemberID: " + memberid +
                             "], [Exception: " + we.InnerException + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("BankLogin")]
        public ActionResult BankLogin(bankLoginInputFormClass inp)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // 1. Attempt to register the user with Synapse
                BankLoginResult registerSynapseUserResult = RegisterUserWithSynapse(inp.memberid);

                res.ssn_verify_status = registerSynapseUserResult.ssn_verify_status; // Will be overwritten if Bank Login is successful below

                if (registerSynapseUserResult.IsSuccess == true)
                {
                    Logger.Info("NoochController -> BankLogin -> Synapse account created successfully! [MemberID: " + inp.memberid +
                                "], [SSN Status: " + registerSynapseUserResult.ssn_verify_status + "]");

                    // 2. Now call the bank login service.
                    //    Response could be: 1.) array[] of banks,  2.) Question-based MFA,  3.) Code-based MFA, or  4.) Failure/Error

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "/SynapseV3AddNode?MemberId=" + inp.memberid + "&BnkName=" + inp.bankname + "&BnkUserName=" + inp.username +
                                           "&BnkPw=" + inp.password;

                    SynapseV3BankLoginResult_ServiceRes bankLoginResult = ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    if (bankLoginResult.Is_success == true)
                    {
                        res.Is_success = true;
                        res.Is_MFA = bankLoginResult.Is_MFA;

                        List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();

                        if (bankLoginResult.Is_MFA)
                        {
                            res.bankoid = bankLoginResult.bankOid;
                            res.mfaMessage = bankLoginResult.mfaQuestion;
                        }
                        else if (bankLoginResult.SynapseNodesList.nodes[0] != null)
                        {
                            foreach (SynapseIndividualNodeClass bankNode in bankLoginResult.SynapseNodesList.nodes)
                            {
                                SynapseBankClass sbc = new SynapseBankClass();
                                sbc.account_class = bankNode.account_class;
                                sbc.account_number_string = bankNode.account_num;
                                sbc.account_type = bankNode.account_type.ToString();
                                sbc.bank_name = bankNode.bank_name;
                                sbc.bankoid = bankNode.oid;
                                sbc.nickname = bankNode.nickname;
                                sbc.routing_number_string = bankNode.routing_num;
                                sbc.is_active = bankNode.is_active;

                                synbanksList.Add(sbc);
                            }
                        }

                        res.SynapseBanksList = new SynapseBanksListClass()
                        {
                            banks = synbanksList,
                            success = true
                        };

                        res.ERROR_MSG = "OK";
                        // res.ssn_verify_status = bankLoginResult.ssn_verify_status; // Should match the ssn_verify_status from Registering User...
                    }
                    else
                    {
                        Logger.Error("NoochController -> BankLogin FAILED -> [MemberID: " + inp.memberid + "], [Error Msg: " + bankLoginResult.errorMsg + "]");
                        res.ERROR_MSG = bankLoginResult.errorMsg;
                    }
                }
                else
                {
                    Logger.Error("NoochController -> BankLogin -> Register Synapse User FAILED -> [MemberID: " + inp.memberid + "], [Error Msg: " + registerSynapseUserResult.Message + "]");
                    res.ERROR_MSG = registerSynapseUserResult.Message;
                }

                // Check if Register method and Bank Login method both got same result for ssn_verify_status
                // ... not sure which one to prioritize yet in the case of a discrepency, but going with BankLogin one for now.
                if (res.ssn_verify_status != registerSynapseUserResult.ssn_verify_status)
                {
                    Logger.Info("NoochController -> BankLogin -> ssn_verify_status from Registering User was [" +
                                registerSynapseUserResult.ssn_verify_status + "], but ssn_verify_status from BankLogin was: [" +
                                res.ssn_verify_status + "]");
                }
            }
            catch (Exception we)
            {
                Logger.Error("NoochController -> BankLogin FAILED - [MemberID: " + inp.memberid +
                             "], [Exception: " + we.InnerException + "]");

                res.ERROR_MSG = "Server error: [" + we.Message + "]";
            }

            return Json(res);
        }



        /// <summary>
        /// For adding a bank with Routing/Account #'s.
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("addBank")]
        public ActionResult addBank(bankaddInputFormClass inp)
        {
            Logger.Info("NoochController -> addBank (for manual routing/account #) Initiated - [MemberID: " + inp.memberid + "]");

            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // Now call the bank login service.
                // Response should be: 1.) array[] of 1 bank, or  2.) Failure/Error

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = "/SynapseV3AddNodeWithAccountNumberAndRoutingNumber?MemberId=" + inp.memberid + "&routing_num=" + inp.routing
                                       + "&account_num=" + inp.account + "&bankNickName=" + inp.nickname + "&accountclass=" + inp.cl + "&accounttype=" + inp.type;

                SynapseBankLoginV3_Response_Int bankAddRes =
                    ResponseConverter<SynapseBankLoginV3_Response_Int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (bankAddRes.Is_success == true)
                {
                    res.Is_success = true;
                    res.Is_MFA = bankAddRes.Is_MFA;

                    List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();

                    foreach (nodes bankNode in bankAddRes.SynapseNodesList.nodes)
                    {
                        SynapseBankClass sbc = new SynapseBankClass();
                        sbc.account_class = bankNode.info._class;
                        sbc.account_number_string = bankNode.info.account_num;
                        sbc.account_type = bankNode.type;
                        sbc.balance = bankNode.info.balance.amount;
                        sbc.bank_name = bankNode.info.bank_name;
                        sbc.bankoid = bankNode._id.ToString();
                        sbc.account_class = bankNode.info._class;
                        sbc.nickname = bankNode.info.nickname;
                        sbc.routing_number_string = bankNode.info.routing_num;
                        sbc.account_type = bankNode.info.type;
                        sbc.is_active = bankNode.is_active;

                        synbanksList.Add(sbc);
                    }

                    res.SynapseBanksList = new SynapseBanksListClass()
                    {
                        banks = synbanksList,
                        success = true
                    };

                    res.ERROR_MSG = "OK";
                }
                else
                {
                    res.ERROR_MSG = bankAddRes.errorMsg;
                }
            }
            catch (Exception we)
            {
                Logger.Error("NoochController CodeBehind -> addBank (Manual) FAILED - [MemberID: " + inp.memberid +
                             "], [Exception: " + we.Message + "]");
                res.ERROR_MSG = "Add Bank exception # 494";
            }

            return Json(res);
        }


        /// <summary>
        /// For checking a user's bank sercurity question response with Synapse V3 during the bank login process.
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MFALogin")]
        public ActionResult MFALogin(MFALoginInputClassForm inp)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                Logger.Info("NoochController -> MFALogin Initiated -> [MemberID: " + inp.memberid + "], [Bank: " + inp.bank + "], [MFA: " + inp.MFA + "]");

                // preparing data for POST type request

                var scriptSerializer = new JavaScriptSerializer();

                SynapseV3VerifyNode_ServiceInput verifyNodeObj = new SynapseV3VerifyNode_ServiceInput();
                verifyNodeObj.BankName = inp.bank; // not required..keeping it for just in case we need something to do with it.
                verifyNodeObj.MemberId = inp.memberid;
                verifyNodeObj.mfaResponse = inp.MFA;
                verifyNodeObj.bankId = inp.ba;   // this is bank_node_id...grabbed during earlier step

                try
                {
                    var json = scriptSerializer.Serialize(verifyNodeObj);

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "/SynapseV3MFABankVerify";

                    SynapseV3BankLoginResult_ServiceRes bnkloginresult = ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                    res.Is_success = bnkloginresult.Is_success;
                    res.Is_MFA = bnkloginresult.Is_MFA;

                    if (bnkloginresult.Is_success == true)
                    {
                        #region MFA Bank Verify Was Successfull

                        Logger.Info("NoochController -> MFALogin Success! -> [MemberID: " + inp.memberid + "], [Bank: " + inp.bank +
                                    "], [Is_MFA (Again): " + bnkloginresult.Is_MFA + "]");

                        if (bnkloginresult.Is_MFA)
                        {
                            res.mfaMessage = !String.IsNullOrEmpty(bnkloginresult.mfaQuestion) && bnkloginresult.mfaQuestion.Length > 10
                                             ? bnkloginresult.mfaQuestion
                                             : "The bank returned with an error unfortunately. Please try again.";
                            res.bankoid = !String.IsNullOrEmpty(bnkloginresult.bankOid) ? bnkloginresult.bankOid : null;

                            //SynapseQuestionBasedMFAClass qbr = new SynapseQuestionBasedMFAClass();
                            //qbr.is_mfa = true;
                            //qbr.response = new SynapseQuestionBasedMFAResponseIntClass()
                            //{
                            //    type = "questions",
                            //    access_token = "",
                            //    mfa = new SynapseQuestionClass[1]
                            //};

                            //qbr.response.mfa[0].question = bnkloginresult.mfaQuestion;
                            //res.SynapseQuestionBasedResponse = qbr;

                        }
                        else if (bnkloginresult.SynapseNodesList != null && bnkloginresult.SynapseNodesList.nodes.Count > 0)
                        {
                            List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();
                            foreach (SynapseIndividualNodeClass bankNode in bnkloginresult.SynapseNodesList.nodes)
                            {
                                SynapseBankClass sbc = new SynapseBankClass();
                                sbc.account_class = bankNode.account_class;
                                sbc.account_number_string = bankNode.account_num;
                                sbc.account_type = bankNode.account_type.ToString();
                                sbc.bank_name = bankNode.bank_name;
                                sbc.bankoid = bankNode.oid;  // will use this ID to set bank account as default
                                sbc.nickname = bankNode.nickname;
                                sbc.routing_number_string = bankNode.routing_num;
                                sbc.is_active = bankNode.is_active;
                                synbanksList.Add(sbc);
                            }

                            res.SynapseBanksList = new SynapseBanksListClass()
                            {
                                banks = synbanksList,
                                success = true
                            };
                        }

                        res.ERROR_MSG = "OK";

                        #endregion MFA Bank Verify Was Successfull
                    }
                    else
                    {
                        #region MFA Bank Verify Was NOT Successfull

                        // Could have been an incorrect answer from the user, check the error msg...
                        res.ERROR_MSG = bnkloginresult.errorMsg;

                        if (bnkloginresult.errorMsg == "-incorrect-")
                        {
                            Logger.Error("NoochController -> MFALogin - Incorrect answer submitted - [Error Msg: " + bnkloginresult.errorMsg +
                                     "], [MemberID: " + inp.memberid + "], [Bank: " + inp.bank + "], [MFA: " + inp.MFA + "]");
                            res.Is_MFA = true;
                            res.mfaMessage = bnkloginresult.mfaQuestion; // Would be "-same-"...JS already has the original question for the user to re-answer.
                        }
                        else
                        {
                            Logger.Error("NoochController -> MFALogin FAILED -> [Error Msg: " + bnkloginresult.errorMsg +
                                         "], [MemberID: " + inp.memberid + "], [Bank: " + inp.bank + "], [MFA: " + inp.MFA + "]");
                        }

                        #endregion MFA Bank Verify Was NOT Successfull
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("NoochController -> MFALogin FAILED - [MemberID: " + inp.memberid + "], [Exception: " + ex + "]");
                    res.ERROR_MSG = "NoochController Exception - 585";
                }
            }
            catch (Exception we)
            {
                Logger.Error("NoochController -> MFALogin FAILED - [MemberID: " + inp.memberid +
                                   "], [Exception: " + we + "]");
            }

            return Json(res);
        }


        /// <summary>
        /// Method for verifying bank MFA microdeposits - used only for a bank added with routing and account numbers.
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="memberid"></param>
        /// <param name="MicroDepositOne"></param>
        /// <param name="MicroDepositTwo"></param>
        /// <param name="ba"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MFALoginWithRoutingAndAccountNumber")]
        public SynapseBankLoginRequestResult MFALoginWithRoutingAndAccountNumber(string bank, string memberid, string MicroDepositOne, string MicroDepositTwo, string ba)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();

            try
            {
                Logger.Info("NoochController -> MFALoginWithRoutingAndAccountNumber Initiated -> [MemberID: " + memberid + "], [Bank: " + bank + "]");

                // preparing data for POST type request

                var scriptSerializer = new JavaScriptSerializer();
                string json;

                SynapseV3VerifyNodeWithMicroDeposits_ServiceInput inpu = new SynapseV3VerifyNodeWithMicroDeposits_ServiceInput();
                inpu.BankName = bank; // not required..keeping it for just in case we need something to do with it.
                inpu.MemberId = memberid;
                inpu.microDespositOne = MicroDepositOne;
                inpu.microDespositTwo = MicroDepositTwo;
                inpu.bankId = ba;   // this is bank_node_id..... must...need to pass this thing in earlier step
                try
                {
                    json = "{\"input\":" + scriptSerializer.Serialize(inpu) + "}";

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "/SynapseV3MFABankVerifyWithMicroDeposits";
                    SynapseBankLoginRequestResult bnkloginresult = ResponseConverter<SynapseBankLoginRequestResult>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);


                    if (bnkloginresult.Is_success == true)
                    {
                        res.Is_success = true;
                        res.Is_MFA = bnkloginresult.Is_MFA;
                        res.SynapseBanksList = bnkloginresult.SynapseBanksList;
                        res.SynapseQuestionBasedResponse = bnkloginresult.SynapseQuestionBasedResponse;
                        res.ERROR_MSG = "OK";
                    }
                    else
                    {
                        Logger.Error("NoochController -> MFALoginWithRoutingAndAccountNumber FAILED -> [Error Msg: " + bnkloginresult.ERROR_MSG +
                                               "], [MemberID: " + memberid + "], [Bank: " + bank + "]");
                        res.Is_success = false;
                        res.ERROR_MSG = bnkloginresult.ERROR_MSG;
                    }
                }
                catch (Exception ec)
                {
                    res.Is_success = false;
                    res.ERROR_MSG = "";
                    Logger.Error("NoochController -> MFALoginWithRoutingAndAccountNumber FAILED - [MemberID: " + memberid +
                                  "], [Exception: " + ec + "]");
                }
            }
            catch (Exception we)
            {
                Logger.Error("NoochController -> MFALoginWithRoutingAndAccountNumber FAILED - [MemberID: " + memberid +
                                   "], [Exception: " + we + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("SetDefaultBank")]
        public ActionResult SetDefaultBank(setDefaultBankInput input)
        {
            Logger.Info("**Add_Bank** CodeBehind -> SetDefaultBank Initiated - [MemberID: " + input.MemberId +
                                   "], [Bank Name: " + input.BankName + "], [BankID: " + input.BankOId + "]");

            SynapseBankSetDefaultResult res = new SynapseBankSetDefaultResult();

            try
            {
                if (String.IsNullOrEmpty(input.MemberId) ||
                String.IsNullOrEmpty(input.BankName) ||
                String.IsNullOrEmpty(input.BankOId))
                {
                    if (String.IsNullOrEmpty(input.BankName))
                    {
                        res.Message = "Invalid data - need Bank Name";
                    }
                    else if (String.IsNullOrEmpty(input.MemberId))
                    {
                        res.Message = "Invalid data - need MemberId";
                    }
                    else if (String.IsNullOrEmpty(input.BankOId))
                    {
                        res.Message = "Invalid data - need Bank Id";
                    }

                    res.Is_success = false;
                }
                else
                {
                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                    string serviceMethod = "/SetSynapseDefaultBank?MemberId=" + input.MemberId + "&BankName=" + input.BankName + "&BankId=" + input.BankOId;
                    SynapseBankSetDefaultResult bnkloginresult = ResponseConverter<SynapseBankSetDefaultResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    res.Is_success = bnkloginresult.Is_success;
                    res.Message = bnkloginresult.Message;
                }
            }
            catch (Exception we)
            {
                Logger.Error("**Add_Bank** CodeBehind -> SetDefaultBank FAILED - [MemberID: " + input.MemberId +
                                   "], [Exception: " + we.InnerException + "]");
            }
            return Json(res);
        }

        #endregion Add Bank Page


        #region DepositMoney Page

        public ActionResult DepositMoney()
        {
            ResultDepositMoney rdm = new ResultDepositMoney();
            Logger.Info("DepositMoney CodeBehind -> Page_load Initiated - [TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");
            rdm.payreqInfo = true;
            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]))
                {
                    // Check if this payment is for Rent Scene
                    if (Request.Params.AllKeys.Contains("rs") && Request["rs"] == "1")
                    {
                        Logger.Info("DepositMoney CodeBehind -> Page_load - RENT SCENE Transaction Detected");
                        rdm.rs = "true";
                    }

                    if (!String.IsNullOrEmpty(Request.QueryString["UserType"]))
                    {
                        string n = Request.QueryString["UserType"].ToString();
                        rdm.usrTyp = CommonHelper.GetDecryptedData(n);
                        Logger.Info("DepositMoney CodeBehind -> Page_load - UserType is: [" + rdm.usrTyp + "]");
                    }

                    // CIP is new for Synapse V3 and tells the page what type of ID verification the new user will need.
                    if (Request.Params.AllKeys.Contains("cip"))
                    {
                        rdm.cip = Request.QueryString["cip"].ToString();
                        Logger.Info("DepositMoney CodeBehind -> Page_load - CIP is: [" + rdm.cip + "]");
                    }

                    rdm = GetTransDetailsForDepositMoney(Request.QueryString["TransactionId"].ToString(), rdm);
                    Response.Write("<script>var errorFromCodeBehind = '0';</script>");
                }
                else
                {
                    // something wrong with query string
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                    rdm.payreqInfo = false;
                }

                rdm.PayorInitialInfo = false;
            }
            catch (Exception ex)
            {
                rdm.payreqInfo = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");

                Logger.Error("DepositMoney CodeBehind -> page_load OUTER EXCEPTION - [TransactionID: " + Request.QueryString["TransactionId"] +
                             "], [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoaddata"] = rdm;
            return View();
        }


        public ResultDepositMoney GetTransDetailsForDepositMoney(string TransactionId, ResultDepositMoney resultDepositMoney)
        {
            ResultDepositMoney rdm = resultDepositMoney;
            Logger.Info("DepositMoney CodeBehind -> GetTransDetails Initiated - TransactionID: [" + TransactionId + "]");

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            Logger.Info("DepositMoney CodeBehind -> GetTransDetails - URL to query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("DepositMoney CodeBehind -> GetTransDetails FAILED - Transaction Not Found - TransactionId: [" + TransactionId + "]");

                rdm.payreqInfo = false;
                rdm.pymnt_status = "0";
                Response.Write("<script>errorFromCodeBehind = '1';</script>");
            }
            else
            {
                rdm.transId = transaction.TransactionId;
                rdm.pymnt_status = transaction.TransactionStatus.ToLower();

                rdm.transMemo = transaction.Memo;

                rdm.senderImage = transaction.SenderPhoto;
                rdm.senderName1 = transaction.Name;
                rdm.bnkName = transaction.BankName;
                string s = transaction.Amount.ToString("n2");
                string[] s1 = s.Split('.');
                if (s1.Length == 2)
                {
                    rdm.transAmountd = s1[0].ToString();
                    rdm.transAmountc = s1[1].ToString();
                }
                else
                {
                    rdm.transAmountd = s1[0].ToString();
                    rdm.transAmountc = "00";
                }

                rdm.memidexst = !String.IsNullOrEmpty(transaction.RecepientId)
                                     ? transaction.RecepientId
                                     : "";

                // Now check what TYPE of invitation (phone or email)
                rdm.invitationType = transaction.IsPhoneInvitation == true ? "p" : "e";

                if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
                {
                    rdm.invitationSentto = transaction.InvitationSentTo;
                }
            }
            return rdm;
        }


        public ActionResult RegisterUserWithSynpForDepositMoney(string transId, string memberId, string userEm, string userPh, string userName,
                                                                string userPw, string ssn, string dob, string address, string zip, string fngprnt,
                                                                string ip, string cip, string fbid, bool isRentScene, string isIdImage = "0", string idImagedata = "")
        {
            Logger.Info("DepositMoney Code Behind -> RegisterUserWithSynpForDepositMoney Initiated - Email: [" + userEm +
                        "], TransID: [" + transId + "], memberId: [" + memberId +
                        "], CIP: [" + cip + "], FBID: [" + fbid +
                        "], isRentScene: [" + isRentScene + "]");

            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";
            res.memberIdGenerated = "";
            res.reason = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RegisterNonNoochUserWithSynapse";

                userPh = CommonHelper.RemovePhoneNumberFormatting(userPh);

                Logger.Info("DepositMoney Code Behind -> RegisterUserWithSynpForDepositMoney -> PARAMETERS for '/RegisterNonNoochUserWithSynapse': transId: " + transId +
                            ", memberId (If existing user): " + memberId + ", userEm: " + userEm +
                            ", userPh: " + userPh + ", userPw: " + userPw +
                            ", ssn: " + ssn + ", dob: " + dob +
                            ", address: " + address + ", zip: " + zip +
                            ", CIP: [" + cip + "], FBID: [" + fbid + "]");

                RegisterUserWithSynapseV3_Input inputClass = new RegisterUserWithSynapseV3_Input();
                inputClass.address = address;
                inputClass.dob = dob;
                inputClass.email = userEm;
                inputClass.fngprnt = fngprnt;
                inputClass.fullname = userName;
                inputClass.idImageData = idImagedata;
                inputClass.ip = ip;
                inputClass.isIdImageAdded = isIdImage;
                inputClass.phone = userPh;
                inputClass.pw = userPw;
                inputClass.ssn = ssn;
                inputClass.transId = transId;
                inputClass.zip = zip;
                inputClass.isRentScene = isRentScene != null ? isRentScene : false;

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputClass);

                Logger.Info("DepositMoney Code-Behind -> RegisterUserWithSynpForDepositMoney - Full Query String: [ " + String.Concat(serviceUrl, serviceMethod) + " ]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (regUserResponse.success == "True")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else if (regUserResponse.success == "False")
                {
                    Logger.Error("DepositMoney Code-Behind -> RegisterUserWithSynpForDepositMoney FAILED - SERVER RETURNED 'success' = 'false' - [TransID: " + transId + "]");
                    res.reason = regUserResponse.reason;
                }
                else
                {
                    Logger.Error("DepositMoney Code-Behind -> RegisterUserWithSynpForDepositMoney FAILED - UNKNOWN ERROR FROM SERVER - [TransID: " + transId + "]");
                }

                res.ssn_verify_status = regUserResponse.ssn_verify_status;

                return Json(res);
            }
            catch (Exception ex)
            {
                Logger.Error("DepositMoney Code-Behind -> RegisterUserWithSynpForDepositMoney attempt FAILED Failed, Reason: [" + res.reason + "], " +
                             "TransId: [" + transId + "], [Exception: " + ex + "]");
                return Json(res);
            }
        }

        #endregion DepositMoney Page


        #region DepositMoneyComplete Page

        public ActionResult DepositMoneyComplete()
        {
            ResultDepositMoneyComplete rdmc = new ResultDepositMoneyComplete();
            rdmc.paymentSuccess = false;
            rdmc.payinfobar = true;

            Logger.Info("DepositMoneyComplete CodeBehind -> page_load Initiated - 'mem_id' Parameter In URL: [" + Request.QueryString["mem_id"] + "]");

            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["mem_id"]))
                {
                    string[] allQueryStrings = (Request.QueryString["mem_id"]).Split(',');

                    if (allQueryStrings.Length > 1)
                    {
                        Response.Write("<script>var errorFromCodeBehind = '0';</script>");

                        string mem_id = allQueryStrings[0];
                        string tr_id = allQueryStrings[1];
                        string isForRentScene = allQueryStrings[2];

                        // Check if this payment is for Rent Scene
                        if (isForRentScene == "true")
                        {
                            Logger.Info("DepositMoneyComplete CodeBehind -> Page_load - RENT SCENE Transaction Detected - TransID: [" + tr_id + "]");
                            rdmc.rs = "true";
                        }

                        // Getting transaction details to check if transaction is still pending
                        rdmc = GetTransDetailsForDepositMoneyComplete(tr_id, rdmc);

                        if (rdmc.IsTransactionStillPending)
                        {
                            rdmc = finishTransaction(mem_id, tr_id, rdmc);
                        }
                    }
                    else
                    {
                        Logger.Error("DepositMoneyComplete CodeBehind -> page_load ERROR - 'mem_id' in query string did not have 2 parts as expected - [mem_id Parameter: " + Request.QueryString["mem_id"] + "]");
                        Response.Write("<script>var errorFromCodeBehind = '2';</script>");
                    }
                }
                else
                {
                    // something wrong with query string
                    Logger.Error("depositMoneyComplete CodeBehind -> page_load ERROR - 'mem_id' in query string was NULL or empty [mem_id Parameter: " + Request.QueryString["mem_id"] + "]");
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("depositMoneyComplete CodeBehind -> page_load OUTER EXCEPTION - [mem_id Parameter: " + Request.QueryString["mem_id"] +
                                       "], [Exception: " + ex + "]");
                rdmc.payinfobar = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
            }

            ViewData["OnLoaddata"] = rdmc;

            return View();
        }


        public ResultDepositMoneyComplete GetTransDetailsForDepositMoneyComplete(string TransactionId, ResultDepositMoneyComplete resultDepositMoneyComplete)
        {
            ResultDepositMoneyComplete rdmc = resultDepositMoneyComplete;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("depositMoneyComplete CodeBehind -> getTransDetails FAILED - Transaction was NULL [TransId: " + TransactionId + "]");

                Response.Write("<script>errorFromCodeBehind = '3';</script>");
                rdmc.IsTransactionStillPending = false;
                return rdmc;
            }
            else
            {
                rdmc.senderImage = transaction.SenderPhoto;
                rdmc.senderName1 = transaction.Name;
                rdmc.transAmountd = transaction.Amount.ToString("n2");
                rdmc.transMemo = transaction.Memo;

                // Check if this was a Rent request from a Landlord
                //if (!String.IsNullOrEmpty(transaction.TransactionType) &&
                //    transaction.TransactionType == "Rent")
                //{
                //    rdmc.usrTyp = "Tenant";
                //    rdmc.payeeMemId = !String.IsNullOrEmpty(transaction.MemberId) ? transaction.MemberId : "none";
                //}

                //// Check if this was a request to an existing, but 'NonRegistered' User
                //else if (transaction.IsExistingButNonRegUser == true)
                //{
                //    rdmc.usrTyp = "Existing";
                //    rdmc.payeeMemId = !String.IsNullOrEmpty(transaction.MemberId) ? transaction.MemberId : "none";
                //}

                #region Check If Still Pending

                Response.Write("<script>var isStillPending = true;</script>");

                if (transaction.TransactionStatus != "Pending")
                {
                    Response.Write("<script>isStillPending = false;</script>");
                    rdmc.IsTransactionStillPending = false;
                    return rdmc;
                }

                #endregion Check If Still Pending
            }

            rdmc.IsTransactionStillPending = true;
            return rdmc;
        }


        private ResultDepositMoneyComplete finishTransaction(string MemberIdAfterSynapseAccountCreation, string TransactionId, ResultDepositMoneyComplete resultDepositMoneyComplete)
        {
            ResultDepositMoneyComplete rdmc = resultDepositMoneyComplete;
            rdmc.paymentSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit?TransactionId=" + TransactionId +
                                       "&MemberIdAfterSynapseAccountCreation=" + MemberIdAfterSynapseAccountCreation +
                                       "&TransactionType=SentToNewUser";

                if ((rdmc.usrTyp == "Existing" || rdmc.usrTyp == "Tenant") &&
                     rdmc.payeeMemId.Length > 5)
                {
                    serviceMethod = serviceMethod + "&recipMemId=" + rdmc.payeeMemId;
                }
                else
                {
                    serviceMethod = serviceMethod + "&recipMemId=";
                }
                Logger.Info("DepositMoneyComplete CodeBehind -> finishTransaction - About to Query Nooch Service to move money - URL: ["
                                      + String.Concat(serviceUrl, serviceMethod) + "]");

                TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (transaction != null)
                {
                    if (transaction.synapseTransResult == "Success")
                    {
                        rdmc.paymentSuccess = true;
                    }
                    else
                    {
                        Logger.Error("DepositMoneyComplete CodeBehind -> completeTrans FAILED - TransId: [" + TransactionId + "]");
                        Response.Write("<script>errorFromCodeBehind = 'failed';</script>");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("depositMoneyComplete CodeBehind -> completeTrans FAILED - TransId: [" + TransactionId +
                             "], Exception: [" + ex + "]");
            }

            return rdmc;
        }

        #endregion DepositMoneyComplete Page


        #region Reset Password Page

        public ActionResult ResetPassword()
        {
            ResultResetPassword resultResetPassword = new ResultResetPassword();

            string strUserAgent = Request.UserAgent.ToLower();
            resultResetPassword.requestExpiredorNotFound = false;

            if (strUserAgent != null)
            {

                if (Request.Browser.IsMobileDevice || strUserAgent.Contains("iphone") ||
                      strUserAgent.Contains("mobile"))
                {
                    resultResetPassword.clientScript = "<script>Show('iPhoneButton','ctl00_detailContentPlaceHolder_activationLinkButton')</script>";
                }
                else
                {
                    resultResetPassword.clientScript = "<script>Show('ctl00_detailContentPlaceHolder_newPasswordLinkButton','iPhoneButton')</script>";
                }
            }

            resultResetPassword.ResetPasswordMessageLabel = false;
            resultResetPassword.messageLabel = false;
            resultResetPassword = bindusermail(resultResetPassword);
            ViewData["OnLoaddata"] = resultResetPassword;
            return View();
        }


        public string ResetPasswordButton_Click(string PWDText, string memberId, string newUser = "")
        {
            var objAesAlgorithm = new AES();
            string encryptedPassword = objAesAlgorithm.Encrypt(PWDText.Trim(), string.Empty);
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            // serviceMethod = "/ResetPassword?memberId=" + memberId + "&newPassword=" + encryptedPassword + "&newUser=true";
            serviceMethod = "/ResetPassword?memberId=" + memberId + "&newPassword=" + encryptedPassword + "&newUser=" + newUser;

            var isMemberPwdResetted = ResponseConverter<Nooch.Common.Entities.BoolResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
            if (isMemberPwdResetted.Result)
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }


        public ActionResult pinNumberVerificationButton_Click(string PINTextBox, string memberId)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();

            var objAesAlgorithm = new AES();
            string encryptedPin = objAesAlgorithm.Encrypt(PINTextBox.Trim(), string.Empty);
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            serviceMethod = "/ValidatePinNumberForPasswordForgotPage?memberId=" + memberId + "&pinNo=" + encryptedPin;

            var isMemberValid = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
            if (isMemberValid.Result.Equals("Success"))
            {
                Page p = HttpContext.Handler as Page;
                p.RegisterStartupScript("showButton", "<script>Show('resetPasswordDiv','pinNumberVerificationDiv')</script>");
                res.isSuccess = true;

                return Json(res);
            }
            else
            {
                res.isSuccess = false;
                res.msg = isMemberValid.Result.ToString();
                return Json(res);
            }
        }


        ResultResetPassword bindusermail(ResultResetPassword rrp)
        {
            ResultResetPassword resultResetPass = rrp;
            string memberId = Request.QueryString["memberId"];

            if (!String.IsNullOrEmpty(memberId))
            {
                string serviceMethod = string.Empty;
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                serviceMethod = "/GetMemberUsernameByMemberId?memberId=" + memberId;

                var isMemberPwdReset = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
                if (isMemberPwdReset.Result != null)
                {
                    resultResetPass.usermail = isMemberPwdReset.Result;
                    resultResetPass = resetlinkvalidationcheck(resultResetPass);
                }
                else
                {
                    resultResetPass.invalidUser = "true";
                    rrp.pin = false;

                }
            }
            else
            {
                resultResetPass.invalidUser = "true";
                rrp.pin = false;

            }

            return resultResetPass;
        }


        ResultResetPassword resetlinkvalidationcheck(ResultResetPassword rrp)
        {
            ResultResetPassword resultResetPass = rrp;
            string memberId = Request.QueryString["memberId"];
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = string.Empty;
            serviceMethod = "/resetlinkvalidationcheck?memberId=" + memberId;
            var isMemberPwdResetted = ResponseConverter<Nooch.Common.Entities.BoolResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
            if (isMemberPwdResetted.Result == false)
            {
                resultResetPass.requestExpiredorNotFound = true;
                resultResetPass.pin = false;
            }

            return resultResetPass;
        }

        #endregion Reset Password Page


        #region PayRequest Page

        public ActionResult PayRequest()
        {
            ResultPayRequest rpr = new ResultPayRequest();
            rpr.payreqInfo = true;
            rpr.PayorInitialInfo = false; // <div> containing the old form for entering name/email/phone...keeping just for reference

            Logger.Info("payRequest CodeBehind -> Page_load Initiated - TransactionId Parameter: [" + Request.QueryString["TransactionId"] + "]");

            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]))
                {
                    if (!String.IsNullOrEmpty(Request.QueryString["UserType"]))
                    {
                        string n = Request.QueryString["UserType"].ToString();
                        rpr.usrTyp = CommonHelper.GetDecryptedData(n);
                        Logger.Info("payRequest CodeBehind -> Page_load - UserType is: [" + rpr.usrTyp + "]");
                    }

                    // CIP is new for Synapse V3 and tells the page what type of ID verification the new user will need.
                    if (Request.Params.AllKeys.Contains("cip"))
                    {
                        rpr.cip = Request.QueryString["cip"].ToString();
                        Logger.Info("payRequest CodeBehind -> Page_load - CIP is: [" + rpr.cip + "]");
                    }

                    // Check if this is a RENT Payment request (from a Landlord)
                    if (Request.Params.AllKeys.Contains("IsRentTrans"))
                    {
                        if (Request["IsRentTrans"].ToLower() == "true")
                        {
                            Logger.Info("payRequest CodeBehind -> Page_load - RENT PAYMENT Detected");

                            rpr.transType = "rent";
                            rpr.usrTyp = "tenant";
                        }
                    }

                    // Check if this payment is for Rent Scene
                    if (Request.Params.AllKeys.Contains("rs") && Request["rs"] == "1")
                    {
                        Logger.Info("payRequest CodeBehind -> Page_load - RENT SCENE Transaction Detected");
                        rpr.rs = "true";
                    }

                    Response.Write("<script>var errorFromCodeBehind = '0';</script>");

                    rpr = GetTransDetailsForPayRequest(Request.QueryString["TransactionId"].ToString(), rpr);
                }
                else
                {
                    // something wrong with query string
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                    rpr.payreqInfo = false;
                }

                rpr.PayorInitialInfo = false;
            }
            catch (Exception ex)
            {
                rpr.payreqInfo = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");

                Logger.Error("payRequest CodeBehind -> page_load OUTER EXCEPTION - [TransactionId Parameter: " + Request.QueryString["TransactionId"] +
                                       "], [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoaddata"] = rpr;

            return View();
        }


        public ResultPayRequest GetTransDetailsForPayRequest(string TransactionId, ResultPayRequest resultPayRequest)
        {
            ResultPayRequest rpr = resultPayRequest;
            Logger.Info("payRequest CodeBehind -> GetTransDetails Initiated - TransactionID: [" + TransactionId + "]");

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            Logger.Info("payRequest CodeBehind -> GetTransDetails - URL to query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("payRequest CodeBehind -> GetTransDetails FAILED - Transaction Not Found - TransactionId: [" + TransactionId + "]");

                rpr.payreqInfo = false;
                rpr.pymnt_status = "0";
                Response.Write("<script>errorFromCodeBehind = '1';</script>");
            }
            else
            {
                if (transaction.MemberId == "852987e8-d5fe-47e7-a00b-58a80dd15b49")
                {
                    rpr.rs = "true";
                }

                rpr.transId = transaction.TransactionId;
                rpr.pymnt_status = transaction.TransactionStatus.ToLower();

                rpr.transMemo = transaction.Memo;

                rpr.senderImage = transaction.RecepientPhoto;
                rpr.senderName1 = (!String.IsNullOrEmpty(transaction.RecepientName) && transaction.RecepientName.Length > 2) ?
                                     transaction.RecepientName :
                                     transaction.Name;

                string s = transaction.Amount.ToString("n2");
                string[] s1 = s.Split('.');
                if (s1.Length == 2)
                {
                    rpr.transAmountd = s1[0].ToString();
                    rpr.transAmountc = s1[1].ToString();
                }
                else
                {
                    rpr.transAmountd = s1[0].ToString();
                    rpr.transAmountc = "00";
                }

                // Check if this was a request to an existing, but 'NonRegistered' User
                if (transaction.IsExistingButNonRegUser == true)
                {
                    if (transaction.TransactionStatus.ToLower() != "pending")
                    {
                        Logger.Info("payRequest CodeBehind -> GetTransDetails - IsExistingButNonRegUser = 'true', but Transaction no longer pending!");
                    }

                    rpr.memidexst = !String.IsNullOrEmpty(transaction.RecepientId)
                                      ? transaction.RecepientId
                                      : "";

                    rpr.bnkName = transaction.BankName;
                    rpr.bnkNickname = transaction.BankId;

                    if (transaction.BankName == "no bank found")
                    {
                        Logger.Error("payRequest CodeBehind -> GetTransDetails - IsExistingButNonRegUser = 'true', but No Bank Found, so JS should display Add-Bank iFrame.");
                    }
                    else
                    {
                        rpr.nonRegUsrContainer = true;
                    }
                }

                else if (rpr.transType == "rent") // Set in Page_Load above based on URL query string
                {
                    Logger.Info("payRequest CodeBehind -> GetTransDetails - Got a RENT Payment!");

                    rpr.memidexst = !String.IsNullOrEmpty(transaction.RecepientId)
                                      ? transaction.RecepientId
                                      : "";
                }

                // Now check what TYPE of invitation (phone or email)
                rpr.invitationType = transaction.IsPhoneInvitation == true ? "p" : "e";

                if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
                {
                    rpr.invitationSentto = transaction.InvitationSentTo;
                }
            }
            return rpr;
        }


        public ActionResult RegisterUserWithSynpForPayRequest(string transId, string memberId, string userEm, string userPh, string userName,
                                                              string userPw, string ssn, string dob, string address, string zip, string fngprnt,
                                                              string ip, string cip, string fbid, bool isRentScene, string isIdImage = "0", string idImagedata = "")
        {
            Logger.Info("PayRequest Code Behind -> RegisterUserWithSynpForPayRequest Initiated - Email: [" + userEm +
                        "], TransID: [" + transId + "], memberId: [" + memberId + "], CIP: [" + cip + "], FBID: [" + fbid + "]");

            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";
            res.memberIdGenerated = "";
            res.reason = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                userPh = CommonHelper.RemovePhoneNumberFormatting(userPh);

                Logger.Info("payRequest Code Behind -> RegisterUserWithSynp -> PARAMETERS to send to Server: transId: " + transId +
                            ", memberId (If existing user): " + memberId + ", userEm: " + userEm +
                            ", userPh: " + userPh + ", userPw: " + userPw +
                            ", ssn: " + ssn + ", dob: " + dob +
                            ", address: " + address + ", zip: " + zip +
                            ", Has ID Img: [" + isIdImage + "], CIP: [" + cip +
                            "], FBID: [" + fbid + "], isRentScene: [" + isRentScene + "]");


                #region Initial Checks

                if (String.IsNullOrEmpty(userEm))
                {
                    res.reason = "Missing user's email address";
                    return Json(res);
                }
                if (String.IsNullOrEmpty(userPh))
                {
                    res.reason = "Missing user's phone";
                    return Json(res);
                }
                if (String.IsNullOrEmpty(dob))
                {
                    res.reason = "Missing user's date of birth";
                    return Json(res);
                }
                if (String.IsNullOrEmpty(address))
                {
                    res.reason = "Missing user's address";
                    return Json(res);
                }
                if (String.IsNullOrEmpty(zip))
                {
                    res.reason = "Missing user's ZIP";
                    return Json(res);
                }

                #endregion Initial Checks

                string serviceMethod = "";
                var scriptSerializer = new JavaScriptSerializer();
                string json = "";

                RegisterUserWithSynapseV3_Input inputClass = new RegisterUserWithSynapseV3_Input();
                inputClass.address = address;
                inputClass.dob = dob;
                inputClass.email = userEm;
                inputClass.fngprnt = fngprnt;
                inputClass.fullname = userName;
                inputClass.isIdImageAdded = isIdImage;
                inputClass.idImageData = idImagedata;
                inputClass.ip = ip;
                inputClass.phone = userPh;
                inputClass.pw = userPw;
                inputClass.ssn = ssn;
                inputClass.transId = transId;
                inputClass.zip = zip;
                inputClass.cip = !String.IsNullOrEmpty(cip) ? cip : "renter";
                inputClass.fbid = fbid;
                inputClass.isRentScene = isRentScene != null ? isRentScene : false;

                if (!String.IsNullOrEmpty(memberId) && memberId.Length > 30)
                {
                    inputClass.memberId = memberId;

                    // Member must already exist, so use RegisterEXISTINGUserWithSynapseV3()
                    serviceMethod = "/RegisterExistingUserWithSynapseV3";
                }
                else
                {
                    // Member DOES NOT already exist, so use RegisterNONNOOCHUserWithSynapse()
                    serviceMethod = "/RegisterNonNoochUserWithSynapse";
                }

                json = scriptSerializer.Serialize(inputClass);

                Logger.Info("PayRequest Code-Behind -> RegisterUserWithSynpForPayRequest - Full Query String: [ " + String.Concat(serviceUrl, serviceMethod) + " ]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (regUserResponse.success.ToLower() == "true")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else if (regUserResponse.success.ToLower() == "false")
                {
                    Logger.Error("PayRequest Code-Behind -> RegisterUserWithSynpForPayRequest FAILED - SERVER RETURNED 'success' = 'false' - [TransID: " + transId + "]");
                    res.reason = regUserResponse.reason;
                }
                else
                {
                    Logger.Error("PayRequest Code-Behind -> RegisterUserWithSynpForPayRequest FAILED - UNKNOWN ERROR FROM SERVER - [TransID: " + transId + "]");
                }

                res.ssn_verify_status = regUserResponse.ssn_verify_status;

                return Json(res);
            }
            catch (Exception ex)
            {
                Logger.Error("PayRequest Code-Behind -> RegisterUserWithSynpForPayRequest attempt FAILED Failed - Reason: [" + res.reason + "], " +
                             "TransId: [" + transId + "], [Exception: " + ex.Message + "]");
                return Json(res);
            }
        }

        #endregion PayRequest Page


        #region PayRequestComplete Page

        public ActionResult PayRequestComplete()
        {
            ResultPayRequestComplete rpc = new ResultPayRequestComplete();

            Logger.Info("PayRequestComplete CodeBehind -> page_load Initiated - 'mem_id' Parameter In URL: [" + Request.QueryString["mem_id"] + "]");

            rpc.paymentSuccess = false;

            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["mem_id"]))
                {
                    string[] allQueryStrings = (Request.QueryString["mem_id"]).Split(',');

                    if (allQueryStrings.Length > 1)
                    {
                        Response.Write("<script>var errorFromCodeBehind = '0';</script>");

                        string mem_id = allQueryStrings[0];
                        string tr_id = allQueryStrings[1];
                        string isForRentScene = allQueryStrings[2];

                        rpc.memId = mem_id;

                        // Check if this payment is for Rent Scene
                        if (isForRentScene == "true")
                        {
                            Logger.Info("PayRequestComplete CodeBehind -> RENT SCENE Transaction Detected - TransID: [" + tr_id + "]");
                            rpc.rs = "true";
                        }

                        // Getting transaction details to check if transaction is still pending
                        rpc = GetTransDetailsForPayRequestComplete(tr_id, rpc);

                        if (rpc.IsTransactionStillPending)
                        {
                            rpc = completeTrans(mem_id, tr_id, rpc);
                        }
                    }
                    else
                    {
                        Logger.Error("PayRequestComplete CodeBehind -> page_load ERROR - 'mem_id' in query string did not have 2 parts as expected - [mem_id Parameter: " + Request.QueryString["mem_id"] + "]");
                        Response.Write("<script>var errorFromCodeBehind = '2';</script>");
                    }
                }
                else
                {
                    // something wrong with query string
                    Logger.Error("PayRequestComplete CodeBehind -> page_load ERROR - 'mem_id' in query string was NULL or empty [mem_id Parameter: " + rpc.memId + "]");
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }

            }
            catch (Exception ex)
            {
                Logger.Error("payRequestComplete CodeBehind -> page_load OUTER EXCEPTION - mem_id Parameter: [" +
                                        rpc.memId + "], [Exception: " + ex + "]");
                rpc.payinfobar = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
            }

            ViewData["OnLoaddata"] = rpc;
            return View();
        }


        private ResultPayRequestComplete completeTrans(string MemberIdAfterSynapseAccountCreation, string TransactionId, ResultPayRequestComplete resultPayComplete)
        {
            ResultPayRequestComplete rpc = resultPayComplete;
            rpc.paymentSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit?TransactionId=" + TransactionId +
                                       "&MemberIdAfterSynapseAccountCreation=" + MemberIdAfterSynapseAccountCreation +
                                       "&TransactionType=RequestToNewUser";

                if ((rpc.usrTyp == "Existing" || rpc.usrTyp == "Tenant") &&
                     rpc.payeeMemId.Length > 5)
                {
                    serviceMethod = serviceMethod + "&recipMemId=" + rpc.payeeMemId;
                }
                else
                {
                    serviceMethod = serviceMethod + "&recipMemId= ";
                }

                Logger.Info("NoochController -> completeTrans - About to Query Nooch Service to move money - URL: [" +
                             String.Concat(serviceUrl, serviceMethod) + "]");

                TransactionDto moveMoneyResult = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (moveMoneyResult != null)
                {
                    if (moveMoneyResult.synapseTransResult == "Success")
                    {
                        rpc.paymentSuccess = true;
                    }
                    else
                    {
                        Logger.Error("NoochController -> completeTrans FAILED - TransId: [" + TransactionId + "]");

                        Response.Write("<script>errorFromCodeBehind = 'failed';</script>");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("payRequestComplete CodeBehind -> completeTrans FAILED - TransId: [" + TransactionId +
                             "], Exception: [" + ex + "]");
            }

            return rpc;
        }


        public ResultPayRequestComplete GetTransDetailsForPayRequestComplete(string TransactionId, ResultPayRequestComplete resultPayRequestComplt)
        {
            Logger.Info("payRequestComplete CodeBehind -> GetTransDetailsForPayRequestComplete Initiated - [TransID: " + TransactionId + "]");

            ResultPayRequestComplete rpc = resultPayRequestComplt;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("payRequestComplete CodeBehind -> getTransDetails FAILED - Transaction was Null [TransId: " + TransactionId + "]");

                Response.Write("<script>errorFromCodeBehind = '3';</script>");
                rpc.IsTransactionStillPending = false;

                return rpc;
            }
            else
            {
                // Logger.LogDebugMessage("** payRequestComplete CodeBehind -> memId.Value: [" + memId.Value + "]");
                // Logger.LogDebugMessage("** payRequestComplete CodeBehind -> transaction.MemberId: [" + transaction.MemberId + "]");
                // Logger.LogDebugMessage("** payRequestComplete CodeBehind -> transaction.TransactionType: [" + transaction.TransactionType + "]");

                rpc.senderImage = transaction.RecepientPhoto;
                rpc.senderName1 = (!String.IsNullOrEmpty(transaction.RecepientName) && transaction.RecepientName.Length > 2) ?
                                    transaction.RecepientName :
                                    transaction.Name;
                rpc.transAmountd = transaction.Amount.ToString("n2");
                rpc.transMemo = transaction.Memo;


                // Check if this was a Rent request from a Landlord
                if (!String.IsNullOrEmpty(transaction.TransactionType) &&
                    transaction.TransactionType == "Rent")
                {
                    Logger.Info("payRequestComplete CodeBehind -> GetTransDetailsForPayRequestComplete - Rent Payment Detected");
                    rpc.usrTyp = "Tenant";
                    rpc.payeeMemId = !String.IsNullOrEmpty(transaction.MemberId) ? transaction.MemberId : "none";
                }

                // Check if this was a request to an existing, but 'NonRegistered' User
                else if (transaction.IsExistingButNonRegUser == true)
                {
                    rpc.usrTyp = "Existing";
                    rpc.payeeMemId = !String.IsNullOrEmpty(transaction.MemberId) ? transaction.MemberId : "none";
                }

                #region Check If Still Pending

                Response.Write("<script>var isStillPending = true;</script>");

                if (transaction.TransactionStatus != "Pending")
                {
                    Logger.Info("payRequestComplete CodeBehind -> GetTransDetailsForPayRequestComplete - Payment No Longer Pending - [TransID: " + TransactionId + "]");

                    Response.Write("<script>isStillPending = false;</script>");
                    rpc.IsTransactionStillPending = false;
                    return rpc;
                }

                #endregion Check If Still Pending
            }
            rpc.IsTransactionStillPending = true;

            return rpc;
        }

        #endregion PayRequestComplete Page


        #region CreateAccount Page

        public ActionResult createAccount(string rs, string TransId, string type, string memId)
        {
            ResultcreateAccount rca = new ResultcreateAccount();

            try
            {
                rca.memId = memId;

                if (!String.IsNullOrEmpty(Request.QueryString["rs"]))
                {
                    Logger.Info("createAccount CodeBehind -> Page_load Initiated - Is a RentScene Payment: [" + Request.QueryString["rs"] + "]");

                    rca.rs = Request.QueryString["rs"].ToLower() == "true" || Request.QueryString["rs"].ToLower() == "yes" ? "true" : "false";
                }

                if (!String.IsNullOrEmpty(Request.QueryString["TransId"]))
                {
                    Logger.Info("createAccount CodeBehind -> Page_load Initiated - [TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");

                    Session["TransId"] = Request.QueryString["TransId"];

                    rca = GetTransDetailsForCreateAccount(Request.QueryString["TransId"].ToString(), rca);
                }
                else if (!String.IsNullOrEmpty(Request.QueryString["type"]))
                {
                    rca.type = Request.QueryString["type"];

                    if (String.IsNullOrEmpty(Request.QueryString["memId"]))
                    {
                        // No MemberID in URL, so must be creating a brand NEW user
                        rca.errorId = "0";
                        rca.isNewUser = true;
                    }
                    else
                    {
                        Logger.Info("createAccount CodeBehind -> Page_load Initiated - MemberID Parameter: [" + Request.QueryString["memId"] + "]");

                        rca = GetMemberDetailsForCreateAccount(Request.QueryString["memId"], rca);
                    }
                }
                else
                {
                    rca.errorId = "2";
                }
            }
            catch (Exception ex)
            {
                rca.errorId = "1";

                Logger.Error("payRequest CodeBehind -> page_load OUTER EXCEPTION - [TransactionId Parameter: " + Request.QueryString["TransactionId"] +
                             "], [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoaddata"] = rca;

            return View();
        }


        public ResultcreateAccount GetTransDetailsForCreateAccount(string TransactionId, ResultcreateAccount resultcreateAccount)
        {
            ResultcreateAccount rca = resultcreateAccount;
            Logger.Info("createAccount Code Behind -> GetTransDetails Initiated - TransactionId: [" + TransactionId + "]");

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                rca.errorId = "3";
            }
            else
            {
                rca.transId = transaction.TransactionId;
                if (transaction.IsPhoneInvitation == true)
                {
                    rca.transType = "phone";
                    rca.sentTo = transaction.PhoneNumberInvited;
                }
                else
                {
                    rca.transType = "em";
                    rca.sentTo = transaction.InvitationSentTo;
                }

                rca.errorId = "0";
            }

            return rca;
        }


        public ResultcreateAccount GetMemberDetailsForCreateAccount(string memberId, ResultcreateAccount resultcreateAccount)
        {
            ResultcreateAccount rca = resultcreateAccount;

            try
            {
                //Logger.Info("createAccount Code Behind -> GetMemberDetailsForCreateAccount Initiated - MemberID: [" + memberId + "]");

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/GetMemberDetailsForLandingPage?memberId=" + memberId;

                Logger.Info("createAccount Code Behind -> GetMemberDetailsForCreateAccount - URL to Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

                MemberDto member = ResponseConverter<MemberDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (member == null)
                {
                    Logger.Error("createAccount Code Behind -> GetMemberDetailsForCreateAccount FAILED - Server did not find a user with the MemberID: [" + memberId + "]");
                    rca.errorId = "4";
                }
                else
                {
                    rca.isNewUser = false;
                    rca.memId = member.MemberId.ToString();
                    rca.name = member.FirstName + " " + member.LastName;
                    rca.address = member.Address;
                    rca.city = member.City;
                    rca.zip = member.Zip;
                    rca.dob = member.DateOfBirth;
                    rca.email = member.UserName;
                    rca.phone = member.ContactNumber;
                    rca.ssn = member.ssnLast4;
                    rca.fngprnt = member.fngrprnt;

                    if (member.companyName != null && member.companyName.Length > 3)
                    {
                        rca.nameInNav = member.companyName;
                        rca.nameInNavContainer = true;

                        if (rca.nameInNav == "Realty Mark llc")
                        {
                            rca.nameInNav = "Realty Mark LLC";
                        }
                    }
                    else if (rca.name.Length > 2)
                    {
                        rca.nameInNav = rca.name;
                        rca.nameInNavContainer = true;

                        if (rca.name == "Realty Mark llc")
                        {
                            rca.name = "";
                            rca.nameInNav = "Realty Mark LLC";
                        }
                    }

                    rca.errorId = "0";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("createAccount Code Behind -> GetMemberDetails FAILED - Outer Exception: [" + ex + "]");
            }

            return rca;
        }


        [HttpPost]
        [ActionName("saveMemberInfo")]
        public ActionResult saveMemberInfo(ResultcreateAccount userData)
        {
            Logger.Info("Create Account Code-Behind -> saveMemberInfo Initiated - MemberID: [" + userData.memId +
                        "], Name: [" + userData.name + "], Email: [" + userData.email +
                        "], Phone: [" + userData.phone + "], DOB: [" + userData.dob +
                        "], SSN: [" + userData.ssn + "], Address: [" + userData.address +
                        "], IP: [" + userData.ip + "], Is Image Sent: [" + userData.isIdImage +
                        "], FBID: [" + userData.fbid + "], isRentScene: [" + userData.rs + "]");

            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial - code behind";

            try
            {
                // Determine if this is for a new or existing user
                bool newUser = String.IsNullOrEmpty(userData.memId);

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = newUser ? "/RegisterNonNoochUserWithSynapse" : "/RegisterExistingUserWithSynapseV3";
                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                RegisterUserWithSynapseV3_Input inputClass = new RegisterUserWithSynapseV3_Input();
                inputClass.fullname = userData.name;
                inputClass.email = userData.email;
                inputClass.phone = userData.phone;
                inputClass.address = userData.address;
                inputClass.zip = userData.zip;
                inputClass.dob = userData.dob;
                inputClass.ssn = userData.ssn;
                inputClass.fngprnt = userData.fngprnt;
                inputClass.ip = userData.ip;
                inputClass.fbid = userData.fbid;
                inputClass.idImageData = userData.idImagedata;
                inputClass.isIdImageAdded = userData.isIdImage;
                inputClass.pw = userData.pw;
                inputClass.memberId = userData.memId;
                inputClass.transId = userData.transId;
                inputClass.isRentScene = userData.rs == "true" ? true : false;

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputClass);


                Logger.Info("Create Account Code-Behind -> saveMemberInfo CHECKPOINT #1 - New User?: [" + newUser + "], URL To Use: [" + urlToUse + "]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                //Logger.Info("Create Account Code-Behind -> saveMemberInfo RESULT: [" + scriptSerializer.Serialize(regUserResponse) + "]");

                if (regUserResponse.success.ToLower() == "true")
                {
                    Logger.Info("Create Account Code-Behind -> saveMemberInfo SUCCESS! - Message: [" + regUserResponse.reason + "]"); 
                    res.success = true;
                    res.msg = "Successfully updated member record on server!";
                }
                else
                {
                    Logger.Info("Create Account Code-Behind -> saveMemberInfo FAILED! - Message: [" + regUserResponse.reason + "]"); 
                    res.msg = regUserResponse.reason;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Create Account Code-Behind -> saveMemberInfo FAILED - MemberID: [" + userData.memId + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during saveMemberInfo";
            }

            return Json(res);
        }


        // CLIFF (5/25/16): Commenting out this method because it is almost a duplicate of SaveMemberInfo() above
        //                  Couple minor differences I still need to account for above, but once finished I will delete this block.
        /*[HttpPost]
        [ActionName("CreateAccountInDB")]
        public ActionResult CreateAccountInDB(ResultcreateAccount userData)
        {
            Logger.Info("Create Account Code-Behind -> CreateAccountInDB Initiated - MemberID: [" + userData.memId +
                        "], Name: [" + userData.name + "], Email: [" + userData.email +
                        "], Phone: [" + userData.phone + "], DOB: [" + userData.dob +
                        "], SSN: [" + userData.ssn + "], Address: [" + userData.address +
                        "], IP: [" + userData.ip + "], Is Image Sent: [" + userData.isIdImage + "]");

            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial - code behind";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RegisterNonNoochUserWithSynapse";
                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                // Member DOES NOT already exist, so use RegisterNONNOOCHUserWithSynapse()
                RegisterUserWithSynapseV3_InputClass inputClass = new RegisterUserWithSynapseV3_InputClass();
                inputClass.fullname = userData.name;
                inputClass.email = userData.email;
                inputClass.phone = userData.phone;
                inputClass.address = userData.address;
                inputClass.zip = userData.zip;
                inputClass.dob = userData.dob;
                inputClass.ssn = userData.ssn;
                inputClass.fngprnt = userData.fngprnt;
                inputClass.ip = userData.ip;
                inputClass.idImageData = userData.idImagedata;
                inputClass.isIdImageAdded = userData.isIdImage;
                inputClass.pw = userData.pw;
                inputClass.transId = userData.transId;

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputClass);

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                Logger.Info("Create Account Code-Behind -> CreateAccountInDB RESULT: [" + Json(regUserResponse) + "]");

                if (regUserResponse.success == "True")
                {
                    res.success = true;
                    res.msg = "Successfully updated member record on server!";
                }
                else
                {
                    res.msg = regUserResponse.reason;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Create Account Code-Behind -> CreateAccountInDB FAILED - MemberID: [" + userData.memId + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during saveMemberInfo";
            }

            return Json(res);

            // checkEmailMsg.Visible = true;
            // if (serviceResult.Result == "Thanks for registering! Check your email to complete activation.")
            // {
            //    transResult.Text = serviceResult.Result;
            // } 
            // else {
            //    transResult.Visible = true;
            //    transResult.Text = serviceResult.Result;
            //    checkEmailMsg.Visible = true;
            // }
        }*/

        #endregion CreateAccount Page


        #region RejectMoney Page

        public ActionResult RejectMoney(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            PageLoadDataRejectMoney res = new PageLoadDataRejectMoney();

            Logger.Info("rejectMoney CodeBehind -> Page_load Initiated - [TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");

            try
            {
                // TransId - transaction id from query string
                // UserType - tells us if user opening link is existing, nonregistered, or completelty brand new user -- need this to show hide create account form later
                // LinkSource - tells us if user is coming from email link or SMS -- need this later to pre-fill create account form

                if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]) &&
                    !String.IsNullOrEmpty(Request.QueryString["UserType"]) &&
                    !String.IsNullOrEmpty(Request.QueryString["LinkSource"]) &&
                    !String.IsNullOrEmpty(Request.QueryString["TransType"]))
                {
                    Session["TransactionId"] = Request.QueryString["TransactionId"];
                    Session["UserType"] = Request.QueryString["UserType"];
                    Session["LinkSource"] = Request.QueryString["LinkSource"];
                    Session["TransType"] = Request.QueryString["TransType"];

                    res.errorFromCodeBehind = "0";

                    ResultCancelRequest TransDetails = GetTransDetailsGenericMethod(Request.QueryString["TransactionId"]);

                    if (TransDetails.IsTransFound)
                    {
                        res.transType = TransDetails.UserType;
                        res.TransId = TransDetails.TransId;
                        res.LinkSource = Request.QueryString["LinkSource"];
                        res.UserType = Request.QueryString["UserType"];
                        res.transStatus = TransDetails.TransStatus;
                        res.transAmout = TransDetails.AmountLabel;
                        res.transMemo = TransDetails.transMemo;

                        if (CommonHelper.GetDecryptedData(res.UserType) == "NonRegistered" || CommonHelper.GetDecryptedData(res.UserType) == "Existing")
                        {
                            res.nameLabel = TransDetails.nameLabel;
                            res.senderImage = TransDetails.senderImage;
                        }
                        else
                        {
                            res.nameLabel = TransDetails.RecepientName;
                            res.senderImage = TransDetails.RecepientPhoto;
                        }

                        if (TransDetails.TransStatus == "pending")
                        {
                            res.clickToReject = true;
                        }
                    }
                    else
                    {
                        res.errorFromCodeBehind = "1";
                    }
                }
                else
                {
                    res.SenderAndTransInfodiv = false;
                    res.clickToReject = false;
                    res.createAccountPrompt = false;

                    // Use TransResult (inside TransactionResult DIV) to display error message (in addition to .swal() alert)
                    res.TransactionResult = true;
                    res.errorFromCodeBehind = "1";

                    Logger.Error("rejectMoney CodeBehind -> page_load ERROR - One of the required fields in query string was NULL or empty - " +
                                 "TransactionId Parameter: [" + Request.QueryString["TransactionId"] + "], " +
                                 "UserType Parameter: [" + Request.QueryString["UserType"] + "], " +
                                 "LinkSource Parameter: [" + Request.QueryString["LinkSource"] + "], " +
                                 "TransType Parameter: [" + Request.QueryString["TransType"] + "]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("rejectMoney CodeBehind -> page_load OUTER EXCEPTION - TransactionId Parameter: [" + Request.QueryString["TransactionId"] +
                             "], Exception: [" + ex.Message + "]");
            }

            return View(res);
        }


        [HttpPost]
        public ActionResult RejectMoneyBtnClick(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            PageLoadDataRejectMoney res = new PageLoadDataRejectMoney();
            res.errorFromCodeBehind = "initial";

            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            serviceMethod = "/RejectMoneyCommon?TransactionId=" + TransactionId +
                            "&UserType=" + UserType +
                            "&LinkSource=" + LinkSource +
                            "&TransType=" + TransType;

            Logger.Info("rejectMoney CodeBehind -> RejectRequest - Full Service URL To Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            var serviceResult = ResponseConverter<StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (!String.IsNullOrEmpty(serviceResult.Result) && serviceResult.Result.IndexOf("Success") > -1)
            {
                res.errorFromCodeBehind = "0";

                res.transStatus = "Request rejected successfully.";

                Logger.Info("rejectMoney CodeBehind -> RejectRequest SUCCESSFUL - [TransactionId Parameter: " +
                            Request.QueryString["TransactionId"] + "]");
            }
            else if (!String.IsNullOrEmpty(serviceResult.Result) && serviceResult.Result.IndexOf("no longer pending") > -1)
            {
                res.errorFromCodeBehind = "Transaction no longer pending";
                res.transStatus = "Already Rejected or Cancelled";
            }
            else
            {
                Logger.Error("rejectMoney CodeBehind -> RejectRequest FAILED - [Server Result: " + serviceResult.Result + "], " +
                             "[TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");
                res.errorFromCodeBehind = "1";
            }

            return Json(res);
        }


        /// <summary>
        /// Only called from the RejectMoney page.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        public ResultCancelRequest GetTransDetailsGenericMethod(string TransactionId)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            rcr.IsTransFound = transaction != null;
            rcr.TransStatus = transaction.TransactionStatus;

            if (transaction.IsPhoneInvitation && transaction.PhoneNumberInvited.Length > 0)
            {
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                rcr.nameLabel = transaction.PhoneNumberInvited;
            }
            else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            {
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                rcr.nameLabel = transaction.InvitationSentTo;
            }
            else
            {
                rcr.senderImage = transaction.SenderPhoto;
                rcr.nameLabel = transaction.Name;
            }

            rcr.AmountLabel = transaction.Amount.ToString("n2");
            rcr.transMemo = transaction.Memo.Trim();

            // Reject money page related stuff
            rcr.RecepientName = transaction.RecepientName;
            rcr.senderImage = transaction.RecepientPhoto;

            if (!String.IsNullOrEmpty(transaction.TransactionType))
                rcr.TransType = transaction.TransactionType;

            if (!String.IsNullOrEmpty(transaction.TransactionId))
                rcr.TransId = transaction.TransactionId;

            return rcr;
        }

        #endregion RejectMoney Page


        protected void createPassword_Click()
        {
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            //serviceMethod = "/CreateNonNoochUserAccountAfterRejectMoney?TransId=" + Request.QueryString["TransId"].ToString() + "&password=" + password.Text + "&EmailId=" + userNameText.Text + "&UserName="+nameText.Text;

            var serviceResult = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (serviceResult.Result == "Thanks for registering! Check your email to complete activation.")
            {
                //checkEmailMsg.Visible = true;
            }
            else
            {
                //checkEmailMsg.Visible = true;
            }
        }


        #region IDVerification Page

        public ActionResult idVerification()
        {
            idVerification idv = new idVerification();

            Logger.Info("idVerification CodeBehind -> Page_load Initiated - ['memid' Parameter: " + Request.QueryString["memid"] + "]");

            idv.error_msg = "initial";
            idv.from = "unknown";
            idv.redUrl = "https://www.nooch.com";

            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["from"]))
                {
                    idv.from = Request.QueryString["from"].ToString();

                    if (idv.from == "addbnk" &&
                        !String.IsNullOrEmpty(Request.QueryString["redUrl"]))
                    {
                        idv.redUrl = Request.QueryString["redUrl"].ToString();
                    }
                }

                if (!String.IsNullOrEmpty(Request.QueryString["memid"]))
                {
                    idv.memid = Request.QueryString["memid"].ToString();
                    idv = getIdVerificationQuestionsV3(Request.QueryString["memid"].ToString(), idv);
                }
                else
                {
                    // something wrong with query string
                    idv.was_error = "true";
                    idv.error_msg = "Bad query string";
                }
            }
            catch (Exception ex)
            {
                idv.was_error = "true";
                idv.error_msg = "Code behind exception";

                Logger.Error("idVerification CodeBehind -> page_load OUTER EXCEPTION - ['memid' Parameter: " +
                             Request.QueryString["memid"] + "], [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoaddata"] = idv;

            return View();
        }


        // Get the ID Verification questions for this user from the SynapseIdVerificationQuestions Table (there should be 5 questions, each with 5 possible answer choices)
        public idVerification getIdVerificationQuestionsV3(string memberId, idVerification IdVerification)
        {
            idVerification idv = IdVerification;
            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/getIdVerificationQuestionsV3?memberid=" + memberId;

                synapseV2_IdVerQsForDisplay_Int questionsFromDb =
                    ResponseConverter<synapseV2_IdVerQsForDisplay_Int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (questionsFromDb == null)
                {
                    Logger.Error("idVerification CodeBehind -> getVerificationQuestionsV2 - Could Not Find Member ['memid' Parameter: " +
                                           Request.QueryString["memid"] + "]");

                    idv.was_error = "true";
                    idv.error_msg = "Member not found";
                }
                else if (questionsFromDb.success == true)
                {
                    idv.was_error = "false";
                    idv.error_msg = "OK";

                    // Set the QuestionSetId value (Hidden Input)                
                    if (!String.IsNullOrEmpty(questionsFromDb.qSetId))
                    {
                        idv.qsetId = questionsFromDb.qSetId.ToString();
                    }
                    else
                    {
                        // No QuestionSetId returned from DB... that's a problem!!
                        idv.was_error = "true";
                        idv.error_msg = "No Question Set found for this member";
                    }

                    #region Set Text For Each Question & Answer Choices

                    // THESE NEED TO BE SENT FROM THE DB IN THE CORRECT ORDER BY 'SynpQuestionId'... otherwise we won't match the answers with the right question

                    if (!String.IsNullOrEmpty(questionsFromDb.questionList[0].question))
                    {
                        idv.question1text = questionsFromDb.questionList[0].question;
                        idv.question1_choice1 = questionsFromDb.questionList[0].answers[0].answer;
                        idv.question1_choice2 = questionsFromDb.questionList[0].answers[1].answer;
                        idv.question1_choice3 = questionsFromDb.questionList[0].answers[2].answer;
                        idv.question1_choice4 = questionsFromDb.questionList[0].answers[3].answer;
                        idv.question1_choice5 = questionsFromDb.questionList[0].answers[4].answer;
                        idv.quest1id = questionsFromDb.questionList[0].id;
                    }

                    if (!String.IsNullOrEmpty(questionsFromDb.questionList[1].question))
                    {
                        idv.question2text = questionsFromDb.questionList[1].question;
                        idv.question2_choice1 = questionsFromDb.questionList[1].answers[0].answer;
                        idv.question2_choice2 = questionsFromDb.questionList[1].answers[1].answer;
                        idv.question2_choice3 = questionsFromDb.questionList[1].answers[2].answer;
                        idv.question2_choice4 = questionsFromDb.questionList[1].answers[3].answer;
                        idv.question2_choice5 = questionsFromDb.questionList[1].answers[4].answer;
                        idv.quest2id = questionsFromDb.questionList[1].id;
                    }

                    if (!String.IsNullOrEmpty(questionsFromDb.questionList[2].question))
                    {
                        idv.question3text = questionsFromDb.questionList[2].question;
                        idv.question3_choice1 = questionsFromDb.questionList[2].answers[0].answer;
                        idv.question3_choice2 = questionsFromDb.questionList[2].answers[1].answer;
                        idv.question3_choice3 = questionsFromDb.questionList[2].answers[2].answer;
                        idv.question3_choice4 = questionsFromDb.questionList[2].answers[3].answer;
                        idv.question3_choice5 = questionsFromDb.questionList[2].answers[4].answer;
                        idv.quest3id = questionsFromDb.questionList[2].id;
                    }

                    if (!String.IsNullOrEmpty(questionsFromDb.questionList[3].question))
                    {
                        idv.question4text = questionsFromDb.questionList[3].question;
                        idv.question4_choice1 = questionsFromDb.questionList[3].answers[0].answer;
                        idv.question4_choice2 = questionsFromDb.questionList[3].answers[1].answer;
                        idv.question4_choice3 = questionsFromDb.questionList[3].answers[2].answer;
                        idv.question4_choice4 = questionsFromDb.questionList[3].answers[3].answer;
                        idv.question4_choice5 = questionsFromDb.questionList[3].answers[4].answer;
                        idv.quest4id = questionsFromDb.questionList[3].id;
                    }

                    if (!String.IsNullOrEmpty(questionsFromDb.questionList[4].question))
                    {
                        idv.question5text = questionsFromDb.questionList[4].question;
                        idv.question5_choice1 = questionsFromDb.questionList[4].answers[0].answer;
                        idv.question5_choice2 = questionsFromDb.questionList[4].answers[1].answer;
                        idv.question5_choice3 = questionsFromDb.questionList[4].answers[2].answer;
                        idv.question5_choice4 = questionsFromDb.questionList[4].answers[3].answer;
                        idv.question5_choice5 = questionsFromDb.questionList[4].answers[4].answer;
                        idv.quest5id = questionsFromDb.questionList[4].id;
                    }

                    #endregion Set Text For Each Question & Answer Choices

                }
                else if (questionsFromDb.msg == "ID already verified successfully" ||
                         questionsFromDb.submitted == true)
                {
                    // THIS USER ALREADY SUBMITTED THE ANSWERS, SO THEY SHOULDN'T BE HERE...
                    Logger.Error("idVerification CodeBehind -> getVerificationQuestionsV2 - Answers Already Submitted (Shouldn't be here then) - " +
                                           "Member [MemberID from URL query string: " + Request.QueryString["memid"] + "]");
                    idv.was_error = "true";
                    idv.error_msg = "Answers already submitted for this user";
                }
                else
                {
                    idv.was_error = "true";
                    idv.error_msg = "Codebehind - error getting questions";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("idVerification CodeBehind -> getVerificationQuestionsV2 FAILED - ['memid' Parameter: " + Request.QueryString["memid"] + "], [Exception: " + ex + "]");
            }

            return idv;
        }


        // Submit the user's answers       
        public ActionResult submitResponses(string MemberId, string questionSetId, string quest1id, string quest2id, string quest3id, string quest4id, string quest5id, string answer1id, string answer2id, string answer3id, string answer4id, string answer5id)
        {
            Logger.Info("idVerification CodeBehind -> submitResponses Initiated");

            synapseV3GenericResponse res = new synapseV3GenericResponse();

            try
            {
                if (String.IsNullOrEmpty(MemberId) || String.IsNullOrEmpty(questionSetId))
                {
                    if (String.IsNullOrEmpty(MemberId))
                    {
                        res.msg = "Invalid Data - Need a MemberID!";
                    }
                    else if (String.IsNullOrEmpty(questionSetId))
                    {
                        res.msg = "Invalid Data - Missing a Question Set ID";
                    }

                    Logger.Info("idVerification CodeBehind -> submitResponses ABORTED - [" + res.msg + "]");

                    res.isSuccess = false;
                }
                // Check for 5 total answers
                else if (answer1id == null || answer2id == null || answer3id == null || answer4id == null || answer5id == null)
                {
                    Logger.Info("idVerification CodeBehind -> submitResponses ABORTED: Missing at least 1 answer. [MemberId: " + MemberId + "]");

                    res.isSuccess = false;
                    res.msg = "Missing at least 1 answer (should have 5 total answers).";
                }
                else
                {
                    Logger.Info("idVerification CodeBehind -> submitResponses Initiated");

                    // All required data exists, now send to NoochService.svc
                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    //string serviceMethod = "/submitIdVerificationAswersV3?memberId=" + memId + "&qSetId=" + qSetId + "&a1=" + a1 + "&a2=" + a2 +
                    //                       "&a3=" + a3 + "&a4=" + a4 + "&a5=" + a5;
                    string serviceMethod = "/submitIdVerificationAswersV3?memberId=" + MemberId + "&questionSetId=" + questionSetId + "&quest1id=" + quest1id +
                        "&quest2id=" + quest2id + "&quest3id=" + quest3id + "&quest4id=" + quest4id + "&quest5id=" + quest5id + "&answer1id=" + answer1id + "&answer2id=" + answer2id +
                        "&answer3id=" + answer3id + "&answer4id=" + answer4id + "&answer5id=" + answer5id;

                    synapseV3GenericResponse svcResponse =
                            ResponseConverter<synapseV3GenericResponse>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    res.isSuccess = svcResponse.isSuccess;
                    res.msg = svcResponse.msg;
                }
            }
            catch (WebException we)
            {
                Logger.Error("idVerification CodeBehind -> submitResponses FAILED - [WebException: " + we.Status + "]");
            }

            return Json(res);
        }

        #endregion IDVerification Page


        #region MakePayment Page

        /// <summary>
        /// For when the /makePayment page first loads.
        /// </summary>
        /// <returns></returns>
        public ActionResult makePayment()
        {
            HiddenField hkf = new HiddenField();
            try
            {
                if (!String.IsNullOrEmpty(Request.QueryString["from"]))
                {
                    if (Request.QueryString["from"] == "rentscene")
                    {
                        Logger.Info("Make Payment CodeBehind -> Page Initiated - Is for RENTSCENE");
                        hkf.from = "rentscene";
                    }
                    else if (Request.QueryString["from"] == "appjaxx")
                    {
                        Logger.Info("Make Payment CodeBehind -> Page Initiated - Is for APPJAXX");
                        hkf.from = "appjaxx";
                    }
                    else
                    {
                        hkf.from = "nooch";
                    }
                }
                else
                {
                    // Set Nooch as the default
                    hkf.from = "nooch";
                }
            }
            catch (Exception ex)
            {
                hkf.errorId = "1";
                Logger.Error("Make Payment CodeBehind -> page_load OUTER EXCEPTION - [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoadData"] = hkf;
            return View();
        }


        /// <summary>
        /// For Submitting a payment from the MakePayment browser page.
        /// Currently only used for making Rent Scene requests - eventually want to add ability to send a transfer as well.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="isRequest"></param>
        /// <param name="amount"></param>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="memo"></param>
        /// <param name="pin"></param>
        /// <param name="ip"></param>
        /// <param name="cip"></param>
        /// <returns></returns>
        public ActionResult submitPayment(string from, bool isRequest, string amount, string name, string email, string memo, string pin, string ip, string cip)
        {
            Logger.Info("Make Payment Code-Behind -> submitPayment Initiated - From: [" + from + "], isRequest: [" + isRequest +
                        "], Name: [" + name + "], Email: [" + email +
                        "], Amount: [" + amount + "], memo: [" + memo +
                        "], PIN: [" + pin + "], IP: [" + ip + "], CIP: [" + cip + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.msg = "Initial - code behind";

            #region Check If Recipient's Email Is Already Registered

            var memberObj = CommonHelper.GetMemberDetailsByUserName(email);

            if (memberObj != null) // This email address is already registered!
            {
                Logger.Info("Make Payment Conde Behind -> submitPayment Attempted - Recipient email already exists: [" + email + "]");

                res.isEmailAlreadyReg = true;
                res.memberId = memberObj.MemberId.ToString();
                res.name = (!String.IsNullOrEmpty(memberObj.FirstName)) ? CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName))
                                                                        : "";
                res.name = (!String.IsNullOrEmpty(memberObj.LastName)) ? res.name + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName))
                                                                       : res.name;
                res.memberStatus = memberObj.Status;
                res.dateCreated = Convert.ToDateTime(memberObj.DateCreated).ToString("MMM d, yyyy");

                var userAndBankInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(memberObj.MemberId.ToString());

                if (userAndBankInfo != null &&
                    userAndBankInfo.wereBankDetailsFound == true &&
                    userAndBankInfo.BankDetails != null)
                {
                    res.isBankAttached = true;
                    res.bankStatus = userAndBankInfo.BankDetails.Status;
                    res.synapseUserPermission = userAndBankInfo.UserDetails.permission;
                    res.synapseBankAllowed = userAndBankInfo.BankDetails.allowed;
                }

                res.msg = "Existing user found!";
                res.note = "";
                res.success = true;
                return Json(res);
            }

            #endregion Check If Recipient's Email Is Already Registered

            try
            {
                #region Lookup PIN

                pin = (String.IsNullOrEmpty(pin) || pin.Length != 4) ? "0000" : pin;

                if (from == "rentscene")
                {
                    pin = CommonHelper.GetMemberPinByUserName("payments@rentscene.com");
                }
                else if (from == "nooch")
                {
                    pin = CommonHelper.GetMemberPinByUserName("team@nooch.com");
                }
                else if (from == "appjaxx")
                {
                    pin = CommonHelper.GetMemberPinByUserName("josh@appjaxx.com");
                }

                if (String.IsNullOrEmpty(pin))
                {
                    res.msg = "Failed to get a PIN from the server.";
                    return Json(res);
                }

                #endregion Lookup PIN

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod;
                string json = "";

                requestFromRentScene response = new requestFromRentScene();
                var scriptSerializer = new JavaScriptSerializer();

                if (isRequest)
                {
                    serviceMethod = "/RequestMoneyForRentScene?from=" + from +
                                                             "&name=" + name +
                                                             "&email=" + email + "&amount=" + amount +
                                                             "&memo=" + memo + "&pin=" + pin +
                                                             "&ip=" + ip +
                                                             "&cip=" + cip +
                                                             "&isRequest=" + isRequest;

                    response = ResponseConverter<requestFromRentScene>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
                }
                else
                {
                    string memIdToUse = "";
                    string accessToken = "";

                    if (from.ToLower() == "rentscene")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("payments@rentscene.com");

                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }
                    else if (from.ToLower() == "nooch")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("team@nooch.com");
                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }
                    else if (from.ToLower() == "appjaxx")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("josh@appjaxx.com");
                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }

                    if (String.IsNullOrEmpty(memIdToUse))
                    {
                        Logger.Error("Service Cntlr -> RequestMoneyForRentScene FAILED - unable to get MemberID based on given username - ['from' param: " + from + "]");
                        res.msg = "Unable to get MemberID based on given username";
                    }

                    TransactionDto transactionDto = new TransactionDto();
                    transactionDto.MemberId = memIdToUse;
                    transactionDto.RecepientName = name;
                    transactionDto.PinNumber = pin;
                    transactionDto.Amount = Convert.ToDecimal(amount);
                    transactionDto.Memo = memo;

                    serviceMethod = "/TransferMoneyToNonNoochUserUsingSynapse?accessToken=" + accessToken + "&inviteType=email&receiverEmailId=" + email;
                    json = scriptSerializer.Serialize(transactionDto);

                    StringResult sr = ResponseConverter<StringResult>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                    if (sr.Result.Contains("successfully"))
                    {
                        response.success = true;
                        response.msg = sr.Result;
                    }
                }

                string urlToUse = String.Concat(serviceUrl, serviceMethod);



                Logger.Info("Make Payment Code-Behind -> submitPayment - URL To Query: [" + urlToUse + "]");

                if (response != null)
                {
                    res = response;

                    Logger.Info("Make Payment Code-Behind -> submitPayment RESULT.Success: [" + response.success + "], RESULT.Msg: [" + response.msg + "]");

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        if (response.isEmailAlreadyReg == true)
                        {
                            // CLIFF (5/15/16): shouldn't ever get here since I added the block above to check if the email
                            //                  is already registered (so it shouldn't even call /RequestMoneyForRentScene).
                            Logger.Info("Make Payment Code-Behind -> submitPayment Success - Email address already registered to an Existing User - " +
                                        "Name: [" + response.name + "], Email: [" + email + "], Status: [" + response.memberStatus + "], MemberID: [" + response.memberId + "]");
                        }
                        else
                        {
                            Logger.Info("Make Payment Code-Behind -> submitPayment Success - Payment Request submitted to NEW user successfully - " +
                                        "Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                        }
                    }
                    else
                    {
                        Logger.Error("Make Payment Code-Behind -> submitPayment FAILED - Server response for RequestMoneyForRentScene() was NOT successful - " +
                                     "Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }

                    #endregion Logging For Debugging
                }
                else
                {
                    res.msg = "Unknown server error - Server's response was null.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Make Payment Code-Behind -> submitPayment FAILED - email: [" + email + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during submitPayment.";
            }

            return Json(res);
        }


        /// <summary>
        /// For Submitting a payment from the MakePayment browser page when the server initially returns
        /// that the recipient already has an account based on the email used in submitPayment() above.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="isRequest"></param>
        /// <param name="amount"></param>
        /// <param name="name"></param>
        /// <param name="email"></param>
        /// <param name="memo"></param>
        /// <param name="pin"></param>
        /// <param name="ip"></param>
        /// <param name="memberId"></param>
        /// <param name="nameFromServer"></param>
        /// <returns></returns>
        public ActionResult submitRequestToExistingUser(string from, bool isRequest, string amount, string name, string email, string memo, string pin, string ip, string memberId, string nameFromServer)
        {
            Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser Initiated - From: [" + from + "], isRequest: [" + isRequest +
                        "], Name: [" + name + "], Email: [" + email +
                        "], Amount: [" + amount + "], memo: [" + memo +
                        "], PIN: [" + pin + "], IP: [" + ip + "]" +
                        "], MemberID: [" + memberId + "], NameFromServer: [" + nameFromServer + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.msg = "Initial - code behind";

            #region Lookup PIN
            string json = "";
            requestFromRentScene response = new requestFromRentScene();
            var scriptSerializer = new JavaScriptSerializer();
            pin = (String.IsNullOrEmpty(pin) || pin.Length != 4) ? "0000" : pin;

            if (from == "rentscene")
            {
                pin = CommonHelper.GetMemberPinByUserName("payments@rentscene.com");
            }
            else if (from == "nooch")
            {
                pin = CommonHelper.GetMemberPinByUserName("team@nooch.com");
            }
            else if (from == "appjaxx")
            {
                pin = CommonHelper.GetMemberPinByUserName("josh@appjaxx.com");
            }

            if (String.IsNullOrEmpty(pin))
            {
                res.msg = "Failed to get a PIN from the server.";
                return Json(res);
            }

            #endregion Lookup PIN

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod;
                string textLoggerHelper = "";

                if (isRequest)
                {
                    textLoggerHelper = " Request";

                    serviceMethod = "/RequestMoneyToExistingUserForRentScene?from=" + from + "&name=" + name +
                                         "&email=" + email + "&amount=" + amount +
                                         "&memo=" + memo + "&pin=" + pin +
                                         "&ip=" + ip + "&isRequest=" + isRequest +
                                         "&memberId=" + memberId + "&nameFromServer=" + nameFromServer;
                    response = ResponseConverter<requestFromRentScene>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));
                }
                else
                {
                    string memIdToUse = "";
                    string accessToken = "";

                    if (from.ToLower() == "rentscene")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("payments@rentscene.com");
                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }
                    else if (from.ToLower() == "nooch")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("team@nooch.com");
                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }
                    else if (from.ToLower() == "appjaxx")
                    {
                        Member member = CommonHelper.GetMemberDetailsByUserName("josh@appjaxx.com");
                        memIdToUse = member.MemberId.ToString();
                        accessToken = member.AccessToken.ToString();
                    }

                    if (String.IsNullOrEmpty(memIdToUse))
                    {
                        Logger.Error("Make Payment Code Behind -> SubmitRequestToExistingUser FAILED - unable to get MemberID based on given username - ['from' param: " + from + "]");
                        res.msg = "Unable to get MemberID based on given username";
                    }

                    string RecepientId = CommonHelper.GetMemberIdByUserName(email.ToString());

                    TransactionDto transactionDto = new TransactionDto();
                    transactionDto.MemberId = memIdToUse;
                    transactionDto.RecepientName = name;
                    transactionDto.PinNumber = pin;
                    transactionDto.Amount = Convert.ToDecimal(amount);
                    transactionDto.Memo = memo;
                    transactionDto.RecepientId = RecepientId;

                    serviceMethod = "/TransferMoneyUsingSynapse?accessToken=" + accessToken;
                    json = scriptSerializer.Serialize(transactionDto);
                    StringResult sr = ResponseConverter<StringResult>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                    if (sr.Result.Contains("successfully"))
                    {
                        response.success = true;
                        response.msg = sr.Result;
                    }
                }
                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser - URL To Query: [" + urlToUse + "], IsRequest: [" + isRequest + "]");

                Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser - Server Response for RequestMoneyToExistingUserForRentScene: " +
                            "RESULT.Success: [" + response.success + "], RESULT.Msg: [" + response.msg + "]");

                if (response != null)
                {
                    res = response;

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser Success - Payment" + textLoggerHelper + " submitted successfully - " +
                                    "ServiceMethod: [" + serviceMethod + "], Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }
                    else
                    {
                        Logger.Error("Make Payment Code-Behind -> submitRequestToExistingUser FAILED - Server response for ServiceMethod: [" + serviceMethod +
                                     "] was NOT successful - Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }

                    #endregion Logging For Debugging
                }
                else
                {
                    res.msg = "Unknown server error - Server's response was null.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Make Payment Code-Behind -> submitRequestToExistingUser FAILED - email: [" + email + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during submitRequestToExistingUser.";
            }

            return Json(res);
        }

        #endregion MakePayment Page


        // Not complete code problem with JS file and GetPayeeDetails method
        #region PayAnyone Page

        public ActionResult payAnyone()
        {
            ResultpayAnyone payAnyone = new ResultpayAnyone();

            if (!String.IsNullOrEmpty(Request.QueryString["pay"]))
            {
                payAnyone = GetPayeesNoochDetails(Request.QueryString["pay"].ToString(), payAnyone);
            }
            else
            {
                // Something wrong with query string
                //ScriptManager.RegisterStartupScript(this, GetType(), "showErrorModal", "showErrorModal('2');", true);
                //payreqInfo.Visible = false;
                //PayorInitialInfo.Visible = false;
                payAnyone.payreqInfo = false;
                payAnyone.PayorInitialInfo = false;
                payAnyone.ErrorID = "2";
            }

            ViewData["OnLoaddata"] = payAnyone;
            return View();
        }


        public ResultpayAnyone GetPayeesNoochDetails(string memberTag, ResultpayAnyone resultpayAnyone)
        {
            ResultpayAnyone payAnyone = resultpayAnyone;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetPayeeDetails?memberTag=" + memberTag;

            MemberDto member = ResponseConverter<MemberDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (member == null)
            {
                //payreqInfo.Visible = false;
                //hidfield.Value = "0";
                // ScriptManager.RegisterStartupScript(this, GetType(), "showErrorModal", "showErrorModal('2');", true);
                payAnyone.payreqInfo = false;
                payAnyone.hidfield = "0";
                payAnyone.ErrorID = "2";
            }
            else
            {
                //payeeName1.Text = member.FirstName + " " + member.LastName;
                //payeeName2.Text = member.FirstName + " " + member.LastName;
                //payeeImage.ImageUrl = member.PhotoUrl;
                payAnyone.payeeName1 = member.FirstName + " " + member.LastName;
                payAnyone.payeeName2 = member.FirstName + " " + member.LastName;
                payAnyone.payeeImage = member.PhotoUrl;

            }
            return resultpayAnyone;
        }


        // Register user with Synapse      
        public RegisterUserSynapseResultClassExt RegisterUserWithSynp(string transId, string userEmail, string userPhone, string userName, string userPassword, string isIdImageAdded = "", string idImagedata = "")
        {
            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RegisterNonNoochUserWithSynapse";
                userPhone = CommonHelper.RemovePhoneNumberFormatting(userPhone);

                RegisterUserWithSynapseV3_Input inputclass = new RegisterUserWithSynapseV3_Input();
                // Member DOES NOT already exist, so use RegisterNONNOOCHUserWithSynapse()
                inputclass.address = "";
                inputclass.dob = "";
                inputclass.email = userEmail;
                inputclass.fngprnt = "";
                inputclass.fullname = userName;
                inputclass.idImageData = idImagedata;
                inputclass.ip = "";
                inputclass.isIdImageAdded = isIdImageAdded;
                inputclass.phone = userPhone;
                inputclass.pw = userPassword;
                inputclass.ssn = "";
                inputclass.transId = transId;
                inputclass.zip = "";

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputclass);


                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (regUserResponse.success == "false")
                {
                    res.success = "false";
                    res.reason = regUserResponse.reason;
                    res.memberIdGenerated = "";
                }
                if (regUserResponse.success == "true")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Info("payRequest Page -> Register Synapse User attempt FAILED Failed, Reason: [" + ex.Message + "]. TransId: [" + transId + "].");
                return res;
            }
        }

        #endregion PayAnyone Page


        /// <summary>
        /// When the /Activation page first loads (page is for verifying an email address).
        /// </summary>
        /// <returns></returns>
        public ActionResult Activation()
        {
            Logger.Info("Email Activation Page -> Initiated");

            ResultActivation resultActivation = new ResultActivation();
            resultActivation.success = false;

            string strUserAgent = Request.UserAgent.ToLower();

            if (strUserAgent != null &&
                (Request.Browser.IsMobileDevice || strUserAgent.Contains("iphone") || strUserAgent.Contains("mobile") || strUserAgent.Contains("iOS")))
            {
                resultActivation.openAppText = true;
            }

            string tokenId = Request.QueryString["tokenId"].Trim();
            string type = null;

            if (Request.QueryString.AllKeys.Any(k => k == "type"))
            {
                type = Request.QueryString["type"].Trim();

                if (type == "ll")// For Landlords
                {
                    resultActivation.toLandlordApp = true;
                }
            }

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "/IsMemberActivated?tokenID=" + tokenId.Trim();

            var result = ResponseConverter<BoolResult>.ConvertToCustomEntity(serviceUrl);

            if (!result.Result)
            {
                serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "/MemberActivation?tokenId=" + tokenId.Trim();
                ResponseConverter<BoolResult>.ConvertToCustomEntity(serviceUrl);

                resultActivation.success = true;
                resultActivation.error = false;
            }
            else
            {
                resultActivation.error = true;
            }

            ViewData["OnLoaddata"] = resultActivation;

            return View();
        }


        /// <summary>
        /// For getting a user's transaction history (Added by Cliff on 5/10/16).
        /// </summary>
        /// <param name="memId"></param>
        /// <param name="rs"></param>
        /// <returns></returns>
        public ActionResult history(string memId, string rs)
        {
            TransactionsPageData res = new TransactionsPageData();
            res.isSuccess = false;

            if (Request.QueryString["memId"] == null)
            {
                res.msg = "No MemberId found in query URL.";
            }
            else
            {
                Logger.Info("History CodeBehind -> Page_load Initiated - [MemberID: " + memId + "]");

                List<TransactionClass> Transactions = new List<TransactionClass>();

                try
                {
                    string memberId = Request.QueryString["memId"];
                    string listType = "ALL";

                    TransactionsDataAccess tda = new TransactionsDataAccess();

                    int totalRecordsCount = 0;
                    var transactionListEntities = tda.GetTransactionsList(memberId, listType, 50, 1, "", out totalRecordsCount);

                    if (transactionListEntities != null && transactionListEntities.Count > 0)
                    {
                        foreach (var trans in transactionListEntities)
                        {
                            try
                            {
                                #region Foreach inside

                                TransactionClass obj = new TransactionClass();

                                obj.TransactionId = trans.TransactionId;
                                obj.TransactionType = CommonHelper.GetDecryptedData(trans.TransactionType);
                                obj.TransactionStatus = trans.TransactionStatus;

                                obj.TransactionDate1 = Convert.ToDateTime(trans.TransactionDate).ToShortDateString();
                                obj.Amount = Math.Round(trans.Amount, 2);

                                obj.Memo = trans.Memo;
                                obj.city = (trans.GeoLocation != null && trans.GeoLocation.City != null) ? trans.GeoLocation.City : string.Empty;
                                obj.state = (trans.GeoLocation != null && trans.GeoLocation.State != null) ? trans.GeoLocation.State : string.Empty;

                                obj.TransLati = (trans.GeoLocation != null && trans.GeoLocation.Latitude != null) ? (float)trans.GeoLocation.Latitude : default(float);
                                obj.TransLongi = (trans.GeoLocation != null && trans.GeoLocation.Longitude != null) ? (float)trans.GeoLocation.Longitude : default(float);

                                #region Transaction Type Transfer

                                if (obj.TransactionType == "Transfer" || obj.TransactionType == "Disputed" || obj.TransactionType == "Reward" || obj.TransactionType == "Invite" || obj.TransactionType == "Rent" || obj.TransactionType == "Request")
                                {
                                    if (String.IsNullOrEmpty(trans.InvitationSentTo) &&
                                        (trans.IsPhoneInvitation == null || trans.IsPhoneInvitation == false))
                                    {
                                        // Transfer type request to existing Nooch user..straight forward
                                        obj.SenderId = trans.SenderId;
                                        obj.SenderName = CommonHelper.GetDecryptedData(trans.Member.FirstName) + " " + CommonHelper.GetDecryptedData(trans.Member.LastName);
                                        obj.SenderNoochId = trans.Member.Nooch_ID;

                                        obj.RecipientId = trans.RecipientId;
                                        obj.RecipienName = CommonHelper.GetDecryptedData(trans.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(trans.Member1.LastName);
                                        obj.RecepientNoochId = trans.Member1.Nooch_ID;
                                        obj.IsInvitation = false;
                                    }
                                    else
                                    {
                                        obj.IsInvitation = true;
                                        obj.SenderId = trans.SenderId;
                                        obj.SenderName = CommonHelper.GetDecryptedData(trans.Member.FirstName) + " " + CommonHelper.GetDecryptedData(trans.Member.LastName);
                                        obj.SenderNoochId = trans.Member.Nooch_ID;

                                        if (!String.IsNullOrEmpty(trans.InvitationSentTo))
                                        {
                                            // invite through email case

                                            obj.RecipienName = CommonHelper.GetDecryptedData(trans.InvitationSentTo);
                                        }
                                        if (trans.IsPhoneInvitation == true &&
                                            !String.IsNullOrEmpty(trans.PhoneNumberInvited))
                                        {
                                            // invite through sms case
                                            obj.RecipienName = CommonHelper.FormatPhoneNumber(CommonHelper.GetDecryptedData(trans.PhoneNumberInvited));
                                        }
                                    }
                                }

                                #endregion

                                Transactions.Add(obj);

                                #endregion Foreach inside
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("History CodeBehind - ERROR - Inner Exception during loop through all transactions - " +
                                             "MemberID: [" + memberId + "], TransID: [" + trans.TransactionId +
                                             "], Amount: [" + trans.Amount.ToString("n2") + "], Exception: [" + ex.Message + "]");
                                continue;
                            }
                        }
                    }

                    res.allTransactionsData = Transactions;
                }
                catch (Exception ex)
                {
                    Logger.Error("History CodeBehind -> OUTER EXCEPTION - [MemberID Parameter: " + memId + "], [Exception: " + ex.Message + "]");
                    res.msg = "Server Error.";
                }

                // Now get the user's Name
                var memberObj = CommonHelper.GetMemberDetails(memId);
                if (memberObj != null && !String.IsNullOrEmpty(memberObj.FirstName))
                {
                    res.usersName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));

                    if (!String.IsNullOrEmpty(memberObj.LastName))
                    {
                        res.usersName += " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));
                    }

                    res.usersEmail = CommonHelper.GetDecryptedData(memberObj.UserName);
                }
            }

            return View(res);
        }
    }


    public class bankLoginInputFormClass
    {
        public string username { get; set; }
        public string password { get; set; }
        public string memberid { get; set; }
        public string bankname { get; set; }
        public bool IsPinRequired { get; set; }
        public string PinNumber { get; set; }
    }

    public class bankaddInputFormClass
    {
        public string memberid { get; set; }
        public string fullname { get; set; }
        public string routing { get; set; }
        public string account { get; set; }
        public string nickname { get; set; }
        public string cl { get; set; }
        public string type { get; set; }
    }

    public class MFALoginInputClassForm
    {
        public string bank { get; set; }
        public string memberid { get; set; }
        public string MFA { get; set; }
        public string ba { get; set; }
    }

    public class setDefaultBankInput
    {
        public string MemberId { get; set; }
        public string BankName { get; set; }
        public string BankOId { get; set; }
    }

    public class setTransferMoneyInput
    {
        public TransactionDto transactionDto { get; set; }
        public string accessToken { get; set; }
        public string inviteType { get; set; }
        public string receiverEmailId { get; set; }
    }
}