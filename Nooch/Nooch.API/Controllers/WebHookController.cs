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
                            " ], Content: [ " + jsonContent + " ]");

                using (NOOCHEntities obj = new NOOCHEntities())
                {
                    // code to store transaction timeline info in db
                    JObject jsonfromsynapse = JObject.Parse(jsonContent);

                    JToken doesTransExists = jsonfromsynapse["trans"];

                    if (doesTransExists != null)
                    {
                        #region Expected Format

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

                                // Notify Cliff if the status is "CANCELED"
                                if (mostRecentToken["status"].ToString() == "CANCELED")
                                    CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> Synapse Payment CANCELED<h3><br/><br/><p><strong>TransactionID:</strong> " +
                                                                       transId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                            }
                        }
                        else
                            Logger.Error("WEBHOOK -> GetTransactionStatusFromSynapse FAILED - TransactionID: [" + transId +
                                         "] - Content: [ " + jsonContent + " ]");

                        #endregion Expected Format
                    }
                    else
                    {
                        #region Secondary Format From Synapse

                        // this time object is without trans key around --- don't know why, synapse is returning data in two different formats now.
                        // CC (8/17/16): I believe this 2nd format is the right one.

                        JToken transIdFromSyanpse = jsonfromsynapse["_id"]["$oid"];

                        if (transIdFromSyanpse != null)
                        {
                            JToken allTimeLineItems = jsonfromsynapse["timeline"];

                            if (allTimeLineItems != null)
                            {
                                foreach (JToken currentItem in allTimeLineItems)
                                {
                                    string note = currentItem["note"] != null ? currentItem["note"].ToString() : null;
                                    string status = currentItem["status"] != null ? currentItem["status"].ToString() : null;
                                    string status_id = currentItem["status_id"] != null ? currentItem["status_id"].ToString() : null;
                                    string status_date = currentItem["date"] != null ? currentItem["date"]["$date"].ToString() : null;

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

                            JToken mostRecentToken = jsonfromsynapse["recent_status"];

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

                                // Notify Cliff if the status is "CANCELED"
                                if (mostRecentToken["status"].ToString() == "CANCELED")
                                {
                                    CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> Synapse Payment CANCELED<h3><br/><br/><p><strong>TransactionID:</strong> " +
                                                                       transId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                                }
                            }
                        }

                        #endregion Secondary Format From Synapse
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WEBHOOK -> GetTransactionStatusFromSynapse FAILED - TransactionID: [" + transId +
                             "] - Outer Exception: [" + ex + "]");
            }
        }


        [HttpPost]
        [ActionName("ViewSubscriptionForUserFromSynapse")]
        public void ViewSubscriptionForUserFromSynapse(string oid)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse Fired - user_id: [ " + oid +
                            " ], Content: [ " + jsonContent + " ]");



        }


        [HttpPost]
        [ActionName("ViewSubscriptionForTransFromSynapse")]
        public void ViewSubscriptionForTransFromSynapse()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;

            Logger.Info("WEBHOOK -> ViewSubscriptionForTransFromSynapse Fired - JSON: [ " + jsonContent + " ]");
            var noochTransId = string.Empty;

            try
            {
                using (NOOCHEntities obj = new NOOCHEntities())
                {
                    // code to store transaction timeline info in db 
                    JObject jsonfromsynapse = JObject.Parse(jsonContent);

                    JToken transIdFromSyanpse = jsonfromsynapse["_id"]["$oid"];

                    if (transIdFromSyanpse != null)
                    {
                        // Lookup the Nooch TransactionID from the Synapse OID
                        var synapseTransObjFromDb = obj.SynapseAddTransactionResults.FirstOrDefault(trans => trans.OidFromSynapse == transIdFromSyanpse.ToString());
                        noochTransId = synapseTransObjFromDb.TransactionId.ToString();

                        JToken allTimeLineItems = jsonfromsynapse["timeline"];

                        if (allTimeLineItems != null)
                        {
                            foreach (JToken currentItem in allTimeLineItems)
                            {
                                string note = currentItem["note"] != null ? currentItem["note"].ToString() : null;
                                string status = currentItem["status"] != null ? currentItem["status"].ToString() : null;
                                string status_id = currentItem["status_id"] != null ? currentItem["status_id"].ToString() : null;
                                string status_date = currentItem["date"] != null ? currentItem["date"]["$date"].ToString() : null;

                                TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                                tas.Nooch_Transaction_Id = noochTransId;
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

                        JToken mostRecentToken = jsonfromsynapse["recent_status"];

                        if (mostRecentToken["status"] != null)
                        {
                            // updating transcaction status in transactions table
                            if (!String.IsNullOrEmpty(mostRecentToken["status"].ToString()) &&
                                !String.IsNullOrEmpty(noochTransId))
                            {
                                Guid transGuid = Utility.ConvertToGuid(noochTransId);
                                Transaction t = obj.Transactions.FirstOrDefault(trans => trans.TransactionId == transGuid);

                                if (t != null)
                                {
                                    t.SynapseStatus = mostRecentToken["status"].ToString();
                                    obj.SaveChanges();
                                }
                            }

                            // Notify Cliff if the status is "CANCELED"
                            if (mostRecentToken["status"].ToString() == "CANCELED")
                                CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> Synapse Payment CANCELED<h3><br/><br/><p><strong>TransactionID:</strong> " +
                                                                   noochTransId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WEBHOOK -> GetTransactionStatusFromSynapse FAILED - TransactionID: [" + noochTransId +
                             "] - Outer Exception: [" + ex + "]");
            }
        }

        [HttpPost]
        [ActionName("ViewSubscriptionForNodeFromSynapse")]
        public void ViewSubscriptionForUserNodeSynapse(string oid)
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;
            Logger.Info("WEBHOOK -> ViewSubscriptionForNodeFromSynapse Fired - node_id: [ " + oid +
                            " ], Content: [ " + jsonContent + " ]");



        }



    }
}