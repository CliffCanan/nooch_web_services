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
 
        
    }
}