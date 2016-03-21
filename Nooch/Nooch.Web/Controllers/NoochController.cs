using System;
using System.Collections.Generic;
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
using Nooch.Common.Entities.LandingPagesRelatedEntities.RejectMoney;
using Nooch.Common.Entities.MobileAppOutputEnities;



namespace Nooch.Web.Controllers
{
    public class NoochController : Controller
    {
        // GET: Nooch
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AddBank(string MemberId)
        {
            if (!String.IsNullOrEmpty(MemberId))
                return View();
            return RedirectToAction("Index");
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
                // something wrong with Query string :'(

                rcr.reslt1 = "false";
                rcr.reslt = "This looks like an invalid transaction - sorry about that!  Please try again or contact Nooch support for more information.";
                rcr.paymentInfo = "false";
                //reslt1.Visible = false;
                //reslt.Text = "This looks like an invalid transaction - sorry about that!  Please try again or contact Nooch support for more information.";

                //paymentInfo.Visible = false;

            }

            ViewData["OnLoaddata"] = rcr;
            return View();

        }


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
                // senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                //nameLabel.Text = transaction.PhoneNumberInvited;
                rcr.nameLabel = transaction.PhoneNumberInvited;
            }
            else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            {
                //senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                //nameLabel.Text = transaction.InvitationSentTo;
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";

                rcr.nameLabel = transaction.InvitationSentTo;
            }
            else
            {
                //nameLabel.Text = transaction.Name;
                //senderImage.ImageUrl = transaction.SenderPhoto;
                rcr.senderImage = transaction.SenderPhoto;
                rcr.nameLabel = transaction.Name;
            }

            //AmountLabel.Text = transaction.Amount.ToString("n2");
            if (transaction.Amount != null)
                rcr.AmountLabel = transaction.Amount.ToString("n2");


            // Reject money page related stuff

            rcr.RecepientName = transaction.RecepientName;
            rcr.senderImage = transaction.RecepientPhoto;



            if (!String.IsNullOrEmpty(transaction.TransactionType))
                rcr.TransType = transaction.TransactionType;

            if (!String.IsNullOrEmpty(transaction.TransactionId))
                rcr.TransId = transaction.TransactionId;


            return rcr;
        }
        public ResultCancelRequest GetTransDetails(string TransactionId)
        {
            ResultCancelRequest rcr = new ResultCancelRequest();
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction != null)
            {
                CancelMoneyRequest(Request.QueryString["TransactionId"], Request.QueryString["MemberId"], Request.QueryString["UserType"]);
                //  CancelMoneyRequest(TransactionId, MemberId, UserType);
            }

            if (transaction.TransactionStatus != "Pending")
            {
                rcr.reslt1 = "false";
                rcr.reslt = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
                rcr.paymentInfo = "true";

                //reslt1.Visible = false;
                //paymentInfo.Visible = true;
                //reslt.Text = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
                //reslt.Visible = true;
            }


            if (transaction.IsPhoneInvitation && transaction.PhoneNumberInvited.Length > 0)
            {
                // senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                //nameLabel.Text = transaction.PhoneNumberInvited;
                rcr.nameLabel = transaction.PhoneNumberInvited;
            }
            else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            {
                //senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                //nameLabel.Text = transaction.InvitationSentTo;
                rcr.senderImage = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";

                rcr.nameLabel = transaction.InvitationSentTo;
            }
            else
            {
                //nameLabel.Text = transaction.Name;
                //senderImage.ImageUrl = transaction.SenderPhoto;
                rcr.senderImage = transaction.SenderPhoto;
                rcr.nameLabel = transaction.Name;
            }

            //AmountLabel.Text = transaction.Amount.ToString("n2");
            rcr.AmountLabel = transaction.Amount.ToString("n2");


            // Reject money page related stuff
            if (!String.IsNullOrEmpty(transaction.TransactionType))
                rcr.TransType = transaction.TransactionType;

            if (!String.IsNullOrEmpty(transaction.TransactionId))
                rcr.TransId = transaction.TransactionId;


            return rcr;
        }


        protected void CancelMoneyRequest(string TransactionId, string MemberId, string userType)
        {
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

            if (userType == "mx5bTcAYyiOf9I5Py9TiLw==")
            {
                // Calling service to cancel a REQUEST to an EXISTING Nooch user
                serviceMethod = "/CancelMoneyRequestForExistingNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;

                var serviceResult = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (serviceResult.Result == "Transaction Cancelled Successfully.")
                {
                    //reslt1.Visible = true;
                    //reslt.Text = "No problem, we change our minds sometimes too.  This request is cancelled.  But you can always send another request...";
                }
                else
                {
                    //reslt1.Visible = false;
                    //reslt.Text = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
                }
            }


            if (userType == "U6De3haw2r4mSgweNpdgXQ==")
            {
                // Calling service to cancel a REQUEST to NON-NOOCH user

                serviceMethod = "/CancelMoneyRequestForNonNoochUser?TransactionId=" + TransactionId + "&MemberId=" + MemberId;

                var serviceResult = ResponseConverter<Nooch.Common.Entities.StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (serviceResult.Result == "Transaction Cancelled Successfully")
                {
                    //reslt1.Visible = true;
                    //reslt.Text = "No problem, we change our minds sometimes too.  This request is cancelled, finito, extinct, gone, not coming back... unless you send another request!";
                }
                else
                {
                    //reslt1.Visible = false;
                    //reslt.Text = "This transaction is no longer pending. Either you already responded by accepting or rejecting, or it was canceled.";
                }
            }
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
            Logger.Info("**Add_Bank** CodeBehind -> RegisterUserWithSynapse Initiated - [MemberID: " + memberid + "]");

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
                    res.Message = transaction.errorMsg;
                }
                if (transaction.success == true)
                {
                    res.IsSuccess = true;
                    res.Message = "OK";
                }

                res.ssn_verify_status = transaction.ssn_verify_status;
            }
            catch (Exception we)
            {
                res.Message = "RegisterUser Web Exception - local";
                Logger.Error("**Add_Bank** CodeBehind -> RegisterUserWithSynapse FAILED - [MemberID: " + memberid +
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
                BankLoginResult accountCreateResult = RegisterUserWithSynapse(inp.memberid);
                res.ssn_verify_status = accountCreateResult.ssn_verify_status; // Will be overwritten if Bank Login is successful below

                if (accountCreateResult.IsSuccess == true)
                {
                    Logger.Info("**Add_Bank** CodeBehind -> BankLogin -> Synapse account created successfully! [MemberID: " + inp.memberid +
                                           "], [SSN Status: " + accountCreateResult.ssn_verify_status + "]");

                    // 2. Now call the bank login service.
                    //    Response could be: 1.) array[] of banks,  2.) Question-based MFA,  3.) Code-based MFA, or  4.) Failure/Error

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    //string serviceMethod = "/SynapseBankLoginRequest?BankName=" + inp.bankname + "&MemberId=" + inp.memberid + "&IsPinRequired=" + inp.IsPinRequired
                    //    + "&UserName=" + inp.username + "&Password=" + inp.password + "&PinNumber=" + inp.PinNumber;
                    string serviceMethod = "/SynapseV3AddNode?MemberId=" + inp.memberid + "&BnkName=" + inp.bankname + "&BnkUserName=" + inp.username
                                           + "&BnkPw=" + inp.password;

                    SynapseV3BankLoginResult_ServiceRes bankLoginResult =
                        ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    if (bankLoginResult.Is_success == true)
                    {
                        res.Is_success = true;
                        res.Is_MFA = bankLoginResult.Is_MFA;

                        if (bankLoginResult.Is_MFA)
                        {
                            res.Bank_Access_Token = bankLoginResult.bankMFA;
                            res.MFA_Type = "questions";    // no more code based as per synapse V3 docs
                        }
                        List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();
                        if (bankLoginResult.SynapseNodesList.nodes[0] != null)
                        {
                            foreach (SynapseIndividualNodeClass bankNode in bankLoginResult.SynapseNodesList.nodes)
                            {
                                SynapseBankClass sbc = new SynapseBankClass();
                                sbc.account_class = bankNode.account_class;
                                sbc.account_number_string = bankNode.account_num;
                                sbc.account_type = bankNode.account_type.ToString();
                                sbc.address = "";
                                //sbc.balance = bankNode.info.balance.amount;
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
                        Logger.Error("**Add_Bank** CodeBehind -> BankLogin FAILED -> [MemberID: " + inp.memberid + "], [Error Msg: " + bankLoginResult.errorMsg + "]");
                        res.ERROR_MSG = bankLoginResult.errorMsg;
                    }
                }
                else
                {
                    Logger.Error("**Add_Bank** CodeBehind -> BankLogin -> Register Synapse User FAILED -> [MemberID: " + inp.memberid + "], [Error Msg: " + accountCreateResult.Message + "]");
                    res.ERROR_MSG = accountCreateResult.Message;
                }

                // Check if Register method and Bank Login method both got same result for ssn_verify_status
                // ... not sure which one to prioritize yet in the case of a discrepency, but going with BankLogin one for now.
                if (res.ssn_verify_status != accountCreateResult.ssn_verify_status)
                {
                    Logger.Info("**Add_Bank** CodeBehind -> BankLogin -> ssn_verify_status from Registering User was [" +
                                            accountCreateResult.ssn_verify_status + "], but ssn_verify_status from BankLogin was: [" +
                                            res.ssn_verify_status + "]");
                }

            }
            catch (Exception we)
            {
                Logger.Error("**Add_Bank** CodeBehind -> BankLogin FAILED - [MemberID: " + inp.memberid +
                                   "], [Exception: " + we.InnerException + "]");

                res.ERROR_MSG = "error occured at server.";
            }

            return Json(res);
        }


        public static SynapseBankLoginRequestResult addBank(bankaddInputFormClass inp)
        {
            Logger.Info("**Add_Bank** CodeBehind -> addBank (for manual routing/account #) Initiated - [MemberID: " + inp.memberid + "]");

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
                    if (bankAddRes.Is_MFA)
                    {
                        res.MFA_Type = "question";    // no more code based as per synapse V3 docs
                    }
                    List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();

                    foreach (nodes bankNode in bankAddRes.SynapseNodesList.nodes)
                    {
                        SynapseBankClass sbc = new SynapseBankClass();
                        sbc.account_class = bankNode.info._class;
                        sbc.account_number_string = bankNode.info.account_num;
                        sbc.account_type = bankNode.type;
                        sbc.address = "";
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
                    //res.SynapseCodeBasedResponse = bankAddRes.SynapseCodeBasedResponse;
                    //res.SynapseQuestionBasedResponse = bankAddRes.SynapseQuestionBasedResponse;
                    res.ERROR_MSG = "OK";
                }
                else
                {
                    res.ERROR_MSG = bankAddRes.errorMsg;
                }
                //res.ssn_verify_status = bankAddRes.;

                return res;
            }
            catch (Exception we)
            {
                Logger.Error("**Add_Bank** CodeBehind -> addBank FAILED - [MemberID: " + inp.memberid +
                                   "], [Exception: " + we.InnerException + "]");

                res.ERROR_MSG = "Error did occur at server. Ohh nooo!!";
                return res;
            }
        }



        // method to call verify bank mfa - to be used with bank login type MFA's
        [HttpPost]
        [ActionName("MFALogin")]
        public ActionResult MFALogin(MFALoginInputClassForm inp)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();

            try
            {
                Logger.Info("**Add_Bank** CodeBehind -> MFALogin Initiated -> [MemberID: " + inp.memberid + "], [Bank: " + inp.bank + "], [MFA: " + inp.MFA + "]");

                // preparing data for POST type request

                var scriptSerializer = new JavaScriptSerializer();
                string json;

                SynapseV3VerifyNode_ServiceInput inpu = new SynapseV3VerifyNode_ServiceInput();
                inpu.BankName = inp.bank; // not required..keeping it for just in case we need something to do with it.
                inpu.MemberId = inp.memberid;
                inpu.mfaResponse = inp.MFA;
                inpu.bankId = inp.ba;   // this is bank_node_id..... must...need to pass this thing in earlier step
                try
                {
                    //json = "{\"input\":" + scriptSerializer.Serialize(inpu) + "}";
                    json = scriptSerializer.Serialize(inpu);

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "/SynapseV3MFABankVerify";
                    SynapseV3BankLoginResult_ServiceRes bnkloginresult = ResponseConverter<SynapseV3BankLoginResult_ServiceRes>.CallServicePostMethod(String.Concat(serviceUrl, serviceMethod), json);



                    if (bnkloginresult.Is_success == true)
                    {
                        res.Is_success = true;
                        res.Is_MFA = bnkloginresult.Is_MFA;
                        //res.MFA_Type = bnkloginresult.;
                        List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();
                        foreach (SynapseIndividualNodeClass bankNode in bnkloginresult.SynapseNodesList.nodes)
                        {
                            SynapseBankClass sbc = new SynapseBankClass();
                            sbc.account_class = bankNode.account_class;
                            sbc.account_number_string = bankNode.account_num;
                            sbc.account_type = bankNode.account_type.ToString();
                            sbc.address = "";

                            //sbc.balance = bankNode.;
                            sbc.bank_name = bankNode.bank_name;
                            sbc.bankoid = bankNode.oid;                            // using this same id to set bank account as default
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

                        if (res.Is_MFA)
                        {
                            SynapseQuestionBasedMFAClass qbr = new SynapseQuestionBasedMFAClass();
                            qbr.is_mfa = true;
                            qbr.response = new SynapseQuestionBasedMFAResponseIntClass()
                            {
                                type = "questions",
                                access_token = "",
                                mfa = new SynapseQuestionClass[1]
                            };
                            qbr.response.mfa[0].question = bnkloginresult.mfaMessage;
                            res.SynapseQuestionBasedResponse = qbr;

                        }


                        res.ERROR_MSG = "OK";
                    }
                    else
                    {
                        Logger.Error("**Add_Bank** CodeBehind -> MFALogin FAILED -> [Error Msg: " + bnkloginresult.errorMsg +
                                               "], [MemberID: " + inp.memberid + "], [Bank: " + inp.bank + "], [MFA: " + inp.MFA + "]");
                        res.Is_success = false;
                        res.ERROR_MSG = bnkloginresult.errorMsg;
                    }
                }
                catch (Exception ec)
                {
                    res.Is_success = false;
                    res.ERROR_MSG = "";
                    Logger.Error("**Add_Bank** CodeBehind -> MFALogin FAILED - [MemberID: " + inp.memberid +
                                  "], [Exception: " + ec + "]");
                }


            }
            catch (Exception we)
            {
                Logger.Error("**Add_Bank** CodeBehind -> MFALogin FAILED - [MemberID: " + inp.memberid +
                                   "], [Exception: " + we + "]");
            }

            return Json(res);
        }



        // method to call verify bank mfa - to be used with routing and account number login
        [HttpPost]
        [ActionName("MFALoginWithRoutingAndAccountNumber")]
        public SynapseBankLoginRequestResult MFALoginWithRoutingAndAccountNumber(string bank, string memberid, string MicroDepositOne, string MicroDepositTwo, string ba)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();

            try
            {
                Logger.Info("**Add_Bank** CodeBehind -> MFALoginWithRoutingAndAccountNumber Initiated -> [MemberID: " + memberid + "], [Bank: " + bank + "]");

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
                        res.MFA_Type = bnkloginresult.MFA_Type;
                        res.SynapseBanksList = bnkloginresult.SynapseBanksList;
                        res.SynapseCodeBasedResponse = bnkloginresult.SynapseCodeBasedResponse;
                        res.SynapseQuestionBasedResponse = bnkloginresult.SynapseQuestionBasedResponse;
                        res.ERROR_MSG = "OK";
                    }
                    else
                    {
                        Logger.Error("**Add_Bank** CodeBehind -> MFALoginWithRoutingAndAccountNumber FAILED -> [Error Msg: " + bnkloginresult.ERROR_MSG +
                                               "], [MemberID: " + memberid + "], [Bank: " + bank + "]");
                        res.Is_success = false;
                        res.ERROR_MSG = bnkloginresult.ERROR_MSG;
                    }
                }
                catch (Exception ec)
                {
                    res.Is_success = false;
                    res.ERROR_MSG = "";
                    Logger.Error("**Add_Bank** CodeBehind -> MFALoginWithRoutingAndAccountNumber FAILED - [MemberID: " + memberid +
                                  "], [Exception: " + ec + "]");
                }


            }
            catch (Exception we)
            {
                Logger.Error("**Add_Bank** CodeBehind -> MFALoginWithRoutingAndAccountNumber FAILED - [MemberID: " + memberid +
                                   "], [Exception: " + we + "]");
            }

            return res;
        }


        public ActionResult PayRequest()
        {
            ResultPayRequest rpr = new ResultPayRequest();
            Logger.Info("payRequest CodeBehind -> Page_load Initiated - [TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");

            try
            {


                if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]))
                {
                    if (!String.IsNullOrEmpty(Request.QueryString["UserType"]))
                    {
                        string n = Request.QueryString["UserType"].ToString();
                        rpr.usrTyp = CommonHelper.GetDecryptedData(n);

                        if (rpr.usrTyp == "NonRegistered" ||
                           rpr.usrTyp == "Existing")
                        {
                            Logger.Info("payRequest CodeBehind -> Page_load - UserType is: [" + rpr.usrTyp + "]");
                        }
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

        public  ActionResult RegisterUserWithSynp(string transId, string memberId, string userEm, string userPh, string userName, string userPw, string ssn, string dob, string address, string zip, string fngprnt, string ip)
        {
            Logger.Info("payRequest Code Behind -> RegisterNonNoochUserWithSynapse Initiated");

            RegisterUserSynapseResultClassExt res = new RegisterUserSynapseResultClassExt();
            res.success = "false";
            res.memberIdGenerated = "";
            res.reason = "Unknown";

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                userPh = CommonHelper.RemovePhoneNumberFormatting(userPh);

                Logger.Info("payRequest Code Behind -> RegisterUserWithSynp -> PARAMETERS: transId: " + transId +
                                       ", memberId (If existing user): " + memberId + ", userEm: " + userEm +
                                       ", userPh: " + userPh + ", userPw: " + userPw +
                                       ", ssn: " + ssn + ", dob: " + dob +
                                       ", address: " + address + ", zip: " + zip);

                string serviceMethod = "";

                if (!String.IsNullOrEmpty(memberId) && memberId.Length > 30)
                {
                    // Member must already exist, so use RegisterEXISTINGUserWithSynapseV3()
                    serviceMethod = "/RegisterExistingUserWithSynapseV3?transId=" + transId +
                                    "&memberId=" + memberId +
                                    "&email=" + userEm +
                                    "&phone=" + userPh +
                                    "&fullname=" + userName +
                                    "&pw=" + userPw +
                                    "&ssn=" + ssn +
                                    "&dob=" + dob +
                                    "&address=" + address +
                                    "&zip=" + zip +
                                    "&fngprnt=" + fngprnt + "&ip=" + ip;
                }
                else
                {
                    // Member DOES NOT already exist, so use RegisterNONNOOCHUserWithSynapse()
                    serviceMethod = "/RegisterNonNoochUserWithSynapse?transId=" + transId +
                                    "&email=" + userEm +
                                    "&phone=" + userPh +
                                    "&fullname=" + userName +
                                    "&pw=" + userPw +
                                    "&ssn=" + ssn +
                                    "&dob=" + dob +
                                    "&address=" + address +
                                    "&zip=" + zip +
                                    "&fngprnt=" + fngprnt +
                                    "&ip=" + ip;
                }

                Logger.Info("PayRequest Code-Behind -> RegisterUserWithSynp - Full Query String: [ " + String.Concat(serviceUrl, serviceMethod) + " ]");

                RegisterUserSynapseResultClassExt regUserResponse = ResponseConverter<RegisterUserSynapseResultClassExt>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                if (regUserResponse.success == "True")
                {
                    res.success = "true";
                    res.reason = "OK";
                    res.memberIdGenerated = regUserResponse.memberIdGenerated;
                }
                else if (regUserResponse.success == "False")
                {
                    Logger.Error("PayRequest Code-Behind -> RegisterUserWithSynp FAILED - SERVER RETURNED 'success' = 'false' - [TransID: " + transId + "]");
                    res.reason = regUserResponse.reason;
                }
                else
                {
                    Logger.Error("PayRequest Code-Behind -> RegisterUserWithSynp FAILED - UNKNOWN ERROR FROM SERVER - [TransID: " + transId + "]");
                }

                res.ssn_verify_status = regUserResponse.ssn_verify_status;

                return Json(res);
            }
            catch (Exception ex)
            {
                Logger.Error("payRequest Code-Behind -> RegisterUserWithSynp attempt FAILED Failed - Reason: [" + res.reason + "], " +
                                       "TransId: [" + transId + "], [Exception: " + ex + "]");
                return Json(res);
            }
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
                        res.TransType = TransDetails.UserType;
                        res.TransId = TransDetails.TransId;
                        res.LinkSource = Request.QueryString["LinkSource"];
                        res.UserType = Request.QueryString["UserType"];
                        res.transStatus = TransDetails.TransStatus;
                        res.TransAmout = TransDetails.AmountLabel;

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

        [HttpGet]
        public ActionResult RejectMoneyBtnClick(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            PageLoadDataRejectMoney res = new PageLoadDataRejectMoney();
            string serviceMethod = string.Empty;
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            serviceMethod = "/RejectMoneyCommon?TransactionId=" + TransactionId +
                            "&UserType=" + UserType +
                            "&LinkSource=" + LinkSource +
                            "&TransType=" + TransType;
            Logger.Info("rejectMoney CodeBehind -> RejectRequest - Full Service URL To Query: [" + String.Concat(serviceUrl, serviceMethod) + "]");
            var serviceResult = ResponseConverter<StringResult>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (serviceResult.Result == "Success." || serviceResult.Result == "Success")
            {
                res.errorFromCodeBehind = "0";

                res.transStatus= "Request rejected successfully.";

                //res.UserType  -- this can be handled client side

                // Check if request is performed by new user
                //if (SessionHelper.GetSessionValue("UserType").ToString() == "New")
                //{
                //    createAccountPrompt.Visible = true; // prompt to create account
                //}

                Logger.Info("rejectMoney CodeBehind -> RejectRequest SUCCESSFUL - [TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");
            }
            else
            {
                Logger.Error("rejectMoney CodeBehind -> RejectRequest FAILED - [Server Result: " + serviceResult.Result + "], " +
                                       "[TransactionId Parameter: " + Request.QueryString["TransactionId"] + "]");
                res.errorFromCodeBehind = "1";
            }
            return Json(res);

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
}