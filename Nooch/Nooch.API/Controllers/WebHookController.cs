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
                        var synapseRes = obj.SynapseCreateUserResults.FirstOrDefault(user => user.user_id == userObj._id.oid.ToString() &&
                                                                                           user.IsDeleted == false);

                        if (synapseRes != null)
                        {
                            var memId = synapseRes.MemberId;
                            var memberObj = obj.Members.FirstOrDefault(mem => mem.MemberId == memId && mem.IsDeleted == false);
                            if (memberObj != null)
                            {
                                try
                                {
                                    // Great, we have found the user!
                                    Logger.Info("Common Helper -> sendDocsToSynapseV3 - RESPONSE SUCCESSFUL: Name: [" + userObj.documents[0].name +
                                                "], Permission: [" + userObj.permission + "], CIP_TAG: [" + userObj.extra.cip_tag +
                                                "], Phys Doc: [" + userObj.doc_status.physical_doc + "], Virt Doc: [" + userObj.doc_status.virtual_doc + "]");

                                    #region Update Permission in SynapseCreateUserResults Table

                                    if (synapseRes != null)
                                    {
                                        if (userObj.permission != null)
                                            synapseRes.permission = userObj.permission.ToString();
                                        if (userObj.doc_status != null)
                                        {
                                            synapseRes.physical_doc = userObj.doc_status.physical_doc.ToString();
                                            synapseRes.virtual_doc = userObj.doc_status.virtual_doc.ToString();
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
                                                            synapseRes.virtual_doc = docObject.status;
                                                            synapseRes.virt_doc_lastupdated = DateTime.Now;
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
                                                            synapseRes.physical_doc = docObject.status;
                                                            synapseRes.phys_doc_lastupdated = DateTime.Now;
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
                                                            synapseRes.social_doc = docObject.status;
                                                            synapseRes.soc_doc_lastupdated = DateTime.Now;
                                                        }
                                                    }
                                                }

                                            }

                                            #endregion Loop Through Outer Documents Object (Should Only Be 1)
                                        }


                                        int save = obj.SaveChanges();
                                        obj.Entry(synapseRes).Reload();

                                        if (save > 0)
                                        {
                                            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - And Successfully updated user's record in SynapseCreateUserRes Table");

                                            // Update Member's DB record
                                            memberObj.IsVerifiedWithSynapse = true;
                                            memberObj.ValidatedDate = DateTime.Now;
                                            memberObj.DateModified = DateTime.Now;
                                        }
                                        else
                                            Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - But FAILED to update user's record in SynapseCreateUserRes Table");
                                    }

                                    #endregion Update Permission in CreateSynapseUserResults Table
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("Common Helper -> sendDocsToSynapseV3 - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
                                                 "MemberID: [" + memId + "], Exception: [" + ex + "]");
                                }

                            }

                        }
                        //        JObject jsonFromSynapse = JObject.Parse(jsonContent);

                        //        JToken synapseUserId = jsonFromSynapse["_id"]["$oid"];

                        //        if (synapseUserId != null)
                        //        {
                        //            var synapseRes = obj.SynapseCreateUserResults.FirstOrDefault(user => user.user_id == synapseUserId.ToString() &&
                        //                                                                                 user.IsDeleted == false);

                        //            if (synapseRes != null)
                        //            {
                        //                var memId = synapseRes.MemberId;

                        //                var memberObj = obj.Members.FirstOrDefault(mem => mem.MemberId == memId &&
                        //                                                                  mem.IsDeleted == false);
                        //                if (memberObj != null)
                        //                {
                        //                    try
                        //                    {
                        //                        // Great, we have found the user!
                        //                        Logger.Info("Common Helper -> sendDocsToSynapseV3 - RESPONSE SUCCESSFUL: Name: [" + jsonFromSynapse["documents"][0]["name"] +
                        //                                    "], Permission: [" + jsonFromSynapse["permission"] + "], CIP_TAG: [" + jsonFromSynapse["extra"]["cip_tag"] +
                        //                                    "], Phys Doc: [" + jsonFromSynapse["doc_status"]["physical_doc"] + "], Virt Doc: [" + jsonFromSynapse["doc_status"]["virtual_doc"] + "]");

                        //                        #region Update Permission in SynapseCreateUserResults Table

                        //                        if (synapseRes != null)
                        //                        {
                        //                            if (jsonFromSynapse["permission"] != null) synapseRes.permission = jsonFromSynapse["permission"].ToString();
                        //                            if (jsonFromSynapse["doc_status"] != null)
                        //                            {
                        //                                synapseRes.physical_doc = jsonFromSynapse["doc_status"]["physical_doc"].ToString();
                        //                                synapseRes.virtual_doc = jsonFromSynapse["doc_status"]["virtual_doc"].ToString();
                        //                            }

                        //                            if (jsonFromSynapse["documents"] != null &&
                        //                                jsonFromSynapse["documents"][0] != null)
                        //                            {
                        //                                #region Loop Through Outer Documents Object (Should Only Be 1)

                        //                                foreach (addDocsResFromSynapse_user_docs doc in synapseResponse.user.documents)
                        //                                {
                        //                                    // Check VIRTUAL_DOCS
                        //                                    if (doc.virtual_docs != null && doc.virtual_docs.Length > 0)
                        //                                    {
                        //                                        short n = 0;
                        //                                        foreach (addDocsResFromSynapse_user_docs_virtualdoc docObject in doc.virtual_docs)
                        //                                        {
                        //                                            n += 1;
                        //                                            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - VIRTUAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                        //                                            if (docObject.document_type == "SSN")
                        //                                            {
                        //                                                synapseRes.virtual_doc = docObject.status;
                        //                                                synapseRes.virt_doc_lastupdated = DateTime.Now;
                        //                                            }
                        //                                        }
                        //                                    }

                        //                                    // Check PHYSICAL_DOCS
                        //                                    if (doc.physical_docs != null && doc.physical_docs.Length > 0)
                        //                                    {
                        //                                        short n = 0;
                        //                                        foreach (addDocsResFromSynapse_user_docs_doc docObject in doc.physical_docs)
                        //                                        {
                        //                                            n += 1;
                        //                                            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - PHYSICAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                        //                                            if (docObject.document_type == "GOVT_ID")
                        //                                            {
                        //                                                synapseRes.physical_doc = docObject.status;
                        //                                                synapseRes.phys_doc_lastupdated = DateTime.Now;
                        //                                            }
                        //                                        }
                        //                                    }

                        //                                    // Check SOCIAL_DOCS
                        //                                    if (doc.social_docs != null && doc.social_docs.Length > 0)
                        //                                    {
                        //                                        short n = 0;
                        //                                        foreach (addDocsResFromSynapse_user_docs_doc docObject in doc.social_docs)
                        //                                        {
                        //                                            n += 1;
                        //                                            Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SOCIAL_DOC #[" + n + "] - Type: [" + docObject.document_type + "], Status: [" + docObject.status + "]");

                        //                                            if (docObject.document_type == "FACEBOOK")
                        //                                            {
                        //                                                synapseRes.social_doc = docObject.status;
                        //                                                synapseRes.soc_doc_lastupdated = DateTime.Now;
                        //                                            }
                        //                                        }
                        //                                    }

                        //                                }

                        //                                #endregion Loop Through Outer Documents Object (Should Only Be 1)
                        //                            }


                        //                            int save = obj.SaveChanges();
                        //                            obj.Entry(synapseRes).Reload();

                        //                            if (save > 0)
                        //                            {
                        //                                Logger.Info("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - And Successfully updated user's record in SynapseCreateUserRes Table");

                        //                                // Update Member's DB record
                        //                                memberObj.IsVerifiedWithSynapse = true;
                        //                                memberObj.ValidatedDate = DateTime.Now;
                        //                                memberObj.DateModified = DateTime.Now;
                        //                            }
                        //                            else
                        //                                Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse - SUCCESS response from Synapse - But FAILED to update user's record in SynapseCreateUserRes Table");
                        //                        }

                        //                        #endregion Update Permission in CreateSynapseUserResults Table
                        //                    }
                        //                    catch (Exception ex)
                        //                    {
                        //                        Logger.Error("Common Helper -> sendDocsToSynapseV3 - EXCEPTION on trying to update User's record in CreateSynapseUserResults Table - " +
                        //                                     "MemberID: [" + memId + "], Exception: [" + ex + "]");
                        //                    }
                        //                }
                        //                else
                        //                    Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Unable to lookup Member by MemberID");
                        //            }
                        //            else
                        //                Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Unable to lookup Synapse User by OID from JSON");
                        //        }
                        //        else
                        //        {
                        //            var error = "WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED: Synapse Result was NULL - Full Synapse Response: [" + jsonFromSynapse + "]";
                        //            Logger.Error(error);
                        //            CommonHelper.notifyCliffAboutError(error);
                        //        }
                        //    }
                        //}
                        //catch (Exception ex)
                        //{
                        //    Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Outer Exception: [" + ex + "]");
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WEBHOOK -> ViewSubscriptionForUserFromSynapse FAILED - Outer Exception: [" + ex + "]");
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
                    JObject jsonfromsynapse = JObject.Parse(jsonContent);

                    JToken transIdFromSyanpse = jsonfromsynapse["_id"]["$oid"];

                    SynSub_Trans transObj = new SynSub_Trans();
                    transObj = JsonConvert.DeserializeObject<SynSub_Trans>(jsonContent);

                    if (transObj != null)
                    {
                        // Lookup the Nooch TransactionID from the Synapse OID
                        var synapseTransObjFromDb = obj.SynapseAddTransactionResults.FirstOrDefault(trans => trans.OidFromSynapse == transObj._id.oid.ToString());
                        noochTransId = synapseTransObjFromDb.TransactionId.ToString();
                        var allTimeLineItems = transObj.timeline;
                        if (allTimeLineItems != null)
                        {
                            foreach (var currentItem in allTimeLineItems)
                            {
                                string note = currentItem.note != null ? currentItem.note.ToString() : null;
                                string status = currentItem.status != null ? currentItem.status.ToString() : null;
                                string status_id = currentItem.status_id != null ? currentItem.status_id.ToString() : null;
                                string status_date = currentItem.date != null ? currentItem.date.date.ToString() : null;

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
                            CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> Synapse Payment CANCELED<h3><br/><br/><p><strong>TransactionID:</strong> " +
                                                               noochTransId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                    }


                    //if (transIdFromSyanpse != null)
                    //{
                    //    // Lookup the Nooch TransactionID from the Synapse OID
                    //    var synapseTransObjFromDb = obj.SynapseAddTransactionResults.FirstOrDefault(trans => trans.OidFromSynapse == transIdFromSyanpse.ToString());
                    //    noochTransId = synapseTransObjFromDb.TransactionId.ToString();

                    //    JToken allTimeLineItems = jsonfromsynapse["timeline"];

                    //    if (allTimeLineItems != null)
                    //    {
                    //        foreach (JToken currentItem in allTimeLineItems)
                    //        {
                    //            string note = currentItem["note"] != null ? currentItem["note"].ToString() : null;
                    //            string status = currentItem["status"] != null ? currentItem["status"].ToString() : null;
                    //            string status_id = currentItem["status_id"] != null ? currentItem["status_id"].ToString() : null;
                    //            string status_date = currentItem["date"] != null ? currentItem["date"]["$date"].ToString() : null;

                    //            TransactionsStatusAtSynapse tas = new TransactionsStatusAtSynapse();
                    //            tas.Nooch_Transaction_Id = noochTransId;
                    //            tas.Transaction_oid = transIdFromSyanpse == null
                    //                ? ""
                    //                : transIdFromSyanpse.ToString();
                    //            tas.status = status;
                    //            tas.status_date = status_date;
                    //            tas.status_id = status_id;
                    //            tas.status_note = note;

                    //            obj.TransactionsStatusAtSynapses.Add(tas);
                    //            obj.SaveChanges();
                    //        }
                    //    }

                    //    // Checking most recent status to update Transactions Table b/c Timeline []
                    //    // may have multiple statuses and that may be confusing to update to most recent status

                    //    JToken mostRecentToken = jsonfromsynapse["recent_status"];

                    //    if (mostRecentToken["status"] != null)
                    //    {
                    //        // updating transcaction status in transactions table
                    //        if (!String.IsNullOrEmpty(mostRecentToken["status"].ToString()) &&
                    //            !String.IsNullOrEmpty(noochTransId))
                    //        {
                    //            Guid transGuid = Utility.ConvertToGuid(noochTransId);
                    //            Transaction t = obj.Transactions.FirstOrDefault(trans => trans.TransactionId == transGuid);

                    //            if (t != null)
                    //            {
                    //                t.SynapseStatus = mostRecentToken["status"].ToString();
                    //                obj.SaveChanges();
                    //            }
                    //        }

                    //        // Notify Cliff if the status is "CANCELED"
                    //        if (mostRecentToken["status"].ToString() == "CANCELED")
                    //            CommonHelper.notifyCliffAboutError("<h3>WEBHOOK -> Synapse Payment CANCELED<h3><br/><br/><p><strong>TransactionID:</strong> " +
                    //                                               noochTransId + " <p>JSON from Synapse:</p><p>" + jsonContent + "</p>");
                    //    }
                    //}
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
        public void ViewSubscriptionForUserNodeSynapse()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;

            Logger.Info("WEBHOOK -> ViewSubscriptionForNodeFromSynapse Fired - node_id: [ " + "Commented oid" +
                            " ], Content: [ " + jsonContent + " ]");

            try
            {
                using (NOOCHEntities obj = new NOOCHEntities())
                {

                    SynSub_Node nodeObj = new SynSub_Node();
                    nodeObj = JsonConvert.DeserializeObject<SynSub_Node>(jsonContent);
                    if (nodeObj==null && nodeObj._id.oid != null)
                    {
                        var synapseRes = obj.SynapseBanksOfMembers.FirstOrDefault(node => node.oid == CommonHelper.GetDecryptedData(nodeObj._id.oid) && node.IsDefault == true);
                        if (synapseRes != null) {

                            // Great, we have found the Node!
                            Logger.Info("Common Helper -> sendDocsToSynapseV3 - RESPONSE SUCCESSFUL:Bank Name: [" + nodeObj.info.bank_name +
                                        "], Permission: [" + nodeObj.allowed + "] ");

                            #region Update Permission in SynapseBanksOfMembers Table
                            synapseRes.is_active = nodeObj.is_active;
                            synapseRes.allowed = nodeObj.allowed;
                            
                            #endregion Update Permission in SynapseBanksOfMembers Table

                        }

                    }
                }
            }
            catch (Exception exc){
                Logger.Error("WEBHOOK -> ViewSubscriptionForNodeFromSynapse FAILED - Outer Exception: [" + exc + "]");
            }



        }

    }
}