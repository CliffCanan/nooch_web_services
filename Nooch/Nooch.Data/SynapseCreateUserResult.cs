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
    
    public partial class SynapseCreateUserResult
    {
        public int Id { get; set; }
        public Nullable<System.Guid> MemberId { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string reason { get; set; }
        public string refresh_token { get; set; }
        public Nullable<bool> success { get; set; }
        public string username { get; set; }
        public string user_id { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<bool> IsForNonNoochUser { get; set; }
        public string NonNoochUserEmail { get; set; }
        public Nullable<System.Guid> TransactionIdFromWhichInvited { get; set; }
        public Nullable<bool> HasNonNoochUserSignedUp { get; set; }
        public Nullable<System.Guid> MemberIdAfterSignup { get; set; }
        public Nullable<bool> is_business { get; set; }
        public string legal_name { get; set; }
        public string permission { get; set; }
        public string Phone_number { get; set; }
        public string photos { get; set; }
    }
}
