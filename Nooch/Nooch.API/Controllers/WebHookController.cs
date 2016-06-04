using System;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Nooch.Common;
using Nooch.Data;

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
            //JObject jsonfromsynapse = JObject.Parse(jsonContent);
            //JToken token = jsonfromsynapse["jsonContent"];
            //string transIdFromSyanpse = jsonfromsynapse["trans"]["_id"]["$oid"].ToString();
            //if (jsonfromsynapse != null)
            //{
            //    foreach (JToken jT in jsonfromsynapse)
            //    {
            //        string dateFromSynase =
            //         jT["date"]["$date"].ToString();

            //        string note = jT["note"].ToString();
            //        string status = jT["status"].ToString();
            //        string status_id = jT["status_id"].ToString();

            //        string transaction_id_from_synapse = jT["status_id"].ToString();

            //        using (NOOCHEntities obj = new NOOCHEntities())
            //        {
            //            TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
            //            tas.Nooch_Transaction_Id = transId;
            //            tas.Transaction_oid

            //            obj.TransactionsStatusAtSynapses
            //        }


            //    }
            //}




        }
    }
}