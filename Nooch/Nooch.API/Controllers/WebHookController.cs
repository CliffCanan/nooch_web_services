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
            try
            {




                Logger.Info("GetTransactionStatusFromSynapse [ WebHook ] loaded for TransactionId : [ " + transId + " ]. At [ " + DateTime.Now + " ]. With Request Body [ " + jsonContent + " ].");


                // code to store transaction timeline info in db 
                JObject jsonfromsynapse = JObject.Parse(jsonContent);

                JToken transIdFromSyanpse = jsonfromsynapse["trans"]["_id"]["$oid"];

                if (transIdFromSyanpse != null)
                {
                    JToken allTimeLineItems = jsonfromsynapse["trans"]["timeline"];

                    if (allTimeLineItems != null)
                    {
                        foreach (JToken currentItem in allTimeLineItems)
                        {


                            string note = currentItem["note"].ToString();
                            string status = currentItem["status"].ToString();
                            string status_id = currentItem["status_id"].ToString();
                            string status_date = currentItem["date"]["$date"].ToString();



                            using (NOOCHEntities obj = new NOOCHEntities())
                            {
                                TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                                tas.Nooch_Transaction_Id = transId;
                                tas.Transaction_oid = transIdFromSyanpse==null ? "" : transIdFromSyanpse.ToString();
                                tas.status = status;
                                tas.status_date = status_date;
                                tas.status_id = status_id;
                                tas.status_note = note;


                                obj.TransactionsStatusAtSynapses.Add(tas);
                                obj.SaveChanges();

                            }

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetTransactionStatusFromSynapse [ WebHook ] failed to save in db for TransactionId : [ " + transId + " ]. At [ " + DateTime.Now + " ]. Exception -> [ " + ex + " ].");

            }



        }
    }
}