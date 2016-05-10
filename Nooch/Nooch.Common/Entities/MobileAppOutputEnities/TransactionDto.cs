using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class TransactionDto
    {
        public string PinNumber { get; set; }
        public string MemberId { get; set; }
        public string NoochId { get; set; }
        public string RecepientId { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public string Name { get; set; }
        public string RecepientName { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public string TransactionDate { get; set; }
        public bool IsPrePaidTransaction { get; set; }
        public string DeviceId { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string DisputeStatus { get; set; }
        public string DisputeId { get; set; }
        public string DisputeReportedDate { get; set; }
        public string DisputeReviewDate { get; set; }
        public string DisputeResolvedDate { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public string Photo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SenderPhoto { get; set; }
        public string RecepientPhoto { get; set; }
        public string synapseTransResult { get; set; }
        public string InvitationSentTo { get; set; }
        public string PhoneNumberInvited { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string LocationId { get; set; }
        public string Location { get; set; }
        public int TotalRecordsCount { get; set; }
        public DateTime TransDate { get; set; }
        public decimal? TransactionFee { get; set; }
        public string AdminNotes { get; set; }
        public string RaisedBy { get; set; }
        public byte[] Picture { get; set; }
        public string BankAccountId { get; set; }
        public string BankId { get; set; }
        public byte[] BankPicture { get; set; }
        public string BankName { get; set; }
        public bool IsPhoneInvitation { get; set; }
        public bool IsExistingButNonRegUser { get; set; }
        public bool doNotSendEmails { get; set; }
        public bool isRentAutoPayment { get; set; }
        public string SsnIsVerified { get; set; }
    }
}
