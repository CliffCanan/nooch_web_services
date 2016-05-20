//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Nooch.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class Transaction
    {
        public System.Guid TransactionId { get; set; }
        public System.Guid SenderId { get; set; }
        public Nullable<System.Guid> RecipientId { get; set; }
        public string DeviceId { get; set; }
        public string DisputeTrackingId { get; set; }
        public decimal Amount { get; set; }
        public Nullable<System.DateTime> TransactionDate { get; set; }
        public bool IsPrepaidTransaction { get; set; }
        public Nullable<decimal> TransactionFee { get; set; }
        public string DisputeStatus { get; set; }
        public string TransactionStatus { get; set; }
        public string TransactionType { get; set; }
        public Nullable<System.Guid> LocationId { get; set; }
        public Nullable<System.DateTime> DisputeDate { get; set; }
        public Nullable<System.DateTime> ReviewDate { get; set; }
        public Nullable<System.DateTime> ResolvedDate { get; set; }
        public string TransactionTrackingId { get; set; }
        public string AdminNotes { get; set; }
        public string AdminName { get; set; }
        public string Subject { get; set; }
        public string RaisedBy { get; set; }
        public Nullable<System.Guid> RaisedById { get; set; }
        public string Memo { get; set; }
        public byte[] Picture { get; set; }
        public string InvitationSentTo { get; set; }
        public Nullable<bool> IsPhoneInvitation { get; set; }
        public string PhoneNumberInvited { get; set; }
        public string SynapseStatus { get; set; }
        public Nullable<System.DateTime> DateAccepted { get; set; }
    
        public virtual GeoLocation GeoLocation { get; set; }
        public virtual Member Member { get; set; }
        public virtual Member Member1 { get; set; }
    }
}
