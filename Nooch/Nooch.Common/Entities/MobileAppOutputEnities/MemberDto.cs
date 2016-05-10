using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class MemberDto
    {
        public string MemberId { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public string PhotoUrl { get; set; }
        public string IsRequiredImmediatley { get; set; }
        public string FacebookAccountLogin { get; set; }
        public string ContactNumber { get; set; }
        public bool IsVerifiedPhone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public bool IsSSNAdded { get; set; }
        public DateTime? DateCreated { get; set; }
        public string DateCreatedString { get; set; }
        public decimal? LastLocationLat { get; set; }
        public decimal? LastLocationLng { get; set; }
        public bool? IsSynapseBankAdded { get; set; }
        public string SynapseBankStatus { get; set; }
        public string DeviceToken { get; set; }
        public bool? IsVerifiedWithSynapse { get; set; }
        public string companyName { get; set; }
    }

    public class genericResponse
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public string note { get; set; }
    }

    public class requestFromRentScene
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public string note { get; set; }
        public bool isEmailAlreadyReg { get; set; }
        public bool isBankAttached { get; set; }
        public string bankStatus { get; set; }
        public string name { get; set; }
        public string dateCreated { get; set; }
        public string memberStatus { get; set; }
        public string memberId { get; set; }
    }
}
