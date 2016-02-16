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
        #endregion
    }
}
