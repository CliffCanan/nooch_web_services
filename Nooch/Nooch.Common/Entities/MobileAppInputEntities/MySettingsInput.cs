using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class MySettingsInput
    {
        public bool ShowInSearch { get; set; }
        public bool? IsVerifiedPhone { get; set; }
        public bool IsValidProfile { get; set; }
        public bool IsBankVerified { get; set; }
        public bool AllowPushNotifications { get; set; }
        public bool UseFacebookPicture { get; set; }
        public byte[] Picture { get; set; }

        public AttachmentDto AttachmentFile { get; set; }
        
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
        public string AuthenticationKey { get; set; }
    }

    public class AttachmentDto
    {
        public int ContentLength { get; set; }
        public string MemberId { get; set; }
        public string FileContent { get; set; }
        public string FileExtension { get; set; }
        public string DateOfBirth { get; set; }
        public string AuthenticationKey { get; set; }
    }
}
