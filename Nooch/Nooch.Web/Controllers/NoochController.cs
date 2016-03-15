using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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

        public ActionResult AddBank()
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
        public static BankNameCheckStatus CheckBankDetails(string bankname)
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
            return res;
        }

        public ActionResult makePayment()
        
        
        {
            HiddenField hkf = new HiddenField();
            try
            {
                                   
                    if (!String.IsNullOrEmpty(Request.QueryString["rs"]))
                    {
                        Logger.Info("Make Payment CodeBehind -> Page_load Initiated - Is a RentScene Payment: [" + Request.QueryString["rs"] + "]");

                        //rs.Value = Request.QueryString["rs"].ToLower();      
                        hkf.rs = Request.QueryString["rs"].ToLower();
                }
            }
            catch (Exception ex)
            {
               // errorId.Value = "1";
                hkf.errorId = "1";
                Logger.Error("Make Payment CodeBehind -> page_load OUTER EXCEPTION - [Exception: " + ex.Message + "]");
            }

            ViewData["OnLoadData"] = hkf;
            return View();
        }

        public ActionResult submitPayment(bool isRequest, string amount, string name, string email, string memo, string pin, string ip)
        {
            Logger.Info("Make Payment Code-Behind -> submitPayment Initiated - isRequest: [" + isRequest +
                                   "], Name: [" + name + "], Email: [" + email +
                                   "], Amount: [" + amount + "], memo: [" + memo +
                                   "], PIN: [" + pin + "], IP: [" + ip + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.msg = "Initial - code behind";

            pin = (String.IsNullOrEmpty(pin) || pin.Length != 4) ? "0000" : pin;
            pin = CommonHelper.GetEncryptedData(pin);

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RequestMoneyForRentScene?name=" + name +
                                       "&email=" + email + "&amount=" + amount +
                                       "&memo=" + memo + "&pin=" + pin +
                                       "&ip=" + ip + "&isRequest=" + isRequest;

                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("Make Payment Code-Behind -> submitPayment - URL To Query: [" + urlToUse + "]");

                requestFromRentScene response = ResponseConverter<requestFromRentScene>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                Logger.Info("Make Payment Code-Behind -> submitPayment RESULT.Success: [" + response.success + "], RESULT.Msg: [" + response.msg + "]");

                if (response != null)
                {
                    res = response;

                    //Logger.LogDebugMessage("Make Payment Code-Behind -> submitPayment");

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        if (response.isEmailAlreadyReg == true)
                        {
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

        public ActionResult submitRequestToExistingUser(bool isRequest, string amount, string name, string email, string memo, string pin, string ip, string memberId, string nameFromServer)
        {
            Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser Initiated - isRequest: [" + isRequest +
                                   "], Name: [" + name + "], Email: [" + email +
                                   "], Amount: [" + amount + "], memo: [" + memo +
                                   "], PIN: [" + pin + "], IP: [" + ip + "]" +
                                   "], MemberID: [" + memberId + "], NameFromServer: [" + nameFromServer + "]");

            requestFromRentScene res = new requestFromRentScene();
            res.success = false;
            res.msg = "Initial - code behind";

            pin = (String.IsNullOrEmpty(pin) || pin.Length != 4) ? "0000" : pin;
            pin = CommonHelper.GetEncryptedData(pin);

            try
            {
                string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
                string serviceMethod = "/RequestMoneyToExistingUserForRentScene?name=" + name +
                                       "&email=" + email + "&amount=" + amount +
                                       "&memo=" + memo + "&pin=" + pin +
                                       "&ip=" + ip + "&isRequest=" + isRequest +
                                       "&memberId=" + memberId + "&nameFromServer=" + nameFromServer;

                string urlToUse = String.Concat(serviceUrl, serviceMethod);

                Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser - URL To Query: [" + urlToUse + "]");

                requestFromRentScene response = ResponseConverter<requestFromRentScene>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

                Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser - Server Response for RequestMoneyToExistingUserForRentScene: " +
                                       "RESULT.Success: [" + response.success + "], RESULT.Msg: [" + response.msg + "]");

                if (response != null)
                {
                    res = response;

                    Logger.Info("Make Payment Code-Behind -> submitPayment");

                    #region Logging For Debugging

                    if (response.success == true)
                    {
                        Logger.Info("Make Payment Code-Behind -> submitRequestToExistingUser Success - Payment Request submitted to NEW user successfully - " +
                                               "Recipient: [" + name + "], Email: [" + email + "], Amount: [" + amount + "], Memo: [" + memo + "]");
                    }
                    else
                    {
                        Logger.Error("Make Payment Code-Behind -> submitRequestToExistingUser FAILED - Server response for RequestMoneyForRentScene() was NOT successful - " +
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
                Logger.Error("Make Payment Code-Behind -> submitRequestToExistingUser FAILED - email: [" + email + "], Exception: [" + ex.Message + "]");
                res.msg = "Code-behind exception during submitRequestToExistingUser.";
            }

            return Json(res);
        }
        
    }
}