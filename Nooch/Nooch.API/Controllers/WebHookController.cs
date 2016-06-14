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
                Logger.Info("WEBHOOK -> GetTransactionStatusFromSynapse Fired - TransactionID: [ " + transId +
                                " ] At [ " + DateTime.Now + " ]. Content: [ " + jsonContent + " ]");

                using (NOOCHEntities obj = new NOOCHEntities())
                {
                    // code to store transaction timeline info in db 
                    JObject jsonfromsynapse = JObject.Parse(jsonContent);

                    JToken doesTransExists = jsonfromsynapse["trans"];

                    if (doesTransExists != null)
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

                            // Checking most recent status to update Transactions Table b/c Timeline []
                            // may have multiple statuses and that may be confusing to update to most recent status

                            JToken mostRecentToken = jsonfromsynapse["trans"]["recent_status"];

                            if (mostRecentToken["status"] != null)
                            {
                                // updating transcaction status in transactions table
                                if (!String.IsNullOrEmpty(mostRecentToken["status"].ToString()) &&
                                    !String.IsNullOrEmpty(transId))
                                {
                                    Guid transGuid = Utility.ConvertToGuid(transId);
                                    Transaction t = obj.Transactions.FirstOrDefault(t2 => t2.TransactionId == transGuid);

                                    if (t != null)
                                    {
                                        t.SynapseStatus = mostRecentToken["status"].ToString();
                                        obj.SaveChanges();
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Error("WEBHOOK -> GetTransactionStatusFromSynapse FAILED - TransactionID: [ " + transId +
                                         " ] At [ " + DateTime.Now + " ]. Content: [ " + jsonContent + " ]");
                        }
                    }
                    else
                    {
                        #region Secondary Format From Synapse

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

                            if (mostRecentToken["status"] != null)
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

                        #endregion Secondary Format From Synapse
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