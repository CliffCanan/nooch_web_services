using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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
        public ActionResult BankVerification()
        {
            return View();
        }

        public ActionResult CancelRequest()
        {
            if (!String.IsNullOrEmpty(Request.QueryString["TransactionId"]) &&
               !String.IsNullOrEmpty(Request.QueryString["MemberId"]) &&
               !String.IsNullOrEmpty(Request.QueryString["UserType"]))
            {
                GetTransDetails(Request.QueryString["TransactionId"]);
            }
            else
            {
                // something wrong with Query string :'(
                //reslt1.Visible = false;
                //reslt.Text = "This looks like an invalid transaction - sorry about that!  Please try again or contact Nooch support for more information.";

                //paymentInfo.Visible = false;
            }
            return View();
        }

        public void GetTransDetails(string TransactionId)
        {
            string serviceUrl = Utility.GetValueFromConfig("ServiceUrl");
            string serviceMethod = "/GetTransactionDetailById?TransactionId=" + TransactionId;

            TransactionDto transaction = ResponseConverter<TransactionDto>.ConvertToCustomEntity(String.Concat(serviceUrl, serviceMethod));

            if (transaction != null)
            {
                CancelMoneyRequest(Request.QueryString["TransactionId"], Request.QueryString["MemberId"], Request.QueryString["UserType"]);
            }

            if (transaction.TransactionStatus != "Pending")
            {
                reslt1.Visible = false;
                paymentInfo.Visible = true;
                reslt.Text = "Looks like this request is no longer pending. You may have cancelled it already or the recipient has already responded by accepting or rejecting.";
                reslt.Visible = true;
            }


            if (transaction.IsPhoneInvitation && transaction.PhoneNumberInvited.Length > 0)
            {
                senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                nameLabel.Text = transaction.PhoneNumberInvited;
            }
            else if (!String.IsNullOrEmpty(transaction.InvitationSentTo))
            {
                senderImage.ImageUrl = "https://www.noochme.com/noochweb/Assets/Images/" + "userpic-default.png";
                nameLabel.Text = transaction.InvitationSentTo;
            }
            else
            {
                nameLabel.Text = transaction.Name;
                senderImage.ImageUrl = transaction.SenderPhoto;
            }

            AmountLabel.Text = transaction.Amount.ToString("n2");
        }



        
    }
}