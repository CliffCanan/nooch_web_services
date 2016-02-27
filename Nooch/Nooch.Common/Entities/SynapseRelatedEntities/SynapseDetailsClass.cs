using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseDetailsClass
    {
        public SynapseDetailsClass_BankDetails BankDetails { get; set; }
        public SynapseDetailsClass_UserDetails UserDetails { get; set; }

        public bool wereBankDetailsFound { get; set; }
        public bool wereUserDetailsFound { get; set; }

        public string UserDetailsErrMessage { get; set; }
        public string AccountDetailsErrMessage { get; set; }
    }
}
