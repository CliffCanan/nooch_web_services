using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class PhoneEmailListDto
    {
        public PhoneEmailMemberPair[] phoneEmailList { get; set; }
    }
    public class PhoneEmailMemberPair
    {
        public string phoneNo { get; set; }
        public string emailAddy { get; set; }
        public string memberId { get; set; }
    }
}
