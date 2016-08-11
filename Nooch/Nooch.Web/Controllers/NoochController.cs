﻿using System;
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
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "VerifySynapseAccount?tokenID=" + tokenId.Trim();

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
                Logger.Error("CancelRequest Page -> CancelRequest() FAILED - Missing URL parameters - URL: [" + Request.RawUrl + "]");

                rcr.success = false;
                rcr.showPaymentInfo = false;
                rcr.resultMsg = "This looks like an invalid transaction - sorry about that!  Please try again or contact Nooch support for more information.";
            }

            rcr.memberId = Request.QueryString["MemberId"].ToString();
            rcr.UserType = Request.QueryString["UserType"];

            ViewData["OnLoaddata"] = rcr;
            return View();
        }

        public ActionResult CancelRequestFinal(string TransId, string memberId, string UserType)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            rcr = CancelMoneyRequest(TransId, memberId, UserType);

            ViewData["OnLoaddata"] = rcr;
            return Json(rcr);
        }

        /// <summary>
        /// Just for CANCELING a payment - called by the CancelRequest() method when the CancelRequest page first loads.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        public ResultCancelRequest GetTransDetails(string TransactionId)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            rcr.success = false;

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction != null)
            {
                rcr.showPaymentInfo = true; // Show payment info (recipient name/pic, amount)

                if (transaction.TransactionStatus == "Pending")
                {
                    //ResultCancelRequest cancelResult = CancelMoneyRequest(transaction.TransactionId, Request.QueryString["MemberId"], Request.QueryString["UserType"]);
                    //rcr = cancelResult;
                    // CLIFF (5/15/16): the transaction's status was actually just updated to 'Cancelled' in CancelMoneyRequest()
                    // but this flag is just for telling the CancelMoney.js to display a SweetAlert if it is NOT initially 'Pending' on page load.
                    rcr.initStatus = "pending";
                }
                else if (transaction.TransactionStatus == "Rejected")
                {
                    Logger.Info("CancelRequest Page -> GetTransDetails - Payment already Rejected - TransID: [" + TransactionId + "]");
                    rcr.resultMsg = "Looks like this payment has already been rejected.";
                    rcr.initStatus = "rejected";
                }
                else if (transaction.TransactionStatus == "Cancelled")
                {
                    Logger.Info("CancelRequest Page -> GetTransDetails - Payment already Cancelled - TransID: [" + TransactionId + "]");
                    rcr.resultMsg = "This payment has already been cancelled.";
                    rcr.initStatus = "cancelled";
                }
            }
            else
            {
                Logger.Error("CancelRequest Page -> GetTransDetails FAILED - TransID: [" + TransactionId + "]");
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


        public ResultCancelRequest CancelMoneyRequest(string TransactionId, string MemberId, string userType)
        {
            ResultCancelRequest res = new ResultCancelRequest();
            res.success = false;

            #region Inititial Data Checks

            if (String.IsNullOrEmpty(TransactionId))
            {
                res.resultMsg = "Missing TransactionID";
                return res;
            }
            if (String.IsNullOrEmpty(MemberId))
            {
                res.resultMsg = "Missing MemberId";
                return res;
            }
            if (String.IsNullOrEmpty(userType))
            {
                res.resultMsg = "Missing userType";
                return res;
            }

            #endregion Inititial Data Checks

            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            #region Call CancelMoneyRequest Service

            if (userType == "mx5bTcAYyiOf9I5Py9TiLw==")
            {
                // Service to cancel a REQUEST to an EXISTING Nooch user
                serviceMethod = "CancelMoneyRequestForExistingNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;
            }
            else if (userType == "U6De3haw2r4mSgweNpdgXQ==")
            {
                // Service to cancel a REQUEST to NON-NOOCH user
                serviceMethod = "CancelMoneyRequestForNonNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;
            }

            var serviceResult = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            Logger.Info(Json(serviceResult));

            if (serviceResult.Result == "Transaction Cancelled Successfully.")
            {
                Logger.Info("CancelRequest Page -> Request Successfully Cancelled - TransID: [" + TransactionId + "], MemberID: [" + MemberId + "]");

                res.showPaymentInfo = true;
                res.success = true;
                res.resultMsg = "No problem, we change our minds sometimes too.  This request is cancelled.  But you can always send another request...";
            }
            else
            {
                Logger.Error("CancelRequest Page -> CancelMoneyRequest FAILED - TransID: [" + TransactionId + "], MemberID: [" + MemberId + "], UserType: [" + userType + "]");
                res.resultMsg = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
            }

            #endregion Call CancelMoneyRequest Service

            return res;
        }

        #endregion Cancel Request Page



        #region Add Bank Page

        public ActionResult AddBank(string MemberId, string redUrl = "", string ll = "")
        {
            Logger.Info("Add Bank Page -> Loaded -> MemberID: [" + MemberId + "], RedURL: [" + redUrl + "]");

            AddBankResult res = new AddBankResult();
            res.success = false;

            if (!String.IsNullOrEmpty(MemberId))
            {
                res.memId = MemberId;
                res.redUrl = (!String.IsNullOrEmpty(redUrl) && redUrl.Length > 2 && redUrl.IndexOf("nooch.com") == -1)
                              ? redUrl
                              : "https://www.nooch.com";

                Member memberObj = CommonHelper.GetMemberDetails(MemberId);

                if (memberObj != null)
                {
                    res.success = true;
                    res.isRs = memberObj.isRentScene ?? false;
                }
                else
                {
                    res.msg = "Member not found";
                }
            }
            else
            {
                res.msg = "Missing MemberID param";
            }

            ViewData["OnLoadData"] = res;
            return View();
        }


        [HttpPost]
        [ActionName("CheckBankDetails")]
        public ActionResult CheckBankDetails(string bankname)
        {
            BankNameCheckStatus res = new BankNameCheckStatus();
            res.IsSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = "CheckSynapseBankDetails?BankName=" + bankname;
                Logger.Info("Add Bank Page -> CheckBankDetails Fired - URL to call: [" + String.Concat(serviceUrl, serviceMethod) + "]");

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
                    // Bank Not Found
                    Logger.Error("Add Bank Page -> CheckBankDetails FAILED - Bank Not Found");
                    res.MFAType = bankInfoFromServer.mfa_type;
                    res.IsPinRequired = bankInfoFromServer.IsPinRequired;
                    res.Message = bankInfoFromServer.Message;
                }
            }
            catch (Exception we)
            {
                res.Message = "CheckBankDetails Web Exception - local";
                Logger.Error("Add Bank Page -> CheckBankDetails FAILED - Bank Name: [" + bankname +
                             "], Exception Msg: [" + we.Message + "], Exception Inner: [" + we.InnerException + "]");
            }
            return Json(res);
        }


        public BankLoginResult RegisterUserWithSynapse(string memberid)
        {
            Logger.Info("Add Bank Page -> RegisterUserWithSynapse Fired - MemberID: [" + memberid + "]");

            BankLoginResult res = new BankLoginResult();
            res.IsSuccess = false;
            res.ssn_verify_status = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "RegisterUserWithSynapseV3?memberId=" + memberid;

                synapseCreateUserV3Result_int transaction = ResponseConverter<synapseCreateUserV3Result_int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (transaction.success == true)
                {
                    res.IsSuccess = true;
                    res.Message = (!String.IsNullOrEmpty(transaction.errorMsg) && transaction.errorMsg.IndexOf("Missing ") > -1) ? transaction.errorMsg : "OK";
                }
                else
                {
                    Logger.Error("Add Bank Page -> RegisterUserWithSynapse FAILED - Success was False, errorMsg: [" + transaction.errorMsg + "]");
                    res.Message = transaction.errorMsg;
                }

                res.ssn_verify_status = transaction.ssn_verify_status;
            }
            catch (Exception we)
            {
                res.Message = "RegisterUser Web Exception - local";
                Logger.Error("Add Bank Page -> RegisterUserWithSynapse FAILED - MemberID: [" + memberid +
                             "], Exception: [" + we.InnerException + "]");
            }

            return res;
        }


        [HttpPost]
        [ActionName("BankLogin")]
        public ActionResult BankLogin(bankLoginInputFormClass inp)
        {
            Logger.Info("Add Bank Page -> BankLogin Initiated - MemberID: [" + inp.memberid +
                        "], Bank Name: [" + inp.bankname + "]");

            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // 1. Attempt to register the user with Synapse
                BankLoginResult registerSynapseUserResult = RegisterUserWithSynapse(inp.memberid);

                res.ssn_verify_status = registerSynapseUserResult.ssn_verify_status; // Will be overwritten if Bank Login is successful below

                if (registerSynapseUserResult.IsSuccess == true)
                {
                    Logger.Info("Add Bank Page -> BankLogin - Successful Response - MemberID: [" + inp.memberid +
                                "], SSN Status: [" + registerSynapseUserResult.ssn_verify_status + "], Message: [" + registerSynapseUserResult.Message + "]");

                    // 2. Now call the bank login service.
                    //    Response could be: 1.) array[] of banks,  2.) Question-based MFA,  3.) Code-based MFA, or  4.) Failure/Error

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "SynapseV3AddNodeBankLogin?MemberId=" + inp.memberid + "&BnkName=" + inp.bankname + "&BnkUserName=" + inp.username +
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
                        Logger.Error("Add Bank Page -> BankLogin FAILED -> MemberID: [" + inp.memberid + "], Error Msg: [" + bankLoginResult.errorMsg + "]");
                        res.ERROR_MSG = bankLoginResult.errorMsg;
                    }
                }
                else
                {
                    Logger.Error("Add Bank Page -> BankLogin ERROR -> Register Synapse User FAILED -> MemberID: [" + inp.memberid + "], Error Msg: [" + registerSynapseUserResult.Message + "]");
                    res.ERROR_MSG = registerSynapseUserResult.Message;
                }

                // Check if Register method and Bank Login method both got same result for ssn_verify_status
                // ... not sure which one to prioritize yet in the case of a discrepency, but going with BankLogin one for now.
                if (res.ssn_verify_status != registerSynapseUserResult.ssn_verify_status)
                {
                    Logger.Info("Add Bank Page -> BankLogin -> ssn_verify_status from Registering User was [" +
                                registerSynapseUserResult.ssn_verify_status + "], but ssn_verify_status from BankLogin was: [" +
                                res.ssn_verify_status + "]");
                }
            }
            catch (Exception we)
            {
                Logger.Error("Add Bank Page -> BankLogin FAILED - MemberID: [" + inp.memberid +
                             "], Exception: [" + we.InnerException + "]");
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
            Logger.Info("Add Bank Page -> AddBank (for manual routing/account #) Loaded - MemberID: [" + inp.memberid + "]");

            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // Now call the bank login service.
                // Response should be: 1.) array[] of 1 bank, or  2.) Failure/Error

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = "SynapseV3AddNodeWithAccountNumberAndRoutingNumber?MemberId=" + inp.memberid + "&routing_num=" + inp.routing +
                                       "&account_num=" + inp.account + "&bankNickName=" + inp.nickname + "&accountclass=" + inp.cl + "&accounttype=" + inp.type;

                SynapseBankLoginV3_Response_Int bankAddRes =
                    ResponseConverter<SynapseBankLoginV3_Response_Int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (bankAddRes.Is_success == true)
                {
                    res.Is_success = true;
                    res.Is_MFA = bankAddRes.Is_MFA;
                    res.ERROR_MSG = "OK";

                    Logger.Info("Add Bank Page -> AddBank - Bank added MANUALLY Saved Successfully - MemberID: [" + inp.memberid +
                                "], Full Name: [" + inp.fullname + "], Bank Nickname: [" + inp.nickname + "]");
                }
                else
                {
                    var error = "Add Bank Page -> AddBank FAILED - Unable to save bank added MANUALLY - MemberID: [" + inp.memberid +
                                "], Full Name: [" + inp.fullname + "], Bank Nickname: [" + inp.nickname + "]";
                    Logger.Info(error);
                    CommonHelper.notifyCliffAboutError(error);
                    res.ERROR_MSG = bankAddRes.errorMsg;
                }
            }
            catch (Exception we)
            {
                var error = "Add Bank Page -> AddBank (Manual) FAILED - MemberID: [" + inp.memberid +
                             "], Exception: [" + we + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                res.ERROR_MSG = "Add Bank exception # 550";
            }

            res.IsBankManulAdded = true;
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
                Logger.Info("Add Bank Page -> MFALogin Fired -> MemberID: [" + inp.memberid + "], Bank: [" + inp.bank + "], MFA Answer: [" + inp.MFA + "]");

                var scriptSerializer = new JavaScriptSerializer();

                SynapseV3VerifyNode_ServiceInput verifyNodeObj = new SynapseV3VerifyNode_ServiceInput();
                verifyNodeObj.BankName = inp.bank; // not required, but keeping it for just in case we need something to do with it.
                verifyNodeObj.MemberId = inp.memberid;
                verifyNodeObj.mfaResponse = inp.MFA;
                verifyNodeObj.bankId = inp.ba; // this is bank_node_id...grabbed during earlier step

                try
                {
                    var json = scriptSerializer.Serialize(verifyNodeObj);

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "SynapseV3MFABankVerify";

                    SynapseV3BankLoginResult_ServiceRes bnkloginresult = ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                    res.Is_success = bnkloginresult.Is_success;
                    res.Is_MFA = bnkloginresult.Is_MFA;

                    if (bnkloginresult.Is_success == true)
                    {
                        #region MFA Bank Verify Was Successfull

                        Logger.Info("Add Bank Page -> MFALogin Success! -> MemberID: [" + inp.memberid + "], Bank: [" + inp.bank +
                                    "], Was MFA Again?: [" + bnkloginresult.Is_MFA + "]");

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
                            Logger.Error("Add Bank Page -> MFALogin - Incorrect answer submitted - Error Msg: [" + bnkloginresult.errorMsg +
                                     "], MemberID: [" + inp.memberid + "], Bank: [" + inp.bank + "], MFA: [" + inp.MFA + "]");
                            res.Is_MFA = true;
                            res.mfaMessage = bnkloginresult.mfaQuestion; // Would be "-same-"...JS already has the original question for the user to re-answer.
                        }
                        else
                        {
                            Logger.Error("Add Bank Page -> MFALogin FAILED -> Error Msg: [" + bnkloginresult.errorMsg +
                                         "], MemberID: [" + inp.memberid + "], Bank: [" + inp.bank + "], MFA: [" + inp.MFA + "]");
                        }

                        #endregion MFA Bank Verify Was NOT Successfull
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Add Bank Page -> MFALogin FAILED - MemberID: [" + inp.memberid + "], Exception: [" + ex + "]");
                    res.ERROR_MSG = "NoochController Exception - 585";
                }
            }
            catch (Exception we)
            {
                Logger.Error("Add Bank Page -> MFALogin FAILED - MemberID: [" + inp.memberid + "], Exception: [" + we + "]");
            }

            return Json(res);
        }


        [HttpPost]
        [ActionName("SetDefaultBank")]
        public ActionResult SetDefaultBank(setDefaultBankInput input)
        {
            Logger.Info("Add Bank Page -> SetDefaultBank Loaded - MemberID: [" + input.MemberId +
                        "], Bank Name: [" + input.BankName + "], BankID: [" + input.BankOId + "]");

            SynapseBankSetDefaultResult res = new SynapseBankSetDefaultResult();

            try
            {
                if (String.IsNullOrEmpty(input.MemberId) ||
                    String.IsNullOrEmpty(input.BankName) ||
                    String.IsNullOrEmpty(input.BankOId))
                {
                    if (String.IsNullOrEmpty(input.BankName))
                        res.Message = "Invalid data - need Bank Name";
                    else if (String.IsNullOrEmpty(input.MemberId))
                        res.Message = "Invalid data - need MemberId";
                    else if (String.IsNullOrEmpty(input.BankOId))
                        res.Message = "Invalid data - need Bank Id";

                    res.Is_success = false;

                    Logger.Error("Add Bank Page -> SetDefaultBank FAILED - [" + res.Message + "]");
                }
                else
                {
                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    res = CommonHelper.SetSynapseDefaultBank(input.MemberId, input.BankName, input.BankOId);

                    // CC (6/22/16): Why bother to make a Get Request when we can just go straight to Common Helper?
                    //string serviceMethod = "SetSynapseDefaultBank?MemberId=" + input.MemberId + "&BankName=" + input.BankName + "&BankId=" + input.BankOId;
                    //SynapseBankSetDefaultResult bnkloginresult = ResponseConverter<SynapseBankSetDefaultResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    //res.Is_success = bnkloginresult.Is_success;
                    //res.Message = bnkloginresult.Message;
                }
            }
            catch (Exception we)
            {
                Logger.Error("Add Bank Page -> SetDefaultBank FAILED - MemberID: [" + input.MemberId +
                             "], Exception: [" + we.InnerException + "]");
            }

            return Json(res);
        }

        #endregion Add Bank Page



        #region DepositMoney Page

        public ActionResult DepositMoney(string transid, string cip)
        {
            Logger.Info("DepositMoney Page -> Loaded - TransID: [" + transid + "], CIP: [" + cip + "]");

            ResultCompletePayment res = new ResultCompletePayment();
            res.showPaymentInfo = false;
            res.PayorInitialInfo = false; // <div> containing the old form for entering name/email/phone...keeping just for reference

            try
            {
                if (!String.IsNullOrEmpty(transid))
                {
                    res = GetTransDetailsForDepositMoney(transid.ToString(), res);

                    //if (!String.IsNullOrEmpty(userType))
                    //{
                    //    string n = userType;
                    //    res.usrTyp = CommonHelper.GetDecryptedData(n);
                    //    Logger.Info("DepositMoney Page ->  UserType is: [" + res.usrTyp + "]");
                    //    if (res.usrTyp == "NonRegistered")
                    //        res.nonRegUsrContainer = true;
                    //}

                    // CIP is new for Synapse V3 and tells the page what type of ID verification the new user will need.
                    if (!String.IsNullOrEmpty(cip))
                    {
                        res.cip = "renter"; // most common

                        if (cip == "2")
                            res.cip = "vendor";
                        else if (cip == "3")
                            res.cip = "landlord";

                        Logger.Info("DepositMoney Page -> Loaded - CIP is: [" + res.cip + "]");
                    }

                    Response.Write("<script>var errorFromCodeBehind = '0';</script>");
                }
                else
                {
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                Logger.Error("DepositMoney Page -> OUTER EXCEPTION - TransID: [" + transid +
                             "], Exception: [" + ex.Message + "]");
            }

            ViewData["OnLoadData"] = res;
            return View();
        }


        public ResultCompletePayment GetTransDetailsForDepositMoney(string transId, ResultCompletePayment input)
        {
            Logger.Info("DepositMoney Page -> GetTransDetails Fired - TransID: [" + transId + "]");

            ResultCompletePayment rdm = input;

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailByIdForRequestPayPage?TransactionId=" + transId;

            Logger.Info("DepositMoney Page -> GetTransDetails - URL to query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("DepositMoney Page -> GetTransDetails FAILED - Transaction Not Found - TransID: [" + transId + "]");

                rdm.showPaymentInfo = false;
                rdm.pymnt_status = "0";
                Response.Write("<script>errorFromCodeBehind = '1';</script>");
            }
            else
            {
                rdm.rs = transaction.isRentScene;
                rdm.transId = transaction.TransactionId;
                rdm.pymnt_status = transaction.TransactionStatus.ToLower();
                rdm.transMemo = transaction.Memo;
                rdm.senderImage = transaction.SenderPhoto;
                rdm.senderName1 = transaction.Name;
                rdm.showPaymentInfo = true;

                if (rdm.senderName1 == "Marvis Burns") rdm.senderName1 = "Rent Scene";

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

                // Check if this was a request to an existing, but 'NonRegistered' User
                if (transaction.IsExistingButNonRegUser == true)
                {
                    rdm.memidexst = !String.IsNullOrEmpty(transaction.RecepientId) ? transaction.RecepientId : "";
                    rdm.bnkName = transaction.BankName;
                    rdm.bnkNickname = transaction.BankId;

                    if (transaction.BankName == "no bank found")
                        Logger.Error("DepositMoney Page -> GetTransDetails - IsExistingButNonRegUser = 'true', but No Bank Found, so JS should display Add-Bank iFrame.");
                    else
                        rdm.nonRegUsrContainer = true;
                }

                // Now check what TYPE of invitation (phone or email)
                rdm.invitationType = transaction.IsPhoneInvitation == true ? "p" : "e";
                rdm.invitationSentto = transaction.InvitationSentTo;
            }

            return rdm;
        }


        /// <summary>
        /// We can assume that only Non-Nooch users will be triggering this method b/c if they were an existing user,
        /// the payment would have just gone to Synapse and this user wouldn't need to take any action like an existing
        /// user who receives a Request, who has to decide whether to Pay or Reject the Request.
        /// </summary>
        /// <param name="transId"></param>
        /// <param name="memberId"></param>
        /// <param name="userEm"></param>
        /// <param name="userPh"></param>
        /// <param name="userName"></param>
        /// <param name="userPw"></param>
        /// <param name="ssn"></param>
        /// <param name="dob"></param>
        /// <param name="address"></param>
        /// <param name="zip"></param>
        /// <param name="fngprnt"></param>
        /// <param name="ip"></param>
        /// <param name="cip"></param>
        /// <param name="fbid"></param>
        /// <param name="isRentScene"></param>
        /// <param name="isIdImage"></param>
        /// <param name="idImagedata"></param>
        /// <returns></returns>
        public ActionResult RegisterUserWithSynpForDepositMoney(string transId, string memberId, string userEm, string userPh, string userName,
                                                                string userPw, string ssn, string dob, string address, string zip, string fngprnt,
                                                                string ip, string cip, string fbid, bool isRentScene, string isIdImage = "0", string idImagedata = "")
        {
            Logger.Info("DepositMoney Page -> RegisterUserWithSynpForDepositMoney Fired - Email: [" + userEm +
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
                string serviceMethod = "RegisterNonNoochUserWithSynapse";

                userPh = CommonHelper.RemovePhoneNumberFormatting(userPh);

                Logger.Info("DepositMoney Page -> RegisterUserWithSynpForDepositMoney -> PARAMETERS for '/RegisterNonNoochUserWithSynapse': transId: " + transId +
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
                inputClass.isRentScene = isRentScene;
                inputClass.cip = cip;

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputClass);

                Logger.Info("DepositMoney Page -> RegisterUserWithSynpForDepositMoney - Full Query String: [ " + String.Concat(serviceUrl, serviceMethod) + " ]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (regUserResponse.success == "true")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else if (regUserResponse.success == "false")
                {
                    Logger.Error("DepositMoney Page -> RegisterUserWithSynpForDepositMoney FAILED - SERVER RETURNED 'success' = 'false' - TransID: [" + transId + "]");
                    res.reason = regUserResponse.reason;
                }
                else
                {
                    Logger.Error("DepositMoney Page -> RegisterUserWithSynpForDepositMoney FAILED - UNKNOWN ERROR FROM SERVER - TransID: [" + transId + "]");
                }

                res.ssn_verify_status = regUserResponse.ssn_verify_status;
            }
            catch (Exception ex)
            {
                Logger.Error("DepositMoney Page -> RegisterUserWithSynpForDepositMoney attempt FAILED Failed, Reason: [" + res.reason + "], " +
                             "TransID: [" + transId + "], [Exception: " + ex + "]");
            }

            return Json(res);
        }

        #endregion DepositMoney Page



        #region DepositMoneyComplete Page

        public ActionResult DepositMoneyComplete()
        {
            ResultMoveMoneyFromLandingPageComplete res = new ResultMoveMoneyFromLandingPageComplete();
            res.paymentSuccess = false;
            res.payinfobar = true;

            Logger.Info("DepositMoneyComplete Page -> Loaded - 'mem_id' Param In URL: [" + Request.QueryString["mem_id"] + "]");

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
                            Logger.Info("DepositMoneyComplete Page -> RENT SCENE Transaction Detected - TransID: [" + tr_id + "]");
                            res.rs = "true";
                        }

                        // Getting transaction details to check if transaction is still pending
                        res = GetTransDetailsForDepositMoneyComplete(tr_id, res);

                        if (res.IsTransactionStillPending)
                            res = finishTransaction(mem_id, tr_id, res);
                    }
                    else
                    {
                        Logger.Error("DepositMoneyComplete Page -> ERROR - 'mem_id' in query string did not have 2 parts as expected - mem_id Parameter: [" + Request.QueryString["mem_id"] + "]");
                        Response.Write("<script>var errorFromCodeBehind = '2';</script>");
                    }
                }
                else
                {
                    // something wrong with query string
                    Logger.Error("depositMoneyComplete Page -> ERROR - 'mem_id' in query string was NULL or empty mem_id Parameter: [" + Request.QueryString["mem_id"] + "]");
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("depositMoneyComplete Page -> OUTER EXCEPTION - mem_id Parameter: [" + Request.QueryString["mem_id"] +
                             "], [Exception: " + ex + "]");
                res.payinfobar = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
            }

            ViewData["OnLoaddata"] = res;

            return View();
        }


        public ResultMoveMoneyFromLandingPageComplete GetTransDetailsForDepositMoneyComplete(string TransactionId, ResultMoveMoneyFromLandingPageComplete resultDepositMoneyComplete)
        {
            ResultMoveMoneyFromLandingPageComplete rdmc = resultDepositMoneyComplete;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("depositMoneyComplete Page -> getTransDetails FAILED - Transaction was NULL - TransId: [" + TransactionId + "]");

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


        private ResultMoveMoneyFromLandingPageComplete finishTransaction(string MemberIdAfterSynapseAccountCreation, string TransactionId, ResultMoveMoneyFromLandingPageComplete resultDepositMoneyComplete)
        {
            ResultMoveMoneyFromLandingPageComplete res = resultDepositMoneyComplete;
            res.paymentSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit?TransactionId=" + TransactionId +
                                       "&MemberIdAfterSynapseAccountCreation=" + MemberIdAfterSynapseAccountCreation +
                                       "&TransactionType=SentToNewUser&recipMemId=";

                if ((res.usrTyp == "Existing" || res.usrTyp == "Tenant") &&
                     res.payeeMemId.Length > 5)
                    serviceMethod = serviceMethod + res.payeeMemId;


                Logger.Info("DepositMoneyComplete Page -> finishTransaction - About to Query Nooch Service to move money - URL: [" +
                             String.Concat(serviceUrl, serviceMethod) + "]");

                TransactionDto moveMoneyResult = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (moveMoneyResult != null)
                {
                    if (moveMoneyResult.synapseTransResult == "Success")
                        res.paymentSuccess = true;
                    else
                    {
                        Logger.Error("DepositMoneyComplete Page -> completeTrans FAILED - TransId: [" + TransactionId + "]");
                        Response.Write("<script>errorFromCodeBehind = 'failed';</script>");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("depositMoneyComplete Page -> completeTrans FAILED - TransId: [" + TransactionId +
                             "], Exception: [" + ex + "]");
            }

            return res;
        }

        #endregion DepositMoneyComplete Page



        #region Reset Password Page

        public ActionResult ResetPassword(string memberId)
        {
            ResultResetPassword resultResetPassword = new ResultResetPassword();
            resultResetPassword.requestExpiredorNotFound = false;
            resultResetPassword.pin = false;

            if (!String.IsNullOrEmpty(memberId))
            {
                var memberObj = CommonHelper.GetMemberDetails(memberId);
                if (memberObj != null)
                {
                    resultResetPassword.usermail = CommonHelper.GetDecryptedData(memberObj.UserName);
                    resultResetPassword.isRs = memberObj.isRentScene ?? false;

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "resetlinkvalidationcheck?memberId=" + memberId;

                    var isMemberPwdResetted = ResponseConverter<Nooch.Common.Entities.BoolResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    if (isMemberPwdResetted.Result == false)
                        resultResetPassword.requestExpiredorNotFound = true;
                }
                else
                    resultResetPassword.invalidUser = "true";
            }
            else
                resultResetPassword.invalidUser = "true";

            ViewData["OnLoaddata"] = resultResetPassword;
            return View();
        }


        public string ResetPasswordSubmit(string PWDText, string memberId, string newUser = "")
        {
            try
            {
                var objAesAlgorithm = new AES();
                string encryptedPassword = objAesAlgorithm.Encrypt(PWDText.Trim(), string.Empty);
                string serviceMethod = string.Empty;
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                serviceMethod = "ResetPassword?memberId=" + memberId + "&newPassword=" + encryptedPassword + "&newUser=" + newUser;

                var isMemberPwdResetted = ResponseConverter<Nooch.Common.Entities.BoolResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                return isMemberPwdResetted.Result ? "true" : "false";
            }
            catch (Exception ex)
            {
                Logger.Error("Reset PW Page -> ResetPasswordButton FAILED - MemberID: [" + memberId + "], Exception: [" + ex + "]");
                return ex.Message;
            }
        }


        public ActionResult pinNumberVerificationButton_Click(string PINTextBox, string memberId)
        {
            synapseV3GenericResponse res = new synapseV3GenericResponse();
            res.isSuccess = false;

            var objAesAlgorithm = new AES();
            string encryptedPin = objAesAlgorithm.Encrypt(PINTextBox.Trim(), string.Empty);
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            serviceMethod = "ValidatePinNumberForPasswordForgotPage?memberId=" + memberId + "&pinNo=" + encryptedPin;

            var isMemberValid = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (isMemberValid.Result.Equals("Success"))
                res.isSuccess = true;
            else
                res.msg = isMemberValid.Result.ToString();

            return Json(res);
        }


        #endregion Reset Password Page



        #region PayRequest Page

        public ActionResult PayRequest(string TransactionId, string UserType, string cip)
        {
            ResultCompletePayment res = new ResultCompletePayment();
            res.showPaymentInfo = false;
            res.PayorInitialInfo = false; // <div> containing the old form for entering name/email/phone...keeping just for reference

            Logger.Info("PayRequest Page -> Loaded - TransID: [" + TransactionId + "], CIP: [" + cip + "]");

            try
            {
                if (!String.IsNullOrEmpty(TransactionId))
                {
                    res = GetTransDetailsForPayRequest(TransactionId.ToString(), res);

                    if (!String.IsNullOrEmpty(UserType))
                    {
                        string n = UserType;
                        res.usrTyp = CommonHelper.GetDecryptedData(n);
                        Logger.Info("PayRequest Page -> UserType is: [" + res.usrTyp + "]");

                        if (res.usrTyp == "NonRegistered")
                            res.nonRegUsrContainer = true;
                    }

                    // CIP is new for Synapse V3 and tells the page what type of ID verification the new user will need.
                    if (!String.IsNullOrEmpty(cip))
                    {
                        res.cip = "renter"; // most common

                        if (cip == "2")
                            res.cip = "vendor";
                        else if (cip == "3")
                            res.cip = "landlord";

                        Logger.Info("PayRequest Page -> CIP is: [" + res.cip + "]");
                    }

                    // Check if this is a RENT Payment request (from a Landlord)
                    if (Request.Params.AllKeys.Contains("IsRentTrans"))
                    {
                        if (Request["IsRentTrans"].ToLower() == "true")
                        {
                            Logger.Info("PayRequest Page -> RENT PAYMENT Detected");

                            res.transType = "rent";
                            res.usrTyp = "tenant";
                        }
                    }

                    Response.Write("<script>var errorFromCodeBehind = '0';</script>");
                }
                else
                {
                    // something wrong with query string
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                Logger.Error("PayRequest Page -> OUTER EXCEPTION - TransID: [" + TransactionId +
                             "], Exception: [" + ex.Message + "]");
            }

            ViewData["OnLoadData"] = res;
            return View();
        }


        public ResultCompletePayment GetTransDetailsForPayRequest(string TransactionId, ResultCompletePayment resultPayRequest)
        {
            Logger.Info("PayRequest Page -> GetTransDetails Fired - TransID: [" + TransactionId + "]");

            ResultCompletePayment rpr = resultPayRequest;

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            Logger.Info("PayRequest Page -> GetTransDetails - URL to Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction != null)
            {
                rpr.rs = transaction.isRentScene;
                rpr.showPaymentInfo = true;
                rpr.transId = transaction.TransactionId;
                rpr.pymnt_status = transaction.TransactionStatus.ToLower();
                rpr.transMemo = transaction.Memo;
                rpr.senderImage = transaction.RecepientPhoto;
                rpr.senderName1 = (!String.IsNullOrEmpty(transaction.RecepientName) && transaction.RecepientName.Length > 2) ?
                                     transaction.RecepientName :
                                     transaction.Name;

                if (rpr.senderName1 == "Marvis Burns") rpr.senderName1 = "Rent Scene";

                string s = transaction.Amount.ToString("n2");
                string[] s1 = s.Split('.');
                rpr.transAmountd = s1[0].ToString();
                rpr.transAmountc = s1.Length == 2 ? s1[1].ToString() : "00";


                // Check if this was a request to an existing, but 'NonRegistered' User
                if (transaction.IsExistingButNonRegUser == true)
                {
                    if (transaction.TransactionStatus.ToLower() != "pending")
                        Logger.Error("PayRequest Page -> GetTransDetails - IsExistingButNonRegUser = 'true', but Transaction no longer pending!");

                    rpr.memidexst = !String.IsNullOrEmpty(transaction.RecepientId) ? transaction.RecepientId : "";
                    rpr.bnkName = transaction.BankName;
                    rpr.bnkNickname = transaction.BankId;

                    if (transaction.BankName == "no bank found")
                    {
                        Logger.Error("PayRequest Page -> GetTransDetails - IsExistingButNonRegUser = 'true', but No Bank Found, so JS should display Add-Bank iFrame.");
                    }
                    else
                    {
                        rpr.nonRegUsrContainer = true;
                    }
                }
                else if (rpr.transType == "rent") // Set in Page_Load above based on URL query string
                {
                    Logger.Info("PayRequest Page -> GetTransDetails - Got a RENT Payment!");
                }
                else
                {
                    Logger.Info("PayRequest Page -> GetTransDetails - Request was to a NEW USER - [" + transaction.InvitationSentTo + "]");
                }

                // Now check what TYPE of invitation (phone or email)
                rpr.invitationType = transaction.IsPhoneInvitation == true ? "p" : "e";
                rpr.invitationSentto = transaction.InvitationSentTo;
            }
            else
            {
                Logger.Error("PayRequest Page -> GetTransDetails FAILED - Transaction Not Found - TransID: [" + TransactionId + "]");

                rpr.showPaymentInfo = false;
                rpr.pymnt_status = "0";
                Response.Write("<script>errorFromCodeBehind = '1';</script>");
            }

            return rpr;
        }


        public ActionResult RegisterUserWithSynpForPayRequest(string transId, string memberId, string userEm, string userPh, string userName,
                                                              string userPw, string ssn, string dob, string address, string zip, string fngprnt,
                                                              string ip, string cip, string fbid, bool isRentScene, string isIdImage = "0", string idImagedata = "")
        {
            Logger.Info("PayRequest Page -> RegisterUserWithSynpForPayRequest Fired - Email: [" + userEm +
                        "], TransID: [" + transId + "], MemberID: [" + memberId + "], CIP: [" + cip + "], FBID: [" + fbid + "]");

            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";
            res.memberIdGenerated = "";
            res.reason = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                userPh = CommonHelper.RemovePhoneNumberFormatting(userPh);

                Logger.Info("payRequest Page -> RegisterUserWithSynp -> PARAMETERS to send to Server: transId: " + transId +
                            ", memberId (If existing user): [" + memberId + "], userEm: [" + userEm +
                            "], userPh: [" + userPh + "], userPw: [" + userPw +
                            "], ssn: [" + ssn + "], dob: [" + dob +
                            "], address: [" + address + "], zip: [" + zip +
                            "], Has ID Img: [" + isIdImage + "], CIP: [" + cip +
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
                inputClass.fbid = fbid != "not connected" ? fbid : null;
                inputClass.isRentScene = isRentScene != null ? isRentScene : false;

                if (!String.IsNullOrEmpty(memberId) && memberId.Length > 30)
                {
                    inputClass.memberId = memberId;

                    // Member must already exist, so use RegisterEXISTINGUserWithSynapseV3()
                    serviceMethod = "RegisterExistingUserWithSynapseV3";
                }
                else // Member DOES NOT already exist, so use RegisterNONNOOCHUserWithSynapse()
                    serviceMethod = "RegisterNonNoochUserWithSynapse";

                string json = scriptSerializer.Serialize(inputClass);

                Logger.Info("PayRequest Page -> RegisterUserWithSynpForPayRequest - URL To Query: [ " + String.Concat(serviceUrl, serviceMethod) + " ]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (regUserResponse.success.ToLower() == "true")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else if (regUserResponse.success.ToLower() == "false")
                {
                    Logger.Error("PayRequest Page -> RegisterUserWithSynpForPayRequest FAILED - Reason: [" + regUserResponse.reason +
                                 "], TransID: [" + transId + "]");
                    res.reason = regUserResponse.reason;
                }
                else
                {
                    Logger.Error("PayRequest Page -> RegisterUserWithSynpForPayRequest FAILED - UNKNOWN ERROR FROM SERVER - TransID: [" + transId + "]");
                    res.reason = regUserResponse.reason;
                }

                res.ssn_verify_status = regUserResponse.ssn_verify_status;
            }
            catch (Exception ex)
            {
                Logger.Error("PayRequest Page -> RegisterUserWithSynpForPayRequest attempt FAILED Failed - Reason: [" + res.reason + "], " +
                             "TransID: [" + transId + "], Exception: [" + ex.Message + "]");
            }

            return Json(res);
        }

        #endregion PayRequest Page



        #region PayRequestComplete Page

        public ActionResult PayRequestComplete()
        {
            ResultMoveMoneyFromLandingPageComplete rpc = new ResultMoveMoneyFromLandingPageComplete();

            Logger.Info("PayRequestComplete Page -> Loaded - 'mem_id' Param In URL: [" + Request.QueryString["mem_id"] + "]");

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
                        bool isForRentScene = allQueryStrings.Length > 2 && allQueryStrings[2] == "true" ? true : false;

                        rpc.memId = mem_id;

                        // Check if this payment is for Rent Scene
                        if (isForRentScene)
                        {
                            Logger.Info("PayRequestComplete Page -> RENT SCENE Transaction Detected - TransID: [" + tr_id + "]");
                            rpc.rs = "true";
                        }

                        // Getting transaction details to check if transaction is still pending
                        rpc = GetTransDetailsForPayRequestComplete(tr_id, rpc);

                        if (rpc.IsTransactionStillPending)
                            rpc = completeTrans(mem_id, tr_id, rpc);
                        else
                            Logger.Error("PayRequestComplete Page -> Transaction No Longer Pending - TransID: [" + tr_id + "]");
                    }
                    else
                    {
                        Logger.Error("PayRequestComplete Page -> ERROR - 'mem_id' in query string did not have 2 parts as expected - [mem_id Parameter: " + Request.QueryString["mem_id"] + "]");
                        Response.Write("<script>var errorFromCodeBehind = '2';</script>");
                    }
                }
                else
                {
                    // something wrong with query string
                    Logger.Error("PayRequestComplete Page FAILED -> ERROR - 'mem_id' in query string was NULL or empty - mem_id Parameter: " + rpc.memId + "]");
                    Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PayRequestComplete Page -> OUTER EXCEPTION - mem_id Param: [" +
                             rpc.memId + "], Exception: [" + ex + "]");
                rpc.payinfobar = false;
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
            }

            ViewData["OnLoaddata"] = rpc;
            return View();
        }


        private ResultMoveMoneyFromLandingPageComplete completeTrans(string MemberIdAfterSynapseAccountCreation, string TransactionId, ResultMoveMoneyFromLandingPageComplete resultPayComplete)
        {
            ResultMoveMoneyFromLandingPageComplete res = resultPayComplete;
            res.paymentSuccess = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "GetTransactionDetailByIdAndMoveMoneyForNewUserDeposit?TransactionId=" + TransactionId +
                                       "&MemberIdAfterSynapseAccountCreation=" + MemberIdAfterSynapseAccountCreation +
                                       "&TransactionType=RequestToNewUser&recipMemId=";

                if ((res.usrTyp == "Existing" || res.usrTyp == "Tenant") &&
                     res.payeeMemId.Length > 5)
                    serviceMethod = serviceMethod + res.payeeMemId;

                Logger.Info("PayRequestComplete Page -> completeTrans - About to Query Nooch Service to move money - URL: [" +
                             String.Concat(serviceUrl, serviceMethod) + "]");

                TransactionDto moveMoneyResult = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (moveMoneyResult != null)
                {
                    if (moveMoneyResult.synapseTransResult == "Success")
                        res.paymentSuccess = true;
                    else
                    {
                        Logger.Error("PayRequestComplete Page -> completeTrans FAILED - TransID: [" + TransactionId + "]");
                        Response.Write("<script>errorFromCodeBehind = 'failed';</script>");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PayRequestComplete Page -> completeTrans FAILED - TransID: [" + TransactionId +
                             "], Exception: [" + ex + "]");
            }

            return res;
        }


        public ResultMoveMoneyFromLandingPageComplete GetTransDetailsForPayRequestComplete(string TransactionId, ResultMoveMoneyFromLandingPageComplete resultPayRequestComplt)
        {
            Logger.Info("payRequestComplete Page -> GetTransDetailsForPayRequestComplete Fired - TransID: [" + TransactionId + "]");

            ResultMoveMoneyFromLandingPageComplete rpc = resultPayRequestComplt;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailByIdForRequestPayPage?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction == null)
            {
                Logger.Error("payRequestComplete Page -> getTransDetails FAILED - Transaction was Null - TransID: [" + TransactionId + "]");

                Response.Write("<script>errorFromCodeBehind = '3';</script>");
                rpc.IsTransactionStillPending = false;

                return rpc;
            }
            else
            {
                rpc.senderImage = transaction.RecepientPhoto;
                rpc.senderName1 = (!String.IsNullOrEmpty(transaction.RecepientName) && transaction.RecepientName.Length > 2) ?
                                    transaction.RecepientName :
                                    transaction.Name;
                rpc.transAmountd = transaction.Amount.ToString("n2");
                rpc.transMemo = transaction.Memo;

                if (rpc.senderName1 == "Marvis Burns") rpc.senderName1 = "Rent Scene";


                // Check if this was a Rent request from a Landlord
                if (!String.IsNullOrEmpty(transaction.TransactionType) &&
                    transaction.TransactionType == "Rent")
                {
                    Logger.Info("payRequestComplete Page -> GetTransDetailsForPayRequestComplete - Rent Payment Detected");
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
                    Logger.Info("payRequestComplete Page -> GetTransDetailsForPayRequestComplete - Payment No Longer Pending - TransID: [" + TransactionId + "]");

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

                if (!String.IsNullOrEmpty(memId))
                {
                    Logger.Info("CreateAccount Page -> Loaded - MemberID: [" + memId + "]");
                    rca = GetMemberDetailsForCreateAccount(memId, rca);
                }
                else if (!String.IsNullOrEmpty(TransId))
                {
                    Logger.Info("CreateAccount Page -> Loaded - TransID [: " + TransId + "]");
                    Session["TransId"] = TransId;
                    rca = GetTransDetailsForCreateAccount(TransId.ToString(), rca);
                }
                else
                {
                    // No MemberID in URL, so must be creating a brand NEW user
                    rca.errorId = "0";
                    rca.isNewUser = true;

                    if (!String.IsNullOrEmpty(rs))
                        rca.rs = (rs.ToLower() == "true" || rs.ToLower() == "yes") ? "true" : "false";

                    if (!String.IsNullOrEmpty(type))
                    {
                        if (type.Length == 1)
                        {
                            if (type == "2") rca.type = "vendor";
                            else if (type == "3") rca.type = "landlord";
                            else rca.type = "renter";
                        }
                        else
                            rca.type = type;
                    }

                    Logger.Info("CreateAccount Page -> Loaded - Is RentScene Payment: [" + rs + "], Type: [" + rca.type + "]");
                }
            }
            catch (Exception ex)
            {
                rca.errorId = "1";

                Logger.Error("CreateAccount Page -> OUTER EXCEPTION - MemberID: [" + memId +
                             "], Exception: [" + ex.Message + "]");
            }

            ViewData["OnLoaddata"] = rca;
            return View();
        }


        public ResultcreateAccount GetTransDetailsForCreateAccount(string TransactionId, ResultcreateAccount resultcreateAccount)
        {
            ResultcreateAccount rca = resultcreateAccount;
            Logger.Info("createAccount Page -> GetTransDetails Fired - TransID: [" + TransactionId + "]");

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailById?TransactionId=" + TransactionId;

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
                //Logger.Info("createAccount Page -> GetMemberDetailsForCreateAccount Initiated - MemberID: [" + memberId + "]");

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "GetMemberDetailsForLandingPage?memberId=" + memberId;

                Logger.Info("createAccount Page -> GetMemberDetailsForCreateAccount - URL to Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

                MemberDto member = ResponseConverter<MemberDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (member == null)
                {
                    Logger.Error("CreateAccount Page -> GetMemberDetailsForCreateAccount FAILED - Server did not find a user with the MemberID: [" + memberId + "]");
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
                    rca.rs = member.isRs ? "true" : "false";

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
                Logger.Error("createAccount Page -> GetMemberDetails FAILED - Outer Exception: [" + ex + "]");
            }

            return rca;
        }


        [HttpPost]
        [ActionName("saveMemberInfo")]
        public ActionResult saveMemberInfo(ResultcreateAccount userData)
        {
           
            Logger.Info("Create Account Page -> saveMemberInfo Fired - MemberID: [" + userData.memId +
                        "], Name: [" + userData.name + "], Email: [" + userData.email +
                        "], Phone: [" + userData.phone + "], DOB: [" + userData.dob +
                        "], SSN: [" + userData.ssn + "], Address: [" + userData.address +
                        "], IP: [" + userData.ip + "], Is Image Sent: [" + userData.isIdImage +
                        "], FBID: [" + userData.fbid + "], isRentScene: [" + userData.rs +
                        "], CIP: [" + userData.cip + "]");

            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";
            res.memberIdGenerated = "";
            res.reason = "Initial - code behind";

            try
            {
                // Determine if this is for a new or existing user
                bool newUser = String.IsNullOrEmpty(userData.memId);

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = newUser ? "RegisterNonNoochUserWithSynapse" : "RegisterExistingUserWithSynapseV3";
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
                inputClass.cip = !String.IsNullOrEmpty(userData.cip) ? userData.cip : "renter";

                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inputClass);

                Logger.Info("Create Account Page -> saveMemberInfo CHECKPOINT #1 - New User?: [" + newUser + "], URL To Use: [" + urlToUse + "]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                //Logger.Info("Create Account Page -> saveMemberInfo RESULT: [" + scriptSerializer.Serialize(regUserResponse) + "]");

                res.ssn_verify_status = regUserResponse.ssn_verify_status;

                if (regUserResponse.success.ToLower() == "true")
                {
                    Logger.Info("Create Account Page -> saveMemberInfo SUCCESS! - Message: [" + regUserResponse.reason + "]");
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else
                {
                    Logger.Error("Create Account Page -> saveMemberInfo FAILED! - Message: [" + regUserResponse.reason + "]");
                    res.reason = regUserResponse.reason;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Create Account Page -> saveMemberInfo FAILED - MemberID: [" + userData.memId + "], Exception: [" + ex.Message + "]");
                res.reason = "Code-behind exception during saveMemberInfo";
            }

            return Json(res);
        }


        [HttpPost]
        [ActionName("submit2FAPin")]
        public ActionResult submit2FAPin(submitValidationPin userData)
        {
            Logger.Info("CreateAccount Page -> submit2FAPin Fired - MemberID: [" + userData.memberId +
                        "], Validation PIN: [" + userData.pin + "]");

            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial - code behind";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "Submit2FAPin?memberId=" + userData.memberId + "&pin=" + userData.pin;
                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("CreateAccount Page -> submit2FAPin CHECKPOINT #1 - URL To Use: [" + urlToUse + "]");

                synapseV3GenericResponse response = ResponseConverter<synapseV3GenericResponse>.ConvertToCustomEntity(urlToUse);

                //Logger.Info("Create Account Page -> saveMemberInfo RESULT: [" + scriptSerializer.Serialize(regUserResponse) + "]");

                if (response.isSuccess)
                {
                    Logger.Info("Create Account Page -> submit2FAPin SUCCESS! - Message: [" + response.msg + "]");
                    res.success = true;
                    res.msg = response.msg;
                }
                else
                {
                    Logger.Info("Create Account Page -> submit2FAPin FAILED! - Message: [" + response.msg + "]");
                    res.msg = response.msg;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Create Account Page -> submit2FAPin FAILED - MemberID: [" + userData.memberId + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during submit2FAPin";
            }

            return Json(res);
        }

        #endregion CreateAccount Page



        #region RejectMoney Page

        public ActionResult RejectMoney(string TransactionId, string UserType, string TransType)
        {
            PageLoadDataRejectMoney res = new PageLoadDataRejectMoney();

            Logger.Info("rejectMoney Page -> Loaded - TransID: [" + TransactionId + "]");

            try
            {
                // TransId - transaction id from query string
                // UserType - tells us if user opening link is existing, nonregistered, or completelty brand new user -- need this to show hide create account form later

                if (!String.IsNullOrEmpty(TransactionId) &&
                    !String.IsNullOrEmpty(UserType))
                {
                    Session["TransactionId"] = TransactionId;
                    Session["UserType"] = UserType;
                    Session["TransType"] = TransType;

                    res.errorFromCodeBehind = "0";

                    ResultCancelRequest TransDetails = GetTransToReject(TransactionId);

                    if (TransDetails.IsTransFound)
                    {
                        res.transType = TransDetails.UserType;
                        res.TransId = TransDetails.TransId;
                        res.UserType = UserType;
                        res.transStatus = TransDetails.TransStatus;
                        res.transAmout = TransDetails.AmountLabel;
                        res.transMemo = TransDetails.transMemo;
                        res.senderImage = TransDetails.senderImage;
                        res.nameLabel = TransDetails.nameLabel;
                        res.isRentScene = TransDetails.isRentScene.ToString().ToLower();
                    }
                    else
                    {
                        Logger.Error("rejectMoney Page -> ERROR - Transaction Not Found - TransID: [" + TransactionId + "]");
                        res.errorFromCodeBehind = "1";
                    }
                }
                else
                {
                    res.SenderAndTransInfodiv = false;
                    res.createAccountPrompt = false;

                    // Use TransResult (inside TransactionResult DIV) to display error message (in addition to .swal() alert)
                    res.TransactionResult = true;
                    res.errorFromCodeBehind = "1";

                    Logger.Error("rejectMoney Page -> ERROR - One of the required fields in query string was NULL or empty - " +
                                 "TransID: [" + TransactionId + "], " +
                                 "UserType: [" + UserType + "], " +
                                 "TransType: [" + TransType + "]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("rejectMoney Page -> OUTER EXCEPTION - TransID: [" + TransactionId +
                             "], Exception: [" + ex.Message + "]");
            }

            return View(res);
        }


        [HttpPost]
        public ActionResult RejectMoneyBtnClick(string TransactionId, string UserType, string TransType)
        {
            PageLoadDataRejectMoney res = new PageLoadDataRejectMoney();
            res.errorFromCodeBehind = "initial";

            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            serviceMethod = "RejectMoneyCommon?TransactionId=" + TransactionId +
                            "&UserType=" + UserType +
                            "&TransType=" + TransType;

            Logger.Info("rejectMoney Page -> RejectRequest - Full Service URL To Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");

            var serviceResult = ResponseConverter<StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (!String.IsNullOrEmpty(serviceResult.Result) && serviceResult.Result.IndexOf("Success") > -1)
            {
                res.errorFromCodeBehind = "0";

                res.transStatus = "Request rejected successfully.";

                Logger.Info("rejectMoney Page -> RejectRequest SUCCESSFUL - [TransactionId Parameter: " + TransactionId + "]");
            }
            else if (!String.IsNullOrEmpty(serviceResult.Result) && serviceResult.Result.IndexOf("no longer pending") > -1)
            {
                res.errorFromCodeBehind = "Transaction no longer pending";
                res.transStatus = "Already Rejected or Cancelled";
            }
            else
            {
                Logger.Error("rejectMoney Page -> RejectRequest FAILED - Server Result: [" + serviceResult.Result + "], " +
                             "TransID: [" + TransactionId + "]");
                res.errorFromCodeBehind = "1";
            }

            return Json(res);
        }


        /// <summary>
        /// Only called from the RejectMoney page.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <returns></returns>
        public ResultCancelRequest GetTransToReject(string TransactionId)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            rcr.IsTransFound = transaction != null;
            rcr.TransStatus = transaction.TransactionStatus;
            rcr.AmountLabel = transaction.Amount.ToString("n2");
            rcr.transMemo = transaction.Memo.Trim();
            rcr.TransType = transaction.TransactionType;
            rcr.TransId = transaction.TransactionId;
            rcr.isRentScene = transaction.isRentScene;

            //if (transaction.IsPhoneInvitation && transaction.PhoneNumberInvited.Length > 0)
            //{
            //    rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
            //    rcr.nameLabel = transaction.PhoneNumberInvited;
            //}
            //else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            //{
            //    rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
            //    rcr.nameLabel = transaction.InvitationSentTo;
            //}
            //else
            //{
            rcr.senderImage = transaction.SenderPhoto;
            rcr.nameLabel = transaction.Name;
            //}

            return rcr;
        }

        #endregion RejectMoney Page



        #region IDVerification Page

        public ActionResult idVerification(string memid, string from, string redUrl)
        {
            idVerification idv = new idVerification();

            Logger.Info("idVerification Page -> Loaded - MemID: [" + memid + "]");

            idv.error_msg = "initial";
            idv.from = "unknown";
            idv.redUrl = "https://www.nooch.com";

            try
            {
                if (!String.IsNullOrEmpty(from))
                {
                    idv.from = from;

                    if (idv.from == "addbnk" && !String.IsNullOrEmpty(redUrl))
                        idv.redUrl = redUrl;
                }

                if (!String.IsNullOrEmpty(memid))
                {
                    idv.memid = memid;
                    idv = getIdVerificationQuestionsV3(memid, idv);
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

                Logger.Error("idVerification Page -> OUTER EXCEPTION - MemID: [" + memid + "], Exception: [" + ex.Message + "]");
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
                string serviceMethod = "getIdVerificationQuestionsV3?memberid=" + memberId;

                synapseV2_IdVerQsForDisplay_Int questionsFromDb =
                    ResponseConverter<synapseV2_IdVerQsForDisplay_Int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (questionsFromDb == null)
                {
                    Logger.Error("idVerification Page -> getVerificationQuestionsV2 - Could Not Find Member - MemID: " + memberId + "]");

                    idv.was_error = "true";
                    idv.error_msg = "Member not found";
                }
                else if (questionsFromDb.success == true)
                {
                    idv.was_error = "false";
                    idv.error_msg = "OK";

                    // Set the QuestionSetId value (Hidden Input)                
                    if (!String.IsNullOrEmpty(questionsFromDb.qSetId))
                        idv.qsetId = questionsFromDb.qSetId.ToString();
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
                    Logger.Error("idVerification Page -> getVerificationQuestionsV2 - Answers Already Submitted (Shouldn't be here then) - " +
                                 "MemberID: " + memberId + "]");
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
                Logger.Error("idVerification Page -> getVerificationQuestionsV2 FAILED - MemberID " + memberId + "], Exception: [" + ex + "]");
            }

            return idv;
        }


        // Submit the user's answers       
        public ActionResult submitResponses(string MemberId, string questionSetId, string quest1id, string quest2id, string quest3id, string quest4id, string quest5id, string answer1id, string answer2id, string answer3id, string answer4id, string answer5id)
        {
            Logger.Info("idVerification Page -> submitResponses Fired");

            synapseV3GenericResponse res = new synapseV3GenericResponse();

            try
            {
                if (String.IsNullOrEmpty(MemberId) || String.IsNullOrEmpty(questionSetId))
                {
                    if (String.IsNullOrEmpty(MemberId))
                        res.msg = "Invalid Data - Need a MemberID!";
                    else if (String.IsNullOrEmpty(questionSetId))
                        res.msg = "Invalid Data - Missing a Question Set ID";

                    Logger.Info("idVerification Page -> submitResponses ABORTED - [" + res.msg + "]");

                    res.isSuccess = false;
                }
                // Check for 5 total answers
                else if (answer1id == null || answer2id == null || answer3id == null || answer4id == null || answer5id == null)
                {
                    Logger.Info("idVerification Page -> submitResponses ABORTED: Missing at least 1 answer. [MemberId: " + MemberId + "]");

                    res.isSuccess = false;
                    res.msg = "Missing at least 1 answer (should have 5 total answers).";
                }
                else
                {
                    // All required data exists, now send to NoochService.svc
                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "submitIdVerificationAswersV3?memberId=" + MemberId + "&questionSetId=" + questionSetId + "&quest1id=" + quest1id +
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
                Logger.Error("idVerification Page -> submitResponses FAILED - WebException: [" + we.Status + "]");
            }

            return Json(res);
        }

        #endregion IDVerification Page



        #region MakePayment Page

        /// <summary>
        /// For when the /makePayment page first loads.
        /// </summary>
        /// <returns></returns>
        public ActionResult makePayment(string from, string TransId, string type)
        {
            makePaymentPg res = new makePaymentPg();
            res.classForPinButton = "hidden";
            res.classForForm = "";

            try
            {
                if (!String.IsNullOrEmpty(from))
                {
                    Logger.Info("Make Payment Page -> Loaded for: [" + from.ToUpper() + "]");

                    if (from.IndexOf("rentscene") > -1)
                    {
                        res.from = "rentscene";
                    }
                    else if (from == "habitat")
                    {
                        res.from = "habitat";

                        // Require PIN for Habitat (for now)
                        res.classForForm = "hidden";
                        res.classForPinButton = "";
                    }
                    else if (from == "appjaxx")
                    {
                        res.from = "appjaxx";

                        // Only requiring the PIN for AppJaxx (for now), which is why the classForPinButton is 'hidden' by default but not classForForm
                        res.classForForm = "hidden";
                        res.classForPinButton = "";
                    }
                    else
                        res.from = "nooch";
                }
                else // Set Nooch as the default
                    res.from = "nooch";
            }
            catch (Exception ex)
            {
                res.errorId = "1";
                Logger.Error("Make Payment Page -> OUTER EXCEPTION: [" + ex.Message + "]");
            }

            ViewData["OnLoadData"] = res;
            return View();
        }


        /// <summary>
        /// For Submitting a payment or request from the MakePayment browser page.
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
            Logger.Info("Make Payment Page -> submitPayment Fired - From: [" + from + "], isRequest: [" + isRequest +
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
                Logger.Info("Make Payment Page -> submitPayment Attempted - Recipient email already exists: [" + email + "]");

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

                pin = String.IsNullOrEmpty(pin) || pin.Length != 4 ? "0000" : pin;

                if (from == "rentscene")
                    pin = CommonHelper.GetMemberPinByUserName("payments@rentscene.com");
                else if (from == "habitat")
                    pin = CommonHelper.GetMemberPinByUserName("andrew@tryhabitat.com");
                else if (from == "nooch")
                    pin = CommonHelper.GetMemberPinByUserName("team@nooch.com");
                else if (from == "appjaxx")
                    pin = CommonHelper.GetMemberPinByUserName("josh@appjaxx.com");

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

                if (isRequest)
                {
                    serviceMethod = "RequestMoneyForRentScene?from=" + from +
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

                    var userName = "";

                    if (from.ToLower() == "rentscene")
                        userName = "payments@rentscene.com";
                    else if (from.ToLower() == "habitat")
                        userName = "andrew@tryhabitat.com";
                    else if (from.ToLower() == "nooch")
                        userName = "team@nooch.com";
                    else if (from.ToLower() == "appjaxx")
                        userName = "josh@appjaxx.com";


                    Member member = CommonHelper.GetMemberDetailsByUserName(userName);

                    if (member.MemberId != null)
                        memIdToUse = member.MemberId.ToString();
                    else
                    {
                        Logger.Error("Make Payment Page -> submitPayment - submitPayment FAILED - unable to get MemberID based on given username - ['from' param: " + from + "]");
                        res.msg = "Unable to get MemberID based on given username";
                    }

                    if (!String.IsNullOrEmpty(member.AccessToken))
                        accessToken = member.AccessToken;
                    else
                    {
                        string newToken = GenerateAccessToken();
                        CommonHelper.UpdateAccessToken(userName, newToken);
                        accessToken = newToken;
                    }

                    TransactionDto transactionDto = new TransactionDto();
                    transactionDto.MemberId = memIdToUse;
                    transactionDto.RecepientName = name;
                    transactionDto.PinNumber = pin;
                    transactionDto.Amount = Convert.ToDecimal(amount);
                    transactionDto.Memo = memo;
                    transactionDto.cip = cip;

                    var scriptSerializer = new JavaScriptSerializer();

                    serviceMethod = "TransferMoneyToNonNoochUserUsingSynapse?accessToken=" + accessToken + "&inviteType=email&receiverEmailId=" + email;
                    json = scriptSerializer.Serialize(transactionDto);

                    // CC (6/17/16): Transfer service just returns a String Result, different from the Request
                    //               service.  We should eventually update to both use the same class.
                    StringResult sr = ResponseConverter<StringResult>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);
                    response.msg = sr.Result;
                    response.success = sr.Result.Contains("successfully") ? true : false;
                }

                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("Make Payment Page -> submitPayment - URL To Query: [" + urlToUse + "]");

                if (response != null)
                {
                    res = response;

                    Logger.Info("Make Payment Page -> submitPayment RESULT.Success: [" + response.success + "], RESULT.Msg: [" + response.msg + "]");

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        if (response.isEmailAlreadyReg == true)
                        {
                            // CLIFF (5/15/16): shouldn't ever get here since I added the block above to check if the email
                            //                  is already registered (so it shouldn't even call /RequestMoneyForRentScene).
                            Logger.Info("Make Payment Page -> submitPayment Success - Email address already registered to an Existing User - " +
                                        "Name: [" + response.name + "], Email: [" + email + "], Status: [" + response.memberStatus + "], MemberID: [" + response.memberId + "]");
                        }
                        else
                        {
                            Logger.Info("Make Payment Page -> submitPayment Success - Payment Request submitted to NEW user successfully - " +
                                        "Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                        }
                    }
                    else
                    {
                        Logger.Error("Make Payment Page -> submitPayment FAILED - Server response for RequestMoneyForRentScene() was NOT successful - " +
                                     "Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }

                    #endregion Logging For Debugging
                }
                else
                {
                    res.msg = "Unknown server error - Server's response was null.";
                    Logger.Error("Make Payment Page -> submitPayment FAILED - " + res.msg + "]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Make Payment Page -> submitPayment FAILED - email: [" + email + "], Exception: [" + ex.Message + "]");
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
            Logger.Info("Make Payment Page -> submitRequestToExistingUser Fired - From: [" + from + "], isRequest: [" + isRequest +
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

                    serviceMethod = "RequestMoneyToExistingUserForRentScene?from=" + from + "&name=" + name +
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
                        Logger.Error("Make Payment Page -> SubmitRequestToExistingUser FAILED - unable to get MemberID based on given username - ['from' param: " + from + "]");
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

                    serviceMethod = "TransferMoneyUsingSynapse?accessToken=" + accessToken;
                    json = scriptSerializer.Serialize(transactionDto);
                    StringResult sr = ResponseConverter<StringResult>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                    if (sr.Result.Contains("successfully"))
                    {
                        response.success = true;
                        response.msg = sr.Result;
                    }
                }
                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("Make Payment Page -> submitRequestToExistingUser - URL To Query: [" + urlToUse + "], IsRequest: [" + isRequest + "]");

                Logger.Info("Make Payment Page -> submitRequestToExistingUser - Server Response for RequestMoneyToExistingUserForRentScene: " +
                            "Success: [" + response.success + "], Msg: [" + response.msg + "]");

                if (response != null)
                {
                    res = response;

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        Logger.Info("Make Payment Page -> submitRequestToExistingUser Success - Payment" + textLoggerHelper + " submitted successfully - " +
                                    "ServiceMethod: [" + serviceMethod + "], Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }
                    else
                    {
                        Logger.Error("Make Payment Page -> submitRequestToExistingUser FAILED - Server response for ServiceMethod: [" + serviceMethod +
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
                Logger.Error("Make Payment Page -> submitRequestToExistingUser FAILED - Email: [" + email + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during submitRequestToExistingUser.";
            }

            return Json(res);
        }


        /// <summary>
        /// For checking the user's PIN before granting access to the MakePayment page.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public ActionResult checkUsersPin(string user, string pin)
        {
            Logger.Info("Make Payment Page -> checkUsersPin Fired - User: [" + user + "], PIN: [" + pin + "]");

            genericResponse res = new genericResponse();
            res.success = false;
            res.msg = "Initial - code behind";

            try
            {
                res = CommonHelper.CheckUserPin(user, pin);
                Logger.Info("Make Payment Page -> checkUsersPin Result -> Message: [" + res.msg + "]");
            }
            catch (Exception ex)
            {
                Logger.Error("Make Payment Page -> checkUsersPin FAILED - User: [" + user + "], Exception: [" + ex.Message + "]");
                res.msg = ex.Message;
            }

            return Json(res);
        }


        /// <summary>
        /// For the Make Payment page: gets the user's recent list for the Autocomplete name input.
        /// Added by Cliff (6/19/16)
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ActionResult getUserSuggestions(string user)
        {
            Logger.Info("Make Payment Page -> getUserSuggestions Fired - User: [" + user + "]");

            suggestedUsers res = new suggestedUsers();
            res.success = false;
            res.msg = "Initial - code behind";

            try
            {
                var memid = string.Empty;

                if (!String.IsNullOrEmpty(user))
                {
                    if (user.ToLower() == "rentscene")
                        memid = "852987e8-d5fe-47e7-a00b-58a80dd15b49";
                    else if (user == "habitat")
                        memid = "45357cf0-e651-40e7-b825-e1ff48bf44d2";
                    else if (user.ToLower() == "appjaxx" || user.ToLower() == "josh")
                        memid = "8b4b4983-f022-4289-ba6e-48d5affb5484";
                    else if (user == "cliff")
                        memid = "b3a6cf7b-561f-4105-99e4-406a215ccf60";
                }

                if (!String.IsNullOrEmpty(memid))
                {
                    res = CommonHelper.GetSuggestedUsers(memid);
                    Logger.Info("Make Payment Page -> getUserSuggestions SUCCESS! - Message: [" + res.msg + "]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Make Payment Page -> getUserSuggestions FAILED - User: [" + user + "], Exception: [" + ex.Message + "]");
                res.msg = ex.Message;
            }

            return Json(res);
        }


        private string GenerateAccessToken()
        {
            byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            byte[] key = Guid.NewGuid().ToByteArray();
            string token = Convert.ToBase64String(time.Concat(key).ToArray());
            return CommonHelper.GetEncryptedData(token);
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
            string serviceMethod = "GetPayeeDetails?memberTag=" + memberTag;

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
                string serviceMethod = "RegisterNonNoochUserWithSynapse";
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
                Logger.Info("Pay Anyone Page -> Register Synapse User attempt FAILED Failed, Reason: [" + ex.Message + "]. TransId: [" + transId + "].");
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
            Logger.Info("Email Activation Page -> Loaded");

            ResultActivation resultActivation = new ResultActivation();
            resultActivation.success = false;

            string strUserAgent = Request.UserAgent.ToLower();

            if (strUserAgent != null &&
                (Request.Browser.IsMobileDevice ||
                 strUserAgent.Contains("iphone") ||
                 strUserAgent.Contains("mobile") ||
                 strUserAgent.Contains("iOS")))
            {
                resultActivation.openAppText = true;
            }

            string tokenId = Request.QueryString["tokenId"].Trim();
            string type = null;

            if (Request.QueryString.AllKeys.Any(k => k == "type"))
            {
                type = Request.QueryString["type"].Trim();

                if (type == "ll")// For Landlords
                    resultActivation.toLandlordApp = true;
            }

            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "IsMemberActivated?tokenID=" + tokenId.Trim();

            var result = ResponseConverter<BoolResult>.ConvertToCustomEntity(serviceUrl);

            if (!result.Result)
            {
                serviceUrl = Utility.GetValueFromConfig("ServiceUrl") + "MemberActivation?tokenId=" + tokenId.Trim();
                ResponseConverter<BoolResult>.ConvertToCustomEntity(serviceUrl);

                resultActivation.success = true;
                resultActivation.error = false;
            }
            else
                resultActivation.error = true;

            ViewData["OnLoaddata"] = resultActivation;

            return View();
        }



        #region History Page

        /// <summary>
        /// For getting a user's transaction history (Added by Cliff on 5/10/16).
        /// </summary>
        /// <param name="memId"></param>
        /// <param name="rs"></param>
        /// <returns></returns>
        public ActionResult history(string memId, string rs, string user)
        {
            TransactionsPageData res = new TransactionsPageData();
            res.isSuccess = false;

            if (String.IsNullOrEmpty(memId) && String.IsNullOrEmpty(user))
                res.msg = "No MemberId found in query URL.";
            else
            {
                if (!String.IsNullOrEmpty(user))
                {
                    if (user.ToLower() == "rentscene")
                        memId = "852987e8-d5fe-47e7-a00b-58a80dd15b49";
                    else if (user == "habitat")
                        memId = "45357cf0-e651-40e7-b825-e1ff48bf44d2";
                    else if (user.ToLower() == "appjaxx")
                        memId = "8b4b4983-f022-4289-ba6e-48d5affb5484";
                    else if (user == "cliff")
                        memId = "b3a6cf7b-561f-4105-99e4-406a215ccf60";
                }

                res.memId = memId;

                Logger.Info("History Page -> Loaded - MemberID: [" + memId + "]");

                List<TransactionClass> Transactions = new List<TransactionClass>();

                try
                {
                    string listType = "ALL";

                    int totalRecordsCount = 0;

                    TransactionsDataAccess tda = new TransactionsDataAccess();
                    var transactionList = tda.GetTransactionsList(memId, listType, 100, 1, "", out totalRecordsCount);

                    if (transactionList != null && transactionList.Count > 0)
                    {
                        foreach (var trans in transactionList)
                        {
                            #region Foreach inside

                            try
                            {
                                if (trans.Memo.ToLower().IndexOf("test") == 0)
                                {
                                    // Exclude transactions where the Memo begins with "test"
                                }
                                else
                                {
                                    TransactionClass obj = new TransactionClass();

                                    obj.TransactionId = trans.TransactionId;
                                    obj.TransactionTrackingId = trans.TransactionTrackingId;
                                    obj.TransactionType = CommonHelper.GetDecryptedData(trans.TransactionType);

                                    obj.TransactionDate1 = Convert.ToDateTime(trans.TransactionDate).ToShortDateString();
                                    obj.Amount = Math.Round(trans.Amount, 2);
                                    obj.Memo = trans.Memo;

                                    obj.city = (trans.GeoLocation != null && trans.GeoLocation.City != null) ? trans.GeoLocation.City : string.Empty;
                                    obj.state = (trans.GeoLocation != null && trans.GeoLocation.State != null) ? trans.GeoLocation.State : string.Empty;
                                    obj.TransLati = (trans.GeoLocation != null && trans.GeoLocation.Latitude != null) ? (float)trans.GeoLocation.Latitude : default(float);
                                    obj.TransLongi = (trans.GeoLocation != null && trans.GeoLocation.Longitude != null) ? (float)trans.GeoLocation.Longitude : default(float);

                                    if (obj.TransactionType == "Request" && trans.TransactionStatus == "Success")
                                        obj.TransactionStatus = "Paid";
                                    else
                                        obj.TransactionStatus = trans.TransactionStatus;

                                    if (trans.DateAccepted != null)
                                        obj.DateAccepted = Convert.ToDateTime(trans.DateAccepted).ToShortDateString();

                                    if (!String.IsNullOrEmpty(trans.SynapseStatus))
                                    {
                                        obj.SynapseStatus = trans.SynapseStatus.ToString();

                                        var transIdString = trans.TransactionId.ToString().ToLower();

                                        var synStatus = CommonHelper.GetTransSynapseStatusNote(transIdString);
                                        obj.SynapseStatusNote = synStatus != "Failure" ? synStatus : "";
                                    }

                                    #region Set Correct Sender/Recipient Info

                                    if (obj.TransactionType == "Transfer" || obj.TransactionType == "Disputed" ||
                                        obj.TransactionType == "Reward" || obj.TransactionType == "Invite" ||
                                        obj.TransactionType == "Rent" || obj.TransactionType == "Request")
                                    {
                                        if (String.IsNullOrEmpty(trans.InvitationSentTo) && trans.IsPhoneInvitation != true)
                                        {
                                            // Payment Request or Transfer to an EXISTING Nooch user... straight forward.
                                            obj.SenderId = trans.SenderId;
                                            obj.SenderName = CommonHelper.GetDecryptedData(trans.Member.FirstName) + " " + CommonHelper.GetDecryptedData(trans.Member.LastName);
                                            obj.SenderNoochId = trans.Member.Nooch_ID;

                                            obj.RecipientId = trans.RecipientId;
                                            obj.RecipientName = CommonHelper.GetDecryptedData(trans.Member1.FirstName) + " " + CommonHelper.GetDecryptedData(trans.Member1.LastName);
                                            obj.RecepientNoochId = trans.Member1.Nooch_ID;
                                        }
                                        else // REQUEST OR INVITE TO NON-NOOCH USER
                                        {
                                            var existingMembersName = CommonHelper.GetDecryptedData(trans.Member.FirstName) + " " +
                                                                      CommonHelper.GetDecryptedData(trans.Member.LastName);

                                            // Request/Invite via Email
                                            if (trans.TransactionStatus == "Success")
                                            {
                                                #region New User Has Accepted & Has A Nooch Account

                                                Member newMember = new Member();

                                                if (!String.IsNullOrEmpty(trans.InvitationSentTo)) // Payment was Accepted, so the invited member must have created a Nooch account.
                                                    newMember = CommonHelper.GetMemberDetailsByUserName(CommonHelper.GetDecryptedData(trans.InvitationSentTo));
                                                else if (!String.IsNullOrEmpty(trans.PhoneNumberInvited))
                                                    newMember = CommonHelper.GetMemberByContactNumber(CommonHelper.GetDecryptedData(trans.PhoneNumberInvited));

                                                if (newMember != null)
                                                {
                                                    var invitedMembersName = CommonHelper.GetDecryptedData(newMember.FirstName) + " " +
                                                                             CommonHelper.GetDecryptedData(newMember.LastName);

                                                    if (obj.TransactionType == "Request")
                                                    {
                                                        obj.SenderName = invitedMembersName;
                                                        obj.SenderId = newMember.MemberId;
                                                        obj.SenderNoochId = newMember.Nooch_ID;

                                                        obj.RecipientName = existingMembersName;
                                                        obj.RecipientId = trans.RecipientId;
                                                        obj.RecepientNoochId = trans.Member.Nooch_ID;
                                                    }
                                                    else
                                                    {
                                                        obj.SenderName = existingMembersName;
                                                        obj.SenderId = trans.SenderId;
                                                        obj.SenderNoochId = trans.Member.Nooch_ID;

                                                        obj.RecipientName = invitedMembersName;
                                                        obj.RecipientId = newMember.MemberId;
                                                        obj.RecepientNoochId = newMember.Nooch_ID;
                                                    }
                                                }

                                                #endregion New User Has Accepted & Has A Nooch Account
                                            }
                                            else
                                            {
                                                // Payment was not (yet) Accepted, so the invited member does NOT have a Nooch account,
                                                // so use the invited email / phone

                                                obj.SenderName = existingMembersName;
                                                obj.SenderId = trans.SenderId;
                                                obj.SenderNoochId = trans.Member.Nooch_ID;

                                                obj.RecipientName = !String.IsNullOrEmpty(trans.PhoneNumberInvited)
                                                                    ? CommonHelper.FormatPhoneNumber(CommonHelper.GetDecryptedData(trans.PhoneNumberInvited))
                                                                    : CommonHelper.GetDecryptedData(trans.InvitationSentTo);
                                                obj.RecipientId = null;
                                                obj.RecepientNoochId = null;
                                            }
                                        }
                                    }

                                    #endregion Set Correct Sender/Recipient Info

                                    Transactions.Add(obj);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("History Page -> ERROR - Inner Exception during loop through all transactions - " +
                                             "MemberID: [" + memId + "], TransID: [" + trans.TransactionId +
                                             "], Amount: [" + trans.Amount.ToString("n2") + "], Exception: [" + ex.Message + "]");
                                continue;
                            }

                            #endregion Foreach inside
                        }
                    }

                    res.allTransactionsData = Transactions;
                }
                catch (Exception ex)
                {
                    Logger.Error("History Page -> OUTER EXCEPTION - MemberID: [" + memId + "], Exception: [" + ex.Message + "]");
                    res.msg = "Server Error.";
                }

                // Now get the user's Name
                var memberObj = CommonHelper.GetMemberDetails(memId);
                if (memberObj != null && !String.IsNullOrEmpty(memberObj.FirstName))
                {
                    res.usersName = user == "rentscene" ? "Rent Scene" : CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.FirstName));
                    res.usersPhoto = !String.IsNullOrEmpty(memberObj.Photo) ? memberObj.Photo : "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";

                    if (user != "rentscene" && !String.IsNullOrEmpty(memberObj.LastName))
                    {
                        res.usersName += " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(memberObj.LastName));
                    }

                    res.usersEmail = CommonHelper.GetDecryptedData(memberObj.UserName);
                }
            }

            return View(res);
        }


        [HttpPost]
        [ActionName("cancelPayment")]
        public ActionResult cancelPayment(ResultCancelRequest input)
        {
            Logger.Info("History Page -> cancelPayment Fired - TransID: [" + input.TransId +
                        "], UserType: [" + input.UserType + "]");

            ResultCancelRequest res = new ResultCancelRequest();
            res.success = false;

            try
            {
                var userType = input.UserType == "new" ? "U6De3haw2r4mSgweNpdgXQ==" : "mx5bTcAYyiOf9I5Py9TiLw==";
                res = CancelMoneyRequest(input.TransId, input.memberId, userType);
            }
            catch (Exception ex)
            {
                Logger.Error("History Page -> cancelPayment FAILED - TransID: [" + input.TransId + "], Exception: [" + ex.Message + "]");
            }

            return Json(res);
        }

        [HttpPost]
        [ActionName("paymentReminder")]

        public ActionResult paymentReminder(ResultCancelRequest input)
        {
            Logger.Info("History Page -> paymentReminder Fired - TransID: [" + input.TransId +
                        "], UserType: [" + input.UserType + "]");

            ResultCancelRequest res = new ResultCancelRequest();
            res.success = false;

            #region Inititial Data Checks

            if (String.IsNullOrEmpty(input.TransId))
            {
                res.resultMsg = "Missing TransactionId";
                return Json(res);
            }
            if (String.IsNullOrEmpty(input.memberId))
            {
                res.resultMsg = "Missing MemberId";
                return Json(res);
            }
            if (String.IsNullOrEmpty(input.UserType))
            {
                res.resultMsg = "Missing ReminderType";
                return Json(res);
            }

            #endregion Inititial Data Checks

            try
            {
                var MemDeatils = CommonHelper.GetMemberDetails(input.memberId);

                if (MemDeatils != null)
                {
                    //var userType = input.UserType == "new" ? "U6De3haw2r4mSgweNpdgXQ==" : "mx5bTcAYyiOf9I5Py9TiLw=="; new & Existing
                    var reminderType = input.UserType == "new" ? "InvitationReminderToNewUser" : "RequestMoneyReminderToExistingUser";

                    string serviceMethod = "SendTransactionReminderEmail?ReminderType=" + reminderType + "&TransactionId=" + input.TransId + "&accessToken=" + MemDeatils.AccessToken + "&MemberId=" + input.memberId;
                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");


                    Logger.Info("URL made for transaction reminder is " + String.Concat(serviceUrl, serviceMethod));

                    var serviceResult = ResponseConverter<StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    //Logger.Info(Json(serviceResult));

                    if (serviceResult.Result == "Reminder mail sent successfully." || serviceResult.Result == "Reminder sms sent successfully.")
                    {
                        Logger.Info("History Page -> paymentReminder - Reminder sent Successfully - TransID: [" + input.TransId + "], MemberID: [" + input.memberId + "]");

                        res.showPaymentInfo = true;
                        res.success = true;
                        res.resultMsg = "Transaction reminder sent successfully.";
                    }
                    else
                    {
                        Logger.Error("History Page -> paymentReminder - paymentReminderService FAILED - TransID: [" + input.TransId + "], MemberID: [" + input.memberId + "], ReminderType: [" + reminderType + "]");
                        res.resultMsg = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
                    }
                }
                else
                {
                    res.resultMsg = "Member not found";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("History Page -> paymentReminder FAILED - TransID: [" + input.TransId + "], Exception: [" + ex.Message + "]");
                res.resultMsg = ex.Message;
            }

            return Json(res);
        }

        #endregion History Page



        #region Micro-Deposit Verification Page

        // MemberId here is plane memberId =- non encrypted
        // NodeId is Id from SynapseBanksOfMembers table - non encrypted
        public ActionResult MicroDepositsVerification(string mid, string NodeId, bool? IsRs)
        {
            Logger.Info("MicroDepositsVerification Page -> Page Loaded - MemberID: [" + mid +
                        "], NodeID: [" + NodeId + "], IsRentScene: [" + IsRs + "]");

            SynapseV3VerifyNodeWithMicroDeposits_ServiceInput MicroDeposit = new SynapseV3VerifyNodeWithMicroDeposits_ServiceInput();
            MicroDeposit.errorMsg = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(mid))
                {
                    Logger.Error("MicroDepositsVerification Page -> Page_load - MemberID is: [" + mid + "]");
                    MicroDeposit.errorMsg = "Missing MemberID";
                }

                if (String.IsNullOrEmpty(MicroDeposit.errorMsg))
                {
                    // Get Bank Info from server
                    MicroDeposit = GetBankDetailsForMicroDepositVerification(mid.Trim());
                }

                if (IsRs == true) // if this flag is in the URL, then force RS branding, regardless of server response
                {
                    MicroDeposit.IsRs = "true";
                    Logger.Info("MicroDepositsVerification Page -> Page_load - RENT SCENE USER Detected");
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                Logger.Error("MicroDepositsVerification Page -> page_load OUTER EXCEPTION - Exception: [" + ex.Message + "]");
            }

            ViewData["OnLoadData"] = MicroDeposit;
            return View();
        }


        public SynapseV3VerifyNodeWithMicroDeposits_ServiceInput GetBankDetailsForMicroDepositVerification(string memberId)
        {
            Logger.Info("MicroDepositsVerification Page -> GetBankDetailsForMicroDepositVerification Fired - MemberID: [" + memberId + "]");

            SynapseV3VerifyNodeWithMicroDeposits_ServiceInput rpr = new SynapseV3VerifyNodeWithMicroDeposits_ServiceInput();
            rpr.success = false;

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "GetMemberInfoForMicroDepositPage?memberId=" + memberId;

                SynapseV3VerifyNodeWithMicroDeposits_ServiceInput details = ResponseConverter<SynapseV3VerifyNodeWithMicroDeposits_ServiceInput>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (details == null)
                {
                    Logger.Error("MicroDepositsVerification Page -> GetTransDetails FAILED - Transaction Not Found - TransId: [" + memberId + "]");
                    rpr.errorMsg = "Unable to find bank record";
                }
                else
                    rpr = details;
            }
            catch (Exception ex)
            {
                Logger.Error("MicroDepositsVerification Page -> GetTransDetails Failed - TransacationID: [" + memberId +
                             "], Exception: [" + ex + "]");
                rpr.errorMsg = "Exception: [" + ex.Message + "]";
            }

            return rpr;
        }


        /// <summary>
        /// For verifying bank MFA microdeposits - used only for a bank added with routing and account numbers.
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="memberid"></param>
        /// <param name="MicroDepositOne"></param>
        /// <param name="MicroDepositTwo"></param>
        /// <param name="NodeId1"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("MFALoginWithRoutingAndAccountNumber")]
        public ActionResult MFALoginWithRoutingAndAccountNumber(string bank, string memberid, string MicroDepositOne, string MicroDepositTwo, string NodeId1)
        {
            SynapseV3BankLoginResult_ServiceRes res = new SynapseV3BankLoginResult_ServiceRes();
            res.Is_success = false;

            try
            {
                Logger.Info("NoochController -> MFALoginWithRoutingAndAccountNumber Initiated -> MemberID: [" + memberid + "], NodeID: [" + NodeId1 + "], Bank: [" + bank + "]");

                SynapseV3VerifyNodeWithMicroDeposits_ServiceInput inpu = new SynapseV3VerifyNodeWithMicroDeposits_ServiceInput();
                inpu.bankName = bank; // not required..keeping it for just in case we need something to do with it.
                inpu.MemberId = memberid;
                inpu.microDespositOne = MicroDepositOne;
                inpu.microDespositTwo = MicroDepositTwo;
                inpu.bankId = NodeId1; // should be the encrypted OID of the bank

                // preparing data for POST type request
                var scriptSerializer = new JavaScriptSerializer();
                string json = scriptSerializer.Serialize(inpu);

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "SynapseV3MFABankVerifyWithMicroDeposits";

                res = ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);

                if (res.Is_success != true)
                {
                    Logger.Error("NoochController -> MFALoginWithRoutingAndAccountNumber FAILED -> Error Msg: [" + res.errorMsg +
                                 "], MemberID: [" + memberid + "], Bank: [" + bank + "]");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("NoochController -> MFALoginWithRoutingAndAccountNumber FAILED - MemberID: [" + memberid +
                             "], Exception: [" + ex + "]");
                res.errorMsg = ex.Message;
            }

            return Json(res);
        }

        #endregion Micro-Deposit Verification Page



        public ActionResult EncryptDecrypt()
        {
            return View();
        }


        [HttpPost]
        public ActionResult EncryptDecryptData(EncDecInput input)
        {
            EncDecInput res = new EncDecInput();
            try
            {
                if (!String.IsNullOrEmpty(input.DataToWorkOn))
                {
                    if (input.OpType == "D")
                    {
                        res.DataToWorkOn = CommonHelper.GetDecryptedData(input.DataToWorkOn);
                    }
                    if (input.OpType == "E")
                    {
                        res.DataToWorkOn = CommonHelper.GetEncryptedData(input.DataToWorkOn);
                    }
                }
            }
            catch (Exception)
            {
                res.DataToWorkOn = "";
            }

            return Json(res);
        }

    }


    public class EncDecInput
    {
        public string OpType { get; set; }
        public string DataToWorkOn { get; set; }
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