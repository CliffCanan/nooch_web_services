using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nooch.Common;
using Nooch.Common.Entities;
using Nooch.Common.Entities.MobileAppInputEntities;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Resources;
using Nooch.Data;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using Nooch.Common.Entities.SynapseRelatedEntities;

namespace Nooch.DataAccess
{
    public class TransactionsDataAccess
    {
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly NOOCHEntities _dbContext = null;

        public TransactionsDataAccess()
        {
            _dbContext = new NOOCHEntities();
        }


        public PendingTransCoutResult GetMemberPendingTransCount(string MemberId)
        {
            Logger.Info("TDA -> GetMemberPendingTransCount - [MemberId: " + MemberId + "]");

            try
            {
                PendingTransCoutResult r = new PendingTransCoutResult();

                var memberId = Utility.ConvertToGuid(MemberId);


                // Get all transactions where Transaction Type could be Transfer, Request or disputed AND
                // Transaction Status is Pending and
                // Dispute status is null or not resolved

                // 'TRANSFER' :  5dt4HUwCue532sNmw3LKDQ==
                // 'REQUEST'  :  T3EMY1WWZ9IscHIj3dbcNw==
                // 'DISPUTED' :  +C1+zhVafHdXQXCIqjU/Zg==
                // 'RESOLVED' :  RZKgHECeAxJW/E+/IRj3Dg==
                // 'INVITE'   :  DrRr1tU1usk7nNibjtcZkA==


                List<Transaction> allPendingRequestsResult = _dbContext.Transactions.Where(
                    t => t.Member1.MemberId == memberId &&
                         t.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" &&
                                              t.TransactionStatus == "Pending" &&
                                             (t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==" || t.DisputeStatus == null)
                    ).ToList();

                int pendingRequestsSent = 0;
                int pendingRequestsReceived = 0;
                int pendingInvitesCount = 0;
                int pendingDisputesTotal = 0;

                if (allPendingRequestsResult != null)
                {
                    pendingRequestsSent = allPendingRequestsResult.Count;
                }

                // Getting count OF REQUESTS where member is RECIPIENT

                var allPendingRequestsReceivedResult = _dbContext.Transactions.Where(
                    t => t.Member.MemberId == memberId &&
                         t.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" &&
                         t.TransactionStatus == "Pending" &&
                         (t.InvitationSentTo == null || t.InvitationSentTo == "") &&
                         (t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==" || t.DisputeStatus == null)).ToList();

                // Getting count of TRANSFERS/INVITES to new users


                var allPendingInvitesCount = _dbContext.Transactions.Where(
                    t => t.Member.MemberId == memberId &&
                         t.TransactionStatus == "Pending" &&
                         t.InvitationSentTo != null &&
                         (t.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                          t.TransactionType == " 5dt4HUwCue532sNmw3LKDQ==") &&
                         (t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==" || t.DisputeStatus == null)).ToList();
                if (allPendingInvitesCount != null)
                {
                    pendingInvitesCount = allPendingInvitesCount.Count;
                }

                // Getting count of DISPUTES that are still pending (not 'Resolved')


                var allDisputeResult = _dbContext.Transactions.Where(
                    t =>
                        (t.Member1.MemberId == memberId || t.Member.MemberId == memberId) &&
                        t.TransactionType == "+C1+zhVafHdXQXCIqjU/Zg==" &&
                        t.TransactionStatus == "Pending" &&
                        t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==").ToList();
                if (allDisputeResult != null)
                {
                    pendingDisputesTotal = allDisputeResult.Count;
                }


                if (allPendingRequestsReceivedResult != null)
                {
                    pendingRequestsReceived = allPendingRequestsReceivedResult.Count;
                }



                r.pendingRequestsSent = pendingRequestsSent.ToString();
                r.pendingRequestsReceived = pendingRequestsReceived.ToString();
                r.pendingInvitationsSent = pendingInvitesCount.ToString();
                r.pendingDisputesNotSolved = pendingDisputesTotal.ToString();

                return r;

            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> GetMemberPendingTransCount FAILED - [MemberID: " + MemberId + "], [Exception: " + ex + "]");
                return null;
            }
        }



        #region Transaction and money matters
        /// <summary>
        /// For cancelling a REQUEST sent to a NON-NOOCH USER.
        /// Called from the cancel request landing page code-behind: CancelRequest.aspx.cs
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        public string CancelMoneyRequestForNonNoochUser(string TransactionId, string MemberId)
        {
            try
            {
                Guid memGuid = Utility.ConvertToGuid(MemberId);
                Guid transid = Utility.ConvertToGuid(TransactionId);

                Logger.Info("TDA -> CancelMoneyRequestForNonNoochUser Initiated - " +
                                       "TransactionID: [" + TransactionId + "], MemberId: [" + MemberId + "]");




                var res = _dbContext.Transactions.FirstOrDefault(m => m.Member.MemberId == memGuid && m.TransactionId == transid
                    && m.TransactionStatus == "Pending" && (m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA=="));


                if (res != null)
                {
                    res.TransactionStatus = "Cancelled";
                    int i = _dbContext.SaveChanges();
                    _dbContext.Entry(res).Reload();

                    if (i > 0)
                    {
                        Logger.Info("TDA -> CancelMoneyRequestForNonNoochUser - Transaction Cancelled SUCCESSFULLY - " +
                                               "TransactionID: [" + TransactionId + "], Member that Cancelled: [" + MemberId + "]");

                        try
                        {
                            string memo = "";
                            if (!string.IsNullOrEmpty(res.Memo))
                            {
                                memo = "For " + res.Memo;
                            }

                            // Updated, now send mail to user who made the request
                            string transDate = Convert.ToDateTime(res.TransactionDate).ToString("MMM dd yyyy");
                            string requesterFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName));
                            string requesterLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.LastName));

                            string amount = res.Amount.ToString("n2");

                            var fromAddress = Utility.GetValueFromConfig("transfersMail");

                            string recipientUserPhoneOrEmail = "";
                            string phoneNumStripped = "";

                            if (res.IsPhoneInvitation == true &&
                                res.PhoneNumberInvited != null)
                            {
                                phoneNumStripped = CommonHelper.GetDecryptedData(res.PhoneNumberInvited);
                                recipientUserPhoneOrEmail = CommonHelper.FormatPhoneNumber(phoneNumStripped);
                            }
                            else
                            {
                                recipientUserPhoneOrEmail = CommonHelper.GetDecryptedData(res.InvitationSentTo);
                            }

                            var tokens = new Dictionary<string, string>
                        {
                            {Constants.PLACEHOLDER_FIRST_NAME, requesterFirstName},
                            {Constants.PLACEHOLDER_Recepient_Email, recipientUserPhoneOrEmail},
                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, amount},
                            {Constants.PLACEHOLDER_DATE, transDate},
                            {Constants.MEMO, memo}
                        };

                            var toAddress = CommonHelper.GetDecryptedData(res.Member.UserName);

                            try
                            {
                                Utility.SendEmail("requestCancelledToSender", fromAddress,
                                    toAddress, null, "Your Nooch request was cancelled", null, tokens, null, null, null);

                                Logger.Info("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToSender email sent " +
                                                       "to Requester: [" + toAddress + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToSender email NOT sent " +
                                                       "to Requester: [" + toAddress + "], Exception: [" + ex + "]");
                            }

                            // Now send SMS to request recipient (the non-user)
                            if (res.IsPhoneInvitation == true &&
                                res.PhoneNumberInvited != null)
                            {
                                string MessageText = requesterFirstName + " " + requesterLastName +
                                                     " just cancelled a payment request to you for $" + amount + ". You're off the hook!";

                                try
                                {
                                    Utility.SendSMS(phoneNumStripped, MessageText);

                                    Logger.Info("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToRecipient SMS sent to [" +
                                                           recipientUserPhoneOrEmail + "] successfully");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToRecipient SMS NOT sent " +
                                                           "to Request Recipient: [" + recipientUserPhoneOrEmail + "], Exception: [" + ex + "]");
                                }
                            }
                            else
                            {
                                var tokens2 = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, recipientUserPhoneOrEmail},
                                {Constants.PLACEHOLDER_LAST_NAME, requesterFirstName + " " + requesterLastName},
                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, amount},
                                {Constants.MEMO, memo}
                            };

                                var toAddress2 = recipientUserPhoneOrEmail;

                                try
                                {
                                    Utility.SendEmail("requestCancelledToRecipient", fromAddress,
                                                                toAddress2, null, requesterFirstName + " " + requesterLastName +
                                                                " cancelled a payment request to you", null, tokens2, null, null, null);

                                    Logger.Info("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToRecipient email sent to [" +
                                                           toAddress + "] successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToRecipient email NOT sent to [" +
                                                           toAddress + "], Exception: [" + ex + "]");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser EXCEPTION - Failure while sending notifications - [" +
                                                   "TransactionID: [" + TransactionId + "], Exception: [" + ex + "]");
                        }

                        return "Transaction Cancelled Successfully";
                    }
                    else
                    {
                        Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser FAILED - Failed to save updates to DB - " +
                                               "TransactionID: [" + TransactionId + "]");

                        return "Something went wrong while updating transaction, please retry.";
                    }
                }
                else
                {
                    Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser FAILED - Transaction Not Found - TransactionID: [" +
                                           TransactionId + "]");

                    return "No Such Transaction Found.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser FAILED - Outer Exception - TransactionID: [" +
                                       TransactionId + "], Exception: [" + ex + "]");

                return "Exception: " + ex.Message.ToString();
            }
        }




        /// <summary>
        /// For Cancelling a REQUEST sent to an EXISTING Nooch user. Called only from the iOS app.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        public string CancelMoneyRequestForExistingNoochUser(string TransactionId, string MemberId)
        {
            try
            {
                Guid memGuid = Utility.ConvertToGuid(MemberId);
                Guid transid = Utility.ConvertToGuid(TransactionId);



                Logger.Info("TDA -> CancelMoneyRequestForExistingNoochUser Initiated for: [" + MemberId + "]");

                var res = _dbContext.Transactions.FirstOrDefault(m => m.Member1.MemberId == memGuid && m.TransactionId == transid
                    && m.TransactionStatus == "Pending" && (m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA=="));

                if (res != null)
                {
                    res.TransactionStatus = "Cancelled";
                    int i = _dbContext.SaveChanges();

                    if (i > 0)
                    {
                        _dbContext.Entry(res).Reload();
                        string memo = "";
                        if (!string.IsNullOrEmpty(res.Memo))
                        {
                            memo = "For " + res.Memo.ToString();
                        }
                        // updated, now send email to user who made the request
                        var tokens = new Dictionary<string, string>
												 {
													 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.FirstName))},
													 {Constants.PLACEHOLDER_Recepient_Email, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName)) + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.LastName))},
													 {Constants.PLACEHOLDER_TRANSFER_AMOUNT, res.Amount.ToString("n2")},
													 {Constants.PLACEHOLDER_DATE, Convert.ToDateTime(res.TransactionDate).ToString("MMM dd yyyy")},
													 {Constants.MEMO, memo}
												 };

