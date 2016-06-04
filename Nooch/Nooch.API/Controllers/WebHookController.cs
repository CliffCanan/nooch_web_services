using System;
using System.Net.Http;
using System.Web.Http;
using Nooch.Common;

namespace Nooch.API.Controllers
{
    public class WebHookController : ApiController
    {
        [HttpPost]
        [ActionName("GetTransactionStatusFromSynapse")]
        public void GetTransactionStatusFromSynapse(string transId)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            Logger.Info("GetTransactionStatusFromSynapse [ WebHook ] loaded for TransactionId : [ " + transId + " ]. At [ " + DateTime.Now + " ]. With Request Body [ " + jsonContent + " ].");


            // code to store transaction timeline info in db 





        }
    }
}