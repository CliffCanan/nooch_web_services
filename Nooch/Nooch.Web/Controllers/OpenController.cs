﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Nooch.Common;
using Nooch.Data;

namespace Nooch.Web.Controllers
{
    public class OpenController : Controller
    {

        public ActionResult VerifyPhoneNumber()
        {

            var FromNumber = Request.QueryString["From"];
            var MessageBody = Request.QueryString["Body"];

            if (!String.IsNullOrEmpty(FromNumber) && !String.IsNullOrEmpty(MessageBody))
            {
                Logger.Info("Open Cntlr -> VerifyPhoneNumber - Incoming message: [" + MessageBody + "]");

                var From = FromNumber;
                var body = MessageBody;
                var isOk = false;
                var memberId = "Not Set";
                var firstName = "";
                var lastName = "";
                var memberPhone = "Not Set";

                if (!String.IsNullOrEmpty(From))
                {
                    var to = (From.Contains('+')) ? From.Trim() : "+" + From.Trim();

                    if (body.Trim().ToLower() == "go")
                    {
                        #region Correct Message Received

                        try
                        {
                            using (var noochConnection = new NOOCHEntities())
                            {
                                var toMatch = From.Trim().Substring(2, From.Length - 2);
                                var toMatch2 = From.Trim();

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
                                                    Logger.Info("OpenController  -> VerifyPhoneNumber -> This is a TENANT - About to update Tenants Table " +
                                                                "MemberID: [" + memberId + "]");

                                                    tenantdObj.IsPhoneVerfied = true;
                                                    tenantdObj.DateModified = DateTime.Now;

                                                    int saveChangesToTenant = noochConnection.SaveChanges();

                                                    if (saveChangesToTenant > 0)
                                                    {
                                                        Logger.Info("OpenController -> VerifyPhoneNumber -> Saved changes to TENANT table successfully - " +
                                                                    "MemberID: [" + memberId + "]");
                                                    }
                                                    else
                                                    {
                                                        Logger.Error("OpenController-> VerifyPhoneNumber -> FAILED to save changes to TENANT table - " +
                                                                     "MemberID: [" + memberId + "]");
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error("OpenController-> VerifyPhoneNumber -> EXCEPTION on checking if this user is a TENANT - " +
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
                            Logger.Error("Open Cntlr -> VerifyPhoneNumber FAILED -> EXCEPTION on checking if this user is a TENANT - " +
                                         "MemberID: [" + memberId + "], Exception: [" + ex + "]");
                            isOk = false;
                        }

                        if (isOk)
                        {
                            Utility.SendSMS(to, "Thanks, " + firstName + "! Your phone number has been verified - have a great day!");

                            Logger.Info("Open Cntlr  -> VerifyPhoneNumber -> Success: Response received from user successfully & Phone is now Verified - " +
                                        "Phone #: [" + memberPhone + "], " +
                                        "Name: " + firstName + " " + lastName + "], " +
                                        "MemberID: [" + memberId + "]");
                        }
                        else
                        {
                            Logger.Error("Open Cntlr  Cntrlr -> VerifyPhoneNumber FAILED - Phone #: [" + memberPhone + "], " +
                                         "Name: [" + firstName + " " + lastName + "], MemberID: [" + memberId + "]");

                            Utility.SendSMS(to, "Whoops, something went wrong. Please try again or contact support@nooch.com for help.");
                        }

                        #endregion Correct Message Received
                    }
                    else if (body.Length > 0)
                    {
                        Utility.SendSMS(to, "Hmm, didn't quite get that! Please try again :-)");

                        Logger.Error("Open Cntlr  -> VerifyPhoneNumber -> Failed: Incoming message was [" + body + "] - " +
                                    "Phone #: [" + FromNumber + "]");
                    }
                    else
                    {
                        Logger.Error("Open Cntlr  -> VerifyPhoneNumber FAILED -> Empty message or invalid format data received.");
                    }
                }
            }
            return View();
        }
    }
}