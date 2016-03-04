using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class SaveMemberDOB_Input
    {
        public string memberId { get; set; }
        public string DOB { get; set; }
        public string accessToken { get; set; }

        
    }

    public class SaveMemberSSN_Input
    {
        public string memberId { get; set; }
        public string SSN { get; set; }
        public string accessToken { get; set; }


    }
}
