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
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetTransactionStatusFromSynapse [ WebHook ] failed to save in db for TransactionId : [ " + transId + " ]. At [ " + DateTime.Now + " ]. Exception -> [ " + ex + " ].");
            }
        }


        [HttpPost]
        [ActionName("VerifyPhoneNumber")]
        public void VerifyPhoneNumber()
        {
            HttpContent requestContent = Request.Content;
            string jsonContent = requestContent.ReadAsStringAsync().Result;

            Logger.Info("WebHook Cntrlr -> VerifyPhoneNumber Fired - Request Body: [ " + jsonContent + " ]");

            JObject jsonfromsynapse = JObject.Parse(jsonContent);

            JToken FromNumber = jsonfromsynapse["From"];
            JToken MessageBody = jsonfromsynapse["Body"];

            if (FromNumber != null && MessageBody != null)
            {
                string From = FromNumber.ToString();
                string Body = MessageBody.ToString();

                bool isOk = false;

                string memberId = "Not Set";
                string firstName = "";
                string lastName = "";
                var memberPhone = "Not Set";

                if (Body.Trim().ToLower() == "go" && !String.IsNullOrEmpty(From))
                {
                    try
                    {
                        using (var noochConnection = new NOOCHEntities())
                        {
                            string toMatch = From.Trim().Substring(2, From.Length - 2);
                            string toMatch2 = From.Trim();

                            var noochMember = noochConnection.Members.FirstOrDefault(m => m.IsDeleted == false &&
                                                                                    (m.ContactNumber == toMatch2 ||
                                                                                    m.ContactNumber == toMatch));

                            if (noochMember != null)
                            {
                                // Update user's DB record
                                noochMember.IsVerifiedPhone = true;
                                noochMember.PhoneVerifiedOn = DateTime.Now;
                                noochMember.DateModified = DateTime.Now;

                                int value = noochConnection.SaveChanges();

                                memberId = Convert.ToString(noochMember.MemberId);
                                firstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.FirstName));
                                lastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(noochMember.LastName));
                                memberPhone = CommonHelper.FormatPhoneNumber(noochMember.ContactNumber);

                                if (value > 0)
                                {
                                    isOk = true;

                                    #region Update Tenant Record If For A Tenant

                                    if (noochMember.Type == "Tenant")
                                    {
                                        try
                                        {
                                            var tenantdObj = noochConnection.Tenants.FirstOrDefault(t => t.MemberId == noochMember.MemberId && t.IsDeleted == false);

                                            if (tenantdObj != null)
                                            {
                                                Logger.Info("WebHook Cntrlr -> VerifyPhoneNumber -> This is a TENANT - About to update Tenants Table " +
                                                            "MemberID: [" + memberId + "]");

                                                tenantdObj.IsPhoneVerfied = true;
                                                tenantdObj.DateModified = DateTime.Now;

                                                int saveChangesToTenant = noochConnection.SaveChanges();

                                                if (saveChangesToTenant > 0)
                                                {
                                                    Logger.Info("WebHook Cntrlr -> VerifyPhoneNumber -> Saved changes to TENANT table successfully - " +
                                                                "MemberID: [" + memberId + "]");
                                                }
                                                else
                                                {
                                                    Logger.Error("WebHook Cntrlr -> VerifyPhoneNumber -> FAILED to save changes to TENANT table - " +
                                                                 "MemberID: [" + memberId + "]");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("WebHook Cntrlr -> VerifyPhoneNumber -> EXCEPTION on checking if this user is a TENANT - " +
                                                         "MemberID: [" + memberId + "], [Exception: " + ex + "]");
                                        }
                                    }

                                    #endregion Update Tenant Record If For A Tenant
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("WebHook Cntrlr -> VerifyPhoneNumber FAILED -> EXCEPTION on checking if this user is a TENANT - " +
                                     "MemberID: [" + memberId + "], [Exception: " + ex + "]");
                        isOk = false;
                    }

                    string to = (From.Contains('+')) ? From.Trim() : "+" + From.Trim();

                    if (isOk)
                    {
                        Utility.SendSMS(to, "Thanks, " + firstName + "! Your phone number has been verified - have a great day!");

                        Logger.Info("WebHook Cntrlr -> VerifyPhoneNumber -> Success: Response received from user successfully & Phone is now Verified - " +
                                    "Phone #: [" + memberPhone + "], " +
                                    "Name: " + firstName + " " + lastName + "], " +
                                    "MemberID: [" + memberId + "]");
                    }
                    else
                    {
                        Logger.Error("WebHook Cntrlr -> VerifyPhoneNumber FAILED - Error #277. [Phone #: " + memberPhone + "], " +
                                     "[Name: " + firstName + " " + lastName + "],  [MemberId: " + memberId + "]");

                        Utility.SendSMS(to, "Whoops, something went wrong. Please try again or contact support@nooch.com for help.");
                    }
                }
                else
                {
                    Logger.Error("SMSResponse -> FAILED --> Empty message or invalid format data received.");
                }
            }

        }
    }
}