using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.LandingPagesRelatedEntities
{
    public class TransactionsPageData
    {
        public List<TransactionClass> allTransactionsData { get; set; }
        public bool isSuccess { get; set; }
        public string msg { get; set; }
        public string usersName { get; set; }
        public string usersEmail { get; set; }
        public string usersPhoto { get; set; }
        public string memId { get; set; }
    }

    public class TransactionClass
    {
        public System.Guid TransactionId { get; set; }
        public System.Guid? SenderId { get; set; }
        public System.Guid? RecipientId { get; set; }

        //public string NoochId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public double? TransLati { get; set; }
        public double? TransLongi { get; set; }
        public double? TransAlti { get; set; }
        public string DateAccepted { get; set; }
        public string SenderName { get; set; }
        public string SenderNoochId { get; set; }
        public string RecepientNoochId { get; set; }
        public string RecipientName { get; set; }
        public string TransactionDate1 { get; set; }
        public string TransactionTime { get; set; }
        public string TransactionStatus { get; set; }
        public string TransactionType { get; set; }
        public string TransactionTrackingId { get; set; }
        public string AdminNotes { get; set; }
        public string Subject { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string InvitationSentTo { get; set; }
        public string FirstName { get; set; }
        public string Memo { get; set; }
        public string SynapseStatus { get; set; }
        public string SynapseStatusNote { get; set; }
    }
}
