using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class userDetailsForMobileApp
    {
        public bool hasSynapseUserAccount { get; set; }
        public bool hasSynapseBank { get; set; }
        public bool isBankVerified { get; set; }
        public bool isProfileComplete { get; set; }
        public bool isVerifiedPhone { get; set; }
        public bool isRequiredImmediately { get; set; }
        public bool showInSearch { get; set; }
        public bool rememberMe { get; set; }

        public byte[] Picture { get; set; }

        public string memberId { get; set; }
        public string status { get; set; }
        public string email { get; set; }
        public string contactNumber { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string userPicture { get; set; }
        public string pin { get; set; }
        public string fbUserId { get; set; }

        public string bankStatus { get; set; }
        public string synUserPermission { get; set; }
        public string synBankAllowed { get; set; }
        public DateTime DateCreated { get; set; }
    }


    public class MySettingsInput
    {
        public bool ShowInSearch { get; set; }
        public bool? IsVerifiedPhone { get; set; }
        public bool? IsVerifiedEmail { get; set; }
        //public bool IsValidProfile { get; set; }
        //public bool IsBankVerified { get; set; }
        public bool UseFacebookPicture { get; set; }
        public bool IsSsnAdded { get; set; }

        public byte[] Picture { get; set; }

        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string Country { get; set; }
        public string ContactNumber { get; set; }
        public string DateOfBirth { get; set; }
        public string MemberId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string SecondaryMail { get; set; }
        public string RecoveryMail { get; set; }
        public string FacebookAcctLogin { get; set; }
        public string Photo { get; set; }
        public string PinNumber { get; set; }
    }
}
