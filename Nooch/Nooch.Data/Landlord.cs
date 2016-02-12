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
    
    public partial class Landlord
    {
        public System.Guid LandlordId { get; set; }
        public Nullable<System.Guid> MemberId { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Nullable<System.DateTime> DateOfBirth { get; set; }
        public string MobileNumber { get; set; }
        public Nullable<bool> IsPhoneVerified { get; set; }
        public string eMail { get; set; }
        public Nullable<bool> IsEmailVerfieid { get; set; }
        public string AddressLineOne { get; set; }
        public string AddressLineTwo { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        public string SSN { get; set; }
        public string UserPic { get; set; }
        public string IpAddresses { get; set; }
        public string FBId { get; set; }
        public string TwitterHandle { get; set; }
        public string InstagramUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEIN { get; set; }
        public string CompanyAddressLineOne { get; set; }
        public string CompanyeAddressLineTwo { get; set; }
        public string CompanyCity { get; set; }
        public string CompanyState { get; set; }
        public string CompaZip { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public string WebAccessToken { get; set; }
        public Nullable<System.DateTime> LastSeenOn { get; set; }
        public Nullable<bool> IsAnyRentReceived { get; set; }
        public Nullable<bool> IsIdVerified { get; set; }
        public string FacebookUserId { get; set; }
        public Nullable<int> MemoFormula { get; set; }
    }
}