                        // for TransferReceived email notification       
                        string adminUserName = Utility.GetValueFromConfig("transfersMail");
                        var fromAddress = adminUserName;
                        var toAddress = CommonHelper.GetDecryptedData(res.Member1.UserName.ToString());
                        try
                        {
                            // email notification
                            Utility.SendEmail("requestCancelledToSender", fromAddress, toAddress, null, "Nooch payment request to " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName)) + " cancelled", null, tokens, null, null, null);
                            Logger.Info("CancelMoneyRequestForExistingNoochUser --> requestCancelledToSender email sent to Sender: [" + toAddress + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Error("CancelMoneyRequestForExistingNoochUser --> requestCancelledToSender email NOT sent to [" + toAddress + "]. Problem occurred in sending mail.");
                        }

                        var tokens2 = new Dictionary<string, string>
												 {
													 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName))},
													 {Constants.PLACEHOLDER_LAST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.FirstName))+" "+CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.LastName))},
													 {Constants.PLACEHOLDER_TRANSFER_AMOUNT, res.Amount.ToString("n2")},
													 {Constants.MEMO, memo}
												 };

                        var toAddress2 = CommonHelper.GetDecryptedData(res.Member.UserName);
                        try
                        {
                            Utility.SendEmail("requestCancelledToRecipient", fromAddress, toAddress2, null, "Nooch payment request cancelled", null, tokens2, null, null, null);
                            Logger.Info("CancelMoneyRequestForExistingNoochUser --> requestCancelledToRecipient email sent to [" + toAddress2 + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Error("CancelMoneyRequestForExistingNoochUser --> requestCancelledToRecipient email NOT sent to [" + toAddress2 + "]. Problem occurred in sending mail.");
                        }

                        return "Transaction Cancelled Successfully.";
                    }
                    else
                    {
                        // not updated returning error
                        return "Something went wrong while updating transaction, please retry.";
                    }
                }
                else
                {
                    return "No Such Transaction Found.";
                }
            }
            catch (Exception ex)
            {
                return "" + ex.ToString();
            }

        }




        public string CancelMoneyTransferForSender(string TransactionId, string MemberId)
        {
            try
            {
                Guid memGuid = Utility.ConvertToGuid(MemberId);
                Guid transGuid = Utility.ConvertToGuid(TransactionId);

                Logger.Info("TransactionDataAccess - CancelMoneyTransferForSender[ CancelMoneyTransferForSender:" + MemberId + "].");



                var res = _dbContext.Transactions.FirstOrDefault(m => m.TransactionStatus == "Pending"
                    && m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" && m.TransactionId == transGuid && m.Member.MemberId == memGuid);
                if (res != null)
                {
                    // found and update
                    res.TransactionStatus = "Cancelled";
                    int i = _dbContext.SaveChanges();
                    if (i > 0)
                    {
                        _dbContext.Entry(res).Reload();
                        // updated
                        // send mail to Money Sender
                        var tokens = new Dictionary<string, string>
												 {
													 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName))+" "+ CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.LastName))},
													 {Constants.TRANSACTION_ID, CommonHelper.GetDecryptedData(res.Member1.UserName)},
													 {Constants.PLACEHOLDER_TRANSFER_AMOUNT, res.Amount.ToString("n2")}													 
												 };

                        string adminUserName = Utility.GetValueFromConfig("transfersMail");
                        var fromAddress = adminUserName;
                        var toAddress = CommonHelper.GetDecryptedData(res.Member.UserName.ToString());
                        try
                        {
                            // email notification
                            Utility.SendEmail("transferCancelledToSender", fromAddress, toAddress, null, "Nooch transfer", null, tokens, null, null, null);
                            Logger.Info("CancelMoneyTransferForSender - CancelMoneyTransferForSender status mail sent to [" + toAddress + "].");
                        }
                        catch (Exception)
                        {
                            Logger.Error("CancelMoneyTransferForSender - CancelMoneyTransferForSender mail not sent to [" + toAddress + "]. Problem occurred in sending mail.");
                        }

                        // Transfer Cancelled to recipient mail
                        string s22 = res.Amount.ToString("n2");
                        string[] s32 = s22.Split('.');

                        var tokens2 = new Dictionary<string, string>
												 {
													 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.FirstName))+" "+CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.LastName))},
													 {Constants.TRANSACTION_ID, CommonHelper.GetDecryptedData(res.Member.UserName)},
													 {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0]},
													 {Constants.PLACEHLODER_CENTS, s32[1]}
												 };

                        var toAddress2 = CommonHelper.GetDecryptedData(res.InvitationSentTo.ToString());
                        try
                        {
                            // email notification
                            Utility.SendEmail("transferCancelledToRecipient", fromAddress, toAddress2, null, "Nooch transfer", null, tokens2, null, null, null);
                            Logger.Info("TransferReceived - TransferReceived status mail sent to [" + toAddress + "].");
                        }
                        catch (Exception)
                        {
                            Logger.Error("TransferReceived - TransferReceived mail not sent to [" + toAddress + "]. Problem occured in sending mail.");
                        }

                        return "Transaction Cancelled Successfully.";
                    }
                    else
                    {
                        // not updated returning error
                        return "Something went wrong while updating transaction, please retry.";
                    }
                }
                else
                {
                    // not found
                    return "No Such Transaction Found.";
                }
            }
            catch (Exception ex)
            {
                return "" + ex.ToString();
            }
        }





        /// <summary>
        /// For Cancelling an INVITE (Send Money) to a NON-NOOCH user. Called only from the iOS app.
        /// </summary>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        public string CancelMoneyTransferToNonMemberForSender(string TransactionId, string MemberId)
        {
            try
            {
                Logger.Info("TDA -> CancelMoneyTransferToNonMemberForSender Initiated - " +
                                       "MemberID: [" + MemberId + "], TransID: [" + TransactionId + "]");

                Guid memGuid = Utility.ConvertToGuid(MemberId);
                Guid transid = Utility.ConvertToGuid(TransactionId);


                var transObj = _dbContext.Transactions.FirstOrDefault(m => m.TransactionStatus == "pending" && m.Member.MemberId == memGuid
                    && m.TransactionId == transid && (m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA=="))
                ;


                if (transObj != null)
                {
                    transObj.TransactionStatus = "Cancelled";
                    int i = _dbContext.SaveChanges();

                    if (i > 0)
                    {
                        _dbContext.Entry(transObj).Reload();
                        string s22 = transObj.Amount.ToString("n2");
                        string[] s32 = s22.Split('.');
                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transObj.Member.LastName));
                        string senderFullName = senderFirstName + " " + senderFirstName;
                        string nonNoochRecipient = CommonHelper.GetDecryptedData(transObj.InvitationSentTo);

                        var fromAddress = Utility.GetValueFromConfig("transfersMail");
                        var toAddress = CommonHelper.GetDecryptedData(transObj.Member.UserName);

                        bool isForRentScene = false;
                        //var logoToDisplay = "noochlogo";

                        if (transObj.Member.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                        {
                            isForRentScene = true;
                            senderFirstName = "Rent Scene";
                            senderLastName = "";
                            senderFullName = "Rent Scene";
                        }

                        fromAddress = isForRentScene ? "payments@rentscene.com"
                                                     : Utility.GetValueFromConfig("transfersMail");


                        if (transObj.IsPhoneInvitation == true &&
                            transObj.PhoneNumberInvited != null &&
                            transObj.InvitationSentTo == null)
                        {
                            nonNoochRecipient = CommonHelper.GetDecryptedData(transObj.PhoneNumberInvited.Trim());
                            nonNoochRecipient = CommonHelper.FormatPhoneNumber(nonNoochRecipient);
                        }
                        else
                        {
                            nonNoochRecipient = CommonHelper.GetDecryptedData(transObj.InvitationSentTo);
                        }

                        // Send email to Sender
                        var tokens = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
								{Constants.TRANSACTION_ID, nonNoochRecipient},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0]},
								{Constants.PLACEHLODER_CENTS, s32[1]}
							};

                        try
                        {
                            Utility.SendEmail("transferCancelledToSender", fromAddress, toAddress,
                                                        null, "Your payment has been cancelled", null, tokens, null, null, null);

                            Logger.Info("TDA -> CancelMoneyTransferToNonMemberForSender - transferCancelledToSender email sent to [" + toAddress + "].");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> CancelMoneyTransferToNonMemberForSender - transferCancelledToSender email not sent to [" + toAddress + "], " +
                                                   "Exception: [" + ex + "]");
                        }


                        if (transObj.IsPhoneInvitation != null &&
                            transObj.IsPhoneInvitation == true &&
                            transObj.PhoneNumberInvited != null)
                        {
                            string MessageToRecepient = senderFullName + " just cancelled a $" + s32[0].ToString() + "." + s32[1].ToString() +
                                " payment to you on Nooch. Sorry about that!";

                            // removing extra stuff from phone number
                            nonNoochRecipient = nonNoochRecipient.Replace("(", "");
                            nonNoochRecipient = nonNoochRecipient.Replace(")", "");
                            nonNoochRecipient = nonNoochRecipient.Replace(" ", "");
                            nonNoochRecipient = nonNoochRecipient.Replace("-", "");

                            string s = Utility.SendSMS(nonNoochRecipient, MessageToRecepient);
                        }
                        else
                        {
                            var tokens2 = new Dictionary<string, string>
								{
									{Constants.PLACEHOLDER_FIRST_NAME,  "there"}, // Cliff (1/18/16): this had been using the email/phone # of the recipient since we don't know their name (although we might sometimes if sent by Rent Scene, but we're not currently storing that name), so changing to "there" as in "Hi there,..."
									{Constants.TRANSACTION_ID, senderFirstName + " " + senderLastName},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0]},
									{Constants.PLACEHLODER_CENTS, s32[1]}
								};

                            var toAddress2 = (transObj.InvitationSentTo == null) ? CommonHelper.GetDecryptedData(transObj.Member1.UserName) : CommonHelper.GetDecryptedData(transObj.InvitationSentTo);

                            try
                            {
                                Utility.SendEmail("transferCancelledToRecipient", fromAddress, toAddress2, null,
                                        senderFullName + " cancelled a $" + s32[0] + "." + s32[1] + " payment to you",
                                        null, tokens2, null, null, null);

                                Logger.Info("TDA -> CancelMoneyTransferToNonMemberForSender - transferCancelledToRecipient email sent to [" + toAddress + "] successfully");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> CancelMoneyTransferToNonMemberForSender - transferCancelledToRecipient email NOT sent to [" + toAddress + "], " +
                                                       "Exception: [" + ex + "]");
                            }
                        }

                        return "Transaction Cancelled Successfully.";
                    }
                    else
                    {
                        // not updated returning error
                        Logger.Error("TDA -> CancelMoneyTransferToNonMemberForSender FAILED - Could not save updates to DB - TransID: [" + TransactionId + "]");
                        return "Something went wrong while updating transaction, please retry.";
                    }
                }
                else
                {
                    // Transaction Not Found
                    return "No Such Transaction Found.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> CancelMoneyTransferToNonMemberForSender FAILED (Outer Exception) - TransID: [" + TransactionId + "]");
                return "" + ex.Message;
            }
        }



        /// <summary>
        /// To Send Reminder Emails for requests and invite transaction types.
        /// </summary>
        /// <param name="ReminderType"></param>
        /// <param name="TransactionId"></param>
        /// <param name="MemberId"></param>
        /// <returns></returns>
        public string SendTransactionReminderEmail(string ReminderType, string TransactionId, string MemberId)
        {
            Logger.Info("TDA -> SendTransactionReminderEmail Initiated. MemberID: [" + MemberId + "], " +
                                   "TransactionId: [" + TransactionId + "], " +
                                   "ReminderType: [" + ReminderType + "]");

            try
            {

                var TransId = Utility.ConvertToGuid(TransactionId);
                var MemId = Utility.ConvertToGuid(MemberId);




                if (ReminderType == "RequestMoneyReminderToNewUser" ||
                    ReminderType == "RequestMoneyReminderToExistingUser")
                {
                    #region Requests - Both Types


                    var trans = _dbContext.Transactions.FirstOrDefault(m => m.Member1.MemberId == MemId && m.TransactionId == TransId
                        && m.TransactionStatus == "Pending" && m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="
                        );



                    if (trans != null)
                    {
                        _dbContext.Entry(trans).Reload();
                        #region Setup Common Variables

                        string fromAddress = Utility.GetValueFromConfig("transfersMail");

                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));

                        //string payLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                        //                               "trans/payRequest.aspx?TransactionId=" + trans.TransactionId);
                        string payLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
 
                                                       "Nooch/payRequest?TransactionId=" + trans.TransactionId);
 
                        string s22 = trans.Amount.ToString("n2");
                        string[] s32 = s22.Split('.');

                        string memo = "";
                        if (!string.IsNullOrEmpty(trans.Memo))
                        {
                            if (trans.Memo.Length > 3)
                            {
                                string firstThreeChars = trans.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = trans.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + trans.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + trans.Memo.ToString();
                            }
                        }


                        bool isForRentScene = false;

                        if (trans.Member.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                        {
                            isForRentScene = true;
                            senderFirstName = "Rent Scene";
                            senderLastName = "";

                            payLink = payLink + "&rs=1";
                        }

                        #endregion Setup Common Variables


                        #region RequestMoneyReminderToNewUser

                        // Now check if this transaction was sent via Email or Phone Number (SMS)
                        if (trans.InvitationSentTo != null) // 'InvitationSentTo' field only used if it's an Email Transaction
                        {
                            #region If invited by email

                            //string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                            //                                  "trans/rejectMoney.aspx?TransactionId=" + trans.TransactionId +
                            //                                  "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                            //                                  "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                            //                                  "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
 
                                                              "Nooch/rejectMoney?TransactionId=" + trans.TransactionId +
 
                                                              "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                              "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            var tokens2 = new Dictionary<string, string>
                                {
								    {Constants.PLACEHOLDER_FIRST_NAME, senderFirstName + " " + senderLastName},
									{Constants.PLACEHOLDER_NEWUSER, ""},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
									{Constants.PLACEHLODER_CENTS, s32[1].ToString()},
									{Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
									{Constants.PLACEHOLDER_TRANSACTION_DATE, Convert.ToDateTime(trans.TransactionDate).ToString("MMM dd yyyy")},
									{Constants.MEMO, memo},
									{Constants.PLACEHOLDER_PAY_LINK, payLink}
								};

                            var templateToUse = isForRentScene ? "requestReminderToNewUser_RentScene"
                                                               : "requestReminderToNewUser";

                            var toAddress = CommonHelper.GetDecryptedData(trans.InvitationSentTo);

                            // Sending Request reminder email to Non-Nooch user
                            try
                            {
                                Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                            senderFirstName + " " + senderLastName + " requested " + "$" + s22.ToString() + " - Reminder",
                                                            null, tokens2, null, null, null);

                                Logger.Info("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                            }

                            return "Reminder mail sent successfully.";

                            #endregion If invited by email
                        }
                        else if (trans.IsPhoneInvitation == true && trans.PhoneNumberInvited != null)
                        {
                            #region If Invited by SMS

                            string RejectShortLink = "";
                            string AcceptShortLink = "";

                            //string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                            //                                  "trans/rejectMoney.aspx?TransactionId=" + trans.TransactionId +
                            //                                  "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                            //                                  "&LinkSource=Um3I3RNHEGWqKM9MLsQ1lg==" +
                            //                                  "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
 
                                                              "Nooch/rejectMoney?TransactionId=" + trans.TransactionId +
 
                                                              "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                              "&LinkSource=Um3I3RNHEGWqKM9MLsQ1lg==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");


                            #region Shortening URLs for SMS

                            string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                            // Shorten the 'Pay' link
                            var cli = new WebClient();
                            cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string response = cli.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + rejectLink + "\"}");
                            googleURLShortnerResponseClass shortRejectLinkFromGoogleResult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response);

                            if (shortRejectLinkFromGoogleResult != null)
                            {
                                RejectShortLink = shortRejectLinkFromGoogleResult.id;
                            }
                            else
                            {
                                // Google short URL API broke...
                                Logger.Error("TDA -> SendTransactionReminderEmail -> requestReceivedToNewUser Google short Reject URL NOT generated. Long Reject URL: [" + rejectLink + "].");
                            }

                            // Now shorten the 'Pay' link
                            cli.Dispose();
                            try
                            {
                                var cli2 = new WebClient();
                                cli2.Headers[HttpRequestHeader.ContentType] = "application/json";
                                string response2 = cli2.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + payLink + "\"}");
                                googleURLShortnerResponseClass googlerejectshortlinkresult2 = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response2);

                                if (googlerejectshortlinkresult2 != null)
                                {
                                    AcceptShortLink = googlerejectshortlinkresult2.id;
                                }
                                else
                                {
                                    // Google short URL API broke...
                                    Logger.Error("TDA -> SendTransactionReminderEmail -> requestReceivedToNewUser Google short Pay URL NOT generated. Long Pay URL: [" + payLink + "].");
                                }
                                cli2.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail -> requestReceivedToNewUser Google short PAY URL NOT generated. Long Pay URL: [" + payLink + "].");
                                return ex.ToString();
                            }

                            #endregion Shortening URLs for SMS


                            #region Sending SMS

                            try
                            {
                                string SMSContent = "Just a reminder, " + senderFirstName + " " + senderLastName + " requested $" +
                                                      s32[0].ToString() + "." + s32[1].ToString() +
                                                      " from you using Nooch, a free app. Tap here to pay: " + AcceptShortLink +
                                                      ". Tap here to reject: " + RejectShortLink;

                                Utility.SendSMS(CommonHelper.GetDecryptedData(trans.PhoneNumberInvited), SMSContent);

                                Logger.Info("TDA -> SendTransactionReminderEmail -> Request Reminder SMS sent to [" + CommonHelper.GetDecryptedData(trans.PhoneNumberInvited) + "].");

                                return "Reminder sms sent successfully.";
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail -> Request Reminder SMS NOT sent to [" + CommonHelper.GetDecryptedData(trans.PhoneNumberInvited) + "]. Problem occured in sending SMS.");
                                return "Unable to send sms reminder.";
                            }

                            #endregion Sending SMS

                            #endregion If Invited by SMS
                        }

                        #endregion RequestMoneyReminderToNewUser


                        #region RequestMoneyReminderToExistingUser

                        else if (trans.Member.MemberId != null)
                        {
                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/RejectMoney?TransactionId=" + TransId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
                            string paylink = "nooch://";

                            senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                            senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));

                            #region Reminder EMAIL

                            string toAddress = CommonHelper.GetDecryptedData(trans.Member.UserName);

                            var tokens2 = new Dictionary<string, string>
                                {
								    {Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
									{Constants.PLACEHOLDER_NEWUSER, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName))},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT,s32[0].ToString()},
									{Constants.PLACEHLODER_CENTS,s32[1].ToString()},
									{Constants.PLACEHOLDER_REJECT_LINK,rejectLink},
									{Constants.PLACEHOLDER_TRANSACTION_DATE,Convert.ToDateTime(trans.TransactionDate).ToString("MMM dd yyyy")},
									{Constants.MEMO, memo},
									{Constants.PLACEHOLDER_PAY_LINK,paylink}
								};

                            var templateToUse = isForRentScene ? "requestReminderToExistingUser_RentScene"
                                                               : "requestReminderToExistingUser";

                            try
                            {
                                Utility.SendEmail(templateToUse, fromAddress,
                                    toAddress, null, senderFirstName + " " + senderLastName + " requested " + "$" + s22.ToString() + " with Nooch - Reminder",
                                    null, tokens2, null, null, null);

                                Logger.Info("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] NOT sent to [" + toAddress + "]. Problem occured in sending mail.");
                            }

                            #endregion Reminder EMAIL

                            #region Reminder PUSH NOTIFICATION

                            if (!String.IsNullOrEmpty(trans.Member.DeviceToken) &&
                                trans.Member.DeviceToken != "(null)")
                            {
                                try
                                {
                                    string firstName = (!String.IsNullOrEmpty(trans.Member.FirstName)) ? " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName)) : "";

                                    string msg = "Hey" + firstName + "! Just a reminder that " + senderFirstName + " " + senderLastName +
                                                 " sent you a Nooch request for $" + trans.Amount.ToString("n2") + ". Might want to pay up!";

                                    Utility.SendNotificationMessage(msg, 1, null, trans.Member.DeviceToken,
                                                                                  Utility.GetValueFromConfig("AppKey"),
                                                                                  Utility.GetValueFromConfig("MasterSecret"));

                                    Logger.Info("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification sent successfully - [Username: " +
                                                           toAddress + "], [Device Token: " + trans.Member.DeviceToken + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification NOT sent - [Username: " +
                                                            toAddress + "], [Device Token: " + trans.Member.DeviceToken + "], [Exception: " + ex.Message + "]");
                                }
                            }

                            #endregion  Reminder PUSH NOTIFICATION

                            return "Reminder mail sent successfully.";
                        }

                        #endregion RequestMoneyReminderToExistingUser

                        else
                        {
                            return "No recipient MemberId found for this transaction.";
                        }
                    }
                    else
                    {
                        Logger.Error("TDA -> SendTransactionReminderEmail FAILED - Could not find the Transaction. MemberID: [" + MemberId + "]. TransactionId: [" + TransactionId + "]");
                        return "No transaction found";
                    }

                    #endregion Requests - Both Types
                }

                else if (ReminderType == "InvitationReminderToNewUser")
                {
                    #region InvitationReminderToNewUser



                    var trans = _dbContext.Transactions.FirstOrDefault(m => m.Member1.MemberId == MemId
                        && m.TransactionId == TransId && m.TransactionStatus == "Pending" && (m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA==")
                        );

                    if (trans != null)
                    {
                        #region Setup Variables

                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));

                        string linkSource = !String.IsNullOrEmpty(trans.PhoneNumberInvited) ? "Um3I3RNHEGWqKM9MLsQ1lg=="  // "Phone"
                                                                                            : "75U7bZRpVVxLNbQuoMQEGQ=="; // "Email"

                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/RejectMoney?TransactionId=" + trans.TransactionId +
                                                          "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                          "&LinkSource=" + linkSource +
                                                          "&TransType=DrRr1tU1usk7nNibjtcZkA==");

                        string acceptLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/DepositMoney?TransactionId=" + trans.TransactionId.ToString());

                        string s22 = trans.Amount.ToString("n2");
                        string[] s32 = s22.Split('.');

                        string memo = "";
                        if (!string.IsNullOrEmpty(trans.Memo))
                        {
                            if (trans.Memo.Length > 3)
                            {
                                string firstThreeChars = trans.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = trans.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + trans.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + trans.Memo.ToString();
                            }
                        }

                        #endregion Setup Variables

                        if (trans.InvitationSentTo != null)
                        {
                            #region Invitation Was Sent By Email

                            var recipientEmail = CommonHelper.GetDecryptedData(trans.InvitationSentTo);

                            var tokens2 = new Dictionary<string, string>
												 {
													{Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
													{Constants.PLACEHOLDER_SENDER_FULL_NAME, senderFirstName + " " + senderLastName},
													{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
													{Constants.PLACEHLODER_CENTS, s32[1].ToString()},
													{Constants.PLACEHOLDER_TRANSFER_REJECT_LINK, rejectLink},
													{Constants.PLACEHOLDER_TRANSACTION_DATE, Convert.ToDateTime(trans.TransactionDate).ToString("MMM dd yyyy")},
													{Constants.MEMO, memo},
													{Constants.PLACEHOLDER_TRANSFER_ACCEPT_LINK, acceptLink}
												 };
                            try
                            {
                                string fromAddress = Utility.GetValueFromConfig("transfersMail");

                                Utility.SendEmail("transferReminderToRecipient", fromAddress,
                                                            recipientEmail, null,
                                                            senderFirstName + " " + senderLastName + " sent you " + "$" + s22.ToString() + " - Reminder",
                                                            null, tokens2, null, null, null);

                                Logger.Info("TDA -> SendTransactionReminderEmail -> Reminder email (Invite, New user) sent to [" + recipientEmail + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail -> Reminder email (Invite, New user) NOT sent to [" + recipientEmail +
                                                       "], Exception: [" + ex + "]");
                            }

                            return "Reminder mail sent successfully.";

                            #endregion Invitation Was Sent By Email
                        }
                        else if (trans.IsPhoneInvitation == true &&
                                  String.IsNullOrEmpty(trans.InvitationSentTo) &&
                                 !String.IsNullOrEmpty(trans.PhoneNumberInvited))
                        {
                            #region Invitation Was Sent By SMS

                            #region Shortening URLs for SMS

                            string RejectShortLink = "";
                            string AcceptShortLink = "";

                            string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                            var cli = new WebClient();
                            cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string response = cli.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + rejectLink + "\"}");

                            googleURLShortnerResponseClass googleRejectShortLinkResult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response);

                            if (googleRejectShortLinkResult != null)
                            {
                                RejectShortLink = googleRejectShortLinkResult.id;
                            }
                            else
                            {
                                // Google short URL link broke...
                            }

                            // Now shorten 'Accept' link
                            cli.Dispose();

                            try
                            {
                                var cli2 = new WebClient();
                                cli2.Headers[HttpRequestHeader.ContentType] = "application/json";
                                string response2 = cli2.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + acceptLink + "\"}");
                                googleURLShortnerResponseClass googlerejectshortlinkresult2 = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response2);

                                if (googlerejectshortlinkresult2 != null)
                                {
                                    AcceptShortLink = googlerejectshortlinkresult2.id;
                                }
                                else
                                {
                                    // google short url link broke...
                                }
                                cli2.Dispose();
                            }
                            catch (Exception ex)
                            {
                                return ex.ToString();
                            }

                            #endregion Shortening URLs for SMS


                            #region Sending SMS
                            try
                            {
                                string SMSContent = "Just a reminder, " + senderFirstName + " " + senderLastName + " wants to send you $" +
                                                      s32[0].ToString() + "." + s32[1].ToString() +
                                                      " using Nooch, a free app. Tap here accept: " + AcceptShortLink +
                                                      ". Or here to reject: " + RejectShortLink;

                                var tophone = CommonHelper.GetDecryptedData(trans.PhoneNumberInvited);
                                Utility.SendSMS(tophone, SMSContent);

                                Logger.Info("TDA -> SendTransactionReminderEmail -> Reminder SMS (Invite, New user) sent to [" + CommonHelper.GetDecryptedData(trans.PhoneNumberInvited) + "] successfully.");

                                return "Reminder sms sent successfully.";
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail -> Reminder SMS (Invite, New user) NOT sent to [" + CommonHelper.GetDecryptedData(trans.PhoneNumberInvited) + "] Problem occured in sending sms.");
                                return "Unable to send sms reminder.";
                            }
                            #endregion Sending SMS

                            #endregion Invitation Was Sent By SMS
                        }
                        else
                        {
                            return "no email mentioned for invited user";
                        }
                    }
                    else
                    {
                        return "No transaction found";
                    }

                    #endregion InvitationReminderToNewUser
                }

                else
                {
                    return "invalid transaction id or memberid";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> SendTransactionReminderEmail FAILED - Outer Exception - [" + ex + "]");
                return "Error";
            }
        }



        /// <summary>
        /// For an existing Nooch user making a payment request to another existing Nooch user.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public string RequestMoney(RequestDto requestDto, out string requestId)
        {
            Logger.Info("TDA -> RequestMoney - [MemberId: " + requestDto.MemberId + "]");

            requestId = string.Empty;

            #region Initial Checks

            // Check uniqueness of requesting and sending user
            if (requestDto.MemberId == requestDto.SenderId)
            {
                return "Not allowed to request money from yourself.";
            }


            var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);

            var sender = new Member();
            if (requestDto.SenderId.IndexOf(',') == -1)
            {
                sender = CommonHelper.GetMemberDetails(requestDto.SenderId);
            }

            // Check that both accounts exist
            if (sender == null || requester == null)
            {
                return "Either sending user or requesting user does not exist.";
            }
            // Validate PIN of requesting user

            string validPinNumberResult = CommonHelper.ValidatePinNumber(requestDto.MemberId, requestDto.PinNumber);
            if (validPinNumberResult != "Success")
            {
                return validPinNumberResult;
            }

            decimal transactionFee = Utility.ConvertToDecimal(Utility.GetValueFromConfig("PayGoModeFee"));
            decimal transactionAmount = Convert.ToDecimal(requestDto.Amount);

            // Individual user transfer limit check
            decimal thisUserTransLimit = 0;
            string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(requestDto.MemberId);

            if (!String.IsNullOrEmpty(indiTransLimit))
            {
                if (indiTransLimit != "0")
                {
                    thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                    if (transactionAmount > thisUserTransLimit)
                    {
                        Logger.Error("TDA -> RequestMoney FAILED - OVER PERSONAL TRANS LIMIT - " +
                                               "Amount Requested: [" + transactionAmount.ToString() + "], " +
                                               "Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                        return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + thisUserTransLimit.ToString("F2");
                    }
                }
            }

            if (CommonHelper.isOverTransactionLimit(requestDto.Amount, requestDto.SenderId, requestDto.MemberId))
            {
                Logger.Error("TDA -> RequestMoney FAILED - OVER GLOBAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                               "], Global Limit: [" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")) + "], MemberId: [" + requestDto.MemberId + "]");
                return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request at a time is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
            }

            #endregion Initial Checks

            // Request Sent to more than one Nooch member
            if (requestDto.SenderId.IndexOf(',') != -1)
            {
                #region If Multiple Recepients

                string[] senders = requestDto.SenderId.Split(',');

                bool alreadySentEmailToSender = false;

                // A loop is added for for requests to multiple users
                for (int i = 0; i < senders.Length; i++)
                {
                    // Create multiple transaction requests
                    var transaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid(),
                        SenderId =

                                Utility.ConvertToGuid(senders[i]),
                        RecipientId =


                        Utility.ConvertToGuid(requestDto.MemberId),

                        Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                        Amount = requestDto.Amount - transactionFee,
                        TransactionDate = DateTime.Now,
                        Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                        DisputeStatus = null,
                        TransactionStatus = "Pending",
                        TransactionType = CommonHelper.GetEncryptedData("Request"),
                        DeviceId = requestDto.DeviceId,
                        TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                        TransactionFee = transactionFee,

                        GeoLocation = new GeoLocation
                        {
                            LocationId = Guid.NewGuid(),
                            Latitude = requestDto.Latitude,
                            Longitude = requestDto.Longitude,
                            Altitude = requestDto.Altitude,
                            AddressLine1 = requestDto.AddressLine1,
                            AddressLine2 = requestDto.AddressLine2,
                            City = requestDto.City,
                            State = requestDto.State,
                            Country = requestDto.Country,
                            ZipCode = requestDto.ZipCode,
                            DateCreated = DateTime.Now
                        }
                    };


                    _dbContext.Transactions.Add(transaction);
                    int dbResult = _dbContext.SaveChanges();

                    if (dbResult > 0)
                    {
                        _dbContext.Entry(transaction).Reload();
                        requestId = transaction.TransactionId.ToString();

                        // BELOW CODE ADDED BY CLIFF (11/26/14) FOR SENDING EMAILS FOR WHEN THERE ARE MULTIPLE RECIPIENTS
                        #region Cliffs Additions

                        var recipientOfRequest = CommonHelper.GetMemberDetails(senders[i]);

                        string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                        string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                        string RequestReceiverFirstName = "";
                        string RequestReceiverLastName = "";
                        string emailTemplateToRecipient = "";
                        var fromAddress = Utility.GetValueFromConfig("transfersMail");

                        if (recipientOfRequest != null)  // Make sure this Recipient is an existing Nooch user
                        {
                            RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(recipientOfRequest.FirstName)).ToString());
                            RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(recipientOfRequest.LastName)).ToString());
                            emailTemplateToRecipient = "requestReceivedToExistingUser";
                        }
                        else
                        {
                            emailTemplateToRecipient = "requestReceivedToNewUser";
                        }
                        string wholeAmount = requestDto.Amount.ToString("n2");
                        string[] s32 = wholeAmount.Split('.');

                        string memo = "";
                        if (!string.IsNullOrEmpty(transaction.Memo))
                        {
                            if (transaction.Memo.Length > 3)
                            {
                                string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = transaction.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + transaction.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + transaction.Memo.ToString();
                            }
                        }
                        string sendersPic;
                        if (!string.IsNullOrEmpty(requester.Photo))
                        {
                            string lastFourOfRecipientsPic = requester.Photo.Substring(requester.Photo.Length - 15);
                            if (lastFourOfRecipientsPic != "gv_no_photo.png")
                            {
                                sendersPic = "";
                            }
                            else
                            {
                                sendersPic = requester.Photo.ToString();
                            }
                        }

                        // Send 1 TOTAL email to Sender for this Request (i.e. If there are 5 recipients, don't need to send a separate email for each)
                        if (!alreadySentEmailToSender)
                        {
                            // This cancel link will currently only cancel the individual request to the 1st recipient (NEED NEW WAY TO CANCEL ALL REQUESTS FOR A GROUP REQUEST)
                            string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/CancelMoneyRequest?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId.ToString() + "&userType=mx5bTcAYyiOf9I5Py9TiLw==");
                            var tokens = new Dictionary<string, string>
								{
									{Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
									{Constants.PLACEHOLDER_NEWUSER, RequestReceiverFirstName + " " + RequestReceiverLastName},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
									{Constants.PLACEHLODER_CENTS, s32[1].ToString()},
									{Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
									{Constants.MEMO, memo}
								};

                            var toAddress = CommonHelper.GetDecryptedData(requester.UserName);

                            try
                            {
                                Utility.SendEmail("requestSent", fromAddress, toAddress, null,
                                    "Your Nooch requests to " + senders.Length + " people is pending", null,
                                    tokens, null, null, null);

                                Logger.Info("requestSent --> Multiple Recipients --> email sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception)
                            {
                                Logger.Error("requestSent --> Multiple Recipients --> email NOT sent to [" + toAddress + "]. Problem occurred in sending mail.");
                            }

                            // So we don't send another email to the Sender in the next loop through...
                            alreadySentEmailToSender = true;
                        }

                        // Send 1 email to EACH Recipient
                        string otherlink2 = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/RejectMoney?TransactionId=" + transaction.TransactionId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
                        var toAddress2 = CommonHelper.GetDecryptedData(transaction.Member.UserName);

                        var tokensRequestMultipleRecipients = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, RequestReceiverFirstName},
								{Constants.PLACEHOLDER_LAST_NAME, RequesterFirstName + " " + RequesterLastName},
								{Constants.PLACEHOLDER_FRIEND_FIRST_NAME, RequesterFirstName},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT,s32[0].ToString()},
								{Constants.PLACEHLODER_CENTS,s32[1].ToString()},
								{Constants.PLACEHOLDER_REJECT_LINK,otherlink2},
								{Constants.MEMO, memo}
							};

                        try
                        {
                            Utility.SendEmail(emailTemplateToRecipient, fromAddress, toAddress2, null,
                                 RequesterFirstName + " " + RequesterLastName + " requested " + "$" + wholeAmount + " with Nooch",
                                 null, tokensRequestMultipleRecipients, null, null, null);

                            Logger.Info("requestReceivedToExistingUser --> Multiple Recipients --> email sent to [" + CommonHelper.GetDecryptedData(requester.UserName) + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Error("requestReceivedToExistingUser --> Multiple Recipients --> email NOT sent to [" + CommonHelper.GetDecryptedData(requester.UserName) + "]. Problem occurred in sending mail.");
                        }


                        // Send Push Notification to EACH Recipient of request (IF an existing user)
                        if (recipientOfRequest != null)  // Make sure this Recipient is an existing Nooch user
                        {
                            var friendDetails = CommonHelper.GetMemberNotificationSettings(recipientOfRequest.MemberId.ToString());
                            if (friendDetails != null)
                            {
                                if ((friendDetails.TransferSent == null)
                                    ? false : friendDetails.TransferSent.Value)
                                {
                                    // for push notification
                                    string deviceId = friendDetails != null ? recipientOfRequest.DeviceToken : null;
                                    string mailBodyText = "Hi, " + RequesterFirstName + " " + RequesterLastName +
                                                          " requested $" + wholeAmount + " from you. Pay up now using Nooch.";

                                    try
                                    {
                                        if (!String.IsNullOrEmpty(deviceId) && (friendDetails.TransferAttemptFailure ?? false))
                                        {
                                            string response = Utility.SendNotificationMessage(mailBodyText,
                                                    1, null, deviceId,
                                                    Utility.GetValueFromConfig("AppKey"),
                                                    Utility.GetValueFromConfig("MasterSecret"));

                                            Logger.Info("Request Received Push notification sent to [" + deviceId + "] successfully.");
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Logger.Error("Request Received Push Notification NOT sent to [" + deviceId + "]. Problem occurred in sending notification. ");
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                    else
                    {
                        return "Request failed.";
                    }
                }

                return "Request made successfully.";

                #endregion If Multiple Recipients
            }
            else
            {
                //Request Sent to only one Nooch member

                #region Create New Transaction Record

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),

                    SenderId =

                                        Utility.ConvertToGuid(requestDto.SenderId),
                    RecipientId =
                    Utility.ConvertToGuid(requestDto.MemberId),

                    Amount = requestDto.Amount,
                    TransactionDate = DateTime.Now,
                    Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                    Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                    DisputeStatus = null,
                    TransactionStatus = "Pending",
                    TransactionType = CommonHelper.GetEncryptedData("Request"),
                    DeviceId = requestDto.DeviceId,
                    TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                    TransactionFee = transactionFee,
                    GeoLocation = new GeoLocation
                    {
                        LocationId = Guid.NewGuid(),
                        Latitude = requestDto.Latitude,
                        Longitude = requestDto.Longitude,
                        Altitude = requestDto.Altitude,
                        AddressLine1 = requestDto.AddressLine1,
                        AddressLine2 = requestDto.AddressLine2,
                        City = requestDto.City,
                        State = requestDto.State,
                        Country = requestDto.Country,
                        ZipCode = requestDto.ZipCode,
                        DateCreated = DateTime.Now
                    }
                };

                #endregion Create New Transaction Record

                _dbContext.Transactions.Add(transaction);

                int dbResult = _dbContext.SaveChanges();

                if (dbResult > 0)
                {
                    _dbContext.Entry(transaction).Reload();
                    requestId = transaction.TransactionId.ToString();

                    #region Send Notifications To Both Users

                    #region Setup Notification Variables

                    string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                    string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                    string RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(sender.FirstName)).ToString());
                    string RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(sender.LastName)).ToString());

                    string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/CancelMoneyRequest?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId.ToString() + "&userType=mx5bTcAYyiOf9I5Py9TiLw==");
                    

                    string wholeAmount = requestDto.Amount.ToString("n2");
                    string[] s32 = wholeAmount.Split('.');

                    string memo = "";
                    if (!string.IsNullOrEmpty(transaction.Memo))
                    {
                        if (transaction.Memo.Length > 3)
                        {
                            string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transaction.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transaction.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transaction.Memo.ToString();
                        }
                    }


                    string requesterPic = "https://www.noochme.com/EmailTemplates/img/userpic-default.png";
                    if (!String.IsNullOrEmpty(requester.Photo) && requester.Photo.Length > 20)
                    {
                        requesterPic = requester.Photo.ToString();
                    }

                    string senderPic = "https://www.noochme.com/EmailTemplates/img/userpic-default.png";
                    if (!String.IsNullOrEmpty(sender.Photo) && sender.Photo.Length > 20)
                    {
                        senderPic = sender.Photo.ToString();
                    }


                    bool isForRentScene = false;
                    //var logoToDisplay = "noochlogo";

                    if (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                    {
                        isForRentScene = true;
                        RequesterFirstName = "Rent Scene";
                        RequesterLastName = "";
                    }

                    var fromAddress = isForRentScene ? "payments@rentscene.com"
                                                     : Utility.GetValueFromConfig("transfersMail");

                    var templateToUse_Sender = isForRentScene ? "requestSent_RentScene"
                                                              : "requestSent";

                    #endregion Setup Notification Variables

                    #region Email To Request Sender

                    try
                    {
                        var tokens = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
								{Constants.PLACEHOLDER_NEWUSER, RequestReceiverFirstName + " " + RequestReceiverLastName},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
								{Constants.PLACEHLODER_CENTS, s32[1].ToString()},
								{Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
								{Constants.MEMO, memo}
							};


                        var toAddress = CommonHelper.GetDecryptedData(requester.UserName);

                        Utility.SendEmail(templateToUse_Sender, fromAddress, toAddress, null,
                                                    "Your payment request to " + RequestReceiverFirstName + " " + RequestReceiverLastName + " is pending", null,
                                                    tokens, null, null, null);

                        Logger.Info("TDA -> RequestMoney - [" + templateToUse_Sender + "] email sent to [" + toAddress + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoney - [" + templateToUse_Sender + "] email NOT sent to [" + CommonHelper.GetDecryptedData(requester.UserName) + "], [Exception: " + ex + "]");
                    }

                    #endregion Email To Request Sender

                    #region Email To Request Recipient

                    try
                    {
                        var tokens2 = new Dictionary<string, string> { };
                        string templateToUse = "";

                        if (sender.Status == "NonRegistered" ||
                            sender.Type == "Personal - Browser")
                        {
                            Logger.Info("TDA -> RequestMoney - Request to a NON-REGISTERED USER - Sending requestReceivedToExistingNonRegUser Email Template...");

                            // Send email to REQUEST RECIPIENT (person who will pay/reject this request)
                            // Include 'UserType', 'LinkSource', and 'TransType' as encrypted along with request
                            // In this case UserType would = 'Nonregistered'  ->  6KX3VJv3YvoyK+cemdsvMA==
                            //              TransType would = 'Request'
                            //              LinkSource would = 'Email'

                            string userType = "6KX3VJv3YvoyK+cemdsvMA=="; // "NonRegistered"

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "Nooch/RejectMoney?TransactionId=" + requestId +
                                                              "&UserType=" + userType +
                                                              "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                           "Nooch/PayRequest?TransactionId=" + requestId +
                                                           "&UserType=" + userType);

                            if (isForRentScene)
                            {
                                rejectLink = rejectLink + "&rs=1";
                                paylink = paylink + "&rs=1";

                                templateToUse = "requestReceivedToExistingNonRegUser_RentScene";
                            }
                            else
                            {
                                templateToUse = "requestReceivedToExistingNonRegUser";
                            }

                            tokens2 = new Dictionary<string, string>
                                {
                                    {Constants.PLACEHOLDER_FIRST_NAME, RequestReceiverFirstName},
                                    {"$UserPicture$", requesterPic},
                                    {Constants.PLACEHOLDER_SENDER_FULL_NAME, RequesterFirstName + " " + RequesterLastName},
                                    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
                                    {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
                                    {Constants.MEMO, memo},
                                    {Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
                                    {Constants.PLACEHOLDER_PAY_LINK, paylink},
                                    {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, RequesterFirstName}
                                };
                        }
                        else
                        {
                            // Send email to Request Receiver -- sending UserType LinkSource TransType as encrypted along with request
                            // In this case UserType would = 'Existing'
                            // TransType would be 'Request'
                            // and link source would be 'Email'

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "Nooch/RejectMoney?TransactionId=" + transaction.TransactionId +
                                                              "&UserType=mx5bTcAYyiOf9I5Py9TiLw==" +
                                                              "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            if (isForRentScene)
                            {
                                rejectLink = rejectLink + "&rs=1";
                                templateToUse = "requestReceivedToExistingUser_RentScene";
                            }
                            else
                            {
                                templateToUse = "requestReceivedToExistingUser";
                            }

                            tokens2 = new Dictionary<string, string>
                                {
                                    {Constants.PLACEHOLDER_FIRST_NAME, RequestReceiverFirstName},
                                    {"$UserPicture$", requesterPic},
                                    {Constants.PLACEHOLDER_LAST_NAME, RequesterFirstName + " " + RequesterLastName},
                                    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
                                    {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
                                    {Constants.MEMO, memo},
                                    {Constants.PLACEHOLDER_REJECT_LINK, rejectLink}, 
                                    {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, RequesterFirstName}
                                };

                        }

                        var toAddress = CommonHelper.GetDecryptedData(transaction.Member.UserName);

                        Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                    RequesterFirstName + " " + RequesterLastName + " requested " + "$" + wholeAmount,
                                                    null, tokens2, null, null, null);

                        Logger.Info("TDA -> RequestMoney - " + templateToUse + " email sent to [" + CommonHelper.GetDecryptedData(requester.UserName) + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoney - RequestReceivedToExistingUser email NOT sent to [" + CommonHelper.GetDecryptedData(requester.UserName) + "], [Exception: " + ex + "]");
                    }

                    #endregion Email To Request Recipient

                    #region Push Notification To Request Recipient

                    if (!String.IsNullOrEmpty(sender.DeviceToken))
                    {
                        var notifSettings = CommonHelper.GetMemberNotificationSettings(sender.MemberId.ToString());

                        if (notifSettings != null)
                        {
                            if (notifSettings.TransferReceived != false)
                            {
                                try
                                {
                                    string deviceId = sender.DeviceToken;
                                    //string emoji = "\U0001F601";

                                    string msg = "Hi " + RequestReceiverFirstName + ", " + RequesterFirstName + " " + RequesterLastName +
                                                      " just requested $" + wholeAmount + " from you. Open Nooch to pay up now (or reject this request)!";


                                    Utility.SendNotificationMessage(msg, 1, null, sender.DeviceToken,
                                                                                  Utility.GetValueFromConfig("AppKey"),
                                                                                  Utility.GetValueFromConfig("MasterSecret"));

                                    Logger.Info("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification sent successfully - [Username: " +
                                                            CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], [Device Token: " + sender.DeviceToken + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> RequestMoney - Request Received Push Notification NOT sent to [Username: " +
                                                            CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], [" + sender.DeviceToken + "], [Exception: " + ex + "]");
                                }
                            }
                        }
                    }

                    #endregion Push Notification To Request Recipient

                    #endregion Send Notifications To Both Users

                    return "Request made successfully.";
                }
                else
                {
                    return "Request failed.";
                }
            }
        }


        #endregion



        public string RejectMoneyRequestForExistingNoochUser(string TrannsactionId)
        {
            Logger.Info("TDA -> RejectMoneyRequestForExistingNoochUser - transactionId: [" + TrannsactionId + "]");

            try
            {
                var transId = Utility.ConvertToGuid(TrannsactionId);


                var transactionDetail = _dbContext.Transactions.FirstOrDefault(m => m.TransactionId == transId
                                                                                    && m.TransactionStatus == "Pending" &&
                                                                                    (m.TransactionType ==
                                                                                     "DrRr1tU1usk7nNibjtcZkA==" ||
                                                                                     m.TransactionType ==
                                                                                     "T3EMY1WWZ9IscHIj3dbcNw=="));


                if (transactionDetail != null)
                {
                    #region IfSomethingFound

                    transactionDetail.TransactionStatus = "Rejected";
                    _dbContext.SaveChanges();
                    _dbContext.Entry(transactionDetail).Reload();

                    string SenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                    string SenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));
                    string RejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                    string RejectorLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));

                    string wholeAmount = transactionDetail.Amount.ToString("n2");
                    string[] s3 = wholeAmount.Split('.');

                    string memo = "";
                    if (!string.IsNullOrEmpty(transactionDetail.Memo))
                    {
                        if (transactionDetail.Memo.Length > 3)
                        {
                            string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transactionDetail.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transactionDetail.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transactionDetail.Memo.ToString();
                        }
                    }

                    #region Push Notification to Request Sender
                    // sending push notification to money sender

                    var noochMemberfornotification = CommonHelper.GetMemberNotificationSettings(transactionDetail.Member1.MemberId.ToString());

                    if (noochMemberfornotification != null)
                    {
                        try
                        {
                            string mailBodyText = RejectorFirstName + " " + RejectorLastName + " just rejected your Nooch payment request for $" + wholeAmount + ".";
                            Utility.SendNotificationMessage(mailBodyText, 1, null,
                                transactionDetail.Member1.DeviceToken,
                                Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));
                        }
                        catch (Exception)
                        {
                            Logger.Error("RejectMoneyRequestForExistingNoochUser - Push notification not sent to [" + transactionDetail.Member1.UDID1 + "].");
                        }
                    }

                    #endregion

                    // Sending email to request sender about rejection

                    var tokens = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, SenderFirstName},
								{Constants.PLACEHOLDER_RECEPIENT_FULL_NAME, RejectorFirstName + " " + RejectorLastName},
								{Constants.PLACEHOLDER_RECEPIENT_FIRST_NAME, RejectorFirstName},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.MEMO, memo}
							};

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");
                    var toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                    try
                    {
                        Utility.SendEmail("requestDeniedToSender",
                            fromAddress, toAddress, null,
                            RejectorFirstName + " " + RejectorLastName + " denied your payment request for $" + wholeAmount, null,
                            tokens, null, null, null);

                        Logger.Info("TDA -> rejectMoneyRequestForExistingNoochUser -> requestDeniedToSender email sent to [" + toAddress + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> rejectMoneyRequestForExistingNoochUser -> requestDeniedToSender email NOT sent to [" + toAddress +
                                               "], Exception: [" + ex.Message + "]");
                    }

                    // sending email to user who rejected this request

                    var tokens2 = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, RejectorFirstName},
								{Constants.PLACEHOLDER_SENDER_FULL_NAME, SenderFirstName + " " + SenderLastName},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.MEMO, memo}
							};

                    toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member.UserName);

                    try
                    {
                        Utility.SendEmail("requestDeniedToRecipient",
                                fromAddress, toAddress, null,
                                "You rejected a Nooch request from " + SenderFirstName + " " + SenderLastName, null,
                                tokens2, null, null, null);

                        Logger.Info("rejectMoneyRequestForExistingNoochUser -> requestDeniedToRecipient email sent to [" + toAddress + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("rejectMoneyRequestForExistingNoochUser -> requestDeniedToRecipient email NOT sent to [" +
                                               toAddress + "], Exception: [" + ex.Message + "]");
                    }

                    return "Request Rejected Successfully.";

                    #endregion
                }
                else
                {
                    return "";
                }

            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> RejectMoneyRequestForExistingNoochUser FAILED. [Exception: " + ex + "]");
                return "";
            }
        }


        /// <summary>
        /// To Get Transaction By Transaction Id.
        /// </summary>
        public Transaction GetTransactionById(string transactionId)
        {
            Logger.Info("TDA -> GetTransactionById Initiated - [Transaction Id: " + transactionId + "]");

            try
            {
                var transId = Utility.ConvertToGuid(transactionId);

                var transactionDetail = _dbContext.Transactions.Where(c => c.TransactionId == transId).FirstOrDefault();

                // var transactionDetail = transactionRepository.SelectAll(transactionSpecification, new[] { "Members", "Members1" }).FirstOrDefault();
                if (transactionDetail != null)
                {
                    _dbContext.Entry(transactionDetail).Reload();
                }
                return transactionDetail;

            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> GetTransactionById EXCEPTION - TransactionID: [" + transactionId + "], Exception: [" + ex + "]");
            }

            return null;
        }


        public List<Transaction> GetTransactionsList(string memberId, string listType, int pageSize, int pageIndex, string SubListType, out int totalRecordsCount)
        {
            totalRecordsCount = 0;
            Logger.Info("TDA -> GetTransactionsList - [MemberId: " + memberId + "]. ListType: [" + listType + "]");

            try
            {

                var id = Utility.ConvertToGuid(memberId);


                //ClearTransactionHistory functionality 
                var member = _dbContext.Members.Where(u => u.MemberId == id).FirstOrDefault();

                if (member != null)
                {
                    _dbContext.Entry(member).Reload();
                    //if (member.ClearTransactionHistory.HasValue && member.ClearTransactionHistory.Value)
                    //{
                    //    return new List<Transactions>();
                    //}

                    // get admin member id.
                    // CLIFF (7/27/15): Don't understand the point of this block... it's trying to lookup a Member by using the adminMail username? Why??
                    /* membersAccountRepository = new Repository<Members, NoochDataEntities>(noochConnection);
                    
                       string adminUserName = Utility.GetValueFromConfig("adminMail");
                       adminUserName = CommonHelper.GetEncryptedData(adminUserName);
                       var adminAccountSpecification = new Specification<Members>
                       {
                           Predicate = accountTemp => accountTemp.UserName.Equals(adminUserName)
                       };
                       var adminAccount = membersAccountRepository.SelectAll(adminAccountSpecification, new[] { "AccountDetails" }).FirstOrDefault();
                       var adminId = adminAccount.MemberId; */


                    var transactions = new List<Transaction>();

                    var transactionTypeTransfer = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);
                    var transactionTypeDonation = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_DONATION);
                    var transactionTypeRequest = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_REQUEST);
                    var transactionTypeDisputed = "+C1+zhVafHdXQXCIqjU/Zg==";
                    var transactionPredicate = "";


                    if (SubListType != "")
                    {
                        #region whenSomethingisPassedForSubList



                        if (listType.ToUpper().Equals("SENT"))
                        {
                            transactions = _dbContext.Transactions.Where(entity => entity.Member.MemberId == id &&
                                        (entity.TransactionType == transactionTypeTransfer || entity.TransactionType == transactionTypeDonation) &&
                                         entity.TransactionStatus == SubListType).ToList();


                        }
                        else if (listType.ToUpper().Equals("RECEIVED"))
                        {


                            transactions = _dbContext.Transactions.Where(entity =>
                                        entity.Member1.MemberId == id &&
                                        entity.TransactionType == transactionTypeTransfer &&
                                        entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("DISPUTED"))
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        ((entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                          entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed) &&
                                          entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL") && SubListType == "Pending") // CR
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                         entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL") && SubListType == "Success") // CR
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                        (entity.TransactionStatus == SubListType || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                        }
                        else if (listType.ToUpper().Equals("DONATION"))
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                         entity.TransactionType == transactionTypeDonation &&
                                         entity.TransactionStatus == SubListType).ToList();

                        }
                        else if (listType.ToUpper().Equals("REQUEST"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                         entity.TransactionType == transactionTypeRequest &&
                                        (entity.TransactionStatus == SubListType || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                        }

                        #endregion


                    }
                    else
                    {
                        #region WhenNothingIsPassedForSubList

                        if (listType.ToUpper().Equals("SENT"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        entity.Member.MemberId == id &&
                                       (entity.TransactionType == transactionTypeTransfer || entity.TransactionType == transactionTypeDonation)).ToList();

                        }
                        else if (listType.ToUpper().Equals("RECEIVED"))
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        entity.Member1.MemberId == id &&
                                        entity.TransactionType == transactionTypeTransfer).ToList();

                        }
                        else if (listType.ToUpper().Equals("DISPUTED"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                       (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                        entity.DisputeStatus != null &&
                                        entity.TransactionType == transactionTypeDisputed).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        entity.Member.MemberId == id || entity.Member1.MemberId == id).ToList();

                        }
                        else if (listType.ToUpper().Equals("DONATION"))
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                         entity.TransactionType == transactionTypeDonation).ToList();
                        }
                        else if (listType.ToUpper().Equals("REQUEST"))
                        {

                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                         entity.TransactionType == transactionTypeRequest).ToList();
                        }
                        #endregion

                    }

                    totalRecordsCount = transactions.Count();

                    if (pageSize == 0 && pageIndex == 0)
                    {
                        transactions = transactions.Take(1000).ToList();
                    }
                    else
                    {
                        transactions = transactions.Skip(pageSize * pageIndex).Take(pageSize).ToList();

                    }

                    if (transactions.Count > 0)
                    {
                        return new List<Transaction>(transactions);
                    }
                }
            }

            catch (Exception ex)
            {
                Logger.Error("TDA -> GetTransactionsList EXCEPTION - [MemberId: " + memberId + "], [Exception: " + ex.Message + "]");
            }

            return new List<Transaction>();
        }

        public Transaction GetLatestReceivedTransaction(string memberId)
        {
            Logger.Info("TDA -> GetLatestReceivedTransaction - memberId: [" + memberId + "]");

            var id = Utility.ConvertToGuid(memberId);


            //ClearTransactionHistory functionality 

            var member = _dbContext.Members.Where(u => u.MemberId == id).FirstOrDefault();
            if (member != null)
            {
                _dbContext.Entry(member).Reload();
                var transaction = new Transaction();

                var transactionType = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);

                //transactionSpecification.Predicate = entity => entity.Members1.MemberId == id && entity.TransactionType == transactionType;




                var disputedTransaction =
                    _dbContext.Transactions.Where(t => t.Member1.MemberId == id && ((t.TransactionType == transactionType) && (t.DisputeStatus != null))).OrderByDescending(c => c.DisputeDate).FirstOrDefault();



                var receivedTransaction =
                   _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id && entity.TransactionType == transactionType).OrderByDescending(c => c.DisputeDate).FirstOrDefault();

                if (disputedTransaction != null)
                {
                    _dbContext.Entry(disputedTransaction).Reload();
                    transaction = disputedTransaction.DisputeDate > receivedTransaction.TransactionDate ? disputedTransaction : receivedTransaction;
                }
                else
                {
                    transaction = receivedTransaction;
                }

                if (transaction != null)
                {
                    return transaction;
                }
            }
            return new Transaction();

        }

        public List<Transaction> GetTransactionsSearchList(string memberId, string friendName, string listType, int pageSize, int pageIndex, string sublist)
        {
            try
            {
                Logger.Info("TDA -> GetTransactionSearchList Initiated - [MemberId: " + memberId + "]");

                var id = Utility.ConvertToGuid(memberId);

                //ClearTransactionHistory functionality 
                var member = _dbContext.Members.Where(u => u.MemberId == id).FirstOrDefault();
                if (member != null)
                {
                    _dbContext.Entry(member).Reload();
                }

                string adminUserName = Utility.GetValueFromConfig("adminMail");

                var transactions = new List<Transaction>();

                friendName = CommonHelper.GetEncryptedData(friendName);

                var transactionTypeTransfer = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);
                var transactionTypeDonation = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_DONATION);
                var transactionTypeRequest = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_REQUEST);
                var totalRecordsCount = 0;
                var transactionTypeDisputed = "+C1+zhVafHdXQXCIqjU/Zg==";

                if (sublist != "")
                {
                    #region If Something Sent for SubList

                    if (listType.ToUpper().Equals("SENT"))
                    {
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id) && entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).ToList();
                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id) && entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).Count();

                    }
                    else if (listType.ToUpper().Equals("ALL") && sublist == "Success")
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && (entity.TransactionStatus == sublist || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && (entity.TransactionStatus == sublist || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                    }
                    else if (listType.ToUpper().Equals("ALL") && sublist == "Pending")
                    {
                        //transactionSpecification.Predicate = entity => (entity.Members.MemberId == id || entity.Members1.MemberId == id) && entity.TransactionStatus == sublist;
                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && entity.TransactionStatus == sublist).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && entity.TransactionStatus == sublist).ToList();
                    }
                    else if (listType.ToUpper().Equals("DONATION") && sublist != "Pending")
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                           entity.TransactionType == transactionTypeDonation && entity.TransactionStatus == sublist).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                            entity.TransactionType == transactionTypeDonation && entity.TransactionStatus == sublist).ToList();
                    }
                    else if (listType.ToUpper().Equals("RECEIVED") && sublist != "Pending")
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id &&
                                                                      (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                       entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).Count();
                        transactions = _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id &&
                                                                      (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                       entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).ToList();
                    }
                    else if (listType.ToUpper().Equals("REQUEST") && sublist == "Success")
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       entity.TransactionType == transactionTypeRequest &&
                                                                      (entity.TransactionStatus == sublist || entity.TransactionStatus == "cancelled" || entity.TransactionStatus == "Rejected")).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       entity.TransactionType == transactionTypeRequest &&
                                                                      (entity.TransactionStatus == sublist || entity.TransactionStatus == "cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                    }
                    else if (listType.ToUpper().Equals("REQUEST") && sublist == "Pending")
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       entity.TransactionType == transactionTypeRequest && entity.TransactionStatus == sublist).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        entity.TransactionType == transactionTypeRequest && entity.TransactionStatus == sublist).ToList();
                    }
                    else if (listType.ToUpper().Equals("TRANSFER"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        entity.TransactionType == transactionTypeTransfer && entity.TransactionStatus == sublist).ToList();
                    }
                    else if (listType.ToUpper().Equals("DISPUTED"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        entity.DisputeStatus != null && entity.TransactionStatus == sublist).Count();



                        //transactions = transactionRepository.SelectAll(transactionSpecification, new[] { "Members", "Members1" },
                        //                                    "TransactionDate", "desc", 0, 0).ToList();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                      entity.DisputeStatus != null && entity.TransactionStatus == sublist).ToList();

                        if (transactions.Any(x => x.Member.MemberId == id))
                        {

                            totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member1.FirstName.Contains(friendName) || entity.Member1.LastName.Contains(friendName)) &&
                                                                               entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed && entity.TransactionStatus == sublist).Count();
                            transactions = _dbContext.Transactions.Where(entity => (entity.Member1.FirstName.Contains(friendName) || entity.Member1.LastName.Contains(friendName)) &&
                                                                            entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed && entity.TransactionStatus == sublist).ToList();
                        }
                        else
                        {

                            totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.FirstName.Contains(friendName) ||
                               entity.Member.LastName.Contains(friendName)) && entity.DisputeStatus != null &&
                           entity.TransactionType == transactionTypeDisputed && entity.TransactionStatus == sublist).Count();

                            transactions = _dbContext.Transactions.Where(entity => (entity.Member.FirstName.Contains(friendName) ||
                                entity.Member.LastName.Contains(friendName)) && entity.DisputeStatus != null &&
                            entity.TransactionType == transactionTypeDisputed && entity.TransactionStatus == sublist).ToList();


                        }
                    }

                    #endregion
                }

                else
                {
                    #region If Nothing Sent for SubList

                    if (listType.ToUpper().Equals("ALL"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName))).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName))).ToList();

                    }
                    else if (listType.ToUpper().Equals("TRANSFER"))
                    {
                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                         entity.TransactionType == transactionTypeTransfer).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                        entity.TransactionType == transactionTypeTransfer).ToList();
                    }
                    else if (listType.ToUpper().Equals("SENT"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id) &&
                                                                      (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                       entity.TransactionType == transactionTypeTransfer).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                        entity.TransactionType == transactionTypeTransfer).ToList();
                    }
                    else if (listType.ToUpper().Equals("RECEIVED"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id &&
                                                                     (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                      entity.TransactionType == transactionTypeTransfer).Count();
                        transactions = _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id &&
                                                                      (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                       entity.TransactionType == transactionTypeTransfer).ToList();
                    }
                    else if (listType.ToUpper().Equals("REQUEST"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                        entity.TransactionType == transactionTypeRequest).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                      (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                       entity.TransactionType == transactionTypeRequest).ToList();


                    }
                    else if (listType.ToUpper().Equals("DONATION"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       entity.TransactionType == transactionTypeDonation).Count();
                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                        entity.TransactionType == transactionTypeDonation).ToList();
                    }
                    else if (listType.ToUpper().Equals("DISPUTED"))
                    {

                        totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                        entity.DisputeStatus != null).Count();

                        transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                       (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                        entity.DisputeStatus != null).ToList();

                        if (transactions.Any(x => x.Member.MemberId == id))
                        {

                            totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member1.FirstName.Contains(friendName) || entity.Member1.LastName.Contains(friendName)) &&
                                                                            entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed).Count();
                            transactions = _dbContext.Transactions.Where(entity => (entity.Member1.FirstName.Contains(friendName) || entity.Member1.LastName.Contains(friendName)) &&
                                                                          entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed).ToList();
                        }
                        else
                        {
                            totalRecordsCount = _dbContext.Transactions.Where(entity => (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                             entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed).Count();
                            transactions = _dbContext.Transactions.Where(entity => (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                            entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed).ToList();
                        }
                    }

                    #endregion
                }

                if (pageSize == 0 && pageIndex == 0)
                {
                    transactions = transactions.Take(1000).ToList();
                }
                else
                {
                    transactions = transactions.Skip(pageSize * pageIndex).Take(pageSize).ToList();
                }

                return transactions;
            }

            catch (Exception ex)
            {
                Logger.Error("TDA -> GetTransactionsSearchList FAILED - MemberID: [" + memberId + "], Exception: [" + ex.Message + "]");
                throw new Exception("Error in GetTransactionsSearchList");
            }
        }

        /// <summary>
        /// To get transaction detail
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="listType"></param>
        /// <param name="transactionId"></param>
        public Transaction GetTransactionDetail(string memberId, string listType, string transactionId)
        {
            Logger.Info("TDA - GetTransactionDetail Initiated -[MemberId: " + memberId + "], [TransactionId: " + transactionId + "]");

            var id = Utility.ConvertToGuid(memberId);
            var txnId = Utility.ConvertToGuid(transactionId);

            var transactions = new Transaction();

            if (listType.ToUpper().Equals("SENT"))
            {
                transactions = _dbContext.Transactions.FirstOrDefault(entity => entity.Member.MemberId == id && entity.TransactionId == txnId);
            }
            else if (listType.ToUpper().Equals("RECEIVED"))
            {
                transactions = _dbContext.Transactions.FirstOrDefault(entity => entity.Member1.MemberId == id && entity.TransactionId == txnId);
            }
            else if (listType.ToUpper().Equals("DISPUTED"))
            {
                transactions = _dbContext.Transactions.FirstOrDefault(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id)
                                                                       && entity.DisputeStatus != null && entity.TransactionId == txnId);
            }
            else // for withdraw
            {
                transactions = _dbContext.Transactions.FirstOrDefault(entity => (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                                                                entity.TransactionId == txnId);
            }

            return transactions != null ? transactions : new Transaction();
        }

        #region Dispute Related Methods
        /// <summary>
        /// Method called when a user raises a dispute about a transaction they previously sent.
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="recipientId"></param>
        /// <param name="transactionId"></param>
        /// <param name="listType"></param>
        /// <param name="ccCollection"></param>
        /// <param name="bccCollection"></param>
        /// <param name="subject"></param>
        /// <param name="bodyText"></param>
        public DisputeResultEntity RaiseDispute(string memberId, string recipientId, string transactionId, string listType, string ccCollection, string bccCollection, string subject, string bodyText)
        {
            Logger.Info("TDA - RaiseDispute Initiated - [MemberId: " + memberId + "], [TransactionId: " + transactionId + "]");

            string DisputeTId = "";
            string DisputeTransId = "";

            var senderId = Utility.ConvertToGuid(memberId);
            var receiverId = Utility.ConvertToGuid(recipientId);
            var txnId = Utility.ConvertToGuid(transactionId);
            var timeZoneKey = string.Empty;
            subject = "Nooch Transfer Disputed";
            var category = listType.ToUpper();
            var transaction = new Transaction();
            var sender = new Member();

            var notifications = new Collection<Notification>();
            if (category.Equals("SENT"))
            {
                transaction = _dbContext.Transactions.Where(transactionTemp => transactionTemp.TransactionId == txnId).FirstOrDefault();
            }

            if (transaction != null)
            {
                _dbContext.Entry(transaction).Reload();
                string disputeTrackingId = GetRandomDisputeTrackingId();
                transaction.DisputeTrackingId = disputeTrackingId;
                DisputeTId = disputeTrackingId.ToString();
                DisputeTransId = transaction.TransactionId.ToString();
                transaction.Subject = "Dispute recorded successfully.";

                var disputeStatus = CommonHelper.GetEncryptedData(Constants.DISPUTE_STATUS_REPORTED);

                transaction.DisputeStatus = disputeStatus;
                transaction.TransactionStatus = "Pending";
                transaction.TransactionType = "+C1+zhVafHdXQXCIqjU/Zg==";

                if (category.Equals("SENT"))
                {
                    transaction.RaisedBy = "Sender";
                    timeZoneKey = !string.IsNullOrEmpty(transaction.Member.TimeZoneKey) ? transaction.Member.TimeZoneKey : null;
                }

                transaction.DisputeDate = DateTime.Now;
                transaction.RaisedById = senderId;

                int result = _dbContext.SaveChanges();

                var mda = new MembersDataAccess();
                var notifSettings = CommonHelper.GetMemberNotificationSettingsByUserName(CommonHelper.GetDecryptedData(transaction.Member.UserName));

                if (result > 0)
                {
                    CreateNotifications(memberId, notifications, transaction.Member.UserName, "Support", disputeTrackingId, bodyText);
                    foreach (var m in notifications)
                    {
                        _dbContext.Notifications.Add(m);
                        _dbContext.SaveChanges();
                    }

                    if (category.Equals("SENT"))
                    {
                        sender = _dbContext.Members.Where(memberTemp => memberTemp.MemberId == senderId).FirstOrDefault();
                    }

                    if (sender != null)
                    {
                        sender.Status = Constants.STATUS_SUSPENDED;
                        sender.DateModified = DateTime.Now;
                        _dbContext.SaveChanges();
                        _dbContext.Entry(sender).Reload();
                        #region Send Email To User About Account Suspension

                        var tokens = new Dictionary<string, string>
							 {
								 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName))}
							 };

                        try
                        {
                            var fromAddress = Utility.GetValueFromConfig("adminMail");
                            string emailAddress = CommonHelper.GetDecryptedData(sender.UserName);
                            Logger.Error("SupendMember - Attempt to send mail for Supend Member[ memberId:" + sender.MemberId + "].");

                            Utility.SendEmail("userSuspended", fromAddress, emailAddress, null, "Your Nooch account has been suspended.", null, tokens, null, null, null); //'MailPriority.High' removed this bez of missmatch para -Surya
                        }
                        catch (Exception)
                        {
                            Logger.Error("TDA -> Raise Dispute - Supend Member email NOT send to [" + sender.MemberId + "]. Problem sending email.");
                        }

                        #endregion Send Email To User About Account Suspension
                    }


                    try
                    {
                        string supportMail = Utility.GetValueFromConfig("supportMail");
                        string s2 = transaction.Amount.ToString("n2");
                        string[] s3 = s2.Split('.');
                        string transferToUsername = "";

                        if (transaction.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" && transaction.InvitationSentTo != null)
                        {
                            transferToUsername = CommonHelper.GetDecryptedData(transaction.InvitationSentTo);
                        }
                        else if (transaction.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" && transaction.RaisedBy == "Sender")
                        {
                            transferToUsername = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member1.FirstName)) + " " +
                                                 CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member1.LastName));
                        }
                        else if (transaction.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" && transaction.RaisedById == receiverId)
                        {
                            transferToUsername = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member.FirstName)) + " " +
                                                 CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member.LastName));
                        }
                        else if (transaction.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")
                        {
                            transferToUsername = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member.FirstName)) + " " +
                                                 CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member.LastName));
                        }
                        else
                        {
                            transferToUsername = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member1.FirstName)) + " " +
                                                 CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transaction.Member1.LastName));
                        }
                        // get name and other details of sender to send email

                        var tokens = new Dictionary<string, string>
												 {
													 {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName))},
													 {Constants.PLACEHOLDER_NEWUSER,transferToUsername},
													 {Constants.PLACEHLODER_CENTS, s3[1].ToString()},
                                                     {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
													 {Constants.PLACEHOLDER_TRANSACTION_CANCELLED_ON, Convert.ToDateTime(transaction.DisputeDate).ToString("MMMM dd, yyyy")},
													 {Constants.PLACEHOLDER_DISPUTEID, DisputeTId}
												 };

                        Utility.SendEmail("disputeRaised", supportMail, CommonHelper.GetDecryptedData(transaction.Member.UserName), null, subject, null, tokens, ccCollection, bccCollection, bodyText);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RaiseDispute - Dispute raised email NOT sent to [" + CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], [Exception: " + ex + "]");

                        return new DisputeResultEntity
                        {
                            Result = "Failure"
                        };
                    }

                    return new DisputeResultEntity
                    {
                        Result = "Dispute has been reported for this transaction. Please note your dispute tracking id: " + disputeTrackingId,
                        DisputeId = disputeTrackingId,
                        DisputeDate = !string.IsNullOrEmpty(timeZoneKey) ? mda.GMTTimeZoneConversion(DateTime.Now.ToString(), timeZoneKey) : DateTime.Now.ToString()
                    };
                }
            }
            return new DisputeResultEntity
            {
                Result = "Transaction not found."
            };
        }

        /// <summary>
        /// To get random alphanumeric 5 digit dispute tracking id.
        /// </summary>
        /// <returns>Nooch Random ID</returns>
        /// 

        public string GetRandomDisputeTrackingId()
        {
            var random = new Random();
            int j = 1;

            for (int i = 0; i <= j; i++)
            {
                var randomId = new string(
                    Enumerable.Repeat(Chars, 5)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());

                var transactionEntity = _dbContext.Transactions.Where(memberTemp => memberTemp.DisputeTrackingId == randomId).FirstOrDefault();

                if (transactionEntity == null)
                {
                    return randomId;
                }

                j += i + 1;
            }
            return null;
        }

        #endregion Dispute Related Methods

        private static void CreateNotifications(string memberId, Collection<Notification> notifications, string mailId, string templateType, string disputeTrackingId, string bodyText)
        {
            var notification = new Notification
            {
                DateCreated = DateTime.Now,
                DateSent = DateTime.Now,
                MailContent = bodyText,
                NotificationId = Guid.NewGuid(),
                Recepient_UserName = mailId,
                SenderId = Utility.ConvertToGuid(memberId),
                TemplateType = templateType,
                DisputeTrackingId = disputeTrackingId
            };

            notifications.Add(notification);
        }

        public string CancelRejectTransaction(string transactionId, string userResponse)
        {
            Logger.Info("TDA -> CancelRejectTransaction Initiated - [transactionId: " + transactionId + "], [userResponse: " + userResponse + "]");

            if (userResponse == "Rejected" || userResponse == "Cancelled")
            {
                try
                {
                    var transId = Utility.ConvertToGuid(transactionId);

                    var transactionDetail = _dbContext.Transactions.Where(memberTemp => memberTemp.TransactionId == transId).FirstOrDefault();

                    transactionDetail.TransactionStatus = userResponse;

                    _dbContext.SaveChanges();
                    _dbContext.Entry(transactionDetail).Reload();

                    // 'Members' IS THE REQUEST REJECTOR
                    // 'Members1' IS THE REQUEST SENDER
                    if (userResponse == "Rejected")
                    {
                        // sending push notification to request maker
                        Guid RequestSenderId = Utility.ConvertToGuid(transactionDetail.Member1.MemberId.ToString());

                        var noochMemberfornotification = _dbContext.Members.Where(memberTemp =>
                                memberTemp.MemberId.Equals(RequestSenderId) && memberTemp.IsDeleted == false && memberTemp.ContactNumber != null && memberTemp.IsVerifiedPhone == true).FirstOrDefault();

                        if (noochMemberfornotification != null)
                        {
                            try
                            {
                                string mailBodyText = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName))
                                      + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName))
                                      + " just declined your Nooch payment request.";

                                Utility.SendNotificationMessage(mailBodyText, 0, null,
                                           noochMemberfornotification.DeviceToken,
                                           Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));
                                Logger.Info("TDA -> Request Denied - Push notification sent to [" + noochMemberfornotification.UDID1 + "] sucessfully");
                            }
                            catch (Exception)
                            {
                                Logger.Info("TDA -> Request Denied - Push notification NOT sent to [" + noochMemberfornotification.UDID1 + "]");
                            }
                        }

                        // sending email TO USER THAT SENT THE REQUEST about rejection
                        string reqSenderFirstName = "";
                        string reqRejectorFullName = "";
                        string reqRejectorFirstName = "";

                        reqSenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                        reqRejectorFullName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName)) + " " +
                                               CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                        reqRejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));

                        string s2 = transactionDetail.Amount.ToString("n2");
                        string[] s3 = s2.Split('.');

                        string memo = "";
                        if (transactionDetail.Memo != null && transactionDetail.Memo != "")
                        {
                            if (transactionDetail.Memo.Length > 3)
                            {
                                string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = transactionDetail.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + transactionDetail.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + transactionDetail.Memo.ToString();
                            }
                        }

                        var tokens = new Dictionary<string, string>
										{
											{Constants.PLACEHOLDER_FIRST_NAME,reqSenderFirstName},
											{Constants.PLACEHOLDER_RECIPIENT_FULL_NAME,reqRejectorFullName},
											{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
											{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
											{Constants.PLACEHOLDER_RECIPIENT_FIRST_NAME,reqRejectorFirstName},
											{Constants.MEMO, memo}
										};

                        var fromAddress = Utility.GetValueFromConfig("transfersMail");
                        var toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToSender",
                                fromAddress, toAddress, null,
                                CommonHelper.UppercaseFirst(
                                    CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName)) + " " + CommonHelper.UppercaseFirst(
                                    CommonHelper.GetDecryptedData(transactionDetail.Member.LastName)) + " denied your payment request", null,
                                tokens, null, null, null);

                            Logger.Info("TDA -> requestDeniedToSender - Email sent to [" + toAddress + "] successfully");
                        }
                        catch (Exception)
                        {
                            Logger.Info("TDA -> requestDeniedToSender FAILED - Email NOT sent to [" + toAddress + "]");
                        }

                        // sending email to user who REJECTED this request (ie.: RECIPIENT)

                        string reqSenderFullName = "";

                        reqRejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                        reqSenderFullName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName)) + " " +
                                          CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));

                        var tokens2 = new Dictionary<string, string>
										{
											{Constants.PLACEHOLDER_FIRST_NAME,reqRejectorFirstName},
											{Constants.PLACEHOLDER_SENDER_FULL_NAME,reqSenderFullName},
											{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
											{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
											{Constants.MEMO, memo}
										};

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToRecipient",
                                fromAddress, toAddress, null,
                                "You rejected a Nooch request from " + reqSenderFullName, null,
                                tokens2, null, null, null);

                            Logger.Info("TDA -> requestDeniedToRecipient - Email sent to [" + toAddress + "] successfully");
                        }
                        catch (Exception)
                        {
                            Logger.Info("TDA -> requestDeniedToRecipient FAILED - Email NOT sent to [" + toAddress + "]");
                        }
                    }

                    return "success";
                }
                catch (Exception ex)
                {
                    Logger.Error("TDA -> CancelRejectTransaction Initiated - [transactionId: " + transactionId + "], Exception: [" + ex.Message + "]");
                    return "";
                }
            }
            else
            {
                return "";
            }
        }


        #region Reject Transaction (All Types) Methods

        // For Non-Nooch users who Reject a Transfer/Invite sent to them.  NOTE: Not for Requests, that's the next method: RejectMoneyRequestForNonNoochUser()
        // CLIFF (JULY 10, 2015) IS THIS METHOD STILL USED NOW THAT WE HAVE THE 'COMMON' METHOD FOR REJECTING??  IF NOT, LET'S DELETE...
        public string RejectMoneyforNonNoochUser(string transactionId)
        {
            Logger.Info("TDA -> RejectMoneyforNonNoochUser Initiated - [transactionId: " + transactionId + "]");

            try
            {
                var transId = Utility.ConvertToGuid(transactionId);

                var transactionDetail = new Transaction();

                transactionDetail = _dbContext.Transactions.FirstOrDefault(memberTemp => memberTemp.TransactionId == transId && memberTemp.TransactionStatus == "Pending"
                    && (memberTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" || memberTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")); // "Invite" or "Request";


                if (transactionDetail != null)
                {                    
                    #region IfSomethingFound

                    transactionDetail.TransactionStatus = "Rejected";

                    _dbContext.SaveChanges();
                    _dbContext.Entry(transactionDetail).Reload();
                    string TransRecipientEmail = "";
                    string TransRecipientPhone = "";
                    string receiverPhoneNumFormatted = "";
                    string TransMakerFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                    string TransMakerLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));

                    #region Push Notification to Sender

                    string pushBodyText = "";
                    if (transactionDetail.IsPhoneInvitation == true &&
                        transactionDetail.PhoneNumberInvited != null)
                    {
                        receiverPhoneNumFormatted = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        if (receiverPhoneNumFormatted.Length == 10)
                        {
                            receiverPhoneNumFormatted = "(" + receiverPhoneNumFormatted;
                            receiverPhoneNumFormatted = receiverPhoneNumFormatted.Insert(4, ")");
                            receiverPhoneNumFormatted = receiverPhoneNumFormatted.Insert(5, " ");
                            receiverPhoneNumFormatted = receiverPhoneNumFormatted.Insert(9, "-");
                        }

                        TransRecipientPhone = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        pushBodyText = receiverPhoneNumFormatted + " declined your Nooch payment.";
                    }
                    else if (transactionDetail.InvitationSentTo != null)
                    {
                        TransRecipientEmail = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);
                        pushBodyText = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo) +
                                              " declined your Nooch payment.";
                    }

                    // sending push notification to money sender
                    Guid RecId = Utility.ConvertToGuid(transactionDetail.Member.MemberId.ToString());


                    var noochMemberfornotification = new Member();
                    noochMemberfornotification = _dbContext.Members.FirstOrDefault(memberTemp =>
                            memberTemp.MemberId.Equals(transactionDetail.Member1.MemberId) &&
                            memberTemp.IsDeleted == false && memberTemp.ContactNumber != null &&
                            memberTemp.IsVerifiedPhone == true);


                    if (noochMemberfornotification != null)
                    {
                        try
                        {
                            _dbContext.Entry(noochMemberfornotification).Reload();

                            Utility.SendNotificationMessage(pushBodyText, 1, null,
                          noochMemberfornotification.DeviceToken,
                          Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));
                            Logger.Info("TDA -> RejectMoneyforNonNoochUser - Push notification sent to [" +
                                                   noochMemberfornotification.UDID1 + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Info("TDA -> RejectMoneyforNonNoochUser - Push notification NOT sent to [" + noochMemberfornotification.UDID1 + "].");
                        }
                    }

                    #endregion

                    // Send email to Sender about rejection

                    #region memo
                    string memo = "";
                    if (transactionDetail.Memo != null && transactionDetail.Memo != "")
                    {
                        if (transactionDetail.Memo.Length > 3)
                        {
                            string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transactionDetail.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transactionDetail.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transactionDetail.Memo.ToString();
                        }
                    }
                    #endregion

                    string wholeAmount = transactionDetail.Amount.ToString("n2");
                    string[] s3 = wholeAmount.Split('.');

                    var tokens = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, TransMakerFirstName},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.PLACEHOLDER_RECIPIENT_FULL_NAME, receiverPhoneNumFormatted},
								{Constants.MEMO, memo}
							};

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");
                    var toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                    try
                    {
                        Utility.SendEmail("transferToNewUserRejected_ToSender", fromAddress, toAddress, null, receiverPhoneNumFormatted + " denied your payment request", null,
                            tokens, null, null, null);
                        Logger.Info("transferDeniedToSender - transferToNewUserRejected_ToSender E-mail sent to [" + toAddress + "].");
                    }
                    catch (Exception)
                    {
                        Logger.Info(
                            "requestDeniedToSender - transferToNewUserRejected_ToSender E-mail NOT sent to [" + toAddress + "].");
                    }


                    #region Notification to request denier either through email or sms

                    if (transactionDetail.IsPhoneInvitation != null && transactionDetail.IsPhoneInvitation == true &&
                                       transactionDetail.PhoneNumberInvited != null)
                    {
                        // Send SMS to request denier

                        string MessageToRecepient = "This is just confirmation that you denied a $" +
                            wholeAmount.ToString() + " payment from " + TransMakerFirstName + " " + TransMakerLastName + ".";

                        // Removing extra stuff from phone number

                        TransRecipientPhone = TransRecipientPhone.Replace("(", "");
                        TransRecipientPhone = TransRecipientPhone.Replace(")", "");
                        TransRecipientPhone = TransRecipientPhone.Replace(" ", "");
                        TransRecipientPhone = TransRecipientPhone.Replace("-", "");

                        string s = Utility.SendSMS(TransRecipientPhone, MessageToRecepient);
                    }
                    else
                    {
                        // sending email to user who Rejected this Transfer in case invitation was using email
                        #region If Email Invitation
                        var tokens2 = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, TransRecipientEmail},
								{Constants.PLACEHOLDER_SENDER_FULL_NAME, TransMakerFirstName + " " + TransMakerLastName},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.MEMO, memo}
							};

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);

                        try
                        {
                            Utility.SendEmail("transferToNewUserRejected_ToRecipient", fromAddress, toAddress, null,
                                "You rejected a Nooch request from " + TransMakerFirstName + " " + TransMakerLastName,
                                null, tokens2, null, null, null);

                            Logger.Info(
                                "TDA -> transferToNewUserRejected_ToRecipient E-mail sent to [" +
                                toAddress + "].");
                        }
                        catch (Exception)
                        {
                            Logger.Info(
                                "TDA -> transferToNewUserRejected_ToRecipient E-mail not sent to [" +
                                toAddress + "]. Problem occurred in sending mail.");
                        }
                        #endregion
                    }
                    #endregion

                    return "success";

                    #endregion
                }
                else
                {
                    return "";
                }
            }
            catch (Exception)
            {
                return "";
            }

        }

        // REQUEST REJECTED by NON-NOOCH user.... emails to be sent to both requester and request receiver
        // request sender = who requested for money and will get money transferred into his account
        // request receiver = who will get this request and pay the requester......
        public string RejectMoneyRequestForNonNoochUser(string TrannsactionId)
        {
            Logger.Info("TDA -> RejectMoneyRequestForNonNoochUser Initiated - [transactionId: " + TrannsactionId + "]");

            try
            {
                var transId = Utility.ConvertToGuid(TrannsactionId);

                var transactionDetail = new Transaction();
                transactionDetail = _dbContext.Transactions.FirstOrDefault(memberTemp => memberTemp.TransactionId == transId &&
                            memberTemp.TransactionStatus == "Pending" &&
                           (memberTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" || memberTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="));


                if (transactionDetail != null)
                {
                    #region IfSomethingFound

                    transactionDetail.TransactionStatus = "Rejected";
                    _dbContext.SaveChanges();
                    _dbContext.Entry(transactionDetail).Reload();

                    string wholeAmount = transactionDetail.Amount.ToString("n2");
                    string[] s3 = wholeAmount.Split('.');

                    string TransRecipId = "";
                    string TransMakerFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName)); ;
                    string TransMakerLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName)); ;

                    if (transactionDetail.IsPhoneInvitation == true && transactionDetail.PhoneNumberInvited != null &&
                        transactionDetail.InvitationSentTo == null)
                    {
                        TransRecipId = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        if (TransRecipId.Length == 10)
                        {
                            TransRecipId = "(" + TransRecipId;
                            TransRecipId = TransRecipId.Insert(4, ")");
                            TransRecipId = TransRecipId.Insert(5, " ");
                            TransRecipId = TransRecipId.Insert(9, "-");
                        }
                    }
                    else
                    {
                        TransRecipId = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);
                    }


                    // Send push notification to Transaction Sender (always an existing user)
                    #region Push Notification to Request Sender

                    Guid RecId = Utility.ConvertToGuid(transactionDetail.Member.MemberId.ToString());

                    var noochMemberfornotification = new Member();

                    noochMemberfornotification = _dbContext.Members.FirstOrDefault(memberTemp =>
                            memberTemp.MemberId.Equals(transactionDetail.Member1.MemberId) &&
                            memberTemp.IsDeleted == false && memberTemp.ContactNumber != null &&
                            memberTemp.IsVerifiedPhone == true);


                    if (noochMemberfornotification != null)
                    {
                        try
                        {
                            string mailBodyText = TransRecipId + " just rejected your Nooch payment request for $" + wholeAmount + ".";
                            Utility.SendNotificationMessage(mailBodyText, 1, null,
                                noochMemberfornotification.DeviceToken,
                                Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));

                            Logger.Info("TDA -> RejectMoneyRequestForNonNoochUser - Push notification sent to [" + noochMemberfornotification.UDID1 + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Info("TDA -> RejectMoneyRequestForNonNoochUser - Push notification not sent to [" + noochMemberfornotification.UDID1 + "].");
                        }
                    }

                    #endregion

                    // Send email to requester about rejection
                    string reqSenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                    string reqSenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));
                    string reqRecUsername = "";

                    #region memo
                    string memo = "";
                    if (transactionDetail.Memo != null && transactionDetail.Memo != "")
                    {
                        if (transactionDetail.Memo.Length > 3)
                        {
                            string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transactionDetail.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transactionDetail.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transactionDetail.Memo.ToString();
                        }
                    }
                    #endregion

                    reqRecUsername = TransRecipId;

                    var tokens = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, TransMakerFirstName},
								{Constants.PLACEHOLDER_RECEPIENT_FULL_NAME, TransRecipId},
								{Constants.PLACEHOLDER_RECEPIENT_FIRST_NAME, TransRecipId},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.MEMO, memo}
							};

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");
                    var toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                    try
                    {
                        Utility.SendEmail("requestDeniedToSender", fromAddress, toAddress, null, TransRecipId + " denied your payment request", null,
                            tokens, null, null, null);

                        Logger.Info("TDA -> RejectMoneyRequestForNonNoochUser - requestDeniedToSender status email sent to [" +
                                               toAddress + "] successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            "TDA -> RejectMoneyRequestForNonNoochUser - requestDeniedToSender email NOT sent to [" + toAddress +
                            "], Exception: [" + ex.Message + "]");
                    }

                    // sending email to user who rejected this request

                    // SMS if sent using Phone Number.
                    if (transactionDetail.IsPhoneInvitation == true && transactionDetail.PhoneNumberInvited != null &&
                        transactionDetail.InvitationSentTo == null)
                    {
                        // code to send sms
                        string MessageToRecepient = "This is just confirmation that you denied a $" +
                            wholeAmount.ToString() + " request from " + TransMakerFirstName + " " + TransMakerLastName + ".";

                        TransRecipId = TransRecipId.Replace("(", "");
                        TransRecipId = TransRecipId.Replace(")", "");
                        TransRecipId = TransRecipId.Replace(" ", "");
                        TransRecipId = TransRecipId.Replace("-", "");

                        string s = Utility.SendSMS(TransRecipId, MessageToRecepient);
                    }
                    else
                    {
                        // Sending email if it wasn't sent via phone number
                        var tokens2 = new Dictionary<string, string>
							{
								{Constants.PLACEHOLDER_FIRST_NAME, TransRecipId},
								{Constants.PLACEHOLDER_SENDER_FULL_NAME, TransMakerFirstName + " " + TransMakerLastName},
								{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
								{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
								{Constants.MEMO, memo}
							};

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);

                        try
                        {
                            Utility.SendEmail("requestDeniedToRecipient", fromAddress, toAddress, null,
                                "You rejected a payment request from " + TransMakerFirstName + " " + TransMakerLastName, null,
                                tokens2, null, null, null);

                            Logger.Info("TDA -> RejectMoneyRequestForNonNoochUser - requestDeniedToRecipient email sent to [" +
                                                   toAddress + "] successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RejectMoneyRequestForNonNoochUser - requestDeniedToRecipient email NOTsent to [" +
                                                   toAddress + "], Exception: [" + ex.Message + "]");
                        }
                    }

                    return "Request Rejected Successfully.";

                    #endregion
                }
                else
                {
                    Logger.Error("TDA -> RejectMoneyRequestForNonNoochUser FAILED - Transaction not found! - TransactionID: [" + transactionDetail + "]");
                    return "";
                }
                //}
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> RejectMoneyRequestForNonNoochUser OUTER EXCEPTION - Exception: [" + ex + "]");
                return "";
            }
        }

        // CREATED: JULY 2015
        // NOTE: Created specifically for the new combined landing page (/trans/RejectMoney.aspx) for rejecting all types of transfers.
        public string RejectMoneyCommon(string TransactionId, string UserType, string LinkSource, string TransType)
        {
            Logger.Info("TDA -> RejectMoneyCommon Initiated - [TransactionId: " + TransactionId +
                                   "], TransType: [" + TransType + "], UserType: [" + UserType + "]");

            try
            {
                var transId = Utility.ConvertToGuid(TransactionId);
                var transactionDetail = new Transaction();
                transactionDetail = _dbContext.Transactions.FirstOrDefault(entity => entity.TransactionId == transId && entity.TransactionStatus == "Pending" //&& (memberTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" || memberTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==")   -- need to discuss with Cliff
                    );


                if (transactionDetail != null)
                {
                    #region If Transaction Found

                    transactionDetail.TransactionStatus = "Rejected";
                    int saveChanges = _dbContext.SaveChanges();
                    _dbContext.Entry(transactionDetail).Reload();

                    string wholeAmount = transactionDetail.Amount.ToString("n2");
                    string[] s3 = wholeAmount.Split('.');

                    #region Memo
                    string memo = "";
                    if (transactionDetail.Memo != null && transactionDetail.Memo != "")
                    {
                        if (transactionDetail.Memo.Length > 3)
                        {
                            string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transactionDetail.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transactionDetail.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transactionDetail.Memo.ToString();
                        }
                    }
                    #endregion Memo

                    #region Setting all variables depending upon trans type

                    string fromAddress = Utility.GetValueFromConfig("transfersMail");
                    string toAddress = "";

                    string SenderFirstName = "";
                    string SenderLastName = "";
                    string SenderFullName = "";

                    string RejectorFirstName = "";
                    string RejectorLastName = "";
                    string RejectorFullName = "";

                    string RejectorEmail = "";
                    string RejectorPhone = "";

                    // Depends on what transaction type 
                    //   5dt4HUwCue532sNmw3LKDQ==   -- Transfer
                    //   DrRr1tU1usk7nNibjtcZkA==   -- Invite
                    //   T3EMY1WWZ9IscHIj3dbcNw==   -- Request

                    var transTypeDecr = CommonHelper.GetDecryptedData(TransType);
                    var userTypeDecr = CommonHelper.GetDecryptedData(UserType);

                    // TransType = request,  UserType = Existing or NonRegistered
                    if (transTypeDecr == "Request" &&
                        (userTypeDecr == "Existing" || userTypeDecr == "NonRegistered"))
                    {
                        Logger.Info("TDA -> RejectMoneyCommon - CHECKPOINT 1 REACHED - User Type = Existing, TransType = Requst");
                        // Request sent to existing user -- 'Rejector' is SenderId in transactionDetail
                        SenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                        SenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));
                        RejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                        RejectorLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                        RejectorFullName = RejectorFirstName + " " + RejectorLastName;
                    }

                    // UserType = NEW, TransType = REQUEST or INVITE
                    else if (userTypeDecr == "New" &&
                             (transTypeDecr == "Request" || transTypeDecr == "Invite"))
                    {
                        Logger.Info("TDA -> RejectMoneyCommon - CHECKPOINT 2 REACHED - User Type = New, TransType = Requst or Invite, Link Source = Email");
                        // Request sent to Non-Nooch user -- 'Rejector' is email address InvitationSentTo in transactionDetail
                        SenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                        SenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                        RejectorFirstName = "";
                        RejectorLastName = "";

                        // LinkSource = EMAIL
                        if (LinkSource == "75U7bZRpVVxLNbQuoMQEGQ==" ||
                            (!String.IsNullOrEmpty(transactionDetail.InvitationSentTo) &&
                            (transactionDetail.IsPhoneInvitation == false ||
                             transactionDetail.IsPhoneInvitation == null)))
                        {
                            RejectorEmail = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);
                        }

                        //LinkSource = PHONE
                        if (LinkSource == "Um3I3RNHEGWqKM9MLsQ1lg==" ||
                            (String.IsNullOrEmpty(transactionDetail.InvitationSentTo) &&
                            (transactionDetail.IsPhoneInvitation != false ||
                             transactionDetail.IsPhoneInvitation != null)))
                        {
                            RejectorPhone = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
                        }
                    }

                    #endregion Setting all variables depending upon trans type


                    #region Notification And Email for transaction Between Existing users

                    // UserType = EXISTING, TransType = REQUEST
                    if (transTypeDecr == "Request" &&
                        (userTypeDecr == "Existing" || userTypeDecr == "NonRegistered"))
                    {
                        Guid RequestSenderId = Utility.ConvertToGuid(transactionDetail.Member1.MemberId.ToString());

                        var memberObj = new Member();
                        memberObj = _dbContext.Members.FirstOrDefault(memberTemp =>
                                            memberTemp.MemberId.Equals(RequestSenderId) &&
                                            memberTemp.IsDeleted == false &&
                                            memberTemp.ContactNumber != null &&
                                            memberTemp.IsVerifiedPhone == true);


                        if (memberObj != null && !String.IsNullOrEmpty(memberObj.DeviceToken))
                        {
                            try
                            {
                                string mailBodyText = RejectorFullName + " just denied your payment request for $" + wholeAmount + ".";

                                Utility.SendNotificationMessage(mailBodyText, 0, null,
                                    memberObj.DeviceToken,
                                    Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));

                                Logger.Info("TDA -> RejectMoneyCommon Request Denied - Push notification sent to [" +
                                                       memberObj.UDID1 + "] successfully");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> RejectMoneyCommon Request Denied - Push notification NOT sent to [" +
                                                       memberObj.UDID1 + "], Exception: [" + ex.Message + "]");
                            }
                        }

                        var tokens = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, SenderFirstName},
                                {Constants.PLACEHOLDER_RECIPIENT_FULL_NAME, RejectorFullName},
                                {Constants.PLACEHLODER_CENTS, s3[1].ToString()},
                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
                                {Constants.PLACEHOLDER_RECIPIENT_FIRST_NAME, RejectorFirstName},
                                {Constants.MEMO, memo}
                            };

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToSender", fromAddress, toAddress, null, RejectorFullName +
                                " denied your payment request", null,
                                tokens, null, null, null);

                            Logger.Info("TDA -> RejectMoneyCommon requestDeniedToSender - Email sent to [" +
                                                   toAddress + "].");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RejectMoneyCommon requestDeniedToSender - Email NOT sent to [" + toAddress +
                                "], Exception: [" + ex.Message + "]");
                        }


                        // Send email to user who REJECTED this request (ie.: RECIPIENT) - will be an EXISTING user

                        SenderFullName = SenderFirstName + " " + SenderLastName;

                        var tokens2 = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, RejectorFirstName},
                                {Constants.PLACEHOLDER_SENDER_FULL_NAME, SenderFullName},
                                {Constants.PLACEHLODER_CENTS, s3[1].ToString()},
                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
                                {Constants.MEMO, memo}
                            };

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToRecipient", fromAddress, toAddress, null,
                                "You rejected a payment request from " + SenderFullName, null,
                                tokens2, null, null, null);

                            Logger.Info("TDA -> RejectMoneyCommon - requestDeniedToRecipient - Email sent to [" +
                                                   toAddress + "] successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(
                                "TDA -> RejectMoneyCommon requestDeniedToRecipient - Email NOT sent to [" + toAddress +
                                "], Exception: [" + ex.Message + "]");
                        }

                        return "Success";
                    }

                    #endregion


                    #region Notifications when trans is b/t EXISTING user and NON-NOOCH user

                    // UserType = NEW,  TransType = REQUEST or INVITE,  LinkSource = EMAIL or PHONE
                    if (userTypeDecr == "New" &&
                        (transTypeDecr == "Request" || transTypeDecr == "Invite") &&
                        (LinkSource == "75U7bZRpVVxLNbQuoMQEGQ==" || LinkSource == "Um3I3RNHEGWqKM9MLsQ1lg=="))
                    {
                        string TransRecipId = "";

                        if (transactionDetail.IsPhoneInvitation == true &&
                            transactionDetail.PhoneNumberInvited != null &&
                            transactionDetail.InvitationSentTo == null)
                        {
                            TransRecipId = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);

                            if (TransRecipId.Length == 10)
                            {
                                TransRecipId = "(" + TransRecipId;
                                TransRecipId = TransRecipId.Insert(4, ")");
                                TransRecipId = TransRecipId.Insert(5, " ");
                                TransRecipId = TransRecipId.Insert(9, "-");
                            }
                        }
                        else
                        {
                            TransRecipId = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);
                        }

                        #region Notify Sender

                        #region Push Notification to Request Sender

                        // Send push notification to Sender (who is always an existing user)
                        Guid RecId = Utility.ConvertToGuid(transactionDetail.Member.MemberId.ToString());

                        var noochMemberfornotification = new Member();
                        noochMemberfornotification = _dbContext.Members.FirstOrDefault(memberTemp =>
                                memberTemp.MemberId.Equals(transactionDetail.Member1.MemberId) &&
                                memberTemp.IsDeleted == false && memberTemp.ContactNumber != null &&
                                memberTemp.IsVerifiedPhone == true);


                        if (noochMemberfornotification != null)
                        {
                            try
                            {
                                string pushBodyText = TransRecipId + " just rejected your payment request for $" + wholeAmount + ".";

                                Utility.SendNotificationMessage(pushBodyText, 1, null,
                                    noochMemberfornotification.DeviceToken,
                                    Utility.GetValueFromConfig("AppKey"), Utility.GetValueFromConfig("MasterSecret"));

                                Logger.Info("TDA -> RejectMoneyCommon - Push notification sent to [" +
                                                       noochMemberfornotification.UDID1 + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> RejectMoneyCommon - Push notification NOT sent to [" +
                                                       noochMemberfornotification.UDID1 + "], Exception: [" + ex.Message + "]");
                            }
                        }

                        #endregion Push Notification to Request Sender

                        #region Send email to Request Sender

                        var tokens = new Dictionary<string, string>
                            {
                                {Constants.PLACEHOLDER_FIRST_NAME, SenderFirstName},
                                {Constants.PLACEHOLDER_RECEPIENT_FULL_NAME, TransRecipId},
                                {Constants.PLACEHOLDER_RECEPIENT_FIRST_NAME, TransRecipId},
                                {Constants.PLACEHLODER_CENTS, s3[1].ToString()},
                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
                                {Constants.MEMO, memo}
                            };

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToSender", fromAddress, toAddress, null, TransRecipId + " denied your payment request", null,
                                tokens, null, null, null);

                            Logger.Info("TDA -> RejectMoneyCommon requestDeniedToSender email sent to [" +
                                                   toAddress + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RejectMoneyCommon requestDeniedToSender email NOT sent to [" +
                                                   toAddress + "], Exception [" + ex.Message + "]");
                        }

                        #endregion Send email to Request Sender

                        #endregion Notify Sender

                        // Now notify (via email or SMS) user who rejected this request
                        #region Notify user that just rejected this transaction

                        // SMS if sent using Phone Number.
                        #region Notify rejector by SMS

                        if (transactionDetail.IsPhoneInvitation == true &&
                            transactionDetail.PhoneNumberInvited != null &&
                            transactionDetail.InvitationSentTo == null)
                        {
                            // code to send sms
                            string MessageToRecepient = "This is just confirmation that you denied a $" +
                                                        wholeAmount.ToString() + " request from " +
                                                        SenderFirstName + " " + SenderLastName + ".";

                            TransRecipId = TransRecipId.Replace("(", "");
                            TransRecipId = TransRecipId.Replace(")", "");
                            TransRecipId = TransRecipId.Replace(" ", "");
                            TransRecipId = TransRecipId.Replace("-", "");

                            string s = Utility.SendSMS(TransRecipId, MessageToRecepient);
                        }

                        #endregion Notify rejector by SMS

                        #region Notify rejector by Email

                        else
                        {
                            // Sending email if this transaction was not send using a phone number
                            var tokens2 = new Dictionary<string, string>
                                {
                                    {Constants.PLACEHOLDER_FIRST_NAME, TransRecipId},
                                    {Constants.PLACEHOLDER_SENDER_FULL_NAME, SenderFirstName + " " + SenderLastName},
                                    {Constants.PLACEHLODER_CENTS, s3[1].ToString()},
                                    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
                                    {Constants.MEMO, memo}
                                };

                            toAddress = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);

                            try
                            {
                                Utility.SendEmail("requestDeniedToRecipient", fromAddress, toAddress, null,
                                    "You rejected a Nooch request from " + SenderFirstName + " " + SenderLastName,
                                    null, tokens2, null, null, null);

                                Logger.Info(
                                    "TDA -> RejectMoneyCommon - requestDeniedToRecipient status email sent to [" +
                                    toAddress + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(
                                    "TDA -> RejectMoneyCommon - requestDeniedToRecipient email NOT sent to [" +
                                    toAddress + "], Exception: [" + ex.Message + "]");
                            }
                        }

                        #endregion Notify rejector by Email

                        return "Success";

                        #endregion
                    }


                    #endregion Notifications when trans is b/t EXISTING user and NON-NOOCH user

                    if (saveChanges > 0)
                    {
                        return "Success.";
                    }

                    #endregion If Transaction Found
                }
                else
                {
                    Logger.Info("TDA -> RejectMoneyCommon FAILED - Transaction not pending or not found! - TransactionID: [" + TransactionId + "]");
                    return "Transaction no more pending or not found.";
                }


                return "Server Error.";
            }
            catch (Exception ex)
            {
                Logger.Info("TDA -> RejectMoneyCommon FAILED - [Exception: " + ex.ToString() + "]");
                return "Server Error.";
            }
        }


        #endregion Reject Transaction (All Types) Methods


        public SynapseV3AddTrans_ReusableClass AddTransSynapseV3Reusable(string sender_oauth, string sender_fingerPrint,
            string sender_bank_node_id, string amount, string fee, string receiver_oauth, string receiver_fingerprint,
            string receiver_bank_node_id, string suppID_or_transID, string senderUserName, string receiverUserName, string iPForTransaction, string senderLastName, string recepientLastName)
        {
            Logger.Info("TDA -> SynapseV3AddTrans_ReusableClass Initiated - [Sender Username: " + senderUserName + "], " +
                                   "[Recipient Username: " + receiverUserName + "], [Amount: " + amount + "]");

            SynapseV3AddTrans_ReusableClass res = new SynapseV3AddTrans_ReusableClass();
            res.success = false;

            try
            {
                bool SenderSynapsePermissionOK = false;
                bool RecipientSynapsePermissionOK = false;

                #region Check Sender Synapse Permissions

                // 1. Check USER permissions for SENDER
                synapseSearchUserResponse senderPermissions = CommonHelper.getUserPermissionsForSynapseV3(senderUserName);

                if (senderPermissions == null || !senderPermissions.success)
                {
                    Logger.Error("TDA -> SynapseV3AddTrans_ReusableClass - SENDER's Synapse Permissions were NULL or not successful :-(");

                    res.ErrorMessage = "Problem with senders synapse user permission.";
                    return res;
                }

                // 2. Check BANK/NODE permission for SENDER
                if (senderPermissions.users != null && senderPermissions.users.Length > 0)
                {
                    foreach (synapseSearchUserResponse_User senderUser in senderPermissions.users)
                    {
                        // iterating each node inside

                        if (senderUser.nodes != null && senderUser.nodes.Length > 0)
                        {
                            NodePermissionCheckResult nodePermCheckRes = CommonHelper.IsNodeActiveInGivenSetOfNodes(senderUser.nodes, sender_bank_node_id);

                            if (nodePermCheckRes.IsPermissionfound == true)
                            {
                                if (nodePermCheckRes.PermissionType == "CREDIT-AND-DEBIT" || nodePermCheckRes.PermissionType == "DEBIT")
                                {
                                    SenderSynapsePermissionOK = true;
                                }
                                // iterate through all users
                                //else
                                //{
                                //    res.success = false;
                                //    res.ErrorMessage = "Sender doesn't have permission to send money from account.";
                                //    return res;
                                //}
                            }
                            // iterate through all users
                            //else
                            //{
                            //    res.success = false;
                            //    res.ErrorMessage = "Sender doesn't have permission to send money from account.";
                            //    return res;
                            //}
                        }
                    }
                }
                #endregion Check Sender Synapse Permissions

                #region Check Recipient Synapse Permissions

                // 3. Check USER permissions for RECIPIENT
                synapseSearchUserResponse recepientPermissions = CommonHelper.getUserPermissionsForSynapseV3(receiverUserName);

                if (recepientPermissions == null || !recepientPermissions.success)
                {
                    Logger.Error("TDA -> SynapseV3AddTrans_ReusableClass - RECIPIENT's Synapse Permissions were NULL or not successful :-(");

                    res.ErrorMessage = "Problem with recepient bank account permission.";
                    return res;
                }

                // 4. Check BANK/NODE permission for RECIPIENT
                if (recepientPermissions.users != null && recepientPermissions.users.Length > 0)
                {
                    foreach (synapseSearchUserResponse_User recUser in recepientPermissions.users)
                    {
                        // iterating each node inside
                        if (recUser.nodes != null && recUser.nodes.Length > 0)
                        {
                            NodePermissionCheckResult nodePermCheckRes = CommonHelper.IsNodeActiveInGivenSetOfNodes(recUser.nodes, receiver_bank_node_id);

                            if (nodePermCheckRes.IsPermissionfound == true)
                            {
                                if (nodePermCheckRes.PermissionType == "CREDIT-AND-DEBIT" || nodePermCheckRes.PermissionType == "DEBIT")
                                {
                                    RecipientSynapsePermissionOK = true;
                                }
                                // iterate through all users
                                //else
                                //{
                                //    res.success = false;
                                //    res.ErrorMessage = "Sender doesn't have permission to send money from account.";
                                //    return res;
                                //}
                            }
                            // iterate through all users
                            //else
                            //{
                            //    res.success = false;
                            //    res.ErrorMessage = "Sender doesn't have permission to send money from account.";
                            //    return res;
                            //}
                        }
                    }
                }
                #endregion Check Recipient Synapse Permissions

                if (!SenderSynapsePermissionOK)
                {
                    res.ErrorMessage = "Sender bank permission problem.";
                    return res;
                }
                if (!RecipientSynapsePermissionOK)
                {
                    res.ErrorMessage = "Recipient bank permission problem.";
                    return res;
                }

                // all set...time to move money between accounts
                try
                {
                    #region Setup Synapse V3 Order Details

                    SynapseV3AddTransInput transParamsForSynapse = new SynapseV3AddTransInput();

                    SynapseV3Input_login login = new SynapseV3Input_login() { oauth_key = sender_oauth };
                    SynapseV3Input_user user = new SynapseV3Input_user() { fingerprint = sender_fingerPrint };
                    transParamsForSynapse.login = login;
                    transParamsForSynapse.user = user;

                    SynapseV3AddTransInput_trans transMain = new SynapseV3AddTransInput_trans();

                    SynapseV3AddTransInput_trans_from from = new SynapseV3AddTransInput_trans_from()
                    {
                        id = sender_bank_node_id,
                        type = "ACH-US"
                    };
                    SynapseV3AddTransInput_trans_to to = new SynapseV3AddTransInput_trans_to()
                    {
                        id = receiver_bank_node_id,
                        type = "ACH-US"
                    };
                    transMain.to = to;
                    transMain.from = from;

                    SynapseV3AddTransInput_trans_amount amountMain = new SynapseV3AddTransInput_trans_amount()
                    {
                        amount = Convert.ToDouble(amount),
                        currency = "USD"
                    };
                    transMain.amount = amountMain;

                    SynapseV3AddTransInput_trans_extra extraMain = new SynapseV3AddTransInput_trans_extra()
                    {
                        supp_id = suppID_or_transID,
                        // This is where we put the ACH memo (customized for Landlords, but just the same template for regular P2P transfers: "Nooch Payment {LNAME SENDER} / {LNAME RECIPIENT})
                        // maybe we should set this in whichever function calls this function because we don't have the names here...
                        // yes modifying this method to add 3 new parameters....sender IP, sender last name, recepient last name... this would be helpfull in keeping this method clean.
                        note = "NOOCH PAYMENT // " + senderLastName + " / " + recepientLastName, // + moneySenderLastName + " / " + requestMakerLastName, 
                        //webhook = "",
                        process_on = 0, // this should be greater then 0 I guess... CLIFF: I don't think so, it's an optional parameter, but we always want it to process immediately, so I guess it should always be 0
                        ip = iPForTransaction // CLIFF:  This is actually required.  It should be the most recent IP address of the SENDER, or if none found, then '54.148.37.21'
                    };
                    transMain.extra = extraMain;

                    SynapseV3AddTransInput_trans_fees feeMain = new SynapseV3AddTransInput_trans_fees();

                    if (Convert.ToDouble(amount) > 10)
                    {
                        feeMain.fee = "0.20"; // to offset the Synapse fee so the user doesn't pay it
                    }
                    else if (Convert.ToDouble(amount) <= 10)
                    {
                        feeMain.fee = "0.10"; // to offset the Synapse fee so the user doesn't pay it
                    }
                    feeMain.note = "Negative Nooch Fee";

                    SynapseV3AddTransInput_trans_fees_to tomain = new SynapseV3AddTransInput_trans_fees_to()
                    {
                        id = "5618028c86c27347a1b3aa0f" // Temporary: ID of Nooch's SYNAPSE account (not bank account)... using temp Sandbox account until we get Production credentials
                    };

                    feeMain.to = tomain;
                    transMain.fees = new SynapseV3AddTransInput_trans_fees[1];
                    transMain.fees[0] = feeMain;

                    transParamsForSynapse.trans = transMain;

                    #endregion Setup Synapse V3 Order Details

                    #region Calling Synapse V3 TRANSACTION ADD

                    string UrlToHitV3 = "";
                    UrlToHitV3 = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox")) ? "https://sandbox.synapsepay.com/api/v3/trans/add" : "https://synapsepay.com/api/v3/trans/add";


                    try
                    {
                        // Calling Add Trans API

                        var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHitV3));
                        http.Accept = "application/json";
                        http.ContentType = "application/json";
                        http.Method = "POST";

                        string parsedContent = JsonConvert.SerializeObject(transParamsForSynapse);
                        ASCIIEncoding encoding = new ASCIIEncoding();
                        Byte[] bytes = encoding.GetBytes(parsedContent);

                        Stream newStream = http.GetRequestStream();
                        newStream.Write(bytes, 0, bytes.Length);
                        newStream.Close();

                        var response = http.GetResponse();
                        var stream = response.GetResponseStream();
                        var sr = new StreamReader(stream);
                        var content = sr.ReadToEnd();

                        var synapseResponse = JsonConvert.DeserializeObject<SynapseV3AddTrans_Resp>(content);

                        if (synapseResponse.success == true ||
                            synapseResponse.success.ToString().ToLower() == "true")
                        {
                            res.success = true;
                            res.ErrorMessage = "OK";
                        }
                        else
                        {
                            res.success = false;
                            res.ErrorMessage = "Check synapse error.";
                        }
                        res.responseFromSynapse = synapseResponse;

                    }
                    catch (WebException ex)
                    {
                        var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(resp);

                        Logger.Info("TDA -> TransferMoneyUsingSynapse FAILED. [Exception: " + jsonFromSynapse.ToString() + "]");

                        JToken token = jsonFromSynapse["error"]["en"];

                        if (token != null)
                        {
                            res.ErrorMessage = token.ToString();
                        }
                        else
                        {
                            res.ErrorMessage = "Error occured in call money transfer service.";
                        }
                    }

                    #endregion Calling Synapse V3 TRANSACTION ADD

                }
                catch (Exception ex)
                {
                    Logger.Error("TDA -> AddTransSynapseV3Reusable FAILED - Inner Exception: [Exception: " + ex + "]");
                    res.ErrorMessage = "Server Error - TDA Inner Exception";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> AddTransSynapseV3Reusable FAILED - Outer Exception: [Exception: " + ex + "]");
                res.ErrorMessage = "TDA Outer Exception";
            }

            return res;
        }



        /// <summary>
        /// To REQUEST money from NON-NOOCH user using SYANPSE through EMAIL (REQUEST VIA PHONE HAS A DIFF METHOD)
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        public string RequestMoneyToNonNoochUserUsingSynapse(RequestDto requestDto, out string requestId)
        {

            var checkuser = CommonHelper.GetMemberDetailsByUserName(requestDto.MoneySenderEmailId);

            if (checkuser == null)
            {
                Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse Initiated - Requestor MemberId: [" + requestDto.MemberId + "].");

                requestId = string.Empty;

                var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);

                // Validate PIN of requesting user
                string validPinNumberResult = CommonHelper.ValidatePinNumber(requestDto.MemberId.ToString(),
                                                                    requestDto.PinNumber.ToString());
                if (validPinNumberResult != "Success")
                {
                    return validPinNumberResult;
                }

                // Check if request Amount is over per-transaction limit
                decimal transactionAmount = Convert.ToDecimal(requestDto.Amount);

                // individual user transfer limit check
                decimal thisUserTransLimit = 0;
                string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(requestDto.MemberId);

                if (!String.IsNullOrEmpty(indiTransLimit))
                {
                    if (indiTransLimit != "0")
                    {
                        thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                        if (transactionAmount > thisUserTransLimit)
                        {
                            Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED - OVER PERSONAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                                   "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");
                            return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + thisUserTransLimit.ToString("F2");
                        }
                    }
                }


                if (CommonHelper.isOverTransactionLimit(transactionAmount, "", requestDto.MemberId))
                {
                    Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED - OVER GLOBAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                                   "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                    return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                }

                #region SenderSynapseAccountDetails



                var requestorInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.MemberId.ToString());

                if (requestorInfo.wereBankDetailsFound == null || requestorInfo.wereBankDetailsFound == false)
                {
                    return "Requester does not have any bank added";
                }

                // Check Requestor's Synapse Bank Account status
                if (requestorInfo.BankDetails != null &&
                    requestorInfo.BankDetails.Status != "Verified")
                {
                    Logger.Info("TDA - RequestMoneyToNonNoochUserUsingSynapse -> Transfer Aborted: No verified bank account for Sender - " +
                                           "Requester MemberID is: [" + requestDto.MemberId + "]");
                    return "Requester does not have any verified bank account.";
                }

                #endregion SenderSynapseAccountDetails

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    SenderId = Utility.ConvertToGuid(requestDto.MemberId),
                    RecipientId = Utility.ConvertToGuid(requestDto.MemberId),

                    Amount = requestDto.Amount,
                    TransactionDate = DateTime.Now,
                    Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                    Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                    DisputeStatus = null,

                    TransactionStatus = "Pending",
                    TransactionType = CommonHelper.GetEncryptedData("Request"),
                    DeviceId = requestDto.DeviceId,
                    TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                    TransactionFee = 0,
                    InvitationSentTo = CommonHelper.GetEncryptedData(requestDto.MoneySenderEmailId),
                    GeoLocation = new GeoLocation
                    {
                        LocationId = Guid.NewGuid(),
                        Latitude = requestDto.Latitude,
                        Longitude = requestDto.Longitude,
                        Altitude = requestDto.Altitude,
                        AddressLine1 = requestDto.AddressLine1,
                        AddressLine2 = requestDto.AddressLine2,
                        City = requestDto.City,
                        State = requestDto.State,
                        Country = requestDto.Country,
                        ZipCode = requestDto.ZipCode,
                        DateCreated = DateTime.Now
                    }
                };


                int dbResult = 0;

                if (requestDto.isTesting == "true")
                {
                    dbResult = 1;
                }
                else
                {
                    _dbContext.Transactions.Add(transaction);
                    dbResult = _dbContext.SaveChanges();
                    _dbContext.Entry(transaction).Reload();

                }

                if (dbResult > 0)
                {
                    requestId = transaction.TransactionId.ToString();

                    #region Send Notifications

                    string s22 = requestDto.Amount.ToString("n2");
                    string[] s32 = s22.Split('.');

                    string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                    string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                    string RequesterEmail = (requestDto.isTesting == "true") ? RequesterLastName.Trim().Replace(" ", "") + "-TEST@nooch.com"
                                                                             : CommonHelper.GetDecryptedData(requester.UserName);

                    string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                      "Nooch/CancelRequest?TransactionId=" + requestId +
                                                      "&MemberId=" + requestDto.MemberId +
                                                      "&UserType=U6De3haw2r4mSgweNpdgXQ==");

                    string recipientName = (!string.IsNullOrEmpty(requestDto.Name) && requestDto.Name.IndexOf("@") == -1)
                                           ? requestDto.Name
                                           : "";
                    string recipientsEmail = (requestDto.isTesting == "true") ? "testering@nooch.com"
                                                                              : requestDto.MoneySenderEmailId;

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");

                    var logoToDisplay = "noochlogo";

                    bool isForRentScene = false;
                    if (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Rent Scene's account
                        requester.MemberId.ToString().ToLower() == "a35c14e9-ee7b-4fc6-b5d5-f54961f2596a")  // Just for testing: "sallyanejones00@nooch.com"
                    {
                        isForRentScene = true;
                        RequesterFirstName = "Rent Scene";
                        RequesterLastName = "";
                        logoToDisplay = "rentscenelogo";
                    }

                    string memo = "";
                    if (transaction.Memo != null && transaction.Memo != "")
                    {
                        if (transaction.Memo.Length > 3)
                        {
                            string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor)
                            {
                                memo = transaction.Memo.ToString();
                            }
                            else
                            {
                                memo = "For " + transaction.Memo.ToString();
                            }
                        }
                        else
                        {
                            memo = "For " + transaction.Memo.ToString();
                        }
                    }

                    var tokens = new Dictionary<string, string>
					    {
                            {"$Logo$", logoToDisplay},
						    {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
						    {Constants.PLACEHOLDER_NEWUSER, recipientsEmail},
						    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
						    {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
			    		    {Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
						    {Constants.MEMO, memo}
					     };

                    try
                    {
                        Utility.SendEmail("requestSent", fromAddress, RequesterEmail, null,
                                                    "Your payment request to " + recipientsEmail + " is pending",
                                                    null, tokens, null, null, null);

                        Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> RequestSent email sent to [" + RequesterEmail + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> RequestSent email NOT sent to [" + RequesterEmail +
                                               "], [Exception: " + ex + "]");
                    }


                    // Send email to Request Receiver -- sending UserType LinkSource TransType as encrypted along with request
                    // In this case UserType would = 'New'
                    // TransType would = 'Request'
                    // and link source would = 'Email'
                    cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                               "Nooch/RejectMoney?TransactionId=" + requestId +
                                               "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                               "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==&" +
                                               "TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                    string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                   "Nooch/PayRequest?TransactionId=" + requestId);

                    if (isForRentScene)
                    {
                        paylink = paylink + "&rs=1";
                    }

                    var tokens2 = new Dictionary<string, string>
                        {
                            {"$Logo$", logoToDisplay},
					        {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
					        {Constants.PLACEHOLDER_NEWUSER, recipientsEmail},
					        {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
					        {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
					        {Constants.PLACEHOLDER_REJECT_LINK, cancelLink},
					        {Constants.PLACEHOLDER_SENDER_FULL_NAME, RequesterFirstName + " " + RequesterLastName},
					        {Constants.MEMO,memo},
					        {Constants.PLACEHOLDER_PAY_LINK, paylink}
					    };

                    try
                    {
                        string subject = isForRentScene
                                         ? "Payment Request from Rent Scene"
                                         : RequesterFirstName + " " + RequesterLastName + " requested " + "$" + s22.ToString() + " with Nooch";

                        Utility.SendEmail("requestReceivedToNewUser", fromAddress,
                                                    recipientsEmail, null, subject, null, tokens2,
                                                    null, null, null);

                        Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> requestReceivedToNewUser email sent to [" + recipientsEmail + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> requestReceivedToNewUser email NOT sent to [" + recipientsEmail +
                                               "], Exception: [" + ex + "]");
                    }

                    #endregion Send Notifications

                    return "Request made successfully.";
                }
                else
                {
                    Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED -> Unable to save transaction in DB - " +
                                           "Requester MemberID: [" + requestDto.MemberId + "], Recipient: [" + requestDto.MoneySenderEmailId + "]");
                    return "Request failed.";
                }
            }
            else
            {

                Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED -> Member Already Exists for email address: [" +
                                       requestDto.MoneySenderEmailId + "], Requester MemberID: [" + requestDto.MemberId + "]");

                // CLIFF (12/24/15): NEED TO ADD A CALL TO THE REGULAR REQUEST METHOD FOR EXISTING USERS.
                requestId = null;
                return "User Already Exists";
            }
        }


        /// <summary>
        /// This method is for making a request from a regular existing user (or Landlord) to a user who previously accepted/paid an
        /// invite or request and so have a Nooch status of "NonRegistered".  They already verified their ID and MAY have added a bank,
        /// so when they arrive at the payRequest landing page, they won't have to re-complete the profile info and add a bank - it will
        /// just have a button to confirm approval.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public string RequestMoneyToExistingButNonregisteredUser(RequestDto requestDto, out string requestId)
        {
            Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser Initiated - Requestor MemberId: [" + requestDto.MemberId + "].");

            requestId = string.Empty;

            // Check uniqueness of requesting and sending user
            if (requestDto.MemberId == requestDto.SenderId)
            {
                return "Not allowed to request money from yourself.";
            }


            var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);
            var requestRecipient = CommonHelper.GetMemberDetails(requestDto.SenderId);

            #region All Checks Before Executing Request

            if (requester == null)
            {
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Requester Member Not Found - [MemberID: " + requestDto.MemberId + "]");
                requestId = null;
                return "Requester Member Not Found";
            }
            else if (requestRecipient == null)
            {
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - requestRecipient (who would pay the request) Member Not Found - [MemberID: " + requestDto.SenderId + "]");
                requestId = null;
                return "Request Recipient Member Not Found";
            }

            // Validate PIN of requesting user - CLIFF (10/20/15): COMMENTING OUT BECAUSE THIS METHOD WILL LIKELY ONLY BE USED BY LANDLORDS, WHO DON'T HAVE A PIN
            /*string validPinNumberResult = mda.ValidatePinNumber(requestDto.MemberId.ToString(),
                                                                requestDto.PinNumber.ToString());
            if (validPinNumberResult != "Success")
            {
                return validPinNumberResult;
            }*/

            // Check if request Amount is over per-transaction limit
            decimal transactionAmount = Convert.ToDecimal(requestDto.Amount);

            // Individual user transfer limit check
            decimal thisUserTransLimit = 0;
            string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(requestDto.MemberId);

            if (!String.IsNullOrEmpty(indiTransLimit))
            {
                if (indiTransLimit != "0")
                {
                    thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                    if (transactionAmount > thisUserTransLimit)
                    {
                        Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - OVER PERSONAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                               "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                        return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + thisUserTransLimit.ToString("F2");
                    }
                }
            }

            if (CommonHelper.isOverTransactionLimit(transactionAmount, "", requestDto.MemberId))
            {
                return "To keep Nooch safe, the maximum amount you can request is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
            }

            #region Get Request Sender's Synapse Account Details

            var requestorSynInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.MemberId.ToString());

            if (requestorSynInfo.wereBankDetailsFound != true)
            {
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Transfer ABORTED: Requester's Synapse bank account NOT FOUND - " +
                                       "Requester MemberID is: [" + requestDto.MemberId + "]");
                return "Requester does not have any bank added";
            }

            // Check Requestor's Synapse Bank Account status
            if (requestorSynInfo.BankDetails != null &&
                requestorSynInfo.BankDetails.Status != "Verified" &&
                requester.IsVerifiedWithSynapse != true)
            {
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Transfer ABORTED: Requester's Synapse Bank exists but is Not Verified, " +
                                       "Requester's \"isVerifiedWithSynapse\" != true - Requester MemberID: [" + requestDto.MemberId + "]");
                return "Requester does not have any verified bank account.";
            }

            #endregion Get Sender's Synapse Account Details

            #region Get Request Recipient's Synapse Account Details

            var requestRecipientSynInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.SenderId.ToString());

            if (requestRecipientSynInfo.wereBankDetailsFound != true)
            {
                Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser -> Request Recipient Does not have a Synapse Bank Linked - " +
                                       "MemberID: [" + requestDto.SenderId + "]");

                // return "Request recipient does not have any bank added";
            }

            // Check Requestor's Synapse Bank Account status
            else if (requestRecipientSynInfo.BankDetails != null &&
                     requestRecipientSynInfo.BankDetails.Status != "Verified" &&
                     requestRecipient.IsVerifiedWithSynapse != true)
            {
                // CLIFF (12/18/15): NEED TO THINK THROUGH HOW TO HANDLE THIS SITUATION.  MIGHT JUST ALLOW IT FOR NOW, AND NOTIFY
                //                   MYSELF BY EMAIL TO CHECK THE PAYMENT MANUALLY.  LANDLORDS WILL USE THIS METHOD THE MOST, SO NEED TO
                //                   BE SURE THE TENANTS DON'T HAVE ISSUES.
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Transfer ABORTED: Request Recipient's Synapse bank found " +
                                       "but is Not Verified, and Request Recipient's \"isVerifiedWithSynapse\" != true - Request Recipient MemberID: [" + requestDto.SenderId + "]");
                return "Request recipient does not have any verified bank account.";
            }

            #endregion Get Sender's Synapse Account Details


            #endregion All Checks Before Executing Request


            #region Create New Transaction Record In DB

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                SenderId =  requestRecipient.MemberId,

                RecipientId = requester.MemberId,
                Amount = requestDto.Amount,
                TransactionDate = DateTime.Now,
                Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                Memo = requestDto.Memo,
                DisputeStatus = null,
                TransactionStatus = "Pending",
                TransactionType = CommonHelper.GetEncryptedData("Request"),
                DeviceId = requestDto.DeviceId,
                TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                TransactionFee = 0,
                IsPhoneInvitation = false,
                InvitationSentTo = !String.IsNullOrEmpty(requestDto.MoneySenderEmailId)
                                   ? CommonHelper.GetEncryptedData(requestDto.MoneySenderEmailId)
                                   : null,
                GeoLocation = new GeoLocation
                {
                    LocationId = Guid.NewGuid(),
                    Latitude = requestDto.Latitude,
                    Longitude = requestDto.Longitude,
                    Altitude = requestDto.Altitude,
                    AddressLine1 = requestDto.AddressLine1,
                    AddressLine2 = requestDto.AddressLine2,
                    City = requestDto.City,
                    State = requestDto.State,
                    Country = requestDto.Country,
                    ZipCode = requestDto.ZipCode,
                    DateCreated = DateTime.Now
                }
            };

            int dbResult = 0;
            bool isTesting = false;

            if (requestDto.isTesting == "true")
            {
                Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser - JUST A TEST!! - isTesting FLAG WAS TRUE... continuing on");
                isTesting = true;
                dbResult = 1;
            }
            else
            {
                _dbContext.Transactions.Add(transaction);
                dbResult = _dbContext.SaveChanges();
                _dbContext.Entry(transaction).Reload();

            }

            #endregion Create New Transaction Record In DB

            if (dbResult > 0)
            {
                requestId = transaction.TransactionId.ToString();

                #region Send Notifications

                #region Set Up Variables

                var fromAddress = Utility.GetValueFromConfig("transfersMail");

                string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                string RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requestRecipient.FirstName)).ToString());
                string RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requestRecipient.LastName)).ToString());

                string requesterPic = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                if (!String.IsNullOrEmpty(requester.Photo) && requester.Photo.Length > 20)
                {
                    requesterPic = requester.Photo.ToString();
                }

                string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/CancelRequest?TransactionId=" + requestId +
                                                                                                "&MemberId=" + requestDto.MemberId +
                                                                                                "&UserType=6KX3VJv3YvoyK+cemdsvMA==");

                string wholeAmount = requestDto.Amount.ToString("n2");
                string[] amountArray = wholeAmount.Split('.');

                string memo = "";
                if (transaction.Memo != null && transaction.Memo != "")
                {
                    if (transaction.Memo.Length > 3)
                    {
                        string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                        bool startWithFor = firstThreeChars.Equals("for");

                        if (startWithFor)
                        {
                            memo = transaction.Memo.ToString();
                        }
                        else
                        {
                            memo = "for " + transaction.Memo.ToString();
                        }
                    }
                    else
                    {
                        memo = "for " + transaction.Memo.ToString();
                    }
                }

                bool isForRentScene = false;
                var logoToDisplay = "noochlogo";

                if (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Rent Scene's account
                    requester.MemberId.ToString().ToLower() == "a35c14e9-ee7b-4fc6-b5d5-f54961f2596a")  // Just for testing: "sallyanejones00@nooch.com"
                {
                    isForRentScene = true;
                    fromAddress = "payments@rentscene.com";
                    RequesterFirstName = "Rent Scene";
                    RequesterLastName = "";
                    logoToDisplay = "rentscenelogo";
                }

                #endregion Set Up Variables


                // Send email to REQUESTER (person who sent this request)
                #region Email To Requester

                var tokens = new Dictionary<string, string>
                    {
                        {"$Logo$", logoToDisplay},
					    {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
					    {Constants.PLACEHOLDER_NEWUSER, RequestReceiverFirstName + " " + RequestReceiverLastName},
					    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, amountArray[0].ToString()},
					    {Constants.PLACEHLODER_CENTS, amountArray[1].ToString()},
					    {Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
					    {Constants.MEMO, memo}
				    };

                var toAddress = isTesting ? "testing_request-sender@nooch.com"
                                          : CommonHelper.GetDecryptedData(requester.UserName);

                try
                {
                    Utility.SendEmail("requestSent", fromAddress, toAddress, null,
                                                "Your payment request to " + RequestReceiverFirstName + " " + RequestReceiverLastName + " is pending",
                                                null, tokens, null, null, null);

                    Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser -> RequestSent email sent to [" + toAddress + "] successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> RequestSent email NOT sent to [" + toAddress +
                                           "], [Exception: " + ex + "]");
                }

                #endregion Email To Requester


                #region Email To Request Recipient

                // Send email to REQUEST RECIPIENT (person who will pay/reject this request)
                // Include 'UserType', 'LinkSource', and 'TransType' as encrypted along with request
                // In this case UserType would = 'Nonregistered'  ->  6KX3VJv3YvoyK+cemdsvMA==
                //              TransType would = 'Request'
                //              LinkSource would = 'Email'

                // Check if both user's are actually "Active" and not NonRegistered...
                string userType = "6KX3VJv3YvoyK+cemdsvMA=="; // "NonRegistered"
                if (requester.Status == "Active" && requestRecipient.Status == "Active")
                {
                    userType = "mx5bTcAYyiOf9I5Py9TiLw=="; // Update to "Existing"
                }

                //string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                //                                  "trans/rejectMoney.aspx?TransactionId=" + requestId +
                //                                  "&UserType=" + userType +
                //                                  "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                //                                  "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
 
                                                  "Nooch/rejectMoney?TransactionId=" + requestId +
 
                                                  "&UserType=" + userType +
                                                  "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                                                  "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                //string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                //                               "trans/payRequest.aspx?TransactionId=" + requestId +
                //                               "&UserType=" + userType);

                string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
 
                                               "Nooch/payRequest?TransactionId=" + requestId +
 
                                               "&UserType=" + userType);

                var tokens2 = new Dictionary<string, string>
                    {
                        {"$Logo$", logoToDisplay},
                        {Constants.PLACEHOLDER_FIRST_NAME, RequestReceiverFirstName},
                        {"$UserPicture$", requesterPic},
                        {Constants.PLACEHOLDER_SENDER_FULL_NAME, RequesterFirstName + " " + RequesterLastName},
                        {Constants.PLACEHOLDER_TRANSFER_AMOUNT, amountArray[0].ToString()},
                        {Constants.PLACEHLODER_CENTS, amountArray[1].ToString()},
                        {Constants.MEMO, memo},
                        {Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
                        {Constants.PLACEHOLDER_PAY_LINK, paylink},
                        {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, RequesterFirstName}
                    };

                toAddress = (isTesting) ? "testing_request-recip@nooch.com" : CommonHelper.GetDecryptedData(requestRecipient.UserName);

                try
                {
                    // ADD CURRENT MONTH IN BEGINNING OF THE SUBJECT, "December Rent Request from Landlord"
                    string subject = isForRentScene
                                     ? "$" + wholeAmount + " Payment Request from Rent Scene"
                                     : "Payment Request from " + RequesterFirstName + " " + RequesterLastName + " - " + "$" + wholeAmount;

                    Utility.SendEmail("requestReceivedToExistingNonRegUser", fromAddress, toAddress, null,
                                                subject, null, tokens2, null, null, null);

                    Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser - requestReceivedToNewUser email sent to [" + toAddress + "] successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser - requestReceivedToNewUser email NOT sent to [" + toAddress +
                                           "], [Exception: " + ex + "]");
                }

                #endregion Email To Request Recipient


                // Send SMS to REQUEST RECIPIENT (person who will pay/reject this request)

                // CLIFF (10/20/15) This block works (tested successfully) but commenting out b/c the Deposit-Money landing page
                //                  needs to be improved for Mobile screen sizes... not a great experience as it currently is.
                #region Send SMS To Non-Nooch Transfer Recipient

                /* string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                // shortening URLs from Google
                string RejectShortLink = rejectLink;
                string AcceptShortLink = paylink;

                #region Call Google URL Shortener API

                try
                {
                    var cli = new WebClient();
                    cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string response = cli.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + rejectLink + "\"}");
                    googleURLShortnerResponseClass googlerejectshortlinkresult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response);

                    if (googlerejectshortlinkresult != null)
                    {
                        RejectShortLink = googlerejectshortlinkresult.id;
                    }
                    else
                    {
                        // Google short URL API broke...
                        Logger.LogErrorMessage("TDA -> RequestMoneyToExistingButNonregisteredUser - GoogleAPI FAILED for Reject Short Link.");
                    }
                    cli.Dispose();

                    // Now shorten Accept link

                    var cli2 = new WebClient();
                    cli2.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string response2 = cli2.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + paylink + "\"}");
                    googleURLShortnerResponseClass googlerejectshortlinkresult2 = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response2);

                    if (googlerejectshortlinkresult2 != null)
                    {
                        AcceptShortLink = googlerejectshortlinkresult2.id;
                    }
                    else
                    {
                        // Google short URL API broke...
                        Logger.LogErrorMessage("TDA -> RequestMoneyToExistingButNonregisteredUser - GoogleAPI FAILED for Accept Short Link.");
                    }
                    cli2.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogErrorMessage("TDA -> RequestMoneyToExistingButNonregisteredUser - GoogleAPI FAILED. [Exception: " + ex + "]");
                }

                #endregion Call Google URL Shortener API

                string toPhoneNumber = requestRecipient.ContactNumber;

                try
                {
                    // Example SMS string: "Cliff Canan wants to send you $10 using Nooch, a free app. Click here to pay: {LINK}. Or here to reject: {LINK}"

                    // Make sure URL is short version (if Google API failued, use long version of Pay link and exclude the Reject link to save space)
                    string SMSContent;

                    if (AcceptShortLink.Length < 30) // Google Short links should be ~ 21 characters
                    {
                        string memoTxtForSms = memo;
                        if (memoTxtForSms.Length < 2)
                        {
                            memoTxtForSms = "from you";
                        }

                        SMSContent = RequesterFirstName + " " + RequesterLastName + " requested $" +
                                              amountArray[0].ToString() + "." + amountArray[1].ToString() + " " +
                                              memoTxtForSms +
                                              " using Nooch. Tap here to pay: " + AcceptShortLink +
                                              ". Or reject: " + RejectShortLink;
                    }
                    else // Google Short link API broke, use long version of Pay link
                    {
                        SMSContent = RequesterFirstName + " " + RequesterLastName + " requested $" +
                                              amountArray[0].ToString() + "." + amountArray[1].ToString() +
                                              " using Nooch. Tap here to accept: " + AcceptShortLink;
                    }

                    string result = UtilityDataAccess.SendSMS(toPhoneNumber, SMSContent, "", "");

                    Logger.LogDebugMessage("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse. SMS sent to recipient [" + toPhoneNumber + "] successfully. [Msg: " + result + "]");
                }
                catch (Exception ex)
                {
                    Logger.LogErrorMessage("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse. SMS NOT sent to recipient [" + toPhoneNumber +
                                           "], [Exception:" + ex + "]");
                }*/

                #endregion Send SMS To Non-Nooch Transfer Recipient


                #endregion Send Notifications

                return "Request made successfully.";
            }
            else
            {
                Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Unable to save Transaction in DB - [Requester MemberID:" + requestDto.MemberId + "]");
                return "Request failed.";
            }
        }


        /// <summary>
        /// To REQUEST money from NON-NOOCH user using SYANPSE through SMS 
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        public string RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(RequestDto requestDto, out string requestId, string PayorPhoneNumber)
        {
            using (NOOCHEntities obj = new NOOCHEntities())
            {
                var checkuser = CommonHelper.GetMemberIdByContactNumber(PayorPhoneNumber);
                

                if (checkuser == null)
                {
                    #region Given Contact No. doesn't exists in Nooch

                    Logger.Info("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse Initiated - Requestor MemberId: [" +
                                requestDto.MemberId + "].");

                    requestId = string.Empty;



                    var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);

                    // Validate PIN of requesting user
                    string validPinNumberResult = CommonHelper.ValidatePinNumber(requestDto.MemberId.ToString(),
                        requestDto.PinNumber.ToString());
                    if (validPinNumberResult != "Success")
                    {
                        return validPinNumberResult;
                    }

                    // Check if request Amount is over per-transaction limit
                    decimal transactionAmount = Convert.ToDecimal(requestDto.Amount);

                    // individual user transfer limit check
                    decimal thisUserTransLimit = 0;
                    string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(requestDto.MemberId);

                    if (!String.IsNullOrEmpty(indiTransLimit))
                    {
                        if (indiTransLimit != "0")
                        {
                            thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                            if (transactionAmount > thisUserTransLimit)
                            {
                                Logger.Error(
                                    "TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED - OVER PERSONAL TRANS LIMIT - Amount Requested: [" +
                                    transactionAmount.ToString() +
                                    "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId +
                                    "]");
                                return
                                    "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" +
                                    thisUserTransLimit.ToString("F2");
                            }
                        }
                    }


                    if (CommonHelper.isOverTransactionLimit(transactionAmount, "", requestDto.MemberId))
                    {
                        Logger.Error(
                            "TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED - OVER GLOBAL TRANS LIMIT - Amount Requested: [" +
                            transactionAmount.ToString() +
                            "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                        return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" +
                               Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction"))
                                   .ToString("F2");
                    }

                    #region SenderSynapseAccountDetails



                    var requestorInfo =
                        CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.MemberId.ToString());

                    if (requestorInfo.wereBankDetailsFound == null || requestorInfo.wereBankDetailsFound == false)
                    {
                        return "Requester does not have any bank added";
                    }

                    // Check Requestor's Synapse Bank Account status
                    if (requestorInfo.BankDetails != null &&
                        requestorInfo.BankDetails.Status != "Verified")
                    {
                        Logger.Info(
                            "TDA - RequestMoneyToNonNoochUserThroughPhoneUsingSynapse -> Transfer Aborted: No verified bank account for Request Sender - " +
                            "Requester MemberID is: [" + requestDto.MemberId + "]");
                        return "Requester does not have any verified bank account.";
                    }

                    #endregion SenderSynapseAccountDetails

                    var transaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid(),
                        SenderId = Utility.ConvertToGuid(requestDto.MemberId),
                        RecipientId = Utility.ConvertToGuid(requestDto.MemberId),

                        Amount = requestDto.Amount,
                        TransactionDate = DateTime.Now,
                        Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                        Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                        DisputeStatus = null,

                        TransactionStatus = "Pending",
                        TransactionType = CommonHelper.GetEncryptedData("Request"),
                        DeviceId = requestDto.DeviceId,
                        TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                        TransactionFee = 0,
                        IsPhoneInvitation = true,
                        PhoneNumberInvited = CommonHelper.GetEncryptedData(PayorPhoneNumber),
                        GeoLocation = new GeoLocation
                        {
                            LocationId = Guid.NewGuid(),
                            Latitude = requestDto.Latitude,
                            Longitude = requestDto.Longitude,
                            Altitude = requestDto.Altitude,
                            AddressLine1 = requestDto.AddressLine1,
                            AddressLine2 = requestDto.AddressLine2,
                            City = requestDto.City,
                            State = requestDto.State,
                            Country = requestDto.Country,
                            ZipCode = requestDto.ZipCode,
                            DateCreated = DateTime.Now
                        }
                    };


                    int dbResult = 0;

                    if (requestDto.isTesting == "true")
                    {
                        dbResult = 1;
                    }
                    else
                    {
                        obj.Transactions.Add(transaction);
                        dbResult = obj.SaveChanges();
                        

                    }

                    if (dbResult > 0)
                    {
                        requestId = transaction.TransactionId.ToString();

                        #region Send Notifications

                        string s22 = requestDto.Amount.ToString("n2");
                        string[] s32 = s22.Split('.');

                        string RequesterFirstName =
                            CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                        string RequesterLastName =
                            CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                        string RequesterEmail = (requestDto.isTesting == "true")
                            ? RequesterLastName.Trim().Replace(" ", "") + "-TEST@nooch.com"
                            : CommonHelper.GetDecryptedData(requester.UserName);

                        string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                            "Nooch/CancelRequest?TransactionId=" + requestId +
                            "&MemberId=" + requestDto.MemberId +
                            "&UserType=U6De3haw2r4mSgweNpdgXQ==");

                        string recipientName = (!string.IsNullOrEmpty(requestDto.Name) &&
                                                requestDto.Name.IndexOf("@") == -1)
                            ? requestDto.Name
                            : "";
                        string recipientsEmail = (requestDto.isTesting == "true")
                            ? "testering@nooch.com"
                            : requestDto.MoneySenderEmailId;

                        string PayorPhoneNumFormatted = CommonHelper.FormatPhoneNumber(PayorPhoneNumber);

                        var fromAddress = Utility.GetValueFromConfig("transfersMail");

                        var logoToDisplay = "noochlogo";

                        var templateToUse = "requestSent";

                        bool isForRentScene = false;
                        if (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                            // Rent Scene's account
                            requester.MemberId.ToString().ToLower() == "a35c14e9-ee7b-4fc6-b5d5-f54961f2596a")
                        // Just for testing: "sallyanejones00@nooch.com"
                        {
                            isForRentScene = true;
                            RequesterFirstName = "Rent Scene";
                            
                            logoToDisplay = "rentscenelogo";
                            RequesterLastName = "";
                            fromAddress = "payments@rentscene.com";
                            templateToUse = "requestSent_RentScene";
                        }

                        string memo = "";
                        if (!string.IsNullOrEmpty(transaction.Memo))
                        {
                            if (transaction.Memo.Length > 3)
                            {
                                string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = transaction.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + transaction.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + transaction.Memo.ToString();
                            }
                        }

                        var tokens = new Dictionary<string, string>
                        {
                            {"$Logo$", logoToDisplay},
                            {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
                            {Constants.PLACEHOLDER_NEWUSER, recipientsEmail},
                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
                            {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
                            {Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
                            {Constants.MEMO, memo}
                        };

                        try
                        {
                            Utility.SendEmail(templateToUse, fromAddress, RequesterEmail, null,
                                "Your payment request to " + PayorPhoneNumFormatted + " is pending",
                                null, tokens, null, null, null);

                            Logger.Info("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse -> RequestSent email sent to [" +
                                        RequesterEmail + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(
                                "TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse -> RequestSent email NOT sent to [" +
                                RequesterEmail +
                                "], [Exception: " + ex + "]");
                        }

                        // Send SMS to Request Receiver -- sending UserType LinkSource TransType as encrypted along with request
                        // In this case UserType would = 'New'
                        // TransType would = 'Request'
                        // and link source would = 'Phone'
                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/RejectMoney?TransactionId=" + requestId +
                                                          "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                          "&LinkSource=Um3I3RNHEGWqKM9MLsQ1lg==" +
                                                          "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                        string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                       "Nooch/PayRequest?TransactionId=" + requestId);

                        if (isForRentScene)
                        {
                            rejectLink = rejectLink + "&rs=1";
                            paylink = paylink + "&rs=1";
                        }

                        string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                        // shortning pay request and reject request link from google url link shortner api
                        #region Send SMS To New User

                        string RejectShortLink = "";
                        string AcceptShortLink = "";

                        #region Shortening URLs for SMS

                        var cli = new WebClient();
                        cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                        string response = cli.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + rejectLink + "\"}");
                        googleURLShortnerResponseClass googleRejectShortLinkResult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response);

                        if (googleRejectShortLinkResult != null)
                        {
                            RejectShortLink = googleRejectShortLinkResult.id;
                        }
                        else
                        {
                            // Google short url link broke... either go with full url or rollback transaction
                            Logger.Error("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse - requestReceivedToNewUser Google short URL not generated for reject request URL: [" + rejectLink + "]");
                        }

                        cli.Dispose();

                        // Now shorten the 'Pay' link
                        try
                        {
                            var cli2 = new WebClient();
                            cli2.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string response2 = cli2.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + paylink + "\"}");
                            googleURLShortnerResponseClass googlePayShortLinkResult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response2);

                            if (googlePayShortLinkResult != null)
                            {
                                AcceptShortLink = googlePayShortLinkResult.id;
                            }
                            else
                            {
                                // Google short url link broke... either go with full url or rollback transaction
                                Logger.Error("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse - requestReceivedToNewUser Google short URL not generated to pay request URL: [" + paylink + "]. ");
                            }
                            cli2.Dispose();
                        }
                        catch (Exception ex)
                        {
                            return ex.ToString();
                        }

                        #endregion Shortening URLs for SMS

                        try
                        {
                            string SMSContent = RequesterFirstName + " " + RequesterLastName + " requested $" +
                                                s32[0].ToString() + "." + s32[1].ToString() +
                                                " from you using Nooch, a free app. Tap here to pay: " + AcceptShortLink +
                                                ". Or reject: " + RejectShortLink;

                            Utility.SendSMS(PayorPhoneNumber, SMSContent);

                            Logger.Info("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse - Request SMS sent to [" + PayorPhoneNumber + "].");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse - Request SMS NOT sent to [" + PayorPhoneNumber +
                                                   "], [Exception: " + ex + "]");
                        }

                        #endregion Send SMS To New User

                        requestId = transaction.TransactionId.ToString();
                       

                        #endregion Send Notifications

                        return "Request made successfully.";
                    }
                    else
                    {
                        Logger.Error(
                            "TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED -> Unable to save transaction in DB - " +
                            "Requester MemberID: [" + requestDto.MemberId + "], Recipient: [" +
                            requestDto.MoneySenderEmailId + "]");
                        return "Request failed.";
                    } 
                    #endregion
                }
                else
                {

                    Logger.Error(
                        "TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED -> Member Already Exists for phone number: [" +
                        PayorPhoneNumber + "], Requester MemberID: [" + requestDto.MemberId + "]");

                    
                    requestId = null;
                    return "User Already Exists";
                }
            }
        }
    }
}
