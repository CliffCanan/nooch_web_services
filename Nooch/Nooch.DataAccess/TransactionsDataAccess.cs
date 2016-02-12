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
    }
}
