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
    
    public partial class SynapseBanksOfMember
    {
        public int Id { get; set; }
        public string account_number_string { get; set; }
        public string bank_name { get; set; }
        public string bankAdddate { get; set; }
        public Nullable<int> bankid { get; set; }
        public Nullable<bool> mfa_verifed { get; set; }
        public string name_on_account { get; set; }
        public string nickname { get; set; }
        public string routing_number_string { get; set; }
        public Nullable<bool> IsDefault { get; set; }
        public Nullable<System.DateTime> AddedOn { get; set; }
        public Nullable<System.Guid> MemberId { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> VerifiedOn { get; set; }
        public string oid { get; set; }
        public string allowed { get; set; }
        public string supp_id { get; set; }
        public string @class { get; set; }
        public string type_bank { get; set; }
        public string type_synapse { get; set; }
        public Nullable<bool> is_active { get; set; }
        public Nullable<bool> IsAddedUsingRoutingNumber { get; set; }
    }
}
