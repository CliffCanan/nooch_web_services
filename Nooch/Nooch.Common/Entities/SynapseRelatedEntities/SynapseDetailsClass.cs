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
     
    public class BankDetailsForMobile
    {
        public bool wereBankDetailsFound { get; set; }
        public bool wereUserDetailsFound { get; set; }
        
        public string errorMsg { get; set; }
        public string bankName { get; set; }
        public string bankNickname { get; set; }
        public string bankStatus { get; set; }
        public string bankAllowed { get; set; }
        public string bankAddedDate { get; set; }
        public string bankVerifiedDate { get; set; }

        public string userPermission { get; set; }
        public string physDocStatus { get; set; }
        public string virtualDocStatus { get; set; }
        public string socDocStatus { get; set; }
        public string cip { get; set; }
    }
}
