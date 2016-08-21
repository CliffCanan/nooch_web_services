using System;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Nooch.Common;
using Nooch.Data;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Newtonsoft.Json;

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
        public void ViewSubscriptionForUserFromSynapse()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;

            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse Fired - JSON: [ " + jsonContent + " ]");

            try
            {
                using (NOOCHEntities obj = new NOOCHEntities())
                {
                    SynSub_User userObj = new SynSub_User();
                    userObj = JsonConvert.DeserializeObject<SynSub_User>(jsonContent);

                    if (userObj._id.oid != null)
                    {
                        var synapseUserObj = obj.SynapseCreateUserResults.FirstOrDefault(user => user.user_id == userObj._id.oid.ToString() &&
                                                                                         user.IsDeleted == false);

                        if (synapseUserObj != null)
                        {
                            var memId = synapseUserObj.MemberId;
                            var memberObj = obj.Members.FirstOrDefault(mem => mem.MemberId == memId &&
                                                                              mem.IsDeleted == false);

                            if (memberObj != null)
                            {
                                try
                                {
                                    // Great, we have found the user!
                                    Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - RESPONSE SUCCESSFUL: Name: [" + userObj.documents[0].name +
                                                "], Permission: [" + userObj.permission + "], CIP_TAG: [" + userObj.extra.cip_tag +
                                                "], Phys Doc: [" + userObj.doc_status.physical_doc + "], Virt Doc: [" + userObj.doc_status.virtual_doc + "]");

                                    #region Update Permission in SynapseCreateUserResults Table

                                    if (synapseUserObj != null)
                                    {
                                        if (userObj.permission != null) synapseUserObj.permission = userObj.permission.ToString();

                                        if (userObj.doc_status != null)
                                        {
                                            synapseUserObj.physical_doc = userObj.doc_status.physical_doc.ToString();
                                            synapseUserObj.virtual_doc = userObj.doc_status.virtual_doc.ToString();
                                        }

                                        if (userObj.documents != null &&
                                            userObj.documents[0] != null)
                                        {
                                            #region Loop Through Outer Documents Object (Should Only Be 1)

                                            foreach (Document doc in userObj.documents)
                                            {
                                                // Check VIRTUAL_DOCS
                                                if (doc.virtual_docs != null && doc.virtual_docs.Length > 0)
                                                {
                                                    short n = 0;
                                                    foreach (Virtual_Docs docObject in doc.virtual_docs)
                                                    {
                                                        n += 1;
                                                        Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - VIRTUAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                                                        if (docObject.document_type == "SSN")
                                                        {
                                                            synapseUserObj.virtual_doc = docObject.status;
                                                            synapseUserObj.virt_doc_lastupdated = DateTime.Now;
                                                        }
                                                    }
                                                }

                                                // Check PHYSICAL_DOCS
                                                if (doc.physical_docs != null && doc.physical_docs.Length > 0)
                                                {
                                                    short n = 0;
                                                    foreach (Physical_Docs docObject in doc.physical_docs)
                                                    {
                                                        n += 1;
                                                        Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - PHYSICAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                                                        if (docObject.document_type == "GOVT_ID")
                                                        {
                                                            synapseUserObj.physical_doc = docObject.status;
                                                            synapseUserObj.phys_doc_lastupdated = DateTime.Now;
                                                        }
                                                    }
                                                }

                                                // Check SOCIAL_DOCS
                                                if (doc.social_docs != null && doc.social_docs.Length > 0)
                                                {
                                                    short n = 0;
                                                    foreach (Social_Docs docObject in doc.social_docs)
                                                    {
                                                        n += 1;
                                                        Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SOCIAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                                                        if (docObject.document_type == "FACEBOOK")
                                                        {
                                                            synapseUserObj.social_doc = docObject.status;
                                                            synapseUserObj.soc_doc_lastupdated = DateTime.Now;
                                                        }
                                                    }
                                                }

                                            }

                                            #endregion Loop Through Outer Documents Object (Should Only Be 1)
                                        }


                                        int save = obj.SaveChanges();
                                        obj.Entry(synapseUserObj).Reload();

                                        if (save > 0)
                                        {
                                            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - And Successfully updated user's record in SynapseCreateUserRes Table");

                                            // Update Member's DB record
                                            memberObj.IsVerifiedWithSynapse = true;
                                            memberObj.ValidatedDate = DateTime.Now;
                                            memberObj.DateModified = DateTime.Now;

                                            int saveMemberTable = obj.SaveChanges();
                                            obj.Entry(memberObj).Reload();
                                        }
                                        else
                                            Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - But FAILED to update user's record in SynapseCreateUserRes Table");
                                    }

                                    #endregion Update Permission in CreateSynapseUserResults Table
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
                                                 "MemberID: [" + memId + "], Exception: [" + ex + "]");
                                }
                            }
                            else
                                Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Unable to lookup Member by MemberID: [" + memId + "]");
                        }
                        else
                            Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Unable to lookup Synapse User by OID from JSON: [" + userObj._id.oid + "]");
                    }
                    else
                    {
                        var error = "WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED: Synapse Result was NULL - Full Synapse Response: [" + jsonContent + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = "WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Outer Exception: [" + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
            }
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
                    // code to store transaction timeline info in DB
                    SynSub_Trans transObj = new SynSub_Trans();
                    transObj = JsonConvert.DeserializeObject<SynSub_Trans>(jsonContent);

                    if (transObj != null && transObj._id.oid != null)
                    {
                        // Lookup the Nooch TransactionID from the Synapse OID
                        var synapseTransObjFromDb = obj.SynapseAddTransactionResults.FirstOrDefault(trans => trans.OidFromSynapse == transObj._id.oid.ToString());
                        noochTransId = synapseTransObjFromDb.TransactionId.ToString();

                        var allTimelineItems = transObj.timeline;

                        if (allTimelineItems != null)
                        {
                            foreach (var currentItem in allTimelineItems)
                            {
                                string status = currentItem.status != null ? currentItem.status.ToString() : null;
                                string status_id = currentItem.status_id != null ? currentItem.status_id.ToString() : null;
                                string status_date = currentItem.date != null ? currentItem.date.date.ToString() : null;
                                string note = currentItem.note != null ? currentItem.note.ToString() : null;

                                TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                                tas.Nooch_Transaction_Id = noochTransId;
                                tas.Transaction_oid = transObj._id.oid;
                                tas.status = status;
                                tas.status_id = status_id;
                                tas.status_date = status_date;
                                tas.status_note = note;

                                obj.TransactionsStatusAtSynapses.Add(tas);
                                obj.SaveChanges();
                            }
                        }


                        // Checking most recent status to update Transactions Table b/c Timeline []
                        // may have multiple statuses and that may be confusing to update to most recent status

                        var mostRecentToken = transObj.recent_status;

                        if (mostRecentToken.status != null)
                        {
                            // updating transcaction status in transactions table
                            if (!String.IsNullOrEmpty(mostRecentToken.status.ToString()) &&
                                !String.IsNullOrEmpty(noochTransId))
                            {
                                Guid transGuid = Utility.ConvertToGuid(noochTransId);
                                Transaction t = obj.Transactions.FirstOrDefault(trans => trans.TransactionId == transGuid);

                                if (t != null)
                                {
                                    t.SynapseStatus = mostRecentToken.status.ToString();
                                    obj.SaveChanges();
                                }
                            }

                            // Notify Cliff if the status is "CANCELED"
                            if (mostRecentToken.status.ToString() == "CANCELED")
                                CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> ViewSubscriptionForTransFromSynapse -> Synapse Payment <span style=\"color:red;\">CANCELED</span><h3><br/><br/><p><strong>Transaction ID:</strong> " +
                                                                   noochTransId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                        }
                    }
                    else
                    {
                        var error = "WEBHOOK -> ViewSubscriptionForTransFromSynapse FAILED - Object was null or missing OID from Synapse - JSON: [" + jsonContent + "]";
                        Logger.Info(error);
                        CommonHelper.notifyCliffAboutError(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WEBHOOK -> GetTransactionStatusFromSynapse FAILED - TransID: [" + noochTransId +
                             "] - Outer Exception: [" + ex + "]");
            }
        }


        [HttpPost]
        [ActionName("ViewSubscriptionForNodeFromSynapse")]
        public void ViewSubscriptionForUserNodeSynapse()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;

            Logger.Info("WEBHOOK -> ViewSubscriptionForNodeFromSynapse Fired - JSON: [ " + jsonContent + " ]");

            try
            {
                using (NOOCHEntities obj = new NOOCHEntities())
                {
                    SynSub_Node nodeObj = new SynSub_Node();
                    nodeObj = JsonConvert.DeserializeObject<SynSub_Node>(jsonContent);

                    if (nodeObj != null && !String.IsNullOrEmpty(nodeObj._id.oid))
                    {
                        string bankOidEncrypted = CommonHelper.GetEncryptedData(nodeObj._id.oid);
                        var synapseRes = obj.SynapseBanksOfMembers.FirstOrDefault(node => node.oid == bankOidEncrypted &&
                                                                                          node.IsDefault == true);

                        if (synapseRes != null)
                        {
                            // Great, we have found the Node!
                            Logger.Info("WEBHOOK -> ViewSubscriptionForNodeFromSynapse - RESPONSE SUCCESSFUL: Bank Name: [" + nodeObj.info.bank_name +
                                        "], Permission: [" + nodeObj.allowed + "] and is active:[" + nodeObj.is_active + "]");

                            synapseRes.is_active = nodeObj.is_active;
                            synapseRes.allowed = nodeObj.allowed;
                            obj.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WEBHOOK -> ViewSubscriptionForNodeFromSynapse FAILED - Outer Exception: [" + ex + "]");
            }
        }

    }
}