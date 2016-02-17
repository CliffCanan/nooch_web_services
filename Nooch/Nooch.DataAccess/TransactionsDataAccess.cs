using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nooch.Common;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Resources;
using Nooch.Data;

namespace Nooch.DataAccess
{
    public class TransactionsDataAccess
    {
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
        #endregion




    }
}
