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

                
                

                var res =  _dbContext.Transactions.FirstOrDefault(m=>m.Member.MemberId==memGuid && m.TransactionId==transid
                    && m.TransactionStatus=="Pending" && (m.TransactionType=="T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType=="DrRr1tU1usk7nNibjtcZkA=="));
                

                if (res != null)
                {
                    res.TransactionStatus = "Cancelled";

                    int i = _dbContext.SaveChanges();

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

                var res = _dbContext.Transactions.FirstOrDefault(m=>m.Member1.MemberId==memGuid && m.TransactionId==transid
                    && m.TransactionStatus=="Pending" && (m.TransactionType=="T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType=="DrRr1tU1usk7nNibjtcZkA=="));
                    
                    
                if (res != null)
                {
                    res.TransactionStatus = "Cancelled";

                    int i = _dbContext.SaveChanges();

                    if (i > 0)
                    {
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
                            Utility.SendEmail("requestCancelledToSender",  fromAddress, toAddress, null, "Nooch payment request to " + CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(res.Member.FirstName)) + " cancelled", null, tokens, null, null, null);
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
                            Utility.SendEmail("requestCancelledToRecipient",  fromAddress, toAddress2, null, "Nooch payment request cancelled", null, tokens2, null, null, null);
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

                

                var res = _dbContext.Transactions.FirstOrDefault(m=>m.TransactionStatus=="Pending"
                    && m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" && m.TransactionId==transGuid && m.Member.MemberId==memGuid);
                if (res != null)
                {
                    // found and update
                    res.TransactionStatus = "Cancelled";
                    int i = _dbContext.SaveChanges();
                    if (i > 1)
                    {
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
                            Utility.SendEmail("transferCancelledToRecipient",  fromAddress, toAddress2, null, "Nooch transfer", null, tokens2, null, null, null);
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

                
                var transObj = _dbContext.Transactions.FirstOrDefault(m=>m.TransactionStatus=="pending" && m.Member.MemberId==memGuid
                    && m.TransactionId==transid && (m.TransactionType=="T3EMY1WWZ9IscHIj3dbcNw==" || m.TransactionType=="DrRr1tU1usk7nNibjtcZkA=="))
                ;
                

                if (transObj != null)
                {
                    transObj.TransactionStatus = "Cancelled";
                    int i =_dbContext.SaveChanges();

                    if (i > 0)
                    {
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
                            Utility.SendEmail("transferCancelledToSender",  fromAddress, toAddress,
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
                                    senderFullName + " cancelled a $" + s32[0]+ "." + s32[1] + " payment to you",
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


                    var trans = _dbContext.Transactions.FirstOrDefault(m=>m.Member1.MemberId==MemId && m.TransactionId== TransId
                        && m.TransactionStatus == "Pending" && m.TransactionType == "T3EMY1WWZ9IscHIj3dbcNw=="
                        );
                    
                    

                    if (trans != null)
                    {
                        #region Setup Common Variables

                        string fromAddress = Utility.GetValueFromConfig("transfersMail");

                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));

                        string payLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                       "trans/payRequest.aspx?TransactionId=" + trans.TransactionId);

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

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "trans/rejectMoney.aspx?TransactionId=" + trans.TransactionId +
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
                                Utility.SendEmail(templateToUse,  fromAddress, toAddress, null,
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

                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                              "trans/rejectMoney.aspx?TransactionId=" + trans.TransactionId +
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
                            string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "trans/rejectMoney.aspx?TransactionId=" + TransId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==&TransType=T3EMY1WWZ9IscHIj3dbcNw==");
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
                                Utility.SendEmail(templateToUse,  fromAddress,
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

                    

                    var trans = _dbContext.Transactions.FirstOrDefault(m=>m.Member1.MemberId==MemId
                        && m.TransactionId == TransId && m.TransactionStatus == "Pending" && (m.TransactionType == "5dt4HUwCue532sNmw3LKDQ==" || m.TransactionType == "DrRr1tU1usk7nNibjtcZkA==")
                        ) ;

                    if (trans != null)
                    {
                        #region Setup Variables

                        string senderFirstName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.FirstName));
                        string senderLastName = CommonHelper.UppercaseFirst(CommonHelper.GetDecryptedData(trans.Member.LastName));

                        string linkSource = !String.IsNullOrEmpty(trans.PhoneNumberInvited) ? "Um3I3RNHEGWqKM9MLsQ1lg=="  // "Phone"
                                                                                            : "75U7bZRpVVxLNbQuoMQEGQ=="; // "Email"

                        string rejectLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "trans/rejectMoney.aspx?TransactionId=" + trans.TransactionId +
                                                          "&UserType=U6De3haw2r4mSgweNpdgXQ==" +
                                                          "&LinkSource=" + linkSource +
                                                          "&TransType=DrRr1tU1usk7nNibjtcZkA==");

                        string acceptLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                          "trans/depositMoney.aspx?TransactionId=" + trans.TransactionId.ToString());

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
                        TransactionTrackingId =  CommonHelper.GetRandomTransactionTrackingId(),
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
                            string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/MyAccounts/CancelMoneyRequestt.aspx?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId.ToString());
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
                                Utility.SendEmail("requestSent",fromAddress, toAddress, null,
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
                        string otherlink2 = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "/MyAccounts/rejectRequest.aspx?TransactionId=" + transaction.TransactionId);
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

                    SenderId=
                    
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
                    requestId = transaction.TransactionId.ToString();

                    #region Send Notifications To Both Users

                    #region Setup Notification Variables

                    string RequesterFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.FirstName)).ToString());
                    string RequesterLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(requester.LastName)).ToString());
                    string RequestReceiverFirstName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(sender.FirstName)).ToString());
                    string RequestReceiverLastName = CommonHelper.UppercaseFirst((CommonHelper.GetDecryptedData(sender.LastName)).ToString());

                    string cancelLink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"), "trans/CancelRequest.aspx?TransactionId=" + requestId + "&MemberId=" + requestDto.MemberId + "&UserType=mx5bTcAYyiOf9I5Py9TiLw==");

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

                        Utility.SendEmail(templateToUse_Sender,  fromAddress, toAddress, null,
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
                                                              "trans/rejectMoney.aspx?TransactionId=" + requestId +
                                                              "&UserType=" + userType +
                                                              "&LinkSource=75U7bZRpVVxLNbQuoMQEGQ==" +
                                                              "&TransType=T3EMY1WWZ9IscHIj3dbcNw==");

                            string paylink = String.Concat(Utility.GetValueFromConfig("ApplicationURL"),
                                                           "trans/payRequest.aspx?TransactionId=" + requestId +
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
                                                              "trans/rejectMoney.aspx?TransactionId=" + transaction.TransactionId +
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

                        Utility.SendEmail(templateToUse,  fromAddress, toAddress, null,
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
                                                                                     "T3EMY1WWZ9IscHIj3dbcNw=="))
                    ;
                

                    if (transactionDetail != null)
                    {
                        #region IfSomethingFound

                        transactionDetail.TransactionStatus = "Rejected";
                        _dbContext.SaveChanges();
                        

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

                            if (pageSize == 0 && pageIndex == 0)
                            {

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
                        }
                        else
                        {
                            #region whenSomethingisPassedForSubList

                            if (pageSize == 0 && pageIndex == 0)
                            {

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

                        }

                        totalRecordsCount = transactions.Count();

                        

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
                 
                    var transaction = new Transaction();

                    var transactionType = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);

                    //transactionSpecification.Predicate = entity => entity.Members1.MemberId == id && entity.TransactionType == transactionType;

                    

                    
                    var disputedTransaction =
                        _dbContext.Transactions.Where(t => t.Member1.MemberId == id && ((t.TransactionType == transactionType) && (t.DisputeStatus != null))).OrderByDescending(c => c.DisputeDate).FirstOrDefault();
                    


                    var receivedTransaction =
                       _dbContext.Transactions.Where(entity => entity.Member1.MemberId == id && entity.TransactionType == transactionType ).OrderByDescending(c => c.DisputeDate).FirstOrDefault();

                    if (disputedTransaction != null)
                    {
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
                    var member = _dbContext.Members.Where(u=>u.MemberId==id).FirstOrDefault();

                    //if (member.ClearTransactionHistory.HasValue && member.ClearTransactionHistory.Value)
                    //{
                    //    return new List<Transactions>();
                    //}

                    //membersAccountRepository = new Repository<Members, NoochDataEntities>(noochConnection);
                    // get admin member id.
                    string adminUserName = Utility.GetValueFromConfig("adminMail");
                     
                 
                    var transactions = new List<Transaction>();

                    friendName = CommonHelper.GetEncryptedData(friendName);

                    var transactionTypeTransfer = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_TRANSFER);
                    var transactionTypeDonation = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_DONATION);
                    var transactionTypeRequest = CommonHelper.GetEncryptedData(Constants.TRANSACTION_TYPE_REQUEST);
                    var totalRecordsCount=0;
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
                           
                               totalRecordsCount = _dbContext.Transactions.Where( entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && (entity.TransactionStatus == sublist || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).Count();
                               transactions = _dbContext.Transactions.Where(entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && (entity.TransactionStatus == sublist || entity.TransactionStatus == "Cancelled" || entity.TransactionStatus == "Rejected")).ToList();
                        }
                        else if (listType.ToUpper().Equals("ALL") && sublist == "Pending")
                        {
                            //transactionSpecification.Predicate = entity => (entity.Members.MemberId == id || entity.Members1.MemberId == id) && entity.TransactionStatus == sublist;
                          totalRecordsCount = _dbContext.Transactions.Where( entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) && entity.TransactionStatus == sublist).Count();
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
                            
                            totalRecordsCount = _dbContext.Transactions.Where( entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
                                                                           (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
                                                                            entity.DisputeStatus != null).Count();

                            transactions =  _dbContext.Transactions.Where( entity => (entity.Member.MemberId == id || entity.Member1.MemberId == id) &&
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
                               totalRecordsCount = _dbContext.Transactions.Where( entity => (entity.Member.FirstName.Contains(friendName) || entity.Member.LastName.Contains(friendName)) &&
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

                if (transactions != null)
                {
                    return transactions;
                }
                return new Transaction();
            
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

                            Utility.SendEmail("disputeRaised", supportMail, CommonHelper.GetDecryptedData(transaction.Member.UserName), null, subject, null, tokens, ccCollection, bccCollection, bodyText); //'MailPriority.High' removed this bez of missmatch para -Surya
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

    }
}
