using Nooch.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nooch.Common.Entities.LandingPagesRelatedEntities
{
    public class SynapseDetailsClass_internal
    {
        public SynapseBanksOfMember BankDetails { get; set; }
        public SynapseCreateUserResult UserDetails { get; set; }

        public bool wereBankDetailsFound { get; set; }
        public bool wereUserDetailsFound { get; set; }

        public string UserDetailsErrMessage { get; set; }
        public string AccountDetailsErrMessage { get; set; }
    }
}
