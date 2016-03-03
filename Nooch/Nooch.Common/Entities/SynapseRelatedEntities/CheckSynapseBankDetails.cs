using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class CheckSynapseBankDetails
    {
        
        public string Message { get; set; }

       
        public bool IsBankFound { get; set; }

        
        public bool IsPinRequired { get; set; }

         
        public string mfa_type { get; set; }
    }
}
