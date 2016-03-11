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
            if(!String.IsNullOrEmpty(MemberId))
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

        public  BankLoginResult RegisterUserWithSynapse(string memberid)
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

        public  SynapseBankLoginRequestResult BankLogin(string username, string password, string memberid, string bankname, bool IsPinRequired, string PinNumber)
        {
            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // 1. Attempt to register the user with Synapse
                BankLoginResult accountCreateResult = RegisterUserWithSynapse(memberid);
                res.ssn_verify_status = accountCreateResult.ssn_verify_status; // Will be overwritten if Bank Login is successful below

                if (accountCreateResult.IsSuccess == true)
                {
                    Logger.Info("**Add_Bank** CodeBehind -> BankLogin -> Synapse account created successfully! [MemberID: " + memberid +
                                           "], [SSN Status: " + accountCreateResult.ssn_verify_status + "]");

                    // 2. Now call the bank login service.
                    //    Response could be: 1.) array[] of banks,  2.) Question-based MFA,  3.) Code-based MFA, or  4.) Failure/Error

                    string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                    string serviceMethod = "/SynapseBankLoginRequest?BankName=" + bankname + "&MemberId=" + memberid + "&IsPinRequired=" + IsPinRequired
                        + "&UserName=" + username + "&Password=" + password + "&PinNumber=" + PinNumber;

                    SynapseBankLoginV3_Response_Int bankLoginResult =
                        ResponseConverter<SynapseBankLoginV3_Response_Int>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                    if (bankLoginResult.Is_success == true)
                    {
                        res.Is_success = true;
                        res.Is_MFA = bankLoginResult.Is_MFA;
                        res.Is_MFA = bankLoginResult.Is_MFA;
                        if (bankLoginResult.Is_MFA)
                        {
                            res.MFA_Type = "question";    // no more code based as per synapse V3 docs
                        }
                        List<SynapseBankClass> synbanksList = new List<SynapseBankClass>();

                        foreach (nodes bankNode in bankLoginResult.SynapseNodesList.nodes)
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
                        }; ;

                        res.ERROR_MSG = "OK";
                       // res.ssn_verify_status = bankLoginResult.ssn_verify_status; // Should match the ssn_verify_status from Registering User...
                    }
                    else
                    {
                        Logger.Error("**Add_Bank** CodeBehind -> BankLogin FAILED -> [MemberID: " + memberid + "], [Error Msg: " + bankLoginResult.errorMsg + "]");
                        res.ERROR_MSG = bankLoginResult.errorMsg;
                    }
                }
                else
                {
                    Logger.Error("**Add_Bank** CodeBehind -> BankLogin -> Register Synapse User FAILED -> [MemberID: " + memberid + "], [Error Msg: " + accountCreateResult.Message + "]");
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
                Logger.Error("**Add_Bank** CodeBehind -> BankLogin FAILED - [MemberID: " + memberid +
                                   "], [Exception: " + we.InnerException + "]");

                res.ERROR_MSG = "error occured at server.";
            }

            return res;
        }

        public static SynapseBankLoginRequestResult addBank(string memberid, string fullname, string routing, string account, string nickname, string cl, string type)
        {
            Logger.Info("**Add_Bank** CodeBehind -> addBank (for manual routing/account #) Initiated - [MemberID: " + memberid + "]");

            SynapseBankLoginRequestResult res = new SynapseBankLoginRequestResult();
            res.Is_success = false;

            try
            {
                // Now call the bank login service.
                // Response should be: 1.) array[] of 1 bank, or  2.) Failure/Error

                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");

                string serviceMethod = "/SynapseV3AddNodeWithAccountNumberAndRoutingNumber?MemberId=" + memberid +  "&routing_num=" + routing
                                       + "&account_num=" + account + "&bankNickName=" + nickname + "&accountclass=" + cl + "&accounttype=" + type;

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
                        sbc.account_number_string= bankNode.info.account_num;
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
                    };;
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
                Logger.Error("**Add_Bank** CodeBehind -> addBank FAILED - [MemberID: " + memberid +
                                   "], [Exception: " + we.InnerException + "]");

                res.ERROR_MSG = "Error did occur at server. Ohh nooo!!";
                return res;
            }
        }

 
        
    }
}