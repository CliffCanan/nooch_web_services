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

                    rpr= GetTransDetailsForPayRequest(Request.QueryString["TransactionId"].ToString(),rpr);
                    }
                    else
                    {
                        // something wrong with query string
                        Response.Write("<script>var errorFromCodeBehind = '1';</script>");
                       rpr.payreqInfo= false;
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

        public ResultPayRequest GetTransDetailsForPayRequest(string TransactionId,ResultPayRequest resultPayRequest)
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

               rpr.senderImage= transaction.RecepientPhoto;
               rpr.senderName1= (!String.IsNullOrEmpty(transaction.RecepientName) && transaction.RecepientName.Length > 2) ?
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
                        rpr.nonRegUsrContainer= true;
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
                rpr.invitationType  = transaction.IsPhoneInvitation == true ? "p" : "e";

                if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
                {
                    rpr.invitationSentto = transaction.InvitationSentTo;
                }
            }
            return rpr;
        }
        
    }
}