using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class MemberRegistrationInputDto
    {
        public byte[] Picture { get; set; }
        public string Photo { get; set; }
        public string UdId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PinNumber { get; set; }
        public string Password { get; set; }
        public string SecondaryMail { get; set; }
        public string RecoveryMail { get; set; }
        public string deviceTokenId { get; set; }
        public string friendRequestId { get; set; }
        public string invitedFriendFacebookId { get; set; }
        public string facebookAccountLogin { get; set; }
        public string inviteCode { get; set; }
        public string sendEmail { get; set; }
        public string type { get; set; }
    }
}
