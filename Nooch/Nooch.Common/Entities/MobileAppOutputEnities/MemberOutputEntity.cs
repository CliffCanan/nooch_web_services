using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class MemberOutputEntity
    {
        public string MemberId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UdId1 { get; set; }
        public string UdId2 { get; set; }
        public string Password { get; set; }
        public string PinNumber { get; set; }
        public string SecondaryEmail { get; set; }
        public bool RememberMeEnabled { get; set; }
        public bool LocationCapture { get; set; }
        public string RecoveryMail { get; set; }
        public string FacebookLogin { get; set; }
    }
}
