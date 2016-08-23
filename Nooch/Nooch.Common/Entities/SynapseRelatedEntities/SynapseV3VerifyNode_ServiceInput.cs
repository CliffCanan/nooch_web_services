using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3VerifyNode_ServiceInput
    {
        public string BankName { get; set; }
        public string MemberId { get; set; }
        public string mfaResponse { get; set; }
        public string bankId { get; set; }
    }

    public class SynapseV3VerifyNodeWithMicroDeposits_ServiceInput
    {
        public bool success { get; set; }
        public bool isAlreadyVerified { get; set; }
        public string verifiedDate { get; set; }
        public string userFirstName { get; set; }
        public string userLastName { get; set; }
        public string bankName { get; set; }
        public string bankNickName { get; set; }
        public string MemberId { get; set; }
        public string microDespositOne { get; set; }
        public string microDespositTwo { get; set; }
        public string bankId { get; set; }
        public string errorMsg { get; set; }
        public string IsRs { get; set; }
        public string NodeId1 { get; set; }
        public List<PendingTransaction> PendingTransactionList{ get; set; }
    }

    public class PendingTransaction
    {
        public System.Guid TransactionId { get; set; }
        public System.Guid SenderId { get; set; }
        public Nullable<System.Guid> RecipientId { get; set; }
        public string userName { get; set; }
        public decimal Amount { get; set; }
        public Nullable<System.DateTime> TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string InvitationSentTo { get; set; }
        
    }
}
