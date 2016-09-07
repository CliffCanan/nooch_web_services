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
        private NOOCHEntities _dbContext = new NOOCHEntities();

        public TransactionsDataAccess()
        {
            _dbContext = new NOOCHEntities();
        }


        public List<Transaction> GetRecentMembers(string memberId)
        {
            Logger.Info("TDA -> GetRecentMembers FIRED - MemberID: [" + memberId + "]");

            int totalRecordsCount = 0;

            List<Transaction> transactionsList = GetTransactionsList(memberId, "ALL", 0, 0, "", out totalRecordsCount);

            if (transactionsList != null && transactionsList.Count > 0)
                return transactionsList;

            return new List<Transaction>();
        }


        public PendingTransCoutResult GetMemberPendingTransCount(string MemberId)
        {
            Logger.Info("TDA -> GetMemberPendingTransCount - MemberID: [" + MemberId + "]");

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
                    pendingRequestsSent = allPendingRequestsResult.Count;

                // Getting count OF REQUESTS where member is RECIPIENT
                var allPendingRequestsReceivedResult = _dbContext.Transactions.Where(
                    t => t.Member.MemberId == memberId &&
                         t.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" &&
                         t.TransactionStatus == "Pending" &&
                         (t.InvitationSentTo == null || t.InvitationSentTo == "") &&
                         (t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==" || t.DisputeStatus == null)).ToList();

                // Getting count of TRANSFERS/INVITES to new users
                var allPendingInvitesCount = _dbContext.Transactions.Where(t => t.Member.MemberId == memberId &&
                         t.TransactionStatus == "Pending" &&
                         t.InvitationSentTo != null &&
                         (t.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                          t.TransactionType == " 5dt4HUwCue532sNmw3LKDQ==") &&
                         (t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==" || t.DisputeStatus == null)).ToList();

                if (allPendingInvitesCount != null)
                    pendingInvitesCount = allPendingInvitesCount.Count;

                // Getting count of DISPUTES that are still pending (not 'Resolved')
                var allDisputeResult = _dbContext.Transactions.Where(t =>
                        (t.Member1.MemberId == memberId || t.Member.MemberId == memberId) &&
                        t.TransactionType == "+C1+zhVafHdXQXCIqjU/Zg==" &&
                        t.TransactionStatus == "Pending" &&
                        t.DisputeStatus != "RZKgHECeAxJW/E+/IRj3Dg==").ToList();

                if (allDisputeResult != null)
                    pendingDisputesTotal = allDisputeResult.Count;

                if (allPendingRequestsReceivedResult != null)
                    pendingRequestsReceived = allPendingRequestsReceivedResult.Count;

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
        /// Called from the cancel request landing page code-behind: CancelRequest
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
                                                toAddress2 + "] successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser - requestCancelledToRecipient email NOT sent to [" +
                                                 toAddress2 + "], Exception: [" + ex + "]");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> CancelMoneyRequestForNonNoochUser EXCEPTION - Failure while sending notifications - [" +
                                         "TransactionID: [" + TransactionId + "], Exception: [" + ex + "]");
                        }

                        return "Transaction Cancelled Successfully.";
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
                            memo = "For " + res.Memo.ToString();

                        // updated, now send email to user who made the request
                        var tokens = new Dictionary<string, string>
                            {
							    {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.FirstName))},
							    {Constants.PLACEHOLDER_Recepient_Email, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName)) + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.LastName))},
							    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, res.Amount.ToString("n2")},
							    {Constants.PLACEHOLDER_DATE, Convert.ToDateTime(res.TransactionDate).ToString("MMM d, yyyy")},
							    {Constants.MEMO, memo}
							};

                        // for TransferReceived email notification       
                        string adminUserName = Utility.GetValueFromConfig("transfersMail");
                        var fromAddress = adminUserName;
                        var toAddress = CommonHelper.GetDecryptedData(res.Member1.UserName.ToString());
                        try
                        {
                            Utility.SendEmail("requestCancelledToSender", fromAddress, toAddress, null, "Nooch payment request to " + toAddress + " cancelled", null, tokens, null, null, null);
                            Logger.Info("CancelMoneyRequestForExistingNoochUser --> requestCancelledToSender email sent to Sender: [" + toAddress + "] successfully.");
                        }
                        catch (Exception)
                        {
                            Logger.Error("CancelMoneyRequestForExistingNoochUser --> requestCancelledToSender email NOT sent to [" + toAddress + "]. Problem occurred in sending mail.");
                        }

                        var tokens2 = new Dictionary<string, string>
                            {
							    {Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName))},
							    {Constants.PLACEHOLDER_LAST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.FirstName)) + " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member1.LastName))},
							    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, res.Amount.ToString("n2")},
							    {Constants.MEMO, memo}
							};

                        var toAddress2 = CommonHelper.GetDecryptedData(res.Member.UserName);
                        try
                        {
                            Utility.SendEmail("requestCancelledToRecipient", fromAddress, toAddress2, null, "Nooch payment request cancelled", null, tokens2, null, null, null);
                            Logger.Info("TDA -> CancelMoneyRequestForExistingNoochUser - requestCancelledToRecipient email sent to [" + toAddress2 + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> CancelMoneyRequestForExistingNoochUser - requestCancelledToRecipient email NOT sent to [" + toAddress2 + "], Exception: [" + ex + "]");
                        }

                        return "Transaction Cancelled Successfully.";
                    }
                    else
                        return "Something went wrong while updating transaction, please retry.";
                }
                else
                    return "No Such Transaction Found.";
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
                    && m.TransactionId == transid && (m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA=="));

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
                            var smsTxt = senderFullName + " just cancelled a $" + s32[0] + "." + s32[1] +
                                         " payment to you on Nooch. Sorry about that!";
                            Utility.SendSMS(CommonHelper.RemovePhoneNumberFormatting(nonNoochRecipient), smsTxt);
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
                else // Transaction Not Found
                    return "No Such Transaction Found.";
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> CancelMoneyTransferToNonMemberForSender FAILED (Outer Exception) - TransID: [" + TransactionId + "]");
                return ex.Message;
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
            Logger.Info("TDA -> SendTransactionReminderEmail Fired - MemberID: [" + MemberId + "], " +
                        "TransactionId: [" + TransactionId + "], ReminderType: [" + ReminderType + "]");

            try
            {
                var TransId = Utility.ConvertToGuid(TransactionId);
                var MemId = Utility.ConvertToGuid(MemberId);

                if (ReminderType == "RequestMoneyReminderToNewUser" ||
                    ReminderType == "RequestMoneyReminderToExistingUser")
                {
                    #region Requests - Both Types

                    var trans = _dbContext.Transactions.FirstOrDefault(m => m.Member1.MemberId == MemId &&
                                                                            m.TransactionId == TransId &&
                                                                            m.TransactionStatus == "Pending" &&
                                                                            m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==");

                    if (trans != null)
                    {
                        _dbContext.Entry(trans).Reload();

                        #region Setup Common Variables

                        string fromAddress = Utility.GetValueFromConfig("transfersMail");
                        string requesterFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                        string requesterLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                        string payLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                       "Nooch/payRequest?TransactionId=" + trans.TransactionId);

                        string transDate = Convert.ToDateTime(trans.TransactionDate).ToString("MMM d, yyyy");
                        string amountFull = trans.Amount.ToString("n2");
                        string[] amountArray = amountFull.Split('.');
                        var dollars = amountArray[0];
                        var cents = amountArray[1];

                        string memo = "";
                        if (!string.IsNullOrEmpty(trans.Memo))
                        {
                            if (trans.Memo.Length > 3)
                            {
                                string firstThreeChars = trans.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor) memo = trans.Memo.ToString();
                                else memo = "For " + trans.Memo.ToString();
                            }
                            else memo = "For " + trans.Memo.ToString();
                        }

                        var company = "nooch";

                        if (trans.Member1.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" || // Rent Scene's account
                            trans.Member.isRentScene == true)
                        {
                            company = "rentscene";
                            requesterFirstName = "Rent Scene";
                            requesterLastName = "";
                        }

                        #endregion Setup Common Variables

                        #region RequestMoneyReminderToNewUser

                        // Now check if this transaction was sent via Email or Phone Number (SMS)
                        if (trans.InvitationSentTo != null) // 'InvitationSentTo' field only used if it's an Email Transaction
                        {
                            #region Request Sent To Email Address

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "Nooch/rejectMoney?TransactionId=" + trans.TransactionId +
                                                              "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            var tokens2 = new Dictionary<string, string>
                                {
								    {Constants.PLACEHOLDER_FIRST_NAME, requesterFirstName + " " + requesterLastName},
									{Constants.PLACEHOLDER_NEWUSER, ""},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT, dollars},
									{Constants.PLACEHLODER_CENTS, cents},
									{Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
									{Constants.PLACEHOLDER_TRANSACTION_DATE, transDate},
									{Constants.MEMO, memo},
									{Constants.PLACEHOLDER_PAY_LINK, payLink}
								};

                            var templateToUse = "requestReminderToNewUser";
                            if (company == "rentscene") templateToUse = "requestReminderToNewUser_RentScene";

                            var toAddress = CommonHelper.GetDecryptedData(trans.InvitationSentTo);

                            // Send Request Reminder email to NON-NOOCH user
                            try
                            {
                                Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                  requesterFirstName + " " + requesterLastName + " requested " + "$" + amountFull.ToString() + " - Reminder",
                                                  null, tokens2, null, null, null);

                                Logger.Info("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail FAILED - [" + templateToUse + "] NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                            }

                            return "Reminder mail sent successfully.";

                            #endregion Request Sent To Email Address
                        }
                        else if (trans.IsPhoneInvitation == true && trans.PhoneNumberInvited != null)
                        {
                            #region If Invited by SMS

                            var phoneNumberRaw = CommonHelper.GetDecryptedData(trans.PhoneNumberInvited);
                            var RejectShortLink = "";
                            var AcceptShortLink = "";

                            var rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                           "Nooch/rejectMoney?TransactionId=" + trans.TransactionId.ToString() +
                                                           "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                           "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            #region Shortening URLs for SMS

                            string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                            // Shorten the 'Pay' link
                            var cli = new WebClient();
                            cli.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string response = cli.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + rejectLink + "\"}");
                            googleURLShortnerResponseClass shortRejectLinkFromGoogleResult = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response);

                            if (shortRejectLinkFromGoogleResult != null)
                                RejectShortLink = shortRejectLinkFromGoogleResult.id;
                            else // Google short URL API broke...
                                Logger.Error("TDA -> SendTransactionReminderEmail -> requestReceivedToNewUser Google short Reject URL NOT generated. Long Reject URL: [" + rejectLink + "].");

                            // Now shorten the 'Pay' link
                            cli.Dispose();
                            try
                            {
                                var cli2 = new WebClient();
                                cli2.Headers[HttpRequestHeader.ContentType] = "application/json";
                                string response2 = cli2.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + googleUrlAPIKey, "{longUrl:\"" + payLink + "\"}");
                                googleURLShortnerResponseClass googlerejectshortlinkresult2 = JsonConvert.DeserializeObject<googleURLShortnerResponseClass>(response2);

                                if (googlerejectshortlinkresult2 != null)
                                    AcceptShortLink = googlerejectshortlinkresult2.id;
                                else // Google short URL API broke...
                                    Logger.Error("TDA -> SendTransactionReminderEmail -> requestReceivedToNewUser Google short Pay URL NOT generated. Long Pay URL: [" + payLink + "].");
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
                                var SMSContent = "Just a reminder, " + requesterFirstName + " " + requesterLastName +
                                                 " requested $" + amountFull +
                                                 " from you using Nooch, a free app. Tap here to pay: " + AcceptShortLink +
                                                 ". Tap here to reject: " + RejectShortLink;

                                Utility.SendSMS(phoneNumberRaw, SMSContent);
                                Logger.Info("TDA -> SendTransactionReminderEmail -> Request Reminder SMS sent to [" + CommonHelper.FormatPhoneNumber(phoneNumberRaw) + "].");
                                return "Reminder sms sent successfully.";
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail -> Request Reminder SMS NOT sent to [" + CommonHelper.FormatPhoneNumber(phoneNumberRaw) + "]. Problem occured in sending SMS.");
                                return "Unable to send sms reminder.";
                            }

                            #endregion Sending SMS

                            #endregion If Invited by SMS
                        }

                        #endregion RequestMoneyReminderToNewUser


                        #region RequestMoneyReminderToExistingUser

                        else if (trans.Member.MemberId != null)
                        {
                            requesterFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.FirstName));
                            requesterLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member1.LastName));
                            var rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/RejectMoney?TransactionId=" + TransId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
                            string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/payRequest?TransactionId=" + TransId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw=="); // Update to "Existing"

                            #region Reminder EMAIL

                            string toAddress = CommonHelper.GetDecryptedData(trans.Member.UserName);

                            var tokens2 = new Dictionary<string, string>
                                {
								    {Constants.PLACEHOLDER_FIRST_NAME, requesterFirstName},
									{Constants.PLACEHOLDER_NEWUSER, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName))},
									{Constants.PLACEHOLDER_TRANSFER_AMOUNT, dollars},
									{Constants.PLACEHLODER_CENTS, cents},
									{Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
									{Constants.PLACEHOLDER_TRANSACTION_DATE, transDate},
									{Constants.MEMO, memo},
									{Constants.PLACEHOLDER_PAY_LINK, paylink}
								};

                            var templateToUse = "requestReminderToExistingUser";
                            if (company == "rentscene") templateToUse = "requestReminderToExistingUser_RentScene";

                            var subject = company == "rentscene"
                                          ? requesterFirstName + " " + requesterLastName + " requested " + "$" + amountFull + " with Nooch - Reminder"
                                          : "Rent Scene Payment Reminder - $" + amountFull;
                            try
                            {
                                Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                  subject, null, tokens2, null, null, null);

                                Logger.Info("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> SendTransactionReminderEmail - [" + templateToUse + "] NOT sent to [" + toAddress + "]. Problem occured in sending mail.");
                            }

                            #endregion Reminder EMAIL

                            #region Reminder PUSH NOTIFICATION

                            if (!String.IsNullOrEmpty(trans.Member.DeviceToken) &&
                                trans.Member.DeviceToken.IndexOf("null") == -1 &&
                                trans.Member.DeviceToken.Length > 6)
                            {
                                try
                                {
                                    string firstName = !String.IsNullOrEmpty(trans.Member.FirstName) ? " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName)) : "";

                                    string msg = "Hi " + firstName + "! Just a reminder that " + requesterFirstName + " " + requesterLastName +
                                                 " sent you a Nooch request for $" + trans.Amount.ToString("n2") + ". Might want to pay up!";

                                    Utility.SendNotificationMessage(msg, "Nooch", trans.Member.DeviceToken, trans.Member.DeviceType);

                                    Logger.Info("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification sent successfully - Username: [" +
                                                toAddress + "], Device Token: [" + trans.Member.DeviceToken + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification NOT sent - Username: [" +
                                                 toAddress + "], Device Token: [" + trans.Member.DeviceToken + "], Exception: [" + ex + "]");
                                }
                            }

                            #endregion  Reminder PUSH NOTIFICATION

                            return "Reminder mail sent successfully.";
                        }

                        #endregion RequestMoneyReminderToExistingUser

                        else return "No recipient MemberId found for this transaction.";
                    }
                    else
                    {
                        Logger.Error("TDA -> SendTransactionReminderEmail FAILED - Transaction Not Found - MemberID: [" + MemberId +
                                     "], TransID: [" + TransactionId + "]");
                        return "No transaction found";
                    }

                    #endregion Requests - Both Types
                }
                else if (ReminderType == "InvitationReminderToNewUser")
                {
                    #region InvitationReminderToNewUser

                    var trans = _dbContext.Transactions.FirstOrDefault(m => m.Member1.MemberId == MemId &&
                                                                            m.TransactionId == TransId &&
                                                                            m.TransactionStatus == "Pending" &&
                                                                           (m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA=="));

                    if (trans != null)
                    {
                        #region Setup Variables

                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));

                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/RejectMoney?TransactionId=" + trans.TransactionId +
                                                          "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                          "&TransType=DrRr1tU1usk7nNibjtcZkA==");

                        string acceptLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/DepositMoney?transid=" + trans.TransactionId.ToString());

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

                                Utility.SendEmail("transferReminderToRecipient", fromAddress, recipientEmail, null,
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
                        else return "no email mentioned for invited user";
                    }
                    else return "No transaction found";

                    #endregion InvitationReminderToNewUser
                }

                else return "invalid transaction id or memberid";
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> SendTransactionReminderEmail FAILED - Outer Exception - [" + ex + "]");
                return "Error";
            }
        }
        #endregion


        public string RejectMoneyRequestForExistingNoochUser(string TrannsactionId)
        {
            Logger.Info("TDA -> RejectMoneyRequestForExistingNoochUser - TransID: [" + TrannsactionId + "]");

            try
            {
                var transId = Utility.ConvertToGuid(TrannsactionId);

                var transactionDetail = _dbContext.Transactions.FirstOrDefault(m => m.TransactionId == transId &&
                                                                                    m.TransactionStatus == "Pending" &&
                                                                                   (m.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" ||
                                                                                    m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="));

                if (transactionDetail != null)
                {
                    #region If Something Found

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

                            if (startWithFor) memo = transactionDetail.Memo.ToString();
                            else memo = "For " + transactionDetail.Memo.ToString();
                        }
                        else memo = "For " + transactionDetail.Memo.ToString();
                    }

                    #region Push Notification to Request Sender

                    var noochMemberfornotification = CommonHelper.GetMemberNotificationSettings(transactionDetail.Member1.MemberId.ToString());

                    if (noochMemberfornotification != null)
                    {
                        try
                        {
                            string mailBodyText = RejectorFirstName + " " + RejectorLastName + " just rejected your Nooch payment request for $" + wholeAmount + ".";


                            Utility.SendNotificationMessage(mailBodyText, "Nooch", transactionDetail.Member1.DeviceToken,
                                                                                   transactionDetail.Member1.DeviceType);
                        }
                        catch (Exception)
                        {
                            Logger.Error("RejectMoneyRequestForExistingNoochUser - Push notification not sent to [" + transactionDetail.Member1.UDID1 + "].");
                        }
                    }

                    #endregion Push Notification to Request Sender

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
                else return "";
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
        public Transaction GetTransactionById(string transId)
        {
            Logger.Info("TDA -> GetTransactionById Fired - TransactionID: [" + transId + "]");

            try
            {
                var transGuid = Utility.ConvertToGuid(transId);

                var transactionDetail = _dbContext.Transactions.Where(c => c.TransactionId == transGuid).FirstOrDefault();

                if (transactionDetail != null) _dbContext.Entry(transactionDetail).Reload();

                return transactionDetail;
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> GetTransactionById EXCEPTION - TransactionID: [" + transId + "], Exception: [" + ex + "]");
            }

            return null;
        }


        public List<Transaction> GetTransactionsList(string memberId, string listType, int pageSize, int pageIndex, string SubListType, out int totalRecordsCount)
        {
            Logger.Info("TDA -> GetTransactionsList Initiated - MemberId: [" + memberId + "] - ListType: [" + listType +
                        "], SubListType: [" + SubListType + "]");

            if (pageSize == null || pageSize < 100) pageSize = 50;

            totalRecordsCount = 0;
            _dbContext = new NOOCHEntities();

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                var member = _dbContext.Members.FirstOrDefault(u => u.MemberId == id);

                if (member != null)
                {
                    _dbContext.Entry(member).Reload();

                    var transactions = new List<Transaction>();

                    var transactionTypeTransfer = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);
                    var transactionTypeDonation = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_DONATION);
                    var transactionTypeRequest = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_REQUEST);
                    var transactionTypeDisputed = "+C1+zhVafHdXQXCIqjU/Zg==";

                    var userNameDecrypted = CommonHelper.GetDecryptedData(member.UserName);
                    var userNameDecryptedLowerCase = CommonHelper.GetDecryptedData(member.UserNameLowerCase);

                    if (!String.IsNullOrEmpty(SubListType))
                    {
                        #region SubList Was Included

                        if (listType.ToUpper().Equals("SENT"))
                        {
                            transactions = _dbContext.Transactions.Where(entity => entity.Member.MemberId == id || entity.InvitationSentTo == member.UserName &&
                                        (entity.TransactionType == transactionTypeTransfer || entity.TransactionType == transactionTypeDonation) &&
                                         entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("RECEIVED"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        entity.Member1.MemberId == id || entity.InvitationSentTo == member.UserName &&
                                        entity.TransactionType == transactionTypeTransfer &&
                                        entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("DISPUTED"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        ((entity.Member1.MemberId == id || entity.Member.MemberId == id || entity.InvitationSentTo == member.UserName) &&
                                          entity.DisputeStatus != null && entity.TransactionType == transactionTypeDisputed) &&
                                          entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL") && SubListType == "Pending") // CR
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member.MemberId == id || entity.Member1.MemberId == id || entity.InvitationSentTo == member.UserName) &&
                                         entity.TransactionStatus == SubListType).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL") && SubListType == "Success") // CR
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member.MemberId == id || entity.Member1.MemberId == id || entity.InvitationSentTo == member.UserName) &&
                                        (entity.TransactionStatus == SubListType || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                        }
                        else if (listType.ToUpper().Equals("DONATION"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id || entity.InvitationSentTo == member.UserName) &&
                                         entity.TransactionType == transactionTypeDonation &&
                                         entity.TransactionStatus == SubListType).ToList();

                        }
                        else if (listType.ToUpper().Equals("REQUEST"))
                        {
                            transactions = _dbContext.Transactions.Where(entity =>
                                        (entity.Member1.MemberId == id || entity.Member.MemberId == id || entity.InvitationSentTo == member.UserName) &&
                                         entity.TransactionType == transactionTypeRequest &&
                                        (entity.TransactionStatus == SubListType || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                        }

                        #endregion SubList Was Included
                    }
                    else
                    {
                        #region SubList NOT Included

                        if (listType.ToUpper().Equals("ALL"))
                        {
                            transactions = _dbContext.Transactions.Where(trans =>
                                                                         trans.SenderId == id ||
                                                                         trans.RecipientId == id ||
                                                                         trans.InvitationSentTo == member.UserName ||
                                                                         trans.InvitationSentTo == member.UserNameLowerCase)
                                                                         .OrderByDescending(r => r.TransactionDate).Take(pageSize).ToList();
                        }
                        else if (listType.ToUpper().Equals("SENT"))
                        {
                            transactions = _dbContext.Transactions.Where(trans =>
                                                                         (trans.SenderId == id ||
                                                                         trans.InvitationSentTo == member.UserName ||
                                                                         trans.InvitationSentTo == member.UserNameLowerCase) &&
                                                                         trans.TransactionType == transactionTypeTransfer)
                                                                         .OrderByDescending(r => r.TransactionDate).Take(pageSize).ToList();
                        }
                        else if (listType.ToUpper().Equals("RECEIVED"))
                        {
                            transactions = _dbContext.Transactions.Where(trans =>
                                                                        (trans.RecipientId == id ||
                                                                         trans.InvitationSentTo == member.UserName ||
                                                                         trans.PhoneNumberInvited == member.ContactNumber) &&
                                                                         trans.TransactionType == transactionTypeTransfer)
                                                                         .OrderByDescending(r => r.TransactionDate).Take(pageSize).ToList();
                        }
                        else if (listType.ToUpper().Equals("DISPUTED"))
                        {
                            transactions = _dbContext.Transactions.Where(trans =>
                                                                        (trans.SenderId == id ||
                                                                         trans.RecipientId == id ||
                                                                         trans.InvitationSentTo == member.UserName) &&
                                                                         trans.DisputeStatus != null &&
                                                                         trans.TransactionType == transactionTypeDisputed)
                                                                         .OrderByDescending(r => r.TransactionDate).Take(pageSize).ToList();
                        }
                        else if (listType.ToUpper().Equals("REQUEST"))
                        {
                            transactions = _dbContext.Transactions.Where(trans =>
                                                                        (trans.SenderId == id ||
                                                                         trans.RecipientId == id ||
                                                                         trans.InvitationSentTo == member.UserName ||
                                                                         trans.InvitationSentTo == member.UserNameLowerCase) &&
                                                                         trans.TransactionType == transactionTypeRequest).ToList();
                        }

                        #endregion SubList NOT Included
                    }

                    totalRecordsCount = transactions.Count();

                    // This is making some problem.
                    if (pageSize == 0 && pageIndex == 0)
                        transactions = transactions.Take(1000).ToList();
                    else
                        transactions = transactions.Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();

                    if (transactions.Count > 0)
                        return new List<Transaction>(transactions);
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

            var member = _dbContext.Members.Where(u => u.MemberId == id).FirstOrDefault();

            if (member != null)
            {
                _dbContext.Entry(member).Reload();
                var transaction = new Transaction();

                var transactionType = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);

                var disputedTransaction = _dbContext.Transactions.Where(t => t.Member1.MemberId == id &&
                                                                            (t.TransactionType == transactionType &&
                                                                             t.DisputeStatus != null)).OrderByDescending(c => c.DisputeDate).FirstOrDefault();

                var receivedTransaction = _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id &&
                                                                                  entity.TransactionType == transactionType).OrderByDescending(c => c.DisputeDate).FirstOrDefault();

                if (disputedTransaction != null)
                {
                    _dbContext.Entry(disputedTransaction).Reload();
                    transaction = disputedTransaction.DisputeDate > receivedTransaction.TransactionDate ? disputedTransaction : receivedTransaction;
                }
                else transaction = receivedTransaction;

                if (transaction != null) return transaction;
            }

            return new Transaction();
        }


        public List<Transaction> GetTransactionsSearchList(string memberId, string friendName, string listType, int pageSize, int pageIndex, string sublist)
        {
            try
            {
                Logger.Info("TDA -> GetTransactionSearchList Initiated - MemberID: [" + memberId + "]");

                var id = Utility.ConvertToGuid(memberId);

                var member = _dbContext.Members.Where(u => u.MemberId == id).FirstOrDefault();
                if (member != null) _dbContext.Entry(member).Reload();

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
                    transactions = transactions.Take(1000).ToList();
                else
                    transactions = transactions.Skip(pageSize * pageIndex).Take(pageSize).ToList();

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
                transactions = _dbContext.Transactions.FirstOrDefault(entity => entity.Member.MemberId == id && entity.TransactionId == txnId);
            else if (listType.ToUpper().Equals("RECEIVED"))
                transactions = _dbContext.Transactions.FirstOrDefault(entity => entity.Member1.MemberId == id && entity.TransactionId == txnId);
            else if (listType.ToUpper().Equals("DISPUTED"))
                transactions = _dbContext.Transactions.FirstOrDefault(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id)
                                                                       && entity.DisputeStatus != null && entity.TransactionId == txnId);
            else // for withdraw
                transactions = _dbContext.Transactions.FirstOrDefault(entity => (entity.Member1.MemberId == id || entity.Member.MemberId == id) &&
                                                                                entity.TransactionId == txnId);

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
            Logger.Info("TDA -> CancelRejectTransaction Fired - TransID: [" + transactionId + "], UserResponse: [" + userResponse + "]");

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
                        // Send push notification to request maker
                        Guid RequestSenderId = Utility.ConvertToGuid(transactionDetail.Member1.MemberId.ToString());

                        var noochMemberfornotification = _dbContext.Members.Where(memberTemp => memberTemp.MemberId.Equals(RequestSenderId) &&
                                                                                                memberTemp.isRentScene != true &&
                                                                                                memberTemp.IsDeleted == false &&
                                                                                                memberTemp.UDID1 != null).FirstOrDefault();

                        if (noochMemberfornotification != null)
                        {
                            try
                            {
                                var smsText = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName)) +
                                              " " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName)) +
                                              " just declined your Nooch payment request.";

                                Utility.SendNotificationMessage(smsText, "Nooch", noochMemberfornotification.DeviceToken,
                                                                noochMemberfornotification.DeviceType);

                                Logger.Info("TDA -> Request Denied - Push notification sent to [" + noochMemberfornotification.UDID1 + "] sucessfully");
                            }
                            catch (Exception)
                            {
                                Logger.Info("TDA -> Request Denied - Push notification NOT sent to [" + noochMemberfornotification.UDID1 + "]");
                            }
                        }

                        // sending email TO USER THAT SENT THE REQUEST about rejection
                        var reqSenderFirstName = "";
                        var reqRejectorFullName = "";
                        var reqRejectorFirstName = "";

                        reqSenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                        reqRejectorFullName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName)) + " " +
                                               CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                        reqRejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));

                        string wholeAmount = transactionDetail.Amount.ToString("n2");
                        string[] s3 = wholeAmount.Split('.');

                        string memo = "";
                        if (transactionDetail.Memo != null && transactionDetail.Memo != "")
                        {
                            if (transactionDetail.Memo.Length > 3)
                            {
                                string firstThreeChars = transactionDetail.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor) memo = transactionDetail.Memo.ToString();
                                else memo = "For " + transactionDetail.Memo.ToString();
                            }
                            else memo = "For " + transactionDetail.Memo.ToString();
                        }

                        var tokens = new Dictionary<string, string>
										{
											{Constants.PLACEHOLDER_FIRST_NAME, reqSenderFirstName},
											{Constants.PLACEHOLDER_RECIPIENT_FULL_NAME, reqRejectorFullName},
											{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
											{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
											{Constants.PLACEHOLDER_RECIPIENT_FIRST_NAME,reqRejectorFirstName},
											{Constants.MEMO, memo}
										};

                        var fromAddress = Utility.GetValueFromConfig("transfersMail");
                        var toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member1.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToSender", fromAddress, toAddress, null,
                                              reqRejectorFullName + " denied your payment request", null,
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
											{Constants.PLACEHOLDER_FIRST_NAME, reqRejectorFirstName},
											{Constants.PLACEHOLDER_SENDER_FULL_NAME, reqSenderFullName},
											{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
											{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
											{Constants.MEMO, memo}
										};

                        toAddress = CommonHelper.GetDecryptedData(transactionDetail.Member.UserName);

                        try
                        {
                            Utility.SendEmail("requestDeniedToRecipient", fromAddress, toAddress, null,
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
            else return "";
        }


        #region Reject Transaction (All Types) Methods

        // REQUEST REJECTED by NON-NOOCH user.... emails to be sent to both requester and request receiver
        // request sender = who requested for money and will get money transferred into his account
        // request receiver = who will get this request and pay the requester......
        /*public string RejectMoneyRequestForNonNoochUser(string TrannsactionId)
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
              }
              catch (Exception ex)
              {
                  Logger.Error("TDA -> RejectMoneyRequestForNonNoochUser OUTER EXCEPTION - Exception: [" + ex + "]");
                  return "";
              }
          }*/


        // CREATED: JULY 2015
        // NOTE: Created specifically for the new combined landing page (/trans/RejectMoney) for rejecting all types of transfers.
        public string RejectMoneyCommon(string TransactionId, string UserType, string TransType)
        {
            Logger.Info("TDA -> RejectMoneyCommon Fired - TransID: [" + TransactionId +
                        "], TransType: [" + TransType + "], UserType: [" + UserType + "]");

            try
            {
                var transId = Utility.ConvertToGuid(TransactionId);
                var transactionDetail = new Transaction();
                transactionDetail = _dbContext.Transactions.FirstOrDefault(entity => entity.TransactionId == transId); // && entity.TransactionStatus == "Pending" //&& (memberTemp.TransactionType == "DrRr1tU1usk7nNibjtcZkA==" || memberTemp.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw==");

                if (transactionDetail != null)
                {
                    if (transactionDetail.TransactionStatus == "Pending")
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
                            // Request sent to existing user -- 'Rejector' is SenderId in transactionDetail
                            if (transactionDetail.Member1.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49")
                                SenderFirstName = "Rent Scene";
                            else
                            {
                                SenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.FirstName));
                                SenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member1.LastName));
                            }
                            RejectorFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                            RejectorLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                            RejectorFullName = RejectorFirstName + " " + RejectorLastName;
                        }

                        // UserType = NEW, TransType = REQUEST or INVITE
                        else if (userTypeDecr == "New" &&
                                 (transTypeDecr == "Request" || transTypeDecr == "Invite"))
                        {
                            //Logger.Info("TDA -> RejectMoneyCommon - CHECKPOINT 2 REACHED - User Type = New, TransType = Requst or Invite");
                            // Request sent to Non-Nooch user -- 'Rejector' is email address InvitationSentTo in transactionDetail

                            if (transactionDetail.Member.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49")
                                SenderFirstName = "Rent Scene";
                            else
                            {
                                SenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.FirstName));
                                SenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(transactionDetail.Member.LastName));
                            }

                            RejectorFirstName = "";
                            RejectorLastName = "";

                            // LinkSource = EMAIL
                            if (!String.IsNullOrEmpty(transactionDetail.InvitationSentTo) &&
                                (transactionDetail.IsPhoneInvitation == false ||
                                 transactionDetail.IsPhoneInvitation == null))
                                RejectorEmail = CommonHelper.GetDecryptedData(transactionDetail.InvitationSentTo);

                            // LinkSource = PHONE
                            else if (String.IsNullOrEmpty(transactionDetail.InvitationSentTo) &&
                                (transactionDetail.IsPhoneInvitation != false ||
                                 transactionDetail.IsPhoneInvitation != null))
                                RejectorPhone = CommonHelper.GetDecryptedData(transactionDetail.PhoneNumberInvited);
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

                            if (memberObj != null && !String.IsNullOrEmpty(memberObj.DeviceToken) && memberObj.DeviceToken.Length > 6)
                            {
                                try
                                {
                                    string mailBodyText = RejectorFullName + " just denied your payment request for $" + wholeAmount + ".";

                                    Utility.SendNotificationMessage(mailBodyText, "Nooch", memberObj.DeviceToken,
                                                                                   memberObj.DeviceType);

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
                                Logger.Error("TDA -> RejectMoneyCommon requestDeniedToRecipient - Email NOT sent to [" + toAddress +
                                             "], Exception: [" + ex.Message + "]");
                            }

                            return "Success";
                        }

                        #endregion


                        #region Notifications when trans is b/t EXISTING user and NON-NOOCH user

                        // UserType = NEW,  TransType = REQUEST or INVITE
                        if (userTypeDecr == "New" &&
                            (transTypeDecr == "Request" || transTypeDecr == "Invite"))
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

                                    Utility.SendNotificationMessage(pushBodyText, "Nooch", noochMemberfornotification.DeviceToken,
                                                                                  noochMemberfornotification.DeviceType);

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
                            return "Success.";

                        #endregion If Transaction Found
                    }
                    else
                    {
                        Logger.Info("TDA -> RejectMoneyCommon FAILED - Transaction no longer pending! - TransactionID: [" + TransactionId + "]");
                        return "Transaction found, but no longer pending.";
                    }
                }
                else
                {
                    Logger.Info("TDA -> RejectMoneyCommon FAILED - Transaction not found! - TransactionID: [" + TransactionId + "]");
                    return "Transaction not found.";
                }

                return "Server Error.";
            }
            catch (Exception ex)
            {
                Logger.Info("TDA -> RejectMoneyCommon FAILED - [Exception: " + ex.ToString() + "]");
                return "Server Error - Outer Exception TDA RejectMoneyCommon.";
            }
        }

        #endregion Reject Transaction (All Types) Methods


        public SynapseV3AddTrans_ReusableClass AddTransSynapseV3Reusable(string sender_oauth, string sender_fingerPrint,
            string sender_bank_node_id, string amount, string receiver_bank_node_id, string suppID_or_transID, string senderUserName,
            string receiverUserName, string iPForTransaction, string senderLastName, string recipientLastName, string memo)
        {
            Logger.Info("TDA -> AddTransSynapseV3Reusable Fired - Sender Last Name: [" + senderLastName +
                        "], Sender Username: " + senderUserName + "], Recip Last Name: [" + recipientLastName +
                        "], Recipient Username: [" + receiverUserName + "], Amount: [" + amount + "]");

            SynapseV3AddTrans_ReusableClass res = new SynapseV3AddTrans_ReusableClass();
            res.success = false;

            try
            {
                bool SenderSynapsePermissionOK = false;
                bool RecipientSynapsePermissionOK = false;

                #region Check Sender Synapse Permissions

                // 1. Check USER permissions for SENDER
                CommonHelper.ResetSearchData();
                synapseSearchUserResponse senderPermissions = CommonHelper.getUserPermissionsForSynapseV3(senderUserName);

                if (senderPermissions == null || !senderPermissions.success)
                {
                    Logger.Error("TDA -> AddTransSynapseV3Reusable - SENDER's Synapse Permissions was NULL or not successful from Synapse :-(");
                    res.ErrorMessage = "Problem getting Sender's bank permission level.";
                    return res;
                }

                // 2. Check BANK/NODE permission for SENDER
                if (senderPermissions.users != null && senderPermissions.users.Length > 0)
                {
                    int senderPermissionUsersCount = senderPermissions.users.Count();

                    #region Loop Through Each Sender User From Synapse List

                    int iterator = 0;

                    foreach (synapseSearchUserResponse_User senderUser in senderPermissions.users)
                    {
                        Logger.Info("TDA -> AddTransSynapseV3Reusable - Got User array for SENDER from Synapse - Name: [" + senderUser.legal_names[0] +
                                    "], User OID: [" + senderUser._id.oid + "] - About to check bank permissions...");

                        iterator++;

                        if (senderUser.nodes != null && senderUser.nodes.Length > 0)
                        {
                            #region Nodes Found For This User Iteration

                            NodePermissionCheckResult nodePermCheckRes = CommonHelper.IsNodeAllowedInGivenSetOfNodes(senderUser.nodes, sender_bank_node_id);

                            if (nodePermCheckRes.IsPermissionfound == true)
                            {
                                if (nodePermCheckRes.PermissionType == "CREDIT-AND-DEBIT") // Sender must have CREDIT-AND-DEBIT
                                {
                                    Logger.Info("TDA -> AddTransSynapseV3Reusable - Success! Found Sender's Bank with \"CREDIT-AND-DEBIT\" Permissions - " +
                                                "OID: [" + sender_bank_node_id + "]");
                                    SenderSynapsePermissionOK = true;
                                    break;
                                }
                                else
                                {
                                    var error = "TDA -> AddTransSynapseV3Reusable FAILED - Sender's Bank Permission returned by Synapse was: [" +
                                                 nodePermCheckRes.PermissionType + "] for Sender_bank_node_id: [" + sender_bank_node_id + "]";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error);
                                    res.ErrorMessage = "Sender's bank has insufficient permission: [" + nodePermCheckRes.PermissionType + "] to complete this payment";
                                    return res;
                                }
                            }
                            else if (iterator == senderPermissionUsersCount) // No nodes found for this 'user', iterate to next one unless this is the last
                            {
                                Logger.Error("TDA -> AddTransSynapseV3Reusable FAEILD - Unable to find Sender's Synapse Bank permission - [Username: " +
                                             senderUserName + "], for [Sender_bank_node_id: " + sender_bank_node_id + "]");
                                res.ErrorMessage = "Unable to find Sender's bank permissions (TDA - 3179)";
                                return res;
                            }

                            #endregion Nodes Found For This User Iteration
                        }
                        else if (senderUser.nodes == null || senderUser.nodes.Length == 0)
                        {
                            if (iterator == senderPermissionUsersCount) // Last one on the list, so abort and return with error
                            {
                                var error = "TDA -> AddTransSynapseV3Reusable FAILED - No Bank Found for Sender in Users List returned by Synapse - Username: [" + senderUserName +
                                             "] [SenderUser OID: " + senderUser._id.oid + "] - Last iteration, so aborting and returning with error.";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                res.ErrorMessage = "No banks found for Sender (TDA - 2948)";
                                return res;
                            }
                            //else // More users in the list to check, so continue iterating
                            //{
                            //Logger.Error("TDA -> AddTransSynapseV3Reusable - No Bank Found for this Sender User - Username: [" + senderUserName +
                            //             "], [SenderUser OID:" + senderUser._id.oid + "] - More users in list from Synapse, continuing to iterate...");
                            //}
                        }
                    }

                    #endregion Loop Through Each Sender User From Synapse List
                }

                if (!SenderSynapsePermissionOK)
                {
                    res.ErrorMessage = "Sender bank permission problem (TDA - 3293)";
                    return res;
                }

                #endregion Check Sender Synapse Permissions

                #region Check Recipient Synapse Permissions

                // 3. Check USER permissions for RECIPIENT
                CommonHelper.ResetSearchData();
                synapseSearchUserResponse recipPermissions = CommonHelper.getUserPermissionsForSynapseV3(receiverUserName);

                if (recipPermissions == null || !recipPermissions.success)
                {
                    Logger.Error("TDA -> SynapseV3AddTrans_ReusableClass - RECIPIENT's Synapse Permissions were NULL or not successful :-(");
                    res.ErrorMessage = "Problem getting Recipient's permission level (TDA - 3216)";
                    return res;
                }

                // 4. Check BANK/NODE permission for RECIPIENT
                if (recipPermissions.users != null && recipPermissions.users.Length > 0)
                {
                    int recipPermissionUsersCount = recipPermissions.users.Count();

                    #region Loop Through Each Recipient User From Synapse List

                    int iterator = 0;

                    // Should usually only be 1 'user' result, contained in an array from Synapse
                    foreach (synapseSearchUserResponse_User recUser in recipPermissions.users)
                    {
                        Logger.Info("TDA -> AddTransSynapseV3Reusable - User array for RECIPIENT from Synapse [Name: " + recUser.legal_names[0] +
                                    "], [User OID:" + recUser._id.oid + "] - About to check bank permissions...");

                        iterator++;

                        // Check if there are any nodes in this 'user'
                        if (recUser.nodes != null && recUser.nodes.Length > 0)
                        {
                            #region Nodes Found for this User Iteration

                            NodePermissionCheckResult nodePermCheckRes = CommonHelper.IsNodeAllowedInGivenSetOfNodes(recUser.nodes, receiver_bank_node_id);

                            if (nodePermCheckRes.IsPermissionfound == true)
                            {
                                if (nodePermCheckRes.PermissionType == "CREDIT-AND-DEBIT" ||
                                    nodePermCheckRes.PermissionType == "CREDIT")
                                {
                                    RecipientSynapsePermissionOK = true;
                                    break;
                                }
                                else
                                {
                                    var error = "TDA -> AddTransSynapseV3Reusable - Recipient's Synapse Permission returned by Synapse was: [" +
                                                 nodePermCheckRes.PermissionType + "] for [Receiver_bank_node_id: " + receiver_bank_node_id + "]";
                                    Logger.Error(error);
                                    CommonHelper.notifyCliffAboutError(error);
                                    res.ErrorMessage = "Recipient has insufficient permissions to complete this payment (TDA - 3019)";
                                    return res;
                                }
                            }
                            else if (iterator == recipPermissionUsersCount) // No nodes found for this 'user', iterate to next user unless this is the last
                            {
                                var error = "TDA -> AddTransSynapseV3Reusable FAILED - Unable to find Recipient's Synapse Bank permission - [Username: " +
                                              receiverUserName + "], for [Receiver_bank_node_id: " + receiver_bank_node_id + "]";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                res.ErrorMessage = "Recipient has insufficient permissions to complete this payment (TDA - 3264)";
                                return res;
                            }

                            #endregion Nodes Found for this User Iteration
                        }
                        else if (recUser.nodes == null || recUser.nodes.Length == 0)
                        {
                            if (iterator == recipPermissionUsersCount) // Last one on the list, so abort and return with error
                            {
                                var error = "TDA -> AddTransSynapseV3Reusable FAILED - No Bank Found for Recipient in Users List returned by Synapse - Username: [" + receiverUserName +
                                             "], [RecipUser OID: " + recUser._id.oid + "] - Last iteration, so aborting and returning with error.";
                                Logger.Error(error);
                                CommonHelper.notifyCliffAboutError(error);
                                res.ErrorMessage = "No banks found for Recipient (TDA - 3276)";
                                return res;
                            }
                            else // More users in the list to check, so continue iterating
                            {
                                Logger.Error("TDA -> AddTransSynapseV3Reusable - No Bank Found for this Recipient User - Username: [" + receiverUserName +
                                             "], [RecipUser OID:" + recUser._id.oid + "] - More users in list from Synapse, continuing to iterate...");
                            }
                        }
                    }

                    #endregion Loop Through Each Recipient User From Synapse List
                }

                if (!RecipientSynapsePermissionOK)
                {
                    res.ErrorMessage = "Recipient bank permission problem (TDA - 3063)";
                    return res;
                }

                #endregion Check Recipient Synapse Permissions


                // All set... time to move money between accounts
                try
                {
                    #region Setup Synapse V3 Order Details

                    bool isTesting = Convert.ToBoolean(Utility.GetValueFromConfig("IsRunningOnSandBox"));

                    var senderMemberDetails = CommonHelper.GetMemberDetailsByUserName(senderUserName);
                    var companyName = "NOOCH";
                    if (senderMemberDetails.isRentScene == true) companyName = "RENT SCENE";
                    else if (senderUserName == "andrew@tryhabitat.com") companyName = "HABITAT";

                    if (receiverUserName == "payments@rentscene.com") recipientLastName = ""; // Blank for the Bank memo since the company will already be in it.

                    SynapseV3AddTransInput transParamsForSynapse = new SynapseV3AddTransInput();

                    SynapseV3Input_login login = new SynapseV3Input_login() { oauth_key = sender_oauth };
                    SynapseV3Input_user user = new SynapseV3Input_user() { fingerprint = sender_fingerPrint };
                    transParamsForSynapse.login = login;
                    transParamsForSynapse.user = user;

                    SynapseV3AddTransInput_trans transMain = new SynapseV3AddTransInput_trans();

                    // CLIFF (5/9/16): Just a note for future reference... (esp for Rent Scene)
                    //                 We may want to consider doing this differently and create 2 orders w/ Synapse,
                    //                 a DEBIT from the sender's ACH Bank to Nooch's Synapse Account (or RS's) and then
                    //                 a CREDIT from Nooch's Synapse Account to the recipient's ACH Bank.
                    //                 By 'fronting' the money for the Sender before the debit actually clears,
                    //                 we would speed up the total processing time. Could only do it with 100% trustworthy users...
                    //                 So maybe we can make it an option here (e.g. via a param "fastProcessing") and then do whichever method.
                    //                 ** NOT A PRIORITY YET **

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
                        amount = amount.Trim(), // CLIFF (5/9/16): Changed this from a double to a string type b/c the Synapse docs say this should be a string... need to test!
                        currency = "USD"
                    };
                    transMain.amount = amountMain;

                    if (String.IsNullOrEmpty(iPForTransaction) || iPForTransaction.Length < 6)
                        iPForTransaction = "54.148.37.21"; // Nooch's Server IP as default

                    var webhooklink = Utility.GetValueFromConfig("NoochWebHookURL") + suppID_or_transID;

                    var noteTxt = companyName + " / " + senderLastName + " / " + recipientLastName;
                    //if (!String.IsNullOrEmpty(memo) && memo.Length > 1)
                    //    noteTxt += memo.Substring(0, 9);

                    SynapseV3AddTransInput_trans_extra extraMain = new SynapseV3AddTransInput_trans_extra()
                    {
                        // This is where we put the ACH memo (customized for Landlords, but just the same template for regular P2P transfers: "Nooch Payment {LNAME SENDER} / {LNAME RECIPIENT})
                        // maybe we should set this in whichever function calls this function because we don't have the names here...
                        // yes modifying this method to add 3 new parameters....sender IP, sender last name, recepient last name... this would be helpfull in keeping this method clean.
                        note = noteTxt,
                        supp_id = suppID_or_transID,
                        process_on = 0, // CLIFF: This is an optional parameter, but we always want it to process immediately, so it should always be 0
                        ip = iPForTransaction, // CLIFF:  This is actually required. It should be the most recent IP address of the SENDER, or if none found, then '54.148.37.21'
                        webhook = webhooklink
                    };
                    transMain.extra = extraMain;

                    SynapseV3AddTransInput_trans_fees feeMain = new SynapseV3AddTransInput_trans_fees();
                    feeMain.note = companyName == "RENT SCENE" ? "Negative Rent Scene Fee" : "Negative Nooch Fee";
                    feeMain.fee = "-0.10"; // to offset the Synapse fee so the user doesn't pay it

                    SynapseV3AddTransInput_trans_fees_to tomain = new SynapseV3AddTransInput_trans_fees_to()
                    {
                        id = isTesting ? "5618028c86c27347a1b3aa0f" // Temporary: ID of Nooch's SYNAPSE account (NOT an ACH (bank) account)!!... using temp Sandbox account until we get Production credentials
                                       : "57436f4395062947f21bbcb6" // CC (6/7/16): THIS IS THE LIVE, PRODUCTION SYNAPSE V3 NODE ** FOR RENT SCENE **
                    };
                    feeMain.to = tomain;

                    transMain.fees = new SynapseV3AddTransInput_trans_fees[1];
                    transMain.fees[0] = feeMain;

                    transParamsForSynapse.trans = transMain;

                    string UrlToHitV3 = isTesting ? "https://sandbox.synapsepay.com/api/v3/trans/add" : "https://synapsepay.com/api/v3/trans/add";

                    #endregion Setup Synapse V3 Order Details


                    #region Calling Synapse V3 TRANSACTION ADD

                    try
                    {
                        // Calling Add Trans API

                        var http = (HttpWebRequest)WebRequest.Create(new Uri(UrlToHitV3));
                        http.Accept = "application/json";
                        http.ContentType = "application/json";
                        http.Method = "POST";

                        var parsedContent = JsonConvert.SerializeObject(transParamsForSynapse);

                        Logger.Info("TDA -> AddTransSynapseV3Reusable - Payload to send to /v3/trans/add API: [" + transParamsForSynapse + "]");//From Bank ID: [" + transParamsForSynapse.trans.from.id +
                        //"], To Bank ID: [" + transParamsForSynapse.trans.to.id + "], Amount: [" + transParamsForSynapse.trans.amount +
                        //"], Note: [" + transParamsForSynapse.trans.extra.note + "]");

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

                        Logger.Info("TDA -> AddTransSynapseV3Reusable - RESPONSE FROM Synapse V3 /trans/add API: " + JObject.Parse(content));

                        if (synapseResponse.success == true ||
                            synapseResponse.success.ToString().ToLower() == "true")
                        {
                            res.success = true;
                            res.ErrorMessage = "OK";

                            // save changes into synapseTransactionResult table in db
                            SynapseAddTransactionResult satr = new SynapseAddTransactionResult();
                            satr.TransactionId = Utility.ConvertToGuid(suppID_or_transID);
                            satr.OidFromSynapse = synapseResponse.trans._id.oid.ToString();
                            satr.Status_DateTimeStamp = synapseResponse.trans.recent_status.date.date.ToString();
                            satr.Status_Id = synapseResponse.trans.recent_status.status_id;
                            satr.Status_Note = synapseResponse.trans.recent_status.note;
                            satr.Status_Text = synapseResponse.trans.recent_status.status;

                            _dbContext.SynapseAddTransactionResults.Add(satr);
                            _dbContext.SaveChanges();

                            // CC (8/17/16): I DON'T THINK WE NEED TO CREATE A NEW SUBSCRIPTION FOR EVERY BANK.
                            //               I THINK WE JUST CREATE THE SUBSCRIPTION *ONCE* (...EVER) AND THAT APPLIES TO
                            //               *ALL* USERS/BANKS/TRANSACTIONS CREATED USING OUR CLIENT ID/SECRET
                            //subscribe this Transaction on synapse
                            //setSubcriptionToTrans(satr.OidFromSynapse.ToString(), senderUserName);
                        }
                        else
                        {
                            var error = "TDA -> AddTransSynapseV3Reusable FAILED - Error From Synapse Service - Response from Synapse: [" +
                                        JObject.Parse(content) + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                            res.success = false;
                            res.ErrorMessage = "Check synapse error.";
                        }

                        res.responseFromSynapse = synapseResponse;
                    }
                    catch (WebException ex)
                    {
                        var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                        JObject jsonFromSynapse = JObject.Parse(resp);

                        var error = "TDA -> AddTransSynapseV3Reusable FAILED - Exception: [" + jsonFromSynapse.ToString() + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);

                        JToken token = jsonFromSynapse["error"]["en"];

                        // CLIFF (5/16/16): Synapse's error msg could be:
                        //                  1.) "You do not have sufficient balance for this transfer"
                        //                  2.) "Sender is not authorizied to send payments. Make the user go through our KYC widget to enable sending function"
                        if (token != null)
                            res.ErrorMessage = token.ToString();
                        else
                            res.ErrorMessage = "Server Error (TDA 3239) in AddTransSynapseV3Reusable.";
                    }

                    #endregion Calling Synapse V3 TRANSACTION ADD
                }
                catch (Exception ex)
                {
                    var error = "TDA -> AddTransSynapseV3Reusable FAILED - Inner Exception (3103): [Exception: " + ex + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);
                    res.ErrorMessage = "Server Error - TDA Inner Exception";
                }
            }
            catch (Exception ex)
            {
                var error = "TDA -> AddTransSynapseV3Reusable FAILED - Outer Exception (3255): [Exception: " + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                res.ErrorMessage = "TDA Outer Exception";
            }

            return res;
        }


        /// <summary>
        /// For an existing Nooch user making a payment request to another existing Nooch user.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public string RequestMoney(RequestDto requestDto)
        {
            Logger.Info("TDA -> RequestMoney Fired - MemberID: [" + requestDto.MemberId +
                        "], SenderID: [" + requestDto.SenderId +
                        "], RecipientID: [" + requestDto.ReceiverId + "]");

            string requestId = string.Empty;

            try
            {
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

                //decimal transactionFee = Utility.ConvertToDecimal(Utility.GetValueFromConfig("PayGoModeFee"));
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
                            SenderId = Utility.ConvertToGuid(senders[i]),
                            RecipientId = Utility.ConvertToGuid(requestDto.MemberId),
                            Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                            Amount = requestDto.Amount - 0,
                            TransactionDate = DateTime.Now,
                            Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                            DisputeStatus = null,
                            TransactionStatus = "Pending",
                            TransactionType = CommonHelper.GetEncryptedData("Request"),
                            DeviceId = requestDto.DeviceId,
                            TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                            TransactionFee = 0,
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

                                    if (startWithFor) memo = transaction.Memo.ToString();
                                    else memo = "For " + transaction.Memo.ToString();
                                }
                                else memo = "For " + transaction.Memo.ToString();
                            }
                            string sendersPic;
                            if (!string.IsNullOrEmpty(requester.Photo))
                            {
                                string lastFourOfRecipientsPic = requester.Photo.Substring(requester.Photo.Length - 15);
                                sendersPic = lastFourOfRecipientsPic != "gv_no_photo.png" ? "" : requester.Photo.ToString();
                            }

                            // Send 1 TOTAL email to Sender for this Request (i.e. If there are 5 recipients, don't need to send a separate email for each)
                            if (!alreadySentEmailToSender)
                            {
                                // This cancel link will currently only cancel the individual request to the 1st recipient (NEED NEW WAY TO CANCEL ALL REQUESTS FOR A GROUP REQUEST)
                                string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/CancelRequest?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId.ToString() + "&userType=mx5bTcAYyiOf9I5Py9TiLw==");

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

                                    Logger.Info("TDA -> RequestMoney - Multiple Recipients - requestSent email sent to [" + toAddress + "] successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> RequestMoney - Multiple Recipients - requestSent email NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                                }

                                // So we don't send another email to the Sender in the next loop through...
                                alreadySentEmailToSender = true;
                            }

                            // Send 1 email to EACH Recipient
                            string otherlink2 = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/RejectMoney?TransactionId=" + transaction.TransactionId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
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

                                Logger.Info("TDA -> RequestMoney requestReceivedToExistingUser - Multiple Recipients - [" + emailTemplateToRecipient +
                                            "] email sent to [" + toAddress2 + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> RequestMoney requestReceivedToExistingUser - Multiple Recipients - [" + emailTemplateToRecipient +
                                             "] email NOT sent to [" + toAddress2 + "], Exception: [" + ex + "]");
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
                                        string deviceId = friendDetails != null ? recipientOfRequest.DeviceToken : null;
                                        string devicety = friendDetails != null ? recipientOfRequest.DeviceType : null;
                                        string mailBodyText = "Hi, " + RequesterFirstName + " " + RequesterLastName +
                                                              " requested $" + wholeAmount + " from you. Pay up now using Nooch.";

                                        try
                                        {
                                            if (!String.IsNullOrEmpty(deviceId) && (friendDetails.TransferAttemptFailure ?? false))
                                            {
                                                Utility.SendNotificationMessage(mailBodyText, "Nooch", deviceId, devicety);

                                                Logger.Info("TDA -> RequestMoney - Request Received Push notification sent to [" + RequesterFirstName + " " + RequesterLastName + "] successfully.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("Request Received Push Notification NOT sent to [" + deviceId + "], Exception: [" + ex + "]");
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                            return "Request failed.";
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

                        SenderId = sender.MemberId,
                        RecipientId = requester.MemberId,
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

                        //string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                        //                                  "/Nooch/CancelMoneyRequest?TransactionId=" + requestId +
                        //                                  "&MemberId=" + requestDto.MemberId.ToString() +
                        //                                  "&userType=mx5bTcAYyiOf9I5Py9TiLw==");
                        string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                         "/Nooch/CancelRequest?TransactionId=" + requestId +
                                                         "&MemberId=" + requestDto.MemberId.ToString() +
                                                         "&userType=mx5bTcAYyiOf9I5Py9TiLw==");

                        string wholeAmount = requestDto.Amount.ToString("n2");
                        string[] s32 = wholeAmount.Split('.');

                        string memo = "";
                        if (!string.IsNullOrEmpty(transaction.Memo))
                        {
                            if (transaction.Memo.Length > 3)
                            {
                                string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor) memo = transaction.Memo.ToString();
                                else memo = "For " + transaction.Memo.ToString();
                            }
                            else memo = "For " + transaction.Memo.ToString();
                        }


                        string requesterPic = (!String.IsNullOrEmpty(requester.Photo) && requester.Photo.Length > 20)
                            ? requester.Photo : "https://www.noochme.com/EmailTemplates/img/userpic-default.png";

                        string senderPic = (!String.IsNullOrEmpty(sender.Photo) && sender.Photo.Length > 20)
                            ? sender.Photo : "https://www.noochme.com/EmailTemplates/img/userpic-default.png";

                        bool isForRentScene = false;

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
                                // Include 'UserType', and 'TransType' as encrypted along with request
                                // In this case UserType would = 'Nonregistered'  ->  6KX3VJv3YvoyK+cemdsvMA==
                                //              TransType would = 'Request'

                                string userType = "6KX3VJv3YvoyK+cemdsvMA=="; // "NonRegistered"

                                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "Nooch/RejectMoney?TransactionId=" + requestId +
                                                                  "&UserType=" + userType +
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
                                // Send email to Request Receiver -- sending UserType, TransType as encrypted along with request
                                // In this case UserType would = 'Existing'
                                // TransType would be 'Request'

                                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "Nooch/RejectMoney?TransactionId=" + transaction.TransactionId +
                                                                  "&UserType=mx5bTcAYyiOf9I5Py9TiLw==" +
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

                            var toAddress = CommonHelper.GetDecryptedData(sender.UserName);

                            Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                              RequesterFirstName + " " + RequesterLastName + " requested " + "$" + wholeAmount,
                                              null, tokens2, null, null, null);

                            Logger.Info("TDA -> RequestMoney - [" + templateToUse + "] email sent to [" + toAddress + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RequestMoney - RequestReceivedToExistingUser email NOT sent to [" + CommonHelper.GetDecryptedData(sender.UserName) + "], [Exception: " + ex + "]");
                        }

                        #endregion Email To Request Recipient

                        #region Push Notification To Request Recipient

                        if (!String.IsNullOrEmpty(sender.DeviceToken) && sender.DeviceToken.Length > 6)
                        {
                            var notifSettings = CommonHelper.GetMemberNotificationSettings(sender.MemberId.ToString());

                            Logger.Info("TDA -> RequestMoney - JUST GOT NOTIFICATION SETTINGS");

                            if (notifSettings != null && notifSettings.TransferReceived != false)
                            {
                                try
                                {
                                    string deviceId = sender.DeviceToken;
                                    //string emoji = "\U0001F601";

                                    string msg = "Hi " + RequestReceiverFirstName + ", " + RequesterFirstName + " " + RequesterLastName +
                                                 " just requested $" + wholeAmount + " from you. Open Nooch to pay up now (or reject this request)!";

                                    Utility.SendNotificationMessage(msg, "Nooch", sender.DeviceToken, sender.DeviceType);

                                    Logger.Info("TDA -> RequestMoney - (B/t 2 Existing Nooch Users) - Push notification sent successfully - Username: [" +
                                                CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], DeviceToken: [" + sender.DeviceToken + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> RequestMoney - Request Received Push Notification NOT sent to Username: [" +
                                                 CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], DeviceToken: [" + sender.DeviceToken + "], Exception: [" + ex + "]");
                                }
                            }
                        }

                        #endregion Push Notification To Request Recipient

                        #endregion Send Notifications To Both Users

                        return "Request made successfully.";
                    }
                    else return "Request failed.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> RequestMoney FAILED - Outer Exception: [" + ex.Message + "]");
            }

            return "Request failed. Outer exception.";
        }


        /// <summary>
        /// Cliff (5/31/16): Not sure why this method exists... it looks like RequestMoney, but we use TransferMoneyUsingSynapse for
        ///                  payments b/t two existing users.  And there was no TransferMoney() in the old architecture, so not sure how this got created.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <returns></returns>
        public string TransferMoney(RequestDto requestDto)
        {
            Logger.Info("TDA -> TransferMoney Fired - MemberID: [" + requestDto.MemberId + "], ReceiverID: [" + requestDto.ReceiverId + "]");

            string requestId = string.Empty;

            try
            {
                #region Initial Checks

                // Check uniqueness of requesting and sending user
                if (requestDto.MemberId == requestDto.SenderId)
                    return "Not allowed to Transfer money to yourself.";

                var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);

                var receiver = new Member();
                if (requestDto.ReceiverId.IndexOf(',') == -1)
                    receiver = CommonHelper.GetMemberDetails(requestDto.ReceiverId);

                // Check that both accounts exist
                if (receiver == null || requester == null)
                    return "Either sending user or requesting user does not exist.";
                // Validate PIN of requesting user

                string validPinNumberResult = CommonHelper.ValidatePinNumber(requestDto.MemberId, requestDto.PinNumber);
                if (validPinNumberResult != "Success")
                    return validPinNumberResult;

                //decimal transactionFee = Utility.ConvertToDecimal(Utility.GetValueFromConfig("PayGoModeFee"));
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
                            Logger.Error("TDA -> TransferMoney FAILED - OVER PERSONAL TRANS LIMIT - " +
                                         "Amount Requested: [" + transactionAmount.ToString() + "], " +
                                         "Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                            return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + thisUserTransLimit.ToString("F2");
                        }
                    }
                }

                if (CommonHelper.isOverTransactionLimit(requestDto.Amount, requestDto.SenderId, requestDto.MemberId))
                {
                    Logger.Error("TDA -> TransferMoney FAILED - OVER GLOBAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                 "], Global Limit: [" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")) + "], MemberId: [" + requestDto.MemberId + "]");
                    return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request at a time is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                }

                #endregion Initial Checks

                // Request Sent to more than one Nooch member
                if (requestDto.ReceiverId.IndexOf(',') != -1)
                {
                    #region If Multiple Recepients

                    string[] receivers = requestDto.ReceiverId.Split(',');

                    bool alreadySentEmailToReceiver = false;

                    // A loop is added for for requests to multiple users
                    for (int i = 0; i < receivers.Length; i++)
                    {
                        // Create multiple transaction requests
                        var transaction = new Transaction
                        {
                            TransactionId = Guid.NewGuid(),
                            RecipientId = Utility.ConvertToGuid(receivers[i]),
                            SenderId = Utility.ConvertToGuid(requestDto.MemberId),
                            Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                            Amount = requestDto.Amount - 0,
                            TransactionDate = DateTime.Now,
                            Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                            DisputeStatus = null,
                            TransactionStatus = "Pending",
                            TransactionType = CommonHelper.GetEncryptedData("Transfer"),
                            DeviceId = requestDto.DeviceId,
                            TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                            TransactionFee = 0,
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

                            //  BELOW CODE ADDED BY CLIFF (11/26/14) FOR SENDING EMAILS FOR WHEN THERE ARE MULTIPLE RECIPIENTS
                            #region Cliffs Additions

                            var recipientOfRequest = CommonHelper.GetMemberDetails(receivers[i]);

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

                            // Send 1 TOTAL email to Recevier for this Request (i.e. If there are 5 recipients, don't need to send a separate email for each)
                            if (!alreadySentEmailToReceiver)
                            {
                                // This cancel link will currently only cancel the individual request to the 1st recipient (NEED NEW WAY TO CANCEL ALL REQUESTS FOR A GROUP REQUEST)
                                string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/CancelRequest?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId.ToString() + "&userType=mx5bTcAYyiOf9I5Py9TiLw==");

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
                                        "Your Nooch requests to " + receivers.Length + " people is pending", null,
                                        tokens, null, null, null);

                                    Logger.Info("requestSent --> Multiple Recipients --> email sent to [" + toAddress + "] successfully.");
                                }
                                catch (Exception)
                                {
                                    Logger.Error("requestSent --> Multiple Recipients --> email NOT sent to [" + toAddress + "]. Problem occurred in sending mail.");
                                }

                                // So we don't send another email to the Receiver in the next loop through...
                                alreadySentEmailToReceiver = true;
                            }

                            // Send 1 email to EACH Recipient
                            string otherlink2 = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/Nooch/RejectMoney?TransactionId=" + transaction.TransactionId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
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
                                        string deviceTy = friendDetails != null ? recipientOfRequest.DeviceType : null;
                                        string mailBodyText = "Hi, " + RequesterFirstName + " " + RequesterLastName +
                                                              " requested $" + wholeAmount + " from you. Pay up now using Nooch.";

                                        try
                                        {
                                            if (!String.IsNullOrEmpty(deviceId) && (friendDetails.TransferAttemptFailure ?? false))
                                            {

                                                string response = Utility.SendNotificationMessage(mailBodyText, "Nooch", deviceId,
                                                                                   deviceTy);


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

                        RecipientId = receiver.MemberId,
                        SenderId = requester.MemberId,
                        Amount = requestDto.Amount,
                        TransactionDate = DateTime.Now,
                        Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                        Memo = (requestDto.Memo == "") ? "" : requestDto.Memo,
                        DisputeStatus = null,
                        TransactionStatus = "Pending",
                        TransactionType = CommonHelper.GetEncryptedData("Transfer"),
                        DeviceId = requestDto.DeviceId,
                        TransactionTrackingId = CommonHelper.GetRandomTransactionTrackingId(),
                        TransactionFee = 0,
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

                        string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)));
                        string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)));
                        string RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(receiver.FirstName)));
                        string RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(receiver.LastName)));

                        //string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                        //                                  "/Nooch/CancelMoneyRequest?TransactionId=" + requestId +
                        //                                  "&MemberId=" + requestDto.MemberId.ToString() +
                        //                                  "&userType=mx5bTcAYyiOf9I5Py9TiLw==");

                        string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                  "/Nooch/CancelRequest?TransactionId=" + requestId +
                                  "&MemberId=" + requestDto.MemberId.ToString() +
                                  "&userType=mx5bTcAYyiOf9I5Py9TiLw==");

                        string wholeAmount = requestDto.Amount.ToString("n2");
                        string[] s32 = wholeAmount.Split('.');

                        string memo = "";
                        if (!string.IsNullOrEmpty(transaction.Memo))
                        {
                            if (transaction.Memo.Length > 3)
                            {
                                string firstThreeChars = transaction.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor) memo = transaction.Memo.ToString();
                                else memo = "For " + transaction.Memo.ToString();
                            }
                            else
                                memo = "For " + transaction.Memo.ToString();
                        }

                        var requesterPic = (!String.IsNullOrEmpty(requester.Photo) && requester.Photo.Length > 20)
                            ? requester.Photo : "https://www.noochme.com/EmailTemplates/img/userpic-default.png";

                        var senderPic = (!String.IsNullOrEmpty(receiver.Photo) && receiver.Photo.Length > 20)
                            ? receiver.Photo : "https://www.noochme.com/EmailTemplates/img/userpic-default.png";

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

                            if (receiver.Status == "NonRegistered" ||
                                receiver.Type == "Personal - Browser")
                            {
                                Logger.Info("TDA -> RequestMoney - Request to a NON-REGISTERED USER - Sending requestReceivedToExistingNonRegUser Email Template...");

                                // Send email to REQUEST RECIPIENT (person who will pay/reject this request)
                                // Include 'UserType', and 'TransType' as encrypted along with request
                                // In this case UserType would = 'Nonregistered'  ->  6KX3VJv3YvoyK+cemdsvMA==
                                //              TransType would = 'Request'

                                string userType = "6KX3VJv3YvoyK+cemdsvMA=="; // "NonRegistered"

                                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "Nooch/RejectMoney?TransactionId=" + requestId +
                                                                  "&UserType=" + userType +
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
                                // Send email to Request Receiver -- sending UserType, TransType as encrypted along with request
                                // In this case UserType would = 'Existing'
                                // TransType would be 'Request'
                                // and link source would be 'Email'

                                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "Nooch/RejectMoney?TransactionId=" + transaction.TransactionId +
                                                                  "&UserType=mx5bTcAYyiOf9I5Py9TiLw==" +
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

                            var toAddress = CommonHelper.GetDecryptedData(receiver.UserName);

                            Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                              RequesterFirstName + " " + RequesterLastName + " requested " + "$" + wholeAmount,
                                              null, tokens2, null, null, null);

                            Logger.Info("TDA -> RequestMoney - [" + templateToUse + "] email sent to [" + toAddress + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> TransferMoney - TransferMoneyToExistingUser email NOT sent to [" + CommonHelper.GetDecryptedData(receiver.UserName) + "], [Exception: " + ex + "]");
                        }

                        #endregion Email To Request Recipient

                        #region Push Notification To Request Recipient

                        if (!String.IsNullOrEmpty(receiver.DeviceToken) && receiver.DeviceToken.Length > 6)
                        {
                            var notifSettings = CommonHelper.GetMemberNotificationSettings(receiver.MemberId.ToString());

                            if (notifSettings != null && notifSettings.TransferReceived != false)
                            {
                                try
                                {
                                    string deviceId = receiver.DeviceToken;
                                    //string emoji = "\U0001F601";

                                    string msg = "Hi " + RequestReceiverFirstName + ", " + RequesterFirstName + " " + RequesterLastName +
                                                 " just requested $" + wholeAmount + " from you. Open Nooch to pay up now (or reject this request)!";

                                    Utility.SendNotificationMessage(msg, "Nooch", receiver.DeviceToken, receiver.DeviceType);

                                    Logger.Info("TDA -> SendTransactionReminderEmail - (B/t 2 Existing Nooch Users) - Push notification sent successfully - Username: [" +
                                                CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], DeviceToken: [" + receiver.DeviceToken + "]");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> RequestMoney - Request Received Push Notification NOT sent to Username: [" +
                                                 CommonHelper.GetDecryptedData(transaction.Member.UserName) + "], DeviceToken: [" + receiver.DeviceToken + "], Exception: [" + ex + "]");
                                }
                            }
                        }

                        #endregion Push Notification To Request Recipient

                        #endregion Send Notifications To Both Users

                        return "Request made successfully.";
                    }
                    else return "Request failed.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> RequestMoney FAILED - Outer Exception: [" + ex + "]");
            }

            return "Request failed. Outer exception.";
        }


        /// <summary>
        /// To REQUEST money from NON-NOOCH user using SYANPSE through EMAIL (REQUEST VIA PHONE HAS A DIFF METHOD)
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        public string RequestMoneyToNonNoochUserUsingSynapse(RequestDto requestDto)
        {
            string requestId = string.Empty;

            // Check if the recipient's email ('MoneySenderEmailId') is already registered to an existing Nooch user.
            var checkuser = CommonHelper.GetMemberDetailsByUserName(requestDto.MoneySenderEmailId);

            if (checkuser == null)
            {
                Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse Fired - Requester MemberID: [" + requestDto.MemberId + "].");

                var requester = CommonHelper.GetMemberDetails(requestDto.MemberId);

                #region Initial Checks

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

                if (requestorInfo.wereBankDetailsFound == false || requestorInfo.BankDetails == null)
                {
                    Logger.Error("TDA - RequestMoneyToNonNoochUserUsingSynapse FAILED -> Transfer ABORTED: No Bank Details found for " +
                                 "Requester - MemberID: [" + requestDto.MemberId + "]");
                    return "Requester does not have any bank added";
                }

                // Check Requestor's Synapse Bank Account STATUS
                if (requestorInfo.BankDetails.Status != "Verified")
                {
                    Logger.Error("TDA - RequestMoneyToNonNoochUserUsingSynapse FAILED -> Transfer ABORTED: No verified bank account for " +
                                 "Requester - MemberID: [" + requestDto.MemberId + "]");
                    return "Requester does not have any verified bank account.";
                }

                #endregion SenderSynapseAccountDetails

                #endregion Initial Checks


                #region Create & Save Transaction Object

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    SenderId = Utility.ConvertToGuid(requestDto.MemberId),
                    RecipientId = Utility.ConvertToGuid(requestDto.MemberId),
                    Amount = requestDto.Amount,
                    TransactionDate = DateTime.Now,
                    Picture = (requestDto.Picture != null) ? requestDto.Picture : null,
                    Memo = !String.IsNullOrEmpty(requestDto.Memo) ? requestDto.Memo : "",
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

                #endregion Create & Save Transaction Object

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

                    bool isForRentScene = (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") ? true : false; // Rent Scene's account

                    var fromAddress = isForRentScene ? "payments@rentscene.com"
                                                     : Utility.GetValueFromConfig("transfersMail");

                    var templateToUse_Recip = isForRentScene ? "requestReceivedToNewUser_RentScene"
                                                             : "requestReceivedToNewUser";

                    var templateToUse_Sender = isForRentScene ? "requestSent_RentScene"
                                                              : "requestSent";

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

                    if (!isForRentScene) // Not sending this one to Rent Scene (just b/c it was considered unnecessary for them)
                    {
                        try
                        {
                            var tokens = new Dictionary<string, string>
					        {
						        {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
						        {Constants.PLACEHOLDER_NEWUSER, recipientsEmail},
						        {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
						        {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
						        {Constants.PLACEHOLDER_OTHER_LINK, cancelLink},
						        {Constants.MEMO, memo}
					         };

                            Utility.SendEmail(templateToUse_Sender, fromAddress, RequesterEmail, null,
                                              "Your payment request to " + recipientsEmail + " is pending",
                                              null, tokens, null, null, null);

                            Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> [" + templateToUse_Sender + "] email sent to [" + RequesterEmail + "] successfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> [" + templateToUse_Sender + "] email NOT sent to [" + RequesterEmail +
                                         "], Exception: [" + ex + "]");
                        }
                    }

                    try
                    {
                        // Send email to Request Receiver
                        // In this case UserType would = 'New'
                        // TransType would = 'Request'
                        // and link source would = 'Email'

                        string cipToUse = "1";
                        switch (requestDto.cipTag)
                        {
                            case "renter":
                                cipToUse = "1";
                                break;
                            case "vendor":
                                cipToUse = "2";
                                break;
                            case "runner":
                                cipToUse = "2";
                                break;
                            case "landlord":
                                cipToUse = "3";
                                break;
                        }

                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                   "Nooch/RejectMoney?TransactionId=" + requestId +
                                                   "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                   "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                        string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                       "Nooch/PayRequest?TransactionId=" + requestId +
                                                       "&UserType=U6De3haw2r4mSgweNpdgXQ==" + // UserType = "New"
                                                       "&cip=" + cipToUse);

                        if (isForRentScene)
                            paylink += "&rs=1";

                        var tokens2 = new Dictionary<string, string>
                        {
					        {Constants.PLACEHOLDER_FIRST_NAME, RequesterFirstName},
					        {Constants.PLACEHOLDER_NEWUSER, recipientsEmail},
					        {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s32[0].ToString()},
					        {Constants.PLACEHLODER_CENTS, s32[1].ToString()},
					        {Constants.PLACEHOLDER_REJECT_LINK, rejectLink},
					        {Constants.PLACEHOLDER_SENDER_FULL_NAME, RequesterFirstName + " " + RequesterLastName},
					        {Constants.MEMO,memo},
					        {Constants.PLACEHOLDER_PAY_LINK, paylink}
					    };

                        string subject = isForRentScene
                                         ? "Payment Request from Rent Scene"
                                         : RequesterFirstName + " " + RequesterLastName + " requested " + "$" + s22.ToString() + " with Nooch";

                        Utility.SendEmail(templateToUse_Recip, fromAddress,
                                          recipientsEmail, null, subject, null, tokens2,
                                          null, null, null);

                        Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> [" + templateToUse_Recip + "] email sent to [" + recipientsEmail + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse -> [" + templateToUse_Recip + "] email NOT sent to [" + recipientsEmail +
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
                //Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED -> Member Already Exists for email address: [" +
                //             requestDto.MoneySenderEmailId + "], Requester MemberID: [" + requestDto.MemberId + "]");

                // CLIFF (12/24/15): NEED TO ADD A CALL TO THE REGULAR REQUEST METHOD FOR EXISTING USERS.
                // UPDATED (5/16/16)

                #region Forward to Regular RequestMoney Method

                Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse - Recipient already exists - [" + requestDto.MoneySenderEmailId + "], about to forward to RequestMoney()");

                // Update the RequestDto object to use the found member's MemberID, and remove the email address that's for sending to a non-Nooch user.
                requestDto.SenderId = checkuser.MemberId.ToString();
                requestDto.MoneySenderEmailId = "";

                var resFromRequestMoney = RequestMoney(requestDto);

                Logger.Info("TDA -> RequestMoneyToNonNoochUserUsingSynapse - RequestMoney() response was: [" + resFromRequestMoney + "]");

                return resFromRequestMoney;

                #endregion Forward to Regular RequestMoney Method
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
        public string RequestMoneyToExistingButNonregisteredUser(RequestDto requestDto)
        {
            try
            {
                Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser Fired - Requester MemberID: [" + requestDto.MemberId + "].");

                string requestId = string.Empty;

                // Check uniqueness of requesting and sending user
                if (requestDto.MemberId == requestDto.SenderId)
                    return "Not allowed to request money from yourself.";

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

                if (requestDto.Amount == null || String.IsNullOrEmpty(requestDto.Amount.ToString()))
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonRegisteredUser FAILED - Missing Amount - [MemberID: " + requestDto.MemberId + "]");
                    return "Missing Amount";
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

                if (requestorSynInfo.wereUserDetailsFound != true || requestorSynInfo.UserDetails == null)
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Transfer ABORTED: Requester's Synapse User Details NOT FOUND - " +
                                 "Requester MemberID: [" + requestDto.MemberId + "]");
                    return "Requester does not have any bank added";
                }
                if (requestorSynInfo.wereBankDetailsFound != true || requestorSynInfo.BankDetails == null)
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Transfer ABORTED: Requester's Synapse bank account NOT FOUND - " +
                                 "Requester MemberID: [" + requestDto.MemberId + "]");
                    return "Requester does not have any bank added";
                }
                if (!(requestorSynInfo.UserDetails.permission == "SEND-AND-RECEIVE" ||
                      requestorSynInfo.UserDetails.permission == "RECEIVE")) // Requester's Permissions
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Sender's Synapse Permission is INSUFFICIENT: [" + requestorSynInfo.UserDetails.permission +
                                 "] MemberID: [" + requestDto.MemberId + "]");
                    //return "Requester does not have any bank added";
                    return "Requester has insufficient permissions: [" + requestorSynInfo.UserDetails.permission + "]";
                }
                if (!(requestorSynInfo.BankDetails.allowed == "CREDIT-AND-DEBIT" ||
                      requestorSynInfo.BankDetails.allowed == "CREDIT")) // Requester ($ recipient) just needs "CREDIT" for 'Allowed' Value
                {
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser ABORTED: " +
                                "Sender's Synapse bank not verified - Bank-Allowed: [" + requestorSynInfo.BankDetails.allowed + "], " +
                                "MemberID: [" + requestDto.MemberId + "], TransactionId: [" + requestDto.TransactionId + "]");
                    //return "Requester does not have any verified bank account.";
                    return "Requester's bank has insufficient permissions: [" + requestorSynInfo.BankDetails.allowed + "]";
                }

                #endregion Get Sender's Synapse Account Details

                #region Get Request Recipient's Synapse Account Details

                var requestRecipientSynInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.SenderId.ToString());

                if (requestRecipientSynInfo.wereBankDetailsFound != true || requestRecipientSynInfo.BankDetails == null)
                {
                    Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser -> Request Recipient Does not have a Synapse Bank Linked - " +
                                "MemberID: [" + requestDto.SenderId + "] - OK because recipient can add one when paying the request - Continuing on...");
                }

                // Check Requestor's Synapse Bank Account status
                else if (requestRecipientSynInfo.BankDetails != null &&
                         requestRecipientSynInfo.BankDetails.Status != "Verified" &&
                         requestRecipient.IsVerifiedWithSynapse != true)
                {
                    // CLIFF (12/18/15): NEED TO THINK THROUGH HOW TO HANDLE THIS SITUATION.  MIGHT JUST ALLOW IT FOR NOW, AND NOTIFY
                    //                   MYSELF BY EMAIL TO CHECK THE PAYMENT MANUALLY.  LANDLORDS WILL USE THIS METHOD THE MOST, SO NEED TO
                    //                   BE SURE THE TENANTS DON'T HAVE ISSUES.
                    Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> Request Recipient's Synapse bank found " +
                                 "but is Not Verified, and Request Recipient's \"isVerifiedWithSynapse\" != true - Request Recipient MemberID: [" +
                                 requestDto.SenderId + "] - Continue On...");
                    //return "Request recipient does not have any verified bank account.";
                }

                #endregion Get Request Recipient's Synapse Account Details


                #endregion All Checks Before Executing Request


                #region Create New Transaction Record In DB

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    SenderId = requestRecipient.MemberId,
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
                    //InvitationSentTo = !String.IsNullOrEmpty(requestDto.MoneySenderEmailId)
                    //                   ? CommonHelper.GetEncryptedData(requestDto.MoneySenderEmailId)
                    //                   : null,
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

                int save = 0;
                bool isTesting = false;

                if (requestDto.isTesting == "true")
                {
                    Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser - JUST A TEST!! - isTesting FLAG WAS TRUE... continuing on");
                    isTesting = true;
                    save = 1;
                }
                else
                {
                    try
                    {
                        _dbContext.Transactions.Add(transaction);
                        _dbContext.SaveChanges();
                        _dbContext.Entry(transaction).Reload();
                        save++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Unable to save Transaction in DB - Exception: [" + ex + "]");
                        save = 0;
                    }
                }

                #endregion Create New Transaction Record In DB

                if (save > 0)
                {
                    requestId = transaction.TransactionId.ToString();

                    #region Send Notifications

                    #region Set Up Variables

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");

                    var RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                    var RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                    var RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requestRecipient.FirstName)).ToString());
                    var RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requestRecipient.LastName)).ToString());

                    var requesterPic = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                    if (!String.IsNullOrEmpty(requester.Photo) && requester.Photo.Length > 20)
                        requesterPic = requester.Photo.ToString();

                    var cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/CancelRequest?TransactionId=" + requestId +
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

                            if (startWithFor) memo = transaction.Memo.ToString();
                            else memo = "for " + transaction.Memo.ToString();
                        }
                        else memo = "for " + transaction.Memo.ToString();
                    }

                    bool isForRentScene = false;

                    if (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                    {
                        isForRentScene = true;
                        fromAddress = "payments@rentscene.com";
                        RequesterFirstName = "Rent Scene";
                        RequesterLastName = "";
                    }

                    var templateToUse_Recip = isForRentScene ? "requestReceivedToExistingNonRegUser_RentScene"
                                                             : "requestReceivedToExistingNonRegUser";

                    var templateToUse_Sender = isForRentScene ? "requestSent_RentScene"
                                                              : "requestSent";
                    #endregion Set Up Variables


                    // Send email to REQUESTER (person who sent this request)
                    #region Email To Requester

                    var tokens = new Dictionary<string, string>
                    {
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
                        Utility.SendEmail(templateToUse_Sender, fromAddress, toAddress, null,
                                          "Your payment request to " + RequestReceiverFirstName + " " + RequestReceiverLastName + " is pending",
                                          null, tokens, null, null, null);

                        Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser -> [" + templateToUse_Sender + "] email sent to [" + toAddress + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser -> [" + templateToUse_Sender + "] email NOT sent to [" + toAddress +
                                     "], Exception: [" + ex + "]");
                    }

                    #endregion Email To Requester


                    #region Email To Request Recipient

                    // Send email to REQUEST RECIPIENT (person who will pay/reject this request)
                    // Include 'UserType' and 'TransType' as encrypted along with request
                    // In this case UserType would = 'Nonregistered'  ->  6KX3VJv3YvoyK+cemdsvMA==
                    //              TransType would = 'Request'

                    // Check if both user's are actually "Active" and not NonRegistered...
                    string userType = "6KX3VJv3YvoyK+cemdsvMA=="; // "NonRegistered"
                    if (requester.Status == "Active" && requestRecipient.Status == "Active")
                        userType = "mx5bTcAYyiOf9I5Py9TiLw=="; // Update to "Existing"

                    string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                      "Nooch/rejectMoney?TransactionId=" + requestId +
                                                      "&UserType=" + userType +
                                                      "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                    string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                   "Nooch/payRequest?TransactionId=" + requestId +
                                                   "&UserType=" + userType);

                    var tokens2 = new Dictionary<string, string>
                    {
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

                    toAddress = isTesting ? "testing_request-recip@nooch.com" : CommonHelper.GetDecryptedData(requestRecipient.UserName);

                    try
                    {
                        // ADD CURRENT MONTH IN BEGINNING OF THE SUBJECT, "December Rent Request from Landlord"
                        string subject = isForRentScene
                                         ? "$" + wholeAmount + " Payment Request from Rent Scene"
                                         : "Payment Request from " + RequesterFirstName + " " + RequesterLastName + " - " + "$" + wholeAmount;

                        Utility.SendEmail(templateToUse_Recip, fromAddress, toAddress, null,
                                          subject, null, tokens2, null, null, null);

                        Logger.Info("TDA -> RequestMoneyToExistingButNonregisteredUser - [" + templateToUse_Recip + "] email sent to [" + toAddress + "] successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> RequestMoneyToExistingButNonregisteredUser - [" + templateToUse_Recip + "] email NOT sent to [" + toAddress +
                                     "], Exception: [" + ex + "]");
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
                    var error = "TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Unable to save Transaction in DB - [Requester MemberID:" + requestDto.MemberId + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);
                    return "Request failed.";
                }
            }
            catch (Exception ex)
            {
                var error = "TDA -> RequestMoneyToExistingButNonregisteredUser FAILED - Outer Exception: [" + ex + "]";
                Logger.Error(error);
                CommonHelper.notifyCliffAboutError(error);
                return "TDA Exception: [" + ex.Message + "]";
            }
        }


        /// <summary>
        /// To REQUEST money from NON-NOOCH user using SYANPSE through SMS 
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="requestId"></param>
        public string RequestMoneyToNonNoochUserThroughPhoneUsingSynapse(RequestDto requestDto, string PayorPhoneNumber)
        {
            string requestId = string.Empty;

            using (NOOCHEntities obj = new NOOCHEntities())
            {
                var checkuser = CommonHelper.GetMemberIdByContactNumber(PayorPhoneNumber);

                if (checkuser == null)
                {
                    #region Given Contact No. doesn't exists in Nooch

                    Logger.Info("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse Initiated - Requestor MemberId: [" +
                                requestDto.MemberId + "].");

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
                                    transactionAmount.ToString() + "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");
                                return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" + thisUserTransLimit.ToString("F2");
                            }
                        }
                    }

                    if (CommonHelper.isOverTransactionLimit(transactionAmount, "", requestDto.MemberId))
                    {
                        Logger.Error("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED - OVER GLOBAL TRANS LIMIT - Amount Requested: [" +
                                     transactionAmount.ToString() + "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + requestDto.MemberId + "]");

                        return "Hold on there cowboy! To keep Nooch safe, the maximum amount you can request is $" +
                               Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                    }

                    #region SenderSynapseAccountDetails

                    var requestorInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(requestDto.MemberId.ToString());

                    if (requestorInfo.wereBankDetailsFound != true)
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

                        // Send SMS to Request Receiver -- sending UserType, TransType as encrypted along with request
                        // In this case UserType would = 'New'
                        // TransType would = 'Request'
                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "Nooch/RejectMoney?TransactionId=" + requestId +
                                                          "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
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
                        Logger.Error("TDA -> RequestMoneyToNonNoochUserUsingSynapse FAILED -> Unable to save transaction in DB - " +
                                     "Requester MemberID: [" + requestDto.MemberId + "], Recipient: [" + requestDto.MoneySenderEmailId + "]");
                        return "Request failed.";
                    }
                    #endregion
                }
                else
                {
                    Logger.Error("TDA -> RequestMoneyToNonNoochUserThroughPhoneUsingSynapse FAILED -> Member Already Exists for phone number: [" +
                                 PayorPhoneNumber + "], Requester MemberID: [" + requestDto.MemberId + "]");

                    requestId = null;
                    return "User Already Exists";
                }
            }
        }


        /// <summary>
        /// For processing an existing user's response to a Nooch Request.
        /// * Only for handling 'accept' response - cancel/reject have different methods. *
        /// </summary>
        /// <param name="handleRequestDto"></param>
        /// <returns></returns>
        public string HandleRequestMoney(RequestDto handleRequestDto)
        {
            try
            {
                Logger.Info("TDA -> HandleRequestMoney Initiated - MemberID: [" + handleRequestDto.MemberId +
                            "], RecipientID: [" + handleRequestDto.ReceiverId + "], Amount: [" + handleRequestDto.Amount + "]");

                #region Get Request Details

                var noochConnection = new NOOCHEntities();

                var transGuid = Utility.ConvertToGuid(handleRequestDto.TransactionId);
                var gMemberId = Utility.ConvertToGuid(handleRequestDto.MemberId);

                string encryptedRequest = CommonHelper.GetEncryptedData("Request");

                var request = noochConnection.Transactions.FirstOrDefault(t => t.Member.MemberId == gMemberId &&
                                                                               t.TransactionId == transGuid &&
                                                                               t.TransactionStatus == "Pending" &&
                                                                               t.TransactionType == encryptedRequest);

                if (request == null)
                {
                    Logger.Error("TDA -> HandleRequestMoney - ERROR: Request not found - TransID: [" + handleRequestDto.TransactionId + "]");
                    return "No such request exists to act upon.";
                }

                #endregion Get Request Details

                #region Transaction Limit Checks

                // Check to make sure transfer amount is less than per-transaction limit
                decimal transactionAmount = Convert.ToDecimal(handleRequestDto.Amount);

                if (CommonHelper.isOverTransactionLimit(transactionAmount, handleRequestDto.SenderId, handleRequestDto.MemberId))
                {
                    Logger.Info("TDA -> HandleRequestMoney - Transaction amount is greater than Nooch's transfer limit. TransactionId: [" +
                                handleRequestDto.TransactionId + "]");
                    return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" +
                           Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                }

                // Weekly limit check
                if (CommonHelper.IsWeeklyTransferLimitExceeded(Utility.ConvertToGuid(handleRequestDto.MemberId), transactionAmount, handleRequestDto.ReceiverId))
                {
                    Logger.Info("TDA -> HandleRequestMoney - Weekly transfer limit exceeded. MemberId: [" + handleRequestDto.MemberId + "]");
                    return "Weekly transfer limit exceeded.";
                }

                #endregion Transaction Limit Checks

                // Validate Sender's PIN
                string validPinNumberResult = CommonHelper.ValidatePinNumber(handleRequestDto.MemberId, handleRequestDto.PinNumber);
                if (validPinNumberResult != "Success")
                {
                    return validPinNumberResult;
                }

                // Code to move money between two users...
                // 1. Get Sender Synapse account details
                // 2. Get Receiver Synapse account details
                // 3. Call to Synapse /v3/trans/add API...

                #region Check Each User's Synapse Status

                // Get SENDER'S Synapse User & Bank Account details

                #region Get Request Payer's Synapse Details
                // NOTE: Sender of the Request will be the 'Receiver' IF the Request is PAID

                var sender = noochConnection.Members.FirstOrDefault(m => m.MemberId == request.SenderId);
                var senderSynDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(request.SenderId.ToString());

                if (senderSynDetails.wereUserDetailsFound == false || senderSynDetails.UserDetails == null)
                {
                    Logger.Error("TDA -> HandleRequestMoney - ABORTED: Sender's Synapse details not found - SenderID: [" +
                                 request.SenderId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    return "Sender user details not found.";
                }
                if (senderSynDetails.wereBankDetailsFound == false || senderSynDetails.BankDetails == null)
                {
                    Logger.Error("TDA -> HandleRequestMoney - ABORTED: No Synapse Bank found for Sender - MemberID: [" +
                                 request.SenderId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    return "Sender have not linked to any bank account yet.";
                }
                if (senderSynDetails.UserDetails.permission != "SEND-AND-RECEIVE") // Sender's Permissions
                {
                    Logger.Error("TDA -> HandleRequestMoney FAILED - Sender's Synapse Permission is INSUFFICIENT: [" + senderSynDetails.UserDetails.permission +
                                 "] MemberID: [" + request.SenderId.ToString() + "]");
                    //return "Sender does not have any bank added";
                    return "Sender has insufficient permissions: [" + senderSynDetails.UserDetails.permission + "]";
                }
                if (senderSynDetails.BankDetails.allowed != "CREDIT-AND-DEBIT")
                {
                    Logger.Error("TDA -> HandleRequestMoney - Transfer ABORTED: " +
                                "Sender's Synapse bank not verified - Bank-Allowed: [" + senderSynDetails.BankDetails.allowed + "], " +
                                "MemberID: [" + request.SenderId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    //return "Sender does not have any verified bank account.";
                    return "Sender's bank has insufficient permissions: [" + senderSynDetails.BankDetails.allowed + "]";
                }

                string senderBankOid = senderSynDetails.BankDetails.bank_oid;
                string sender_oauth = senderSynDetails.UserDetails.access_token;

                Logger.Info("TDA -> HandleRequestMoney - Sender's Synapse Permission: [" + senderSynDetails.UserDetails.permission + "], " +
                            "Bank-Allowed: [" + senderSynDetails.BankDetails.allowed + "] - Continuing On...");

                #endregion Get Sender Synapse Details


                #region Get Recipient Synapse Details

                var requester = noochConnection.Members.FirstOrDefault(m => m.MemberId == request.RecipientId);
                var recipientSynDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(request.RecipientId.ToString());

                if (recipientSynDetails.wereUserDetailsFound == false || recipientSynDetails.UserDetails == null)
                {
                    Logger.Info("TDA -> HandleRequestMoney - ABORTED: Recipient's Synapse User details not found - MemberID (Recip): [" +
                                 request.RecipientId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    return "Recepient user details not found.";
                }
                if (recipientSynDetails.wereBankDetailsFound == false || recipientSynDetails.BankDetails == null)
                {
                    Logger.Info("TDA -> HandleRequestMoney - ABORTED: No active Synapse Bank found for Recipient - MemberID (Recip): [" +
                                request.RecipientId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    return "Recepient have not linked to any bank account yet.";
                }
                if (!(recipientSynDetails.UserDetails.permission == "SEND-AND-RECEIVE" ||
                      recipientSynDetails.UserDetails.permission == "RECEIVE")) // Recipient just needs "RECEIVE" or higher Permissions
                {
                    Logger.Error("TDA -> HandleRequestMoney FAILED - Recipient's Synapse Permission is INSUFFICIENT: [" +
                                 recipientSynDetails.UserDetails.permission + "] MemberID (Recip): [" + request.RecipientId.ToString() + "]");
                    //return "Recipient does not have any bank added";
                    return "Recipient has insufficient permissions: [" + recipientSynDetails.UserDetails.permission + "]";
                }
                if (!(recipientSynDetails.BankDetails.allowed == "CREDIT-AND-DEBIT" ||
                      recipientSynDetails.BankDetails.allowed == "CREDIT")) // Recipient just needs "CREDIT" for 'Allowed' Value
                {
                    Logger.Error("TDA -> HandleRequestMoney - Transfer ABORTED: " +
                                "Recipient's Synapse bank not verified - Bank-Allowed: [" + recipientSynDetails.BankDetails.allowed + "], " +
                                "MemberID (Recip): [" + request.RecipientId.ToString() + "], TransactionID: [" + request.TransactionId + "]");
                    //return "Recipient does not have any verified bank account.";
                    return "Recipient's bank has insufficient permissions: [" + recipientSynDetails.BankDetails.allowed + "]";
                }


                string recipBankOid = recipientSynDetails.BankDetails.bank_oid; // This is not actually needed for the transaction to happen, we can remove this check later

                Logger.Info("TDA -> HandleRequestMoney - Recipient's Synapse Permission: [" + recipientSynDetails.UserDetails.permission + "], " +
                            "Bank-Allowed: [" + recipientSynDetails.BankDetails.allowed + "] - Continuing On...");

                #endregion Get Recipient Synapse Details

                // Cliff (6/14/16): Adding this to check: a.) is this a payment to Rent Scene?
                //                  and b.) what kind of user the recipient is: Client or Vendor, which determines which Node ID to use for Rent Scene

                // 575ad909950629625ca88262 - Corp Checking - USE FOR ALL NON-PASSTHROUGH PAYMENTS, i.e.: Payments TO Vendors, and Application fees from Clients to RS
                // 574f45d79506295ff7a81db8 - Passthrough (Linked to Rent Scene's parent account - USE FOR RENT PAYMENTS - ANYTHING OVER $1,000)
                // 5759005795062906e1359a8e - Passthrough (Linked to Marvis Burn's Nooch account - NEVER USE)
                if ((sender.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                    senderBankOid == "5759005795062906e1359a8e" ||
                    senderBankOid == "574f45d79506295ff7a81db8") &&
                    requester.cipTag.ToLower() == "vendor")
                {
                    // Sender is Rent Scene and recipient is a 'Vendor'
                    senderBankOid = "575ad909950629625ca88262";
                    Logger.Info("TDA -> HandleRequestMoney - RENT SCENE VENDOR Payment Detected - " +
                                "Substituting Sender_Bank_NodeID to use RS's Corporate Checking account");
                }
                else if (transactionAmount < 200 &&
                         (requester.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                          recipBankOid == "574f45d79506295ff7a81db8" ||
                          recipBankOid == "5759005795062906e1359a8e") &&
                         (sender.cipTag.ToLower() == "renter" || sender.cipTag.ToLower() == "client"))
                {
                    // Recipient is Rent Scene AND Sender is a Client AND Amount is < $200 (so it's probably an application fee)
                    // So use RS's Corporate Checking account.
                    recipBankOid = "575ad909950629625ca88262";
                    Logger.Info("TDA -> HandleRequestMoney - RENT SCENE Payment Detected Under $200 - " +
                                "Substituting Receiver_Bank_NodeID to use RS's Corporate Checking account");
                }

                #endregion Check Each User's Synapse Status


                string moneySenderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(request.Member.FirstName));
                string moneySenderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(request.Member.LastName));
                string requesterPic = "https://www.noochme.com/noochservice/UploadedPhotos/Photos/" + requester.MemberId.ToString() + ".png";
                string requestMakerFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(request.Member1.FirstName));
                string requestMakerLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(request.Member1.LastName));


                #region Setup Synapse V3 Order API Details

                // Use AddTransSynapseV3Reusable() to handle this job from now on.

                string sender_fingerPrint = sender.UDID1;
                string amount = request.Amount.ToString();
                string suppID_or_transID = request.TransactionId.ToString();
                string senderUserName = CommonHelper.GetDecryptedData(sender.UserName).ToLower();
                string receiverUserName = CommonHelper.GetDecryptedData(requester.UserName).ToLower();
                string iPForTransaction = CommonHelper.GetRecentOrDefaultIPOfMember(sender.MemberId);
                string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName));
                string recipientLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(requester.LastName));
                string memoForSyn = !string.IsNullOrEmpty(request.Memo) ? request.Memo : "";

                #endregion SYNAPSE V3

                SynapseV3AddTrans_ReusableClass transactionResultFromSynapseAPI = AddTransSynapseV3Reusable(sender_oauth, sender_fingerPrint,
                                                senderBankOid, amount, recipBankOid, suppID_or_transID,
                                                senderUserName, receiverUserName, iPForTransaction, senderLastName, recipientLastName, memoForSyn);

                if (transactionResultFromSynapseAPI.success == true)
                {
                    // Transaction successfully created w/ Synapse... now save data in DB and send email/sms/push notifications

                    #region Update This Request In Transactions Table

                    try
                    {
                        request.GeoLocation = new GeoLocation
                        {
                            LocationId = Guid.NewGuid(),
                            Latitude = handleRequestDto.Latitude,
                            Longitude = handleRequestDto.Longitude,
                            Altitude = handleRequestDto.Altitude,
                            AddressLine1 = handleRequestDto.AddressLine1,
                            AddressLine2 = handleRequestDto.AddressLine2,
                            City = handleRequestDto.City,
                            State = handleRequestDto.State,
                            Country = handleRequestDto.Country,
                            ZipCode = handleRequestDto.ZipCode,
                            DateCreated = DateTime.Now
                        };
                        request.DeviceId = handleRequestDto.DeviceId;
                        request.TransactionStatus = "Success";
                        request.DateAccepted = DateTime.Now;
                        request.TransactionType = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);

                        int i = noochConnection.SaveChanges();

                        if (i <= 0)
                        {
                            Logger.Error("TDA -> HandleRequestMoney - Success from Synapse but FAILED to update this request in DB - " +
                                         "[Request TransId is: " + request.TransactionId + "]");

                            return "Internal error. Funds were transferred, but transaction not updated in DB.";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> HandleRequestMoney -> Success from Synapse but FAILED to update this request in DB - [Request TransID: " +
                                     request.TransactionId + "], [Exception: " + ex.Message + "]");
                    }

                    #endregion Update This Request In Transactions DB


                    #region Success Notifications to Sender and Receiver

                    try
                    {
                        #region Setup Notification Variables

                        string s2 = request.Amount.ToString("n2");

                        #region Add Memo

                        string memo = "";
                        if (!string.IsNullOrEmpty(request.Memo))
                        {
                            if (request.Memo.Length > 3)
                            {
                                string firstThreeChars = request.Memo.Substring(0, 3).ToLower();
                                bool startWithFor = firstThreeChars.Equals("for");

                                if (startWithFor)
                                {
                                    memo = request.Memo.ToString();
                                }
                                else
                                {
                                    memo = "For " + request.Memo.ToString();
                                }
                            }
                            else
                            {
                                memo = "For " + request.Memo.ToString();
                            }
                        }

                        #endregion Add Memo

                        #endregion Setup Notification Variables

                        #region Push Notification to Request Maker

                        if (!String.IsNullOrEmpty(requester.DeviceToken) && requester.DeviceToken.Length > 6)
                        {
                            // Send Push Notification To Sender of Request
                            try
                            {
                                string pushMsgTxt = moneySenderFirstName + " " + moneySenderLastName + " just paid your $" + s2 + " Nooch request";
                                if (memo.Length > 1 &&
                                    (pushMsgTxt.Length + memo.Length < 230)) // Make sure the total string isn't too long
                                {
                                    pushMsgTxt = pushMsgTxt + " f" + memo.Substring(1) + "!"; // Append the memo (but replace 1st letter with lowercase "f"
                                }
                                else
                                {
                                    pushMsgTxt = pushMsgTxt + "!";
                                }


                                Utility.SendNotificationMessage(pushMsgTxt, "Nooch", requester.DeviceToken,
                                                                                   requester.DeviceType);

                                Logger.Info("TDA -> HandleRequestMoney - Request Paid Push notification sent to SENDER of request: [" +
                                                       requestMakerFirstName + " " + requestMakerLastName + "]");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> HandleRequestMoney - Request Paid Push notification FAILED - Push NOT sent to sender of request: [" +
                                                       requestMakerFirstName + " " + requestMakerLastName + "], [Exception: " + ex + "]");
                            }
                        }

                        #endregion Push Notification to Request Maker

                        #region Send Email Notifications

                        try
                        {
                            #region Notify Payment Sender (Recipient Of Request)

                            var tokens = new Dictionary<string, string>
		                            {
		                                {Constants.PLACEHOLDER_FIRST_NAME, moneySenderFirstName},
                                        {"$UserPicture$", requesterPic},
		                                {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, requestMakerFirstName},
		                                {Constants.PLACEHOLDER_FRIEND_LAST_NAME, requestMakerLastName},
		                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s2},
		                                {
		                                    Constants.PLACEHOLDER_TRANSACTION_DATE,
		                                    Convert.ToDateTime(request.TransactionDate).ToString("MMM dd yyyy")
		                                },
		                                {Constants.MEMO, memo}
		                            };

                            var fromAddress = Utility.GetValueFromConfig("transfersMail");
                            var toAddress = CommonHelper.GetDecryptedData(request.Member.UserName);

                            try
                            {
                                Utility.SendEmail("requestPaidToRecipient", fromAddress,
                                    toAddress, null, "You paid a Nooch request from " + requestMakerFirstName + " " + requestMakerLastName,
                                    null, tokens, null, null, null);

                                Logger.Info("TDA -> HandleRequestMoney - requestPaidToRecipient Email sent to [" + toAddress + "]");
                            }
                            catch (Exception)
                            {
                                Logger.Error("TDA -> HandleRequestMoney - requestPaidToRecipient Email NOT sent to [" + toAddress + "]");
                            }

                            #endregion Notify Payment Sender (Recipient Of Request)

                            #region Notify Payment Recipient (Sender Of Request)

                            var tokens2 = new Dictionary<string, string>
		                            {
		                                {Constants.PLACEHOLDER_FIRST_NAME, requestMakerFirstName},
		                                {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, moneySenderFirstName},
		                                {Constants.PLACEHOLDER_FRIEND_LAST_NAME, moneySenderLastName},
		                                {Constants.PLACEHOLDER_TRANSFER_AMOUNT, s2},
		                                {
		                                    Constants.PLACEHOLDER_TRANSACTION_DATE,
		                                    Convert.ToDateTime(request.TransactionDate).ToString("MMM dd yyyy")
		                                },
		                                {Constants.MEMO, memo}
		                            };

                            var toAddress2 = CommonHelper.GetDecryptedData(request.Member1.UserName);

                            try
                            {
                                Utility.SendEmail("requestPaidToSender", fromAddress,
                                    toAddress2, null, moneySenderFirstName + " paid your request on Nooch", null, tokens2,
                                    null, null, null);

                                Logger.Info("TDA -> HandleRequestMoney - requestPaidToSender Email  sent to [" + toAddress2 + "]");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA - > HandleRequestMoney - requestPaidToSender Email NOT sent to [" + toAddress2 + "], " +
                                                       "[Exception: " + ex + "]");
                            }

                            #endregion Notify Payment Recipient (Sender Of Request)
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> Handle Request Money - Success Emails NOT sent to [" +
                                                   CommonHelper.GetDecryptedData(request.Member.UserName) + "] OR [" +
                                                   CommonHelper.GetDecryptedData(request.Member1.UserName) + "], " +
                                                   "[Exception: " + ex + "]");
                        }

                        #endregion Send Email Notifications
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> Handle Request Money - Attempted to Notify Users after success, but caught Exception #1959: [" + ex + "]");
                    }

                    return "Request processed successfully.";

                    #endregion Success Notifications to Sender and Receiver
                }
                else
                {
                    // Transaction failed return appropriate error
                    Logger.Info("TDA -> HandleRequestMoney -> Synapse Order API FAILED. Transaction aborted. Request TransId is: [" + request.TransactionId + "]");

                    request.TransactionStatus = "Pending";
                    noochConnection.SaveChanges();

                    return "Funds transfer failed, please retry after checking bank account details.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("HandleRequestMoney -> Synapse Order API FAILED (1812). Outermost Exception: [" + ex + "]");
                return "Sorry There Was A Problem";
            }
        }


        // CLIFF (10/11/15): Pretty sure this method is not used at all. The app sends the user to How Much scrn when user wants to "Pay Back" another
        //                   user that has previously sent them money from the History screens.  So we can *probably* remove this...
        // Malkit : 10 May 2016 : Added this method because Cliff pointed this method as missing.
        public string PayBackTransaction(string transactionId, string userResponse, GeoLocation location)
        {
            Logger.Info("TDA -> PayBackTransaction [TransactionId: " + transactionId + "], userResponse: [" + userResponse + "]");

            if (userResponse == "Paid")
            {
                try
                {
                    var transId = Utility.ConvertToGuid(transactionId);

                    using (var noochConnection = new NOOCHEntities())
                    {
                        var transactionDetail =
                            noochConnection.Transactions.FirstOrDefault(m => m.TransactionId == transId);

                        Transaction newTrans = transactionDetail;

                        //Check if the member is having the required balance

                        Member sender = CommonHelper.GetMemberDetails(transactionDetail.Member1.MemberId.ToString());
                        Member receiver = CommonHelper.GetMemberDetails(transactionDetail.Member.MemberId.ToString());

                        if (sender != null)
                        {
                            decimal senderBalance = Convert.ToDecimal(CommonHelper.GetDecryptedData(sender.Deposit));
                            decimal receiverBalance = Convert.ToDecimal(CommonHelper.GetDecryptedData(receiver.Deposit));
                            decimal transAmount = decimal.Parse(transactionDetail.Amount.ToString());

                            if (senderBalance >= transactionDetail.Amount)
                            {
                                newTrans.Member.MemberId = transactionDetail.Member1.MemberId;
                                newTrans.Member1.MemberId = transactionDetail.Member.MemberId;
                                newTrans.DisputeTrackingId = null;
                                newTrans.TransactionStatus = "Success";
                                newTrans.TransactionType = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);
                                newTrans.TransactionDate = DateTime.Now;
                                newTrans.Memo = null;

                                if (location != null) newTrans.GeoLocation = location;

                                // Legacy code to add a fee - just setting to 0
                                newTrans.IsPrepaidTransaction = true;
                                newTrans.TransactionFee = 0;

                                sender.TotalNoochTransfersCount = sender.TotalNoochTransfersCount + 1;
                                receiver.TotalNoochTransfersCount = receiver.TotalNoochTransfersCount + 1;

                                sender.DateModified = DateTime.Now;
                                receiver.DateModified = DateTime.Now;

                                noochConnection.Transactions.Add(newTrans);

                                noochConnection.SaveChanges();
                            }
                            return "success";
                        }
                        else
                        {
                            Logger.Error("TDA -> PayBackTransaction FAILED - Could not find SENDER in DB [TransactionID: " + transactionId + "]");
                            return "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("TDA -> PayBackTransaction FAILED - [TransactionID: " + transactionId + "], [Exception: " + ex + "]");
                    return "Server failure - caught outer exception :-(";
                }
            }
            else return "";
        }


        private Transaction SetTransactionDetails(TransactionEntity transactionEntity, string transactionType, decimal? transactionFee)
        {
            string transactionTrackingId = CommonHelper.GetRandomTransactionTrackingId();

            var entity = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                SenderId = Utility.ConvertToGuid(transactionEntity.MemberId),
                RecipientId = Utility.ConvertToGuid(transactionEntity.RecipientId),
                Amount = transactionEntity.Amount,
                TransactionDate = DateTime.Now,
                DisputeStatus = null,
                TransactionStatus = "Pending",
                TransactionType = CommonHelper.GetEncryptedData(transactionType),
                DeviceId = transactionEntity.DeviceId,
                TransactionTrackingId = transactionTrackingId,
                IsPrepaidTransaction = false,
                TransactionFee = 0,
            };

            return entity;
        }


        /// <summary>
        /// Transfer money between 2 existing Nooch users using Synapse's Order API
        /// </summary>
        /// <param name="transInput"></param>
        /// <param name="trnsactionId"></param>
        public string TransferMoneyUsingSynapse(TransactionEntity transInput)
        {
            Logger.Info("TDA -> TransferMoneyUsingSynapse Initiated - " +
                        "SenderID: [" + transInput.MemberId + "], " +
                        "Amount: [" + transInput.Amount + "], " +
                        "RecipientID: [" + transInput.RecipientId + "], " +
                        "Memo: [" + transInput.Memo + "], " +
                        "isRentAutoPayment: [" + transInput.isRentAutoPayment + "], " +
                        "doNotSendEmails: [" + transInput.doNotSendEmails + "]");

            DateTime TransDateTime = DateTime.Now;

            string trnsactionId = string.Empty;

            #region Initial checks

            if (transInput.MemberId == null)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTING - Missing MemberID- [MemberId: " + transInput.MemberId + "]");
                return "Missing MemberID (Sender)";
            }
            if (transInput.RecipientId == null)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTING - Missing RecipientID - [MemberId: " + transInput.MemberId + "]");
                return "Missing MemberID (Recipient)";
            }
            if (transInput.Amount == null)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTING - Missing Amount - [MemberId: " + transInput.MemberId + "]");
                return "Missing Amount";
            }

            // Check to make sure sender and recipient are not the same user
            if (transInput.MemberId == transInput.RecipientId)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTING - Sender and recipient MemberIDs were the same - MemberId: [" +
                             transInput.MemberId + "]");
                return "Not allowed for send money to the same user.";
            }

            // Check PIN
            string validPinNumberResult = CommonHelper.ValidatePinNumber(transInput.MemberId, transInput.PinNumber);

            if (validPinNumberResult != "Success")
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTING - Sender's PIN was incorrect - MemberId: [" +
                                       transInput.MemberId + "]");
                return validPinNumberResult;
            }

            // Check to make sure transfer amount is less than per-transaction limit
            decimal transactionAmount = Convert.ToDecimal(transInput.Amount);

            // Individual user transfer limit check
            decimal thisUserTransLimit = 0;
            string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(transInput.MemberId);

            if (!String.IsNullOrEmpty(indiTransLimit))
            {
                if (!(indiTransLimit == "0" || indiTransLimit == "0.0" || indiTransLimit == "0.00"))
                {
                    thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                    if (transactionAmount > thisUserTransLimit)
                    {
                        Logger.Error("TDA -> TransferMoneyUsingSynapse FAILED - OVER PERSONAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                     "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + transInput.MemberId + "]");
                        return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + thisUserTransLimit.ToString("F2");
                    }
                }
            }

            if (CommonHelper.isOverTransactionLimit(transactionAmount, transInput.MemberId, transInput.RecipientId))
            {
                var transLimit = Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                Logger.Error("TransferMoneyUsingSynapse -> Transaction amount [$" + transactionAmount + "] is over the transfer limit [$" + transLimit +
                             "], TransactionId: [" + transInput.TransactionId + "]");
                return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + transLimit;
            }

            // Check weekly transfer limit
            if (CommonHelper.IsWeeklyTransferLimitExceeded(Utility.ConvertToGuid(transInput.MemberId), transactionAmount, transInput.RecipientId))
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse -> Weekly transfer limit exceeded. MemberId: [" + transInput.MemberId + "]");
                return "Weekly transfer limit exceeded.";
            }


            var SenderGuid = Utility.ConvertToGuid(transInput.MemberId);
            var RecepientGuid = Utility.ConvertToGuid(transInput.RecipientId);

            #region Get Sender Synapse Details

            string senderBankOid = "";
            string sender_oauth = "";

            var senderSynDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(transInput.MemberId);
            var senderNoochDetails = CommonHelper.GetMemberDetails(transInput.MemberId);

            if (senderSynDetails.wereUserDetailsFound == false || senderSynDetails.UserDetails == null)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTED: Sender's Synapse details not found - MemberID: [" +
                             transInput.MemberId + "], TransactionId: [" + transInput.TransactionId + "]");
                return "Sender user details not found.";
            }
            if (senderSynDetails.wereBankDetailsFound == false || senderSynDetails.BankDetails == null)
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - ABORTED: No Synapse Bank found for Sender - MemberID: [" +
                             transInput.MemberId + "], TransactionId: [" + transInput.TransactionId + "]");
                return "Sender have not linked to any bank account yet.";
            }
            if (senderSynDetails.UserDetails.permission != "SEND-AND-RECEIVE") // Sender's Permissions
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse FAILED - Sender's Synapse Permission is INSUFFICIENT: [" + senderSynDetails.UserDetails.permission +
                             "] MemberID: [" + transInput.MemberId + "]");
                return "Sender has insufficient permissions: [" + senderSynDetails.UserDetails.permission + "]";
            }
            if (senderSynDetails.BankDetails.allowed != "CREDIT-AND-DEBIT")
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - Transfer ABORTED: " +
                            "Sender's Synapse bank not verified - Bank-Allowed: [" + senderSynDetails.BankDetails.allowed + "], " +
                            "MemberID: [" + transInput.MemberId + "], TransactionId: [" + transInput.TransactionId + "]");
                return "Sender's bank has insufficient permissions: [" + senderSynDetails.BankDetails.allowed + "]";
            }

            senderBankOid = senderSynDetails.BankDetails.bank_oid;
            sender_oauth = senderSynDetails.UserDetails.access_token;

            Logger.Info("TDA -> TransferMoneyUsingSynapse - Sender's Synapse Permission: [" + senderSynDetails.UserDetails.permission + "], " +
                        "Bank-Allowed: [" + senderSynDetails.BankDetails.allowed + "] - Continuing On...");

            #endregion Get Sender Synapse Details

            #region Get Recipient Synapse Details

            string recipBankOid = ""; // This is not actually needed for the transaction to happen, we can remove this check later

            var recipientNoochDetails = CommonHelper.GetMemberDetails(transInput.RecipientId);
            var recipientSynapseDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(transInput.RecipientId);

            if (recipientSynapseDetails.wereUserDetailsFound == false || recipientSynapseDetails.UserDetails == null)
            {
                Logger.Info("TDA -> TransferMoneyUsingSynapse - ABORTED: Recipient's Synapse User details not found - MemberID (Recip): [" +
                             transInput.RecipientId + "], TransactionId: [" + transInput.TransactionId + "]");
                return "Recepient user details not found.";
            }
            if (recipientSynapseDetails.wereBankDetailsFound == false || recipientSynapseDetails.BankDetails == null)
            {
                Logger.Info("TDA -> TransferMoneyUsingSynapse - ABORTED: No active Synapse Bank found for Recipient - MemberID (Recip): [" +
                            transInput.MemberId + "], [TransactionId: " + transInput.TransactionId + "]");
                return "Recepient have not linked to any bank account yet.";
            }
            if (!(recipientSynapseDetails.UserDetails.permission == "SEND-AND-RECEIVE" ||
                  recipientSynapseDetails.UserDetails.permission == "RECEIVE")) // Recipient just needs "RECEIVE" or higher Permissions
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse FAILED - Recipient's Synapse Permission is INSUFFICIENT: [" +
                             recipientSynapseDetails.UserDetails.permission + "] MemberID (Recip): [" + transInput.RecipientId + "]");
                //return "Recipient does not have any bank added";
                return "Recipient has insufficient permissions: [" + recipientSynapseDetails.UserDetails.permission + "]";
            }
            if (!(recipientSynapseDetails.BankDetails.allowed == "CREDIT-AND-DEBIT" ||
                  recipientSynapseDetails.BankDetails.allowed == "CREDIT")) // Recipient just needs "CREDIT" for 'Allowed' Value
            {
                Logger.Error("TDA -> TransferMoneyUsingSynapse - Transfer ABORTED: " +
                            "Recipient's Synapse bank not verified - Bank-Allowed: [" + recipientSynapseDetails.BankDetails.allowed + "], " +
                            "MemberID (Recip): [" + transInput.RecipientId + "], TransactionId: [" + transInput.TransactionId + "]");
                //return "Recipient does not have any verified bank account.";
                return "Recipient's bank has insufficient permissions: [" + recipientSynapseDetails.BankDetails.allowed + "]";
            }


            recipBankOid = recipientSynapseDetails.BankDetails.bank_oid;

            Logger.Info("TDA -> TransferMoneyUsingSynapse - Recipient's Synapse Permission: [" + recipientSynapseDetails.UserDetails.permission + "], " +
                        "Bank-Allowed: [" + recipientSynapseDetails.BankDetails.allowed + "] - Continuing On...");

            #endregion Get Recipient Synapse Details

            #region Rent Scene Custom Checks

            // Cliff (6/14/16): Adding this to check: a.) is this a payment to Rent Scene?
            //                  and b.) what kind of user the recipient is: Client or Vendor, which determines which Node ID to use for Rent Scene

            // 575ad909950629625ca88262 - Corp Checking - USE FOR ALL NON-PASSTHROUGH PAYMENTS, i.e.: Payments TO Vendors, and Application fees from Clients to RS
            // 574f45d79506295ff7a81db8 - Passthrough (Linked to Rent Scene's parent account - USE FOR RENT PAYMENTS - ANYTHING OVER $1,000)
            // 5759005795062906e1359a8e - Passthrough (Linked to Marvis Burn's Nooch account - NEVER USE)
            if ((senderNoochDetails.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                senderBankOid == "5759005795062906e1359a8e" ||
                senderBankOid == "574f45d79506295ff7a81db8") &&
                recipientNoochDetails.cipTag.ToLower() == "vendor")
            {
                // Sender is Rent Scene and recipient is a 'Vendor'
                senderBankOid = "575ad909950629625ca88262";
                Logger.Info("TDA -> TransferMoneyUsingSynapse - RENT SCENE VENDOR Payment Detected - " +
                            "Substituting Sender_Bank_NodeID to use RS's Corporate Checking account");
            }
            else if (transactionAmount < 200 &&
                     (recipientNoochDetails.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49" ||
                      recipBankOid == "574f45d79506295ff7a81db8" ||
                      recipBankOid == "5759005795062906e1359a8e") &&
                     (senderNoochDetails.cipTag.ToLower() == "renter" || senderNoochDetails.cipTag.ToLower() == "client"))
            {
                // Recipient is Rent Scene AND Sender is a Client AND Amount is < $200 (so it's probably an application fee)
                // So use RS's Corporate Checking account.
                recipBankOid = "575ad909950629625ca88262";
                Logger.Info("TDA -> TransferMoneyUsingSynapse - RENT SCENE Payment Detected Under $200 - " +
                            "Substituting Receiver_Bank_NodeID to use RS's Corporate Checking account");
            }

            #endregion Rent Scene Custom Checks

            #endregion Initial checks


            //Logger.Info("TDA -> TransferMoneyUsingSynapse CHECKPOINT #1 - ALL INITIAL INPUT CHECKS PASSED");

            using (var noochConnection = new NOOCHEntities())
            {
                try
                {
                    //Logger.Info("TDA -> TransferMoneyUsingSynapse CHECKPOINT #2 - BOTH USERS' SYNAPSE INFO RETRIEVED SUCCESSFULLY");

                    // Prepare variables for Email/SMS notifications (This block is this early because these vars will be used for both success & failure scenarios below)
                    #region Define Variables From Transaction for Notifications

                    string senderUserName = CommonHelper.GetDecryptedData(senderNoochDetails.UserName);
                    string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(senderNoochDetails.FirstName));
                    string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(senderNoochDetails.LastName));

                    string receiverUserName = CommonHelper.GetDecryptedData(recipientNoochDetails.UserName);
                    string recipientFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipientNoochDetails.FirstName));
                    string recipientLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(recipientNoochDetails.LastName));

                    string wholeAmount = transInput.Amount.ToString("n2");
                    string[] s3 = wholeAmount.Split('.');
                    string ce = "";
                    string dl = "";
                    if (s3.Length <= 1)
                    {
                        dl = s3[0].ToString();
                        ce = "00";
                    }
                    else
                    {
                        ce = s3[1].ToString();
                        dl = s3[0].ToString();
                    }

                    string memo = "";
                    if (!String.IsNullOrEmpty(transInput.Memo))
                    {
                        if (transInput.Memo.Length > 3)
                        {
                            string firstThreeChars = transInput.Memo.Substring(0, 3).ToLower();
                            bool startsWithFor = firstThreeChars.Equals("for");

                            if (startsWithFor) memo = transInput.Memo.ToString();
                            else memo = "For: " + transInput.Memo.ToString();
                        }
                        else memo = "For: " + transInput.Memo.ToString();
                    }

                    string senderPic = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                    string recipientPic;

                    bool isForRentScene = false;
                    bool isRentScenePayrollPayment = false;
                    bool isAutoPay = false;

                    if (senderNoochDetails.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                    {
                        isForRentScene = true;
                        senderFirstName = "Rent Scene";
                        senderLastName = "";

                        // Now check if the recipient is one of Rent Scene's employees
                        if (recipientNoochDetails.Nooch_ID.ToString() == "q40l9MnO") // Tori Moore
                        {
                            Logger.Info("TDA -> TransferMoneyUsingSynapse - RENT SCENE PAYROLL PAYMENT DETECTED - continuing on, but won't send email to Sender (Rent Scene)");
                            isRentScenePayrollPayment = true;
                        }
                    }
                    else if (recipientNoochDetails.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                    {
                        isForRentScene = true;
                        recipientFirstName = "Rent Scene";
                        recipientLastName = "";
                    }

                    if (transInput.isRentAutoPayment)
                    {
                        Logger.Info("TDA -> TransferMoneyUsingSynapse - RENT AUTO-PAYMENT DETECTED - continuing on!");
                        isAutoPay = true;
                    }

                    var fromAddress = isForRentScene ? "payments@rentscene.com"
                                                     : Utility.GetValueFromConfig("transfersMail");

                    #endregion Define Variables From Transaction for Notifications


                    // Make call to SYNAPSE Order API service

                    #region Synapse V3 Add Trans

                    // CC (6/20/16): Must get the TransactionID from 'SetTransactionDetails' here so we send the right webhook to Synapse.
                    Transaction transactionDetail = new Transaction();
                    transactionDetail = SetTransactionDetails(transInput, Constants.TRANSACTION_TYPE_TRANSFER, 0);

                    string senderFingerprint = senderNoochDetails.UDID1;
                    string amount = transInput.Amount.ToString();
                    string iPForTransaction = CommonHelper.GetRecentOrDefaultIPOfMember(SenderGuid);
                    string memoForSyn = !string.IsNullOrEmpty(transInput.Memo) ? transInput.Memo : "";
                    string suppID_or_transID = transactionDetail.TransactionId.ToString();

                    SynapseV3AddTrans_ReusableClass transactionResultFromSynapseAPI = new SynapseV3AddTrans_ReusableClass();

                    #region Habitat Custom Checks

                    if (transInput.MemberId.ToString().ToLower() == "45357cf0-e651-40e7-b825-e1ff48bf44d2" &&
                        recipientNoochDetails.cipTag == "vendor")
                    {
                        Logger.Info("MDA -> TransferMoneyUsingSynapse - HABITAT Payment!!");

                        // Need to create 2 transactions with Synapse: 1 from Habitat to Rent Scene, & 1 from Rent Scene to the Vendor

                        senderFirstName = "Habitat";
                        senderLastName = "";

                        // Insert RENT SCENE values
                        var recipBankOid2 = "574f45d79506295ff7a81db8";
                        var recipEmail2 = "payments@rentscene.com";
                        var moneyRecipientLastName2 = "Rent Scene";

                        var log = "TDA -> TransferMoneyUsingSynapse - About to call AddTransSynapseV3Reusable() in TDA - " +
                                    "TransID: [" + suppID_or_transID + "], Amount: [" + amount + "], Sender Name: [" + senderFirstName + " " + senderLastName +
                                    "], Sender BankOID: [" + senderBankOid + "], Recip Name: [" + recipientFirstName + " " + recipientLastName +
                                    "], Recip BankOID: [" + recipBankOid2 + "]";
                        Logger.Info(log);

                        // 1st payment - from Habitat -> Rent Scene
                        transactionResultFromSynapseAPI = AddTransSynapseV3Reusable(sender_oauth, senderFingerprint,
                            senderBankOid, amount, recipBankOid2, suppID_or_transID, senderUserName, recipEmail2,
                            CommonHelper.GetRecentOrDefaultIPOfMember(senderNoochDetails.MemberId),
                            senderLastName, moneyRecipientLastName2, memoForSyn);


                        if (transactionResultFromSynapseAPI.success)
                        {
                            #region 2nd Synapse Payment (RS to Vendor)

                            // 1st Payment was successful, now do the 2nd one (from RS -> original recipient)
                            Logger.Info("MDA -> GetTokensAndTransferMoneyToNewUser - 1st HABITAT Payment Successful (to RS) - Response From SYNAPSE's /order/add API - " +
                                        "OrderID: [" + transactionResultFromSynapseAPI.responseFromSynapse.trans._id.oid + "]");

                            senderFirstName = "Rent Scene";
                            senderLastName = "";

                            var rentSceneMemId = "852987e8-d5fe-47e7-a00b-58a80dd15b49"; // Rent Scene's MemberID
                            var rentSceneMemGuid = Utility.ConvertToGuid(rentSceneMemId);

                            var createSynapseUserObj = _dbContext.SynapseCreateUserResults.FirstOrDefault(m => m.MemberId == rentSceneMemGuid &&
                                                                                                               m.IsDeleted == false);
                            sender_oauth = createSynapseUserObj.access_token;

                            senderBankOid = "574f45d79506295ff7a81db8";
                            senderFingerprint = "6d441f70cc6891e7831432baac2e50d7";
                            var senderIP = CommonHelper.GetRecentOrDefaultIPOfMember(new Guid(rentSceneMemId));
                            var senderEmail2 = "payments@rentscene.com";


                            log = "TDA -> GetTokensAndTransferMoneyToNewUser - About to call 2nd HABITAT Payment (to the Vendor) - " +
                                        "TransID: [" + suppID_or_transID + "], Amount: [" + amount + "], Sender Name: [" + senderFirstName + " " + senderLastName +
                                        "], Sender BankOID: [" + senderBankOid + "], Recip Name: [" + recipientFirstName + " " + recipientLastName +
                                        "], Recip BankOID: [" + recipBankOid + "]";

                            Logger.Info(log);

                            transactionResultFromSynapseAPI = AddTransSynapseV3Reusable(sender_oauth, senderFingerprint,
                                senderBankOid, amount, recipBankOid, suppID_or_transID, senderEmail2, receiverUserName,
                                senderIP, senderLastName, recipientLastName, memoForSyn);

                            #endregion 2nd Synapse Payment (RS to Vendor)
                        }
                        else
                        {
                            var error = "MDA -> GetTokensAndTransferMoneyToNewUser - 1st HABITAT Payment (to RS) FAILED - Response From SYNAPSE's /order/add API - " +
                                        "ErrorMessage: [" + transactionResultFromSynapseAPI.ErrorMessage + "], Synapse Error: [" + transactionResultFromSynapseAPI.responseFromSynapse.error.en + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                        }
                    }

                    #endregion Habitat Custom Checks

                    else
                    {
                        // Expected path for all Payments except for Habitat
                        transactionResultFromSynapseAPI = AddTransSynapseV3Reusable(sender_oauth, senderFingerprint, senderBankOid, amount,
                                                                                    recipBankOid, suppID_or_transID, senderUserName, receiverUserName,
                                                                                    iPForTransaction, senderLastName, recipientLastName, memoForSyn);
                    }


                    short shouldSendFailureNotifications = 0;

                    int saveToTransTable = 0;

                    if (transactionResultFromSynapseAPI.success == true)
                    {
                        Logger.Info("TDA -> TransferMoneyUsingSynapse - SUCCESS Response From SYNAPSE's /order/add API - " +
                                    "Synapse OrderID: [" + transactionResultFromSynapseAPI.responseFromSynapse.trans._id.oid + "]");

                        #region Save Info in Transaction Details Table

                        transactionDetail.TransactionStatus = "Success";
                        transactionDetail.Memo = transInput.Memo;
                        transactionDetail.Picture = transInput.Picture;
                        transactionDetail.Amount = transInput.Amount;
                        transactionDetail.AdminNotes = isAutoPay
                                                       ? "RENT AUTO PAYMENT"
                                                       : null;

                        transactionDetail.GeoLocation = new GeoLocation
                        {
                            LocationId = Guid.NewGuid(),
                            Latitude = transInput.Location.Latitude,
                            Longitude = transInput.Location.Longitude,
                            Altitude = transInput.Location.Altitude,
                            AddressLine1 = transInput.Location.AddressLine1,
                            AddressLine2 = transInput.Location.AddressLine2,
                            City = transInput.Location.City,
                            State = transInput.Location.State,
                            Country = transInput.Location.Country,
                            ZipCode = transInput.Location.ZipCode,
                            DateCreated = TransDateTime,
                        };

                        noochConnection.Transactions.Add(transactionDetail);
                        //noochConnection.SaveChanges();

                        // Set output to be the just-created TransactionID
                        // Cliff (5/17/16): Not sure this is needed anymore with the new architecture...
                        trnsactionId = transactionDetail.TransactionId.ToString();

                        #endregion Save Info in Transaction Details Table


                        #region Update TotalNoochTransfersCount

                        try
                        {
                            if (senderNoochDetails.TotalNoochTransfersCount != null)
                            {
                                senderNoochDetails.TotalNoochTransfersCount = senderNoochDetails.TotalNoochTransfersCount + 1;
                                senderNoochDetails.DateModified = DateTime.Now;
                            }
                            else
                            {
                                senderNoochDetails.TotalNoochTransfersCount = 1;
                                senderNoochDetails.DateModified = DateTime.Now;
                            }

                            if (recipientNoochDetails.TotalNoochTransfersCount != null)
                            {
                                recipientNoochDetails.TotalNoochTransfersCount = recipientNoochDetails.TotalNoochTransfersCount + 1;
                                recipientNoochDetails.DateModified = DateTime.Now;
                            }
                            else
                            {
                                recipientNoochDetails.TotalNoochTransfersCount = 1;
                                recipientNoochDetails.DateModified = DateTime.Now;
                            }

                            // Update Users in Members Table
                            try
                            {
                                saveToTransTable = noochConnection.SaveChanges();
                            }
                            catch
                            {
                                saveToTransTable = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> TransferMoneyUsingSynapse - Attempted to update Sender / Recipient Member Records Failed - " +
                                         "[Exception: " + ex + "]");
                        }

                        #endregion Update TotalNoochTransfersCount


                        #region Transaction Updated in DB

                        if (saveToTransTable > 0)
                        {
                            //Logger.Info("TDA -> TransferMoneyUsingSynapse CHECKPOINT #6 - ALL DB OPERATIONS SAVED SUCCESSFULLY");

                            if (transInput.doNotSendEmails != true)
                            {
                                #region Send Email to Sender on transfer success

                                // CC (6/18/16): Bypassing the Notifcation Settings Check since no users currently can edit those,
                                //               so it should always return true... so just skip it until we do something with the Notification Settings again.
                                //var sendersNotificationSets = CommonHelper.GetMemberNotificationSettings(senderNoochDetails.MemberId.ToString());

                                if (//((sendersNotificationSets != null && sendersNotificationSets.EmailTransferSent != false) ||
                                    //isForRentScene) &&
                                    !isRentScenePayrollPayment) // don't send email to sender if sender is Rent Scene and it's a payment to a RS employee
                                {
                                    if (!String.IsNullOrEmpty(recipientNoochDetails.Photo) && recipientNoochDetails.Photo.Length > 20)
                                    {
                                        recipientPic = recipientNoochDetails.Photo.ToString();
                                    }

                                    var tokens = new Dictionary<string, string>
	                                        {
	                                            {Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
	                                            {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, recipientFirstName + " " + recipientLastName},
	                                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, dl},
	                                            {Constants.PLACEHLODER_CENTS, ce},
	                                            {Constants.MEMO, memo}
	                                        };

                                    var toAddress = senderUserName;

                                    try
                                    {
                                        string subject;
                                        string template;
                                        // string month = 

                                        if (isAutoPay)
                                        {
                                            subject = "Your Rent Payment To " + recipientFirstName + " " + recipientLastName;

                                            if (isForRentScene)
                                            {
                                                template = "TransferSent_RentSceneAutoPay";
                                            }
                                            else
                                            {
                                                template = "TransferSent";
                                            }
                                        }
                                        else
                                        {
                                            subject = "Your $" + wholeAmount + " payment to " + recipientFirstName;

                                            if (isForRentScene)
                                            {
                                                template = "TransferSent_RentScene";
                                            }
                                            else
                                            {
                                                template = "TransferSent";
                                            }
                                        }

                                        Utility.SendEmail(template, fromAddress, toAddress, null,
                                                                    subject, null, tokens, null, null, null);

                                        Logger.Info("TDA -> TransferMoneyUsingSynapse - [" + template + "] email sent to [" +
                                                    toAddress + "] successfully. Subject: [" + subject + "]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TDA -> TransferMoneyUsingSynapse -> EMAIL TO RECIPIENT FAILED: TransferReceived Email NOT sent to [" +
                                                     toAddress + "], [Exception: " + ex + "]");
                                    }
                                }

                                #endregion Send Email to Sender on transfer success

                                // Now notify the recipient...

                                #region Send Notifications to Recipient on transfer success

                                // CC (6/18/16): Bypassing the Notifcation Settings Check since no users currently can edit those,
                                //               so it should always return true... so just skip it until we do something with the Notification Settings again.
                                //var recipNotificationSets = CommonHelper.GetMemberNotificationSettings(recipientNoochDetails.MemberId.ToString());

                                //if (recipNotificationSets != null)
                                //{
                                // First, send push notification
                                #region Push notification to Recipient

                                if (//recipNotificationSets.TransferReceived == true &&
                                    !String.IsNullOrEmpty(recipientNoochDetails.DeviceToken) &&
                                    recipientNoochDetails.DeviceToken.Length > 6)
                                {
                                    string recipDeviceId = recipientNoochDetails.DeviceToken;

                                    string pushBodyText = isAutoPay
                                                          ? "Rent Auto Payment for $" + wholeAmount + " received from " +
                                                            senderFirstName + " " + senderLastName + "."
                                                          : "You received $" + wholeAmount + " from " + senderFirstName +
                                                            " " + senderLastName + "! Spend it wisely :-)";
                                    try
                                    {


                                        Utility.SendNotificationMessage(pushBodyText, "Nooch", recipDeviceId,
                                                                                   recipientNoochDetails.DeviceType);

                                        Logger.Info("TDA -> TransferMoneyUsingSynapse -> SUCCESS - Push notification sent to " +
                                                    "Recipient [" + recipientFirstName + " " + recipientLastName + "] successfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TDA -> TransferMoneyUsingSynapse -> Success - BUT Push notification FAILURE - Push to Recipient NOT sent [" +
                                                     recipientFirstName + " " + recipientLastName + "], Exception: [" + ex + "]");
                                    }
                                }

                                #endregion Push notification to Recipient

                                // Now send email notification
                                #region Email notification to Recipient

                                if (true)//recipNotificationSets.EmailTransferReceived ?? false || isForRentScene)
                                {
                                    if (!String.IsNullOrEmpty(senderNoochDetails.Photo) && senderNoochDetails.Photo.Length > 20)
                                    {
                                        senderPic = senderNoochDetails.Photo.ToString();
                                    }

                                    var templateToUse = (isAutoPay || isForRentScene) ? "TransferReceived_RentScene" : "TransferReceived";

                                    var tokensR = new Dictionary<string, string>
	                                        {
                                                {Constants.PLACEHOLDER_FIRST_NAME, recipientFirstName},
	                                            {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, senderFirstName + " " + senderLastName},
                                                {"$UserPicture$", senderPic},
	                                            {Constants.PLACEHOLDER_TRANSFER_AMOUNT, wholeAmount},
	                                            {Constants.PLACEHOLDER_TRANSACTION_DATE, Convert.ToDateTime(transInput.TransactionDateTime).ToString("MMM dd")},
	                                            {Constants.MEMO, memo}
	                                        };

                                    var toAddress = receiverUserName;

                                    try
                                    {
                                        string subject = "";
                                        if (isAutoPay)
                                        {
                                            subject = "Rent AutoPayment from " + senderFirstName + " " + senderLastName + " - $" + wholeAmount;
                                        }
                                        else if (senderFirstName == "Rent Scene")
                                        {
                                            subject = "Payment Received From Rent Scene for $" + wholeAmount;
                                        }
                                        else
                                        {
                                            subject = senderFirstName + " " + senderLastName + " sent you $" + wholeAmount;
                                        }

                                        Utility.SendEmail(templateToUse, fromAddress, toAddress, null,
                                                          subject, null, tokensR, null, null, null);

                                        Logger.Info("TDA -> TransferMoneyUsingSynapse - [" + templateToUse + "] Email sent to [" +
                                                    toAddress + "] successfully");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TDA -> TransferMoneyUsingSynapse -> EMAIL TO RECIPIENT FAILED: " +
                                                     "[" + templateToUse + "] Email NOT sent to [" + toAddress + "], [Exception: " + ex + "]");
                                    }
                                }

                                #endregion Email notification to Recipient

                                #endregion Send Notifications to Recipient on transfer success
                            }
                            else
                                Logger.Info("TDA -> TransferMoneyUsigSynapse - shouldSendEmail flag is [" + transInput.doNotSendEmails +
                                            "] - SO NOT SENDING TRANSFER SENT EMAIL TO SENDER: [" + senderUserName + "] OR RECIPIENT: [" + receiverUserName + "]");

                            return "Your cash was sent successfully";
                        }
                        else
                        {
                            // Transaction NOT updated in DB
                            var error = "TDA -> TransferMoneyUsingSynapse - Synapse returned successfully, BUT FAILED to update Nooch DB with transaction info - " +
                                        "MemberID : [" + transInput.MemberId + "], Amount: [$" + transInput.Amount + "]";
                            Logger.Error(error);
                            CommonHelper.notifyCliffAboutError(error);
                            shouldSendFailureNotifications = 1;
                        }

                        #endregion Transaction Updated in DB
                    }

                    #region Failure Sections

                    else // Synapse Order API returned failure in response
                    {
                        var error = "TDA - TransferMoneyUsingSynapse FAILED - Got Synapse response, but not successful - ErrorMsg: [" + transactionResultFromSynapseAPI.ErrorMessage +
                                    "], Response From Synapse: [" + transactionResultFromSynapseAPI.responseFromSynapse + "]";
                        Logger.Error(error);
                        CommonHelper.notifyCliffAboutError(error);
                        shouldSendFailureNotifications = 2;
                    }

                    // Check if there was a failure above and we need to send the failure Email/SMS notifications to the sender.
                    if (shouldSendFailureNotifications > 0)
                    {
                        Logger.Error("TDA -> TransferMoneyUsingSynapse - THERE WAS A FAILURE - Sending Failure Notifications to both Users - " +
                                     "shouldSendFailureNotifications = [" + shouldSendFailureNotifications + "]");

                        // CC (8/31/16): Commenting out failure notifications
                        /*if (isAutoPay == false)
                        {
                            #region Notify Sender about failure

                            var senderNotificationSettings = CommonHelper.GetMemberNotificationSettings(senderNoochDetails.MemberId.ToString());

                            if (senderNotificationSettings != null)
                            {
                                #region Push Notification to Sender about failure

                                // COMMETNED BY CLIFF 11.24.14  (Don't think a push notification is necessary for a transfer failure... email is enough)
                                // UN-COMMENTED BY CLIFF (7/10/15): After more thought, we might as well notify via push as well.

                                if (senderNotificationSettings.TransferAttemptFailure == true)
                                {
                                    string senderDeviceId = senderNotificationSettings != null ? senderNoochDetails.DeviceToken : null;
                                    string senderDeviceTy = senderNotificationSettings != null ? senderNoochDetails.DeviceType : null;

                                    string mailBodyText = "Your attempt to send $" + transInput.Amount.ToString("n2") +
                                                          " to " + recipientFirstName + " " + recipientLastName + " failed ;-(  Contact Nooch support for more info.";

                                    if (!String.IsNullOrEmpty(senderDeviceId))
                                    {
                                        try
                                        {

                                            Utility.SendNotificationMessage(mailBodyText, "Nooch", senderDeviceId,
                                                                                   senderDeviceTy);

                                            Logger.Info("TDA -> TransferMoneyUsingSynapse FAILED - Push notif sent to Sender: [" +
                                                senderFirstName + " " + senderLastName + "] successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error(
                                                "TDA -> TransferMoneyUsingSynapse FAILED - Push notif FAILED also, SMS NOT sent to [" +
                                                senderFirstName + " " + senderLastName + "],  [Exception: " + ex + "]");
                                        }
                                    }
                                }

                                #endregion Push Notification to Sender about failure

                                #region Email notification to Sender about failure

                                if (senderNotificationSettings.EmailTransferAttemptFailure ?? false)
                                {
                                    var tokens = new Dictionary<string, string>
	                                {
	                                    {Constants.PLACEHOLDER_FIRST_NAME, senderFirstName + " " + senderLastName},
	                                    {Constants.PLACEHOLDER_FRIEND_FIRST_NAME, recipientFirstName + " " + recipientLastName},
	                                    {Constants.PLACEHOLDER_TRANSFER_AMOUNT, dl},
	                                    {Constants.PLACEHLODER_CENTS, ce},
	                                };

                                    var toAddress = CommonHelper.GetDecryptedData(senderNoochDetails.UserName);

                                    try
                                    {
                                        Utility.SendEmail("transferFailure", fromAddress, toAddress, null,
                                                          "Nooch transfer failure :-(", null, tokens, null, null, null);

                                        Logger.Info("TDA -> TransferMoneyUsingSynapse FAILED - Email sent to Sender: [" +
                                                    toAddress + "] successfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TDA -> TransferMoneyUsingSynapse --> Error: TransferAttemptFailure mail " +
                                                               "NOT sent to [" + toAddress + "],  [Exception: " + ex + "]");
                                    }
                                }

                                #endregion Email notification to Sender about failure
                            }

                            #endregion Notify Sender about failure
                        }*/

                        if (shouldSendFailureNotifications == 1) return "There was a problem updating Nooch DB tables.";
                        else if (shouldSendFailureNotifications == 2) return "There was a problem with Synapse.";
                        else return "Unknown Failure";
                    }

                    #endregion Failure Sections

                    #endregion Synapse V3 Add Trans

                    return "Uh oh - Unknown Failure"; // This should never be reached b/c code should hit the failure section
                }
                catch (Exception ex)
                {
                    var error = "TDA -> TransferMoneyUsingSynapse FAILED (#6943) - Exception: [" + ex.ToString() + "]";
                    Logger.Error(error);
                    CommonHelper.notifyCliffAboutError(error);
                    return "Sorry there was a problem (#6946): [" + ex.Message + "]";
                }
            }
        }


        /// <summary>
        /// To Transfer (SEND) money to NON-NOOCH USER using SYNAPSE - (VIA EMAIL ONLY... VIA PHONE HAS A DIFF METHOD)
        /// </summary>
        /// <param name="inviteType"></param>
        /// <param name="receiverEmailId"></param>
        /// <param name="transactionEntity"></param>
        /// <param name="trnsactionId"></param>
        public string TransferMoneyToNonNoochUserUsingSynapse(string inviteType, string receiverEmailId, TransactionEntity transactionEntity, string cip = "renter")
        {
            Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse Fired - " +
                        "MemberID: [" + transactionEntity.MemberId + "], " +
                        "Recipient: [" + receiverEmailId + "], " +
                        "Amount: [" + transactionEntity.Amount + "], CIP: [" + cip + "]");

            string trnsactionId = string.Empty;

            // Check if the recipient's email already exists

            var checkuser = CommonHelper.GetMemberIdByUserName(receiverEmailId);

            if (checkuser == null)
            {
                // Receiver's email NOT already associated with a Nooch account

                #region Initial Checks

                if (transactionEntity.MemberId == transactionEntity.RecipientId)
                {
                    return "Not allowed for send money to the same user.";
                }

                // Check Sender's PIN
                string validPinNumberResult = CommonHelper.ValidatePinNumber(transactionEntity.MemberId, transactionEntity.PinNumber);
                if (validPinNumberResult != "Success")
                {
                    return validPinNumberResult;
                }

                // Check per-transaction limit
                decimal transactionAmount = Convert.ToDecimal(transactionEntity.Amount);

                // Individual user transfer limit check
                decimal thisUserTransLimit = 0;
                string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(transactionEntity.MemberId);

                if (!String.IsNullOrEmpty(indiTransLimit))
                {
                    if (indiTransLimit != "0")
                    {
                        thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                        if (transactionAmount > thisUserTransLimit)
                        {
                            Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED - OVER PERSONAL TRANS LIMIT - " +
                                         "Amount Requested: [" + transactionAmount.ToString() + "], " +
                                         "Indiv. Limit: [" + thisUserTransLimit + "], " +
                                         "MemberId: [" + transactionEntity.MemberId + "]");

                            return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + thisUserTransLimit.ToString("F2");
                        }
                    }
                }

                if (CommonHelper.isOverTransactionLimit(transactionAmount, "", transactionEntity.MemberId))
                {
                    return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                }

                // Check weekly transfer limit
                if (CommonHelper.IsWeeklyTransferLimitExceeded(Utility.ConvertToGuid(transactionEntity.MemberId), transactionAmount, null))
                {
                    Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse -> Weekly transfer limit exceeded. MemberId: [" +
                                           transactionEntity.MemberId + "]");
                    return "Weekly transfer limit exceeded.";
                }

                // Check Sender's Synapse account details
                var senderSynDetails = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(transactionEntity.MemberId.ToString());

                if (senderSynDetails.wereUserDetailsFound == false || senderSynDetails.UserDetails == null)
                {
                    Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED - Synapse User Details Not Found - MemberId - [" + transactionEntity.MemberId + "]");
                    return "Sender does not have any bank added";
                }
                if (senderSynDetails.wereBankDetailsFound == false || senderSynDetails.BankDetails == null)
                {
                    Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED - Synapse User Details Not Found - MemberId - [" + transactionEntity.MemberId + "]");
                    return "Sender does not have any bank added";
                }
                if (senderSynDetails.UserDetails.permission != "SEND-AND-RECEIVE")
                {
                    Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED - User's Synapse Permission is INSUFFICIENT: [" + senderSynDetails.UserDetails.permission +
                                 "] MemberID: [" + transactionEntity.MemberId + "]");
                    //return "Sender does not have any bank added";
                    return "Sender has insufficient permissions: [" + senderSynDetails.UserDetails.permission + "]";
                }
                // Check if Sender's Synapse bank account is Verified
                if (senderSynDetails.BankDetails.allowed != "CREDIT-AND-DEBIT")
                {
                    Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse - Transfer ABORTED: " +
                                "Sender's Synapse bank not verified - Bank-Allowed: [" + senderSynDetails.BankDetails.allowed + "], " +
                                "MemberID: [" + transactionEntity.MemberId + "], TransactionId: [" + transactionEntity.TransactionId + "]");
                    //return "Sender does not have any verified bank account.";
                    return "Sender's bank has insufficient permissions: [" + senderSynDetails.BankDetails.allowed + "]";
                }

                #endregion Initial Checks

                // Save transaction details along with device and location details...
                using (var noochConnection = new NOOCHEntities())
                {
                    var memid = Utility.ConvertToGuid(transactionEntity.MemberId);

                    transactionEntity.RecipientId = transactionEntity.MemberId;

                    Transaction transactionDetail = SetTransactionDetails(transactionEntity, Constants.TRANSACTION_TYPE_INVITE, 0);
                    transactionDetail.Memo = transactionEntity.Memo;
                    transactionDetail.TransactionStatus = "Pending";
                    transactionDetail.Picture = transactionEntity.Picture;
                    transactionDetail.InvitationSentTo = CommonHelper.GetEncryptedData(receiverEmailId.ToLower());
                    // Add location details
                    transactionDetail.GeoLocation = new GeoLocation
                    {
                        LocationId = Guid.NewGuid(),
                        Latitude = transactionEntity.Location.Latitude,
                        Longitude = transactionEntity.Location.Longitude,
                        Altitude = transactionEntity.Location.Altitude,
                        AddressLine1 = transactionEntity.Location.AddressLine1,
                        AddressLine2 = transactionEntity.Location.AddressLine2,
                        City = transactionEntity.Location.City,
                        State = transactionEntity.Location.State,
                        Country = transactionEntity.Location.Country,
                        ZipCode = transactionEntity.Location.ZipCode,
                        DateCreated = DateTime.Now,
                    };


                    noochConnection.Transactions.Add(transactionDetail);

                    trnsactionId = transactionDetail.TransactionId.ToString();
                    int value = 0;

                    var sender = CommonHelper.GetMemberDetails(transactionEntity.MemberId);
                    int updateSenderInDB = 0;

                    try
                    {
                        // Update TotalNoochTransfersCount by +1
                        if (sender.TotalNoochTransfersCount == null)
                            sender.TotalNoochTransfersCount = 0;
                        else
                            sender.TotalNoochTransfersCount = sender.TotalNoochTransfersCount + 1;
                        // Update Sender in DB
                        sender.DateModified = DateTime.Now;

                        value = updateSenderInDB = noochConnection.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse ERROR - FAILED to update Sender's Total Transfer Count in DB - " +
                                     "MemberID: [" + sender.MemberId.ToString() + "], [Exception: " + ex + "]");
                    }

                    #region Increment Invite Code

                    try
                    {
                        var memberInviteCodeDetails = noochConnection.InviteCodes.FirstOrDefault(m => m.InviteCodeId == sender.InviteCodeId);

                        if (memberInviteCodeDetails != null)
                        {
                            if (memberInviteCodeDetails.count < 10)
                            {
                                memberInviteCodeDetails.count++;
                            }

                            noochConnection.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse ERROR - FAILED to update Sender's invite code in DB - MemberID: [" +
                                     sender.MemberId.ToString() + "], [Exception: " + ex + "]");
                    }

                    #endregion Increment Invite Code


                    #region Define Variables From Transaction for email/sms notifications

                    var fromAddress = Utility.GetValueFromConfig("transfersMail");

                    string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName));
                    string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName));

                    string senderPic = "https://www.noochme.com/noochweb/Assets/Images/userpic-default.png";
                    if (!String.IsNullOrEmpty(sender.Photo) && sender.Photo.Length > 20)
                        senderPic = sender.Photo.ToString();

                    string wholeAmount = transactionEntity.Amount.ToString("n2");
                    string[] amountArray = wholeAmount.Split('.');

                    string memo = "";
                    if (!string.IsNullOrEmpty(transactionEntity.Memo))
                    {
                        if (transactionEntity.Memo.Length > 3)
                        {
                            string firstThreeChars = transactionEntity.Memo.Substring(0, 3).ToLower();
                            bool startWithFor = firstThreeChars.Equals("for");

                            if (startWithFor) memo = transactionEntity.Memo.ToString();
                            else memo = "For " + transactionEntity.Memo.ToString();
                        }
                        else memo = "For " + transactionEntity.Memo.ToString();
                    }

                    string company = "nooch";
                    if (sender.MemberId.ToString().ToLower() == "852987e8-d5fe-47e7-a00b-58a80dd15b49") // Rent Scene's account
                    {
                        fromAddress = "payments@rentscene.com";
                        company = "rentscene";
                        senderFirstName = "Rent Scene";
                        senderLastName = "";
                    }
                    else if (sender.MemberId.ToString().ToLower() == "45357cf0-e651-40e7-b825-e1ff48bf44d2") // Habitat's account
                    {
                        fromAddress = "payments@tryhabitat.com";
                        company = "habitat";
                        senderFirstName = "Habitat LLC";
                        senderLastName = "";
                    }

                    #endregion Define Variables From Transaction for email/sms notifications


                    #region Sender Updated in DB Successfully

                    if (updateSenderInDB > 0)
                    {
                        // Now notify Sender of transfer success
                        #region Email to Sender

                        //var senderNotifSets = CommonHelper.GetMemberNotificationSettingsByUserName(CommonHelper.GetDecryptedData(sender.UserName));

                        if (company != "rentscene") //senderNotifSets != null && (senderNotifSets.EmailTransferSent ?? false))
                        {
                            string otherlink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/CancelRequest?TransactionId=" + transactionDetail.TransactionId +
                                                                                                           "&MemberId=" + transactionEntity.MemberId +
                                                                                                           "&UserType=mx5bTcAYyiOf9I5Py9TiLw==");

                            var tokens = new Dictionary<string, string>
												 {
													{Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
													{Constants.PLACEHOLDER_FRIEND_FIRST_NAME, receiverEmailId},
													{Constants.PLACEHOLDER_TRANSFER_AMOUNT, amountArray[0].ToString()},
													{Constants.PLACEHLODER_CENTS, amountArray[1].ToString()},
													{Constants.PLACEHOLDER_OTHER_LINK, otherlink},
													{Constants.MEMO, memo}
												 };

                            var toAddress = CommonHelper.GetDecryptedData(sender.UserName);

                            var template = "transferSentToNonMember";
                            var subject = "Your Nooch payment is pending";

                            if (company == "habitat")
                            {
                                template = "transferSentToNonMember_habitat";
                                subject = "Your Pending Payment to " + receiverEmailId;
                            }

                            try
                            {
                                Utility.SendEmail(template, fromAddress, toAddress, null,
                                                  subject, null, tokens, null, null, null);

                                Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse - " + template +
                                            " email sent to [" + toAddress + "] successfully.");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse - " + template +
                                             " email NOT sent to [" + toAddress + "], Exception: [" + ex + "]");
                            }
                        }

                        #endregion Email to Sender


                        // Now Send email to Recipient (non-Nooch user in this case)
                        #region Email To Recipient New User

                        // In this case UserType would = 'New' and TransType would = 'Invite'

                        try
                        {
                            string cipToUse = "1";
                            switch (cip)
                            {
                                case "renter":
                                    cipToUse = "1";
                                    break;
                                case "vendor":
                                    cipToUse = "2";
                                    break;
                                case "runner":
                                    cipToUse = "2";
                                    break;
                                case "landlord":
                                    cipToUse = "3";
                                    break;
                            }

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "Nooch/RejectMoney?TransactionId=" + transactionDetail.TransactionId +
                                                              "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                              "&TransType=DrRr1tU1usk7nNibjtcZkA==");

                            string acceptLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "Nooch/DepositMoney?transid=" + transactionDetail.TransactionId +
                                                              "&cip=" + cipToUse);

                            var tokens2 = new Dictionary<string, string>
												 {
                                                     {"$UserPicture$", senderPic},
													 {Constants.PLACEHOLDER_FIRST_NAME, senderFirstName + " " + senderLastName},
													 {Constants.PLACEHOLDER_TRANSFER_AMOUNT, amountArray[0].ToString()},
													 {Constants.PLACEHLODER_CENTS, amountArray[1].ToString()},
													 {Constants.PLACEHOLDER_TRANSFER_REJECT_LINK, rejectLink },
													 {Constants.PLACEHOLDER_TRANSFER_ACCEPT_LINK, acceptLink },
													 {Constants.MEMO, memo}
												 };

                            string cents = (amountArray[1] == "00") ? "" : "." + amountArray[1];

                            var template = "transferReceivedNewUser";
                            var subject = "Payment From " + senderFirstName + " " + senderLastName + " - $" + amountArray[0] + cents;
                            if (company == "rentscene")
                            {
                                template = "transferReceivedNewUser_rentscene";
                                subject = "Payment From Rent Scene - $" + amountArray[0];
                            }
                            else if (company == "habitat")
                            {
                                template = "transferReceivedNewUser_habitat";
                                subject = "Payment From Habitat LLC - $" + amountArray[0];
                            }

                            Utility.SendEmail(template, fromAddress, receiverEmailId, null,
                                              subject, null, tokens2, null, null, null);

                            Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse - [" + template + "] email sent to recipient: [" + receiverEmailId + "] successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse - transferReceivedNewUser email NOT sent to [" + receiverEmailId +
                                         "], Exception: [" + ex + "]");
                        }

                        #endregion Email To Recipient New User
                    }

                    #endregion Sender Updated in DB Successfully

                    #region Sender NOT updated in DB

                    else
                    {
                        // Send email to sender on sending failure
                        #region Notify Sender on Transfer Failure

                        var senderNotifSets = CommonHelper.GetMemberNotificationSettingsByUserName(CommonHelper.GetDecryptedData(sender.UserName));

                        if (senderNotifSets != null)
                        {
                            if (!String.IsNullOrEmpty(sender.DeviceToken) &&
                                sender.DeviceToken.Length > 6)
                            {
                                #region Push Notification to Sender About transfer failure

                                if ((senderNotifSets.TransferAttemptFailure == null)
                                    ? false : senderNotifSets.TransferAttemptFailure.Value)
                                {
                                    string senderDeviceId = senderNotifSets != null ? sender.DeviceToken : null;
                                    string pushBodyText = "Your $" + wholeAmount + " payment to " + receiverEmailId + " failed unfortunately. Contact Nooch support for more info.";

                                    try
                                    {

                                        Utility.SendNotificationMessage(pushBodyText, "Nooch", senderDeviceId,
                                                                                   sender.DeviceType);

                                        Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED and Failure Push notif sent to [" +
                                            senderFirstName + " " + senderLastName + "] succesfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse FAILED, and Push FAILED to [" +
                                            senderFirstName + " " + senderLastName + "], [Exception: " + ex + "]");
                                    }
                                }
                                #endregion Push Notification to Sender about transfer failure
                            }

                            #region Email Notification to Sender about transfer failure

                            if (senderNotifSets != null && (senderNotifSets.EmailTransferAttemptFailure ?? false))
                            {
                                var tokensF = new Dictionary<string, string>
												 {
													{Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName)) +" "+CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName))},
													{Constants.PLACEHOLDER_FRIEND_FIRST_NAME, receiverEmailId},
													{Constants.PLACEHOLDER_TRANSFER_AMOUNT, amountArray[0].ToString()},
													{Constants.PLACEHLODER_CENTS, amountArray[1].ToString()}
												 };

                                var toAddress = CommonHelper.GetDecryptedData(sender.UserName);

                                try
                                {
                                    Utility.SendEmail("transferFailure", fromAddress, toAddress, null,
                                                      "Nooch Payment Failure", null, tokensF, null, "transferFailure@nooch.com", null);

                                    Logger.Info("TDA -> TransferMoneyToNonNoochUserUsingSynapse - FAILED, email to sender [" + toAddress + "] sent successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserUsingSynapse - FAILED, email sending also FAILED, email NOT sent to [" + toAddress +
                                                 "], [Exception: " + ex + "]");
                                }
                            }

                            #endregion Email Notification to Sender about transfer failure
                        }

                        #endregion Notify Sender on Transfer Failure
                    }

                    #endregion Sender NOT updated in DB


                    return value > 0 ? "Your cash was sent successfully" : "Failure";
                }
            }
            else
            {
                trnsactionId = null;
                return "User Already Exists";
            }
        }


        /// <summary>
        /// To TRANSFER (SEND/INVITE) money to NON-NOOCH USER using SYNAPSE through SMS
        /// </summary>
        /// <param name="inviteType"></param>
        /// <param name="receiverPhoneNumber"></param>
        /// <param name="transactionEntity"></param>
        /// <param name="trnsactionId"></param>
        public string TransferMoneyToNonNoochUserThroughPhoneUsingsynapse(string inviteType, string receiverPhoneNumber, TransactionEntity transactionEntity)
        {
            Logger.Info("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse Initiated - MemberID: [" + transactionEntity.MemberId + "], InviteType: [" +
                        inviteType + "], receiverPhoneNumber: [" + receiverPhoneNumber + "]");

            string trnsactionId = string.Empty;

            try
            {
                if (!String.IsNullOrEmpty(receiverPhoneNumber.Trim()))
                {
                    receiverPhoneNumber = CommonHelper.RemovePhoneNumberFormatting(receiverPhoneNumber);

                    // Check if the user email already exists
                    var checkuser = CommonHelper.GetMemberIdByContactNumber(receiverPhoneNumber);

                    if (checkuser == null)
                    {
                        #region Initial Checks

                        if (transactionEntity.MemberId == transactionEntity.RecipientId)
                        {
                            return "Not allowed for send money to the same user.";
                        }

                        // Check PIN
                        string validPinNumberResult = CommonHelper.ValidatePinNumber(transactionEntity.MemberId, transactionEntity.PinNumber);
                        if (validPinNumberResult != "Success")
                        {
                            return validPinNumberResult;
                        }

                        // Check per-transaction limit
                        decimal transactionAmount = Convert.ToDecimal(transactionEntity.Amount);

                        // Individual user transfer limit check
                        decimal thisUserTransLimit = 0;
                        string indiTransLimit = CommonHelper.GetGivenMemberTransferLimit(transactionEntity.MemberId);
                        if (!String.IsNullOrEmpty(indiTransLimit))
                        {
                            if (indiTransLimit != "0")
                            {
                                thisUserTransLimit = Convert.ToDecimal(indiTransLimit);

                                if (transactionAmount > thisUserTransLimit)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - OVER PERSONAL TRANS LIMIT - Amount Requested: [" + transactionAmount.ToString() +
                                                   "], Indiv. Limit: [" + thisUserTransLimit + "], MemberId: [" + transactionEntity.MemberId + "]");

                                    return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + thisUserTransLimit.ToString("F2");
                                }
                            }
                        }

                        if (CommonHelper.isOverTransactionLimit(transactionAmount, "", transactionEntity.MemberId))
                        {
                            return "Whoa now big spender! To keep Nooch safe, the maximum amount you can send at a time is $" + Convert.ToDecimal(Utility.GetValueFromConfig("MaximumTransferLimitPerTransaction")).ToString("F2");
                        }

                        // Check weekly transaction limit
                        if (CommonHelper.IsWeeklyTransferLimitExceeded(Utility.ConvertToGuid(transactionEntity.MemberId), transactionAmount, null))
                        {
                            Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse -> Weekly transfer limit exceeded. MemberId: [" +
                                                   transactionEntity.MemberId + "]");
                            return "Weekly transfer limit exceeded.";
                        }

                        // Check sender's Synapse bank account details


                        var senderSynapseInfo = CommonHelper.GetSynapseBankAndUserDetailsforGivenMemberId(transactionEntity.MemberId.ToString());

                        if (senderSynapseInfo.wereBankDetailsFound == false) // Does the sender have a Synapse Bank account?
                        {
                            Logger.Error(
                                "TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - Transfer ABORTED: No Synapse bank account found for Sender. TransactionId: [" +
                                transactionEntity.TransactionId + "]");

                            return "Requester does not have any bank added";
                        }

                        if (senderSynapseInfo.BankDetails != null &&
                            senderSynapseInfo.BankDetails.Status != "Verified") // Is the sender's Synapse Bank account VERIFIED?
                        {
                            Logger.Error(
                                "TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - Transfer ABORTED: No verified bank account found for Sender. TransactionId: [" +
                                transactionEntity.TransactionId + "]");

                            return "Sender does not have any verified bank account.";
                        }

                        #endregion Initial Checks


                        using (var noochConnection = new NOOCHEntities())
                        {
                            #region Save Transaction Details In DB


                            var sender = CommonHelper.GetMemberDetails(transactionEntity.MemberId);

                            transactionEntity.RecipientId = transactionEntity.MemberId;

                            Transaction transactionDetail = SetTransactionDetails(transactionEntity, Constants.TRANSACTION_TYPE_INVITE, 0);
                            transactionDetail.Memo = transactionEntity.Memo;
                            transactionDetail.TransactionStatus = "Pending";
                            transactionDetail.Picture = transactionEntity.Picture;
                            transactionDetail.PhoneNumberInvited = CommonHelper.GetEncryptedData(receiverPhoneNumber);
                            transactionDetail.IsPhoneInvitation = true;

                            // Now add location details
                            transactionDetail.GeoLocation = new GeoLocation
                            {
                                LocationId = Guid.NewGuid(),
                                Latitude = transactionEntity.Location.Latitude,
                                Longitude = transactionEntity.Location.Longitude,
                                Altitude = transactionEntity.Location.Altitude,
                                AddressLine1 = transactionEntity.Location.AddressLine1,
                                AddressLine2 = transactionEntity.Location.AddressLine2,
                                City = transactionEntity.Location.City,
                                State = transactionEntity.Location.State,
                                Country = transactionEntity.Location.Country,
                                ZipCode = transactionEntity.Location.ZipCode,
                                DateCreated = DateTime.Now,
                            };

                            noochConnection.Transactions.Add(transactionDetail);


                            int addTransToDB = noochConnection.SaveChanges();

                            #endregion Save Transaction Details In DB

                            // Setting up all notification variables - used for both success and failure emails below
                            #region Setup Notification Variables

                            var fromAddress = Utility.GetValueFromConfig("transfersMail");

                            string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName));
                            string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName));
                            string receiverPhoneNumFormatted = CommonHelper.FormatPhoneNumber(receiverPhoneNumber);

                            string wholeAmount = transactionEntity.Amount.ToString("n2");
                            string[] s3 = wholeAmount.Split('.');

                            string memo = "";
                            if (!string.IsNullOrEmpty(transactionEntity.Memo))
                            {
                                if (transactionEntity.Memo.Length > 3)
                                {
                                    string firstThreeChars = transactionEntity.Memo.Substring(0, 3).ToLower();
                                    bool startWithFor = firstThreeChars.Equals("for");

                                    if (startWithFor)
                                    {
                                        memo = transactionEntity.Memo.ToString();
                                    }
                                    else
                                    {
                                        memo = "For " + transactionEntity.Memo.ToString();
                                    }
                                }
                                else
                                {
                                    memo = "For " + transactionEntity.Memo.ToString();
                                }
                            }

                            #endregion Setup Notification Variables

                            if (addTransToDB > 0)
                            {
                                trnsactionId = transactionDetail.TransactionId.ToString();

                                #region Email To Sender

                                var friendDetails = CommonHelper.GetMemberNotificationSettingsByUserName(CommonHelper.GetDecryptedData(sender.UserName));

                                if (friendDetails != null && (friendDetails.EmailTransferSent ?? false))
                                {
                                    string otherlink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "Nooch/CancelRequest?TransactionId=" + transactionDetail.TransactionId +
                                                                                                                   "&MemberId=" + transactionEntity.MemberId +
                                                                                                                   "&UserType=mx5bTcAYyiOf9I5Py9TiLw==");

                                    var tokens = new Dictionary<string, string>
												 {
													{Constants.PLACEHOLDER_FIRST_NAME, senderFirstName},
													{Constants.PLACEHOLDER_FRIEND_FIRST_NAME, receiverPhoneNumFormatted},
													{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
													{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()},
													{Constants.PLACEHOLDER_OTHER_LINK,otherlink},
													{Constants.MEMO, memo}
												 };

                                    var toAddress = CommonHelper.GetDecryptedData(sender.UserName);
                                    try
                                    {
                                        Utility.SendEmail("transferSentToNonMember", fromAddress, toAddress, null,
                                            "Your Nooch payment is pending",
                                            null, tokens, null, null, null);

                                        Logger.Info("TransferMoneyToNonNoochUserThroughPhoneUsingsynapse -> email sent to [" + toAddress + "] succesfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Error("TransferMoneyToNonNoochUserThroughPhoneUsingsynapse -> email NOT sent to [" + toAddress + "], Exception: [" + ex.Message + "]");
                                    }
                                }

                                #endregion Email To Sender

                                try
                                {
                                    sender.DateModified = DateTime.Now;
                                    // Increment TotalNoochTransfersCount
                                    sender.TotalNoochTransfersCount += 1;
                                    noochConnection.SaveChanges();

                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - EXCEPTION on trying to update Sender in DB, but continuing on to send notifications. " +
                                                 "[Exception: " + ex + "]");
                                }

                                #region Send SMS To Non-Nooch Transfer Recipient

                                // In this case UserType would be 'New'
                                // TransType would be 'Invite'
                                // Link Source would be 'Phone'

                                string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "trans/RejectMoney?TransactionId=" + transactionDetail.TransactionId +
                                                                  "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                                  "&TransType=DrRr1tU1usk7nNibjtcZkA==");

                                string acceptLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                                  "Nooch/DepositMoney?transid=" + transactionDetail.TransactionId);

                                string googleUrlAPIKey = Utility.GetValueFromConfig("GoogleURLAPI");

                                // shortening URLs from Google
                                string RejectShortLink = "";
                                string AcceptShortLink = "";

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
                                        Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - GoogleAPI FAILED for Reject Short Link.");
                                    }
                                    cli.Dispose();

                                    // Now shorten Accept link

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
                                        // Google short URL API broke...
                                        Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - GoogleAPI FAILED for Accept Short Link.");
                                    }
                                    cli2.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - GoogleAPI FAILED. [Exception: " + ex + "]");
                                    return "Google API URL Failure";
                                }

                                #endregion Call Google URL Shortener API

                                try
                                {
                                    // Example SMS string: "Cliff Canan wants to send you $10 using Nooch, a free app. Click here to pay: {LINK}. Or here to reject: {LINK}"

                                    string SMSContent = senderFirstName + " " + senderLastName + " wants to send you $" +
                                                          s3[0].ToString() + "." + s3[1].ToString() +
                                                          " using Nooch, a free app. Tap here to accept: " + AcceptShortLink +
                                                          ". Or here to reject: " + RejectShortLink;

                                    Utility.SendSMS(receiverPhoneNumber, SMSContent);

                                    Logger.Info("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse. SMS sent to recipient [" + receiverPhoneNumber + "] successfully.");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse. SMS NOT sent to recipient [" + receiverPhoneNumber +
                                                 "], [Exception:" + ex + "]");
                                }

                                #endregion Send SMS To Non-Nooch Transfer Recipient


                                #region Increment Invite Code Count
                                try
                                {

                                    var inviteCodeObj =
                                        noochConnection.InviteCodes.FirstOrDefault(
                                            m => m.InviteCodeId == sender.InviteCodeId);



                                    if (inviteCodeObj != null)
                                    {
                                        if (inviteCodeObj.count < 10)
                                        {
                                            inviteCodeObj.count++;
                                        }
                                        noochConnection.SaveChanges();

                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - EXCEPTION on trying to update Invite Code Count in DB - " +
                                                           "[Exception: " + ex + "]");
                                }
                                #endregion Increment Invite Code Count

                                return "Your cash was sent successfully";
                            }
                            else // Saving Transaction in DB Failed
                            {
                                #region Saving Transaction In DB Failed

                                Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - Could not save Transaction to DB - [Receiver Phone #: " + receiverPhoneNumber + "]");

                                // for push notification in case of failure

                                var friendDetails = CommonHelper.GetMemberNotificationSettingsByUserName(CommonHelper.GetDecryptedData(sender.UserName));

                                if (friendDetails != null)
                                {
                                    string deviceId = friendDetails != null ? sender.DeviceToken : null;
                                    string smsText = "Your attempt to send $" + wholeAmount + " to " + receiverPhoneNumFormatted + " failed :-(";

                                    if ((friendDetails.TransferAttemptFailure == null)
                                        ? false : friendDetails.TransferAttemptFailure.Value)
                                    {
                                        try
                                        {
                                            if (!String.IsNullOrEmpty(deviceId) && (friendDetails.TransferAttemptFailure ?? false))
                                            {

                                                Utility.SendNotificationMessage(smsText, "Nooch", deviceId,
                                                                                   sender.DeviceType);

                                                Logger.Info("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - Push notification sent to [" + deviceId + "] successfully.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - And Push notification NOT sent to [" +
                                                                   deviceId + "], [Exception: " + ex + "]");
                                        }
                                    }
                                    if (friendDetails.EmailTransferAttemptFailure ?? false)
                                    {
                                        // for TransferSent email notification
                                        var tokensF = new Dictionary<string, string>
												 {
													{Constants.PLACEHOLDER_FIRST_NAME, CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.FirstName)) +" "+CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(sender.LastName))},
													{Constants.PLACEHOLDER_FRIEND_FIRST_NAME, receiverPhoneNumFormatted},
													{Constants.PLACEHLODER_CENTS, s3[1].ToString()},
													{Constants.PLACEHOLDER_TRANSFER_AMOUNT, s3[0].ToString()}
												 };

                                        // For TransferAttemptFailure email notification                            
                                        var toAddress = CommonHelper.GetDecryptedData(sender.UserName);

                                        try
                                        {
                                            Utility.SendEmail("transferFailure",
                                                 fromAddress, toAddress, null,
                                                "Nooch transfer failure :-(", null, tokensF, null, null, null);

                                            Logger.Info("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - TransferAttemptFailure email sent to [" + toAddress + "] successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse - TransferAttemptFailure email NOT sent to [" +
                                                                    toAddress + "], [Exception: " + ex + "]");
                                        }
                                    }
                                }

                                #endregion Saving Transaction In DB Failed
                            }
                        }
                    }
                    else
                    {
                        // CLIFF (12/24/15): NEED TO UPDATE THIS TO STILL SEND THE PAYMENT TO THE EXISTING USER, NO REASON TO JUST FAIL... WE HAVE THE
                        //                   RECIPIENT'S INFO IF THEY'RE ALREADY A NOOCH USER...
                        Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED. Phone Number Used already exists - [Phone #:" + receiverPhoneNumber + "]");

                        trnsactionId = null;
                        return "User Already Exists";
                    }
                }
                else
                {
                    Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - receiverPhoneNumber Parameter was NULL or empty");
                    return "Missing receiver phone number";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TDA -> TransferMoneyToNonNoochUserThroughPhoneUsingsynapse FAILED - Outer Exception: [" + ex + "]");
            }

            return "Failure";
        }
    }
}
