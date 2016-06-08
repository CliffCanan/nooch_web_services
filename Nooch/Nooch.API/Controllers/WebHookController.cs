using System;
using System.Data.Entity;
using System.Linq;
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


                using (NOOCHEntities obj = new NOOCHEntities())
                {

                    Logger.Info("GetTransactionStatusFromSynapse [ WebHook ] loaded for TransactionId : [ " + transId +
                                " ]. At [ " + DateTime.Now + " ]. With Request Body [ " + jsonContent + " ].");


                    // code to store transaction timeline info in db 
                    JObject jsonfromsynapse = JObject.Parse(jsonContent);

                    JToken isTransExists = jsonfromsynapse["trans"];

                    if (isTransExists != null)
                    {
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




                                    TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                                    tas.Nooch_Transaction_Id = transId;
                                    tas.Transaction_oid = transIdFromSyanpse == null
                                        ? ""
                                        : transIdFromSyanpse.ToString();
                                    tas.status = status;
                                    tas.status_date = status_date;
                                    tas.status_id = status_id;
                                    tas.status_note = note;


                                    obj.TransactionsStatusAtSynapses.Add(tas);
                                    obj.SaveChanges();




                                }
                            }

                            // checking most recent status for updating in transactions table because timeline array may have multiple statuses and that may make confussion to update to most recent status

                            JToken mostRecentToken = jsonfromsynapse["trans"]["recent_status"];

                            if (mostRecentToken.Contains("status"))
                            {
                                // updating transcaction status in transactions table
                                if (!String.IsNullOrEmpty(mostRecentToken["status"].ToString()) &&
                                    !String.IsNullOrEmpty(transId))
                                {
                                    Guid transGuid = Utility.ConvertToGuid(transId);
                                    Transaction t =
                                        obj.Transactions.FirstOrDefault(t2 => t2.TransactionId == transGuid);
                                    if (t != null)
                                    {
                                        t.SynapseStatus = mostRecentToken["status"].ToString();
                                        obj.SaveChanges();
                                    }
                                }
                            }




                        }

                    }
                    else
                    {
                        // this time object is without trans key around --- don't know why, synapse is returning data in two different formats now.
                        JToken transIdFromSyanpse = jsonfromsynapse["_id"]["$oid"];

                        if (transIdFromSyanpse != null)
                        {
                            JToken allTimeLineItems = jsonfromsynapse["timeline"];

                            if (allTimeLineItems != null)
                            {
                                foreach (JToken currentItem in allTimeLineItems)
                                {


                                    string note = currentItem["note"].ToString();
                                    string status = currentItem["status"].ToString();
                                    string status_id = currentItem["status_id"].ToString();
                                    string status_date = currentItem["date"]["$date"].ToString();



                                    TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                                    tas.Nooch_Transaction_Id = transId;
                                    tas.Transaction_oid = transIdFromSyanpse == null
                                        ? ""
                                        : transIdFromSyanpse.ToString();
                                    tas.status = status;
                                    tas.status_date = status_date;
                                    tas.status_id = status_id;
                                    tas.status_note = note;


                                    obj.TransactionsStatusAtSynapses.Add(tas);
                                    obj.SaveChanges();





                                }
                            }


                            // checking most recent status for updating in transactions table because timeline array may have multiple statuses and that may make confussion to update to most recent status

                            JToken mostRecentToken = jsonfromsynapse["recent_status"];

                            if (mostRecentToken.Contains("status"))
                            {
                                // updating transcaction status in transactions table
                                if (!String.IsNullOrEmpty(mostRecentToken["status"].ToString()) &&
                                    !String.IsNullOrEmpty(transId))
                                {
                                    Guid transGuid = Utility.ConvertToGuid(transId);
                                    Transaction t =
                                        obj.Transactions.FirstOrDefault(t2 => t2.TransactionId == transGuid);
                                    if (t != null)
                                    {
                                        t.SynapseStatus = mostRecentToken["status"].ToString();
                                        obj.SaveChanges();
                                    }
                                }
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