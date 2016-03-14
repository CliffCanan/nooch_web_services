using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3VerifyNode_ServiceInput
    {
        public string BankName { get; set; }   // this is not required I guess... still leaving it..Cliff might have added in mda method with some thouhgt to use it somewhere.

        
        public string MemberId { get; set; }

        
        public string mfaResponse { get; set; }

        
        public string bankId { get; set; }    // this would be bankoid which is sent in response to add node service response.
    }

    public class SynapseV3VerifyNodeWithMicroDeposits_ServiceInput
    {
        public string BankName { get; set; }   // this is not required I guess... still leaving it..Cliff might have added in mda method with some thouhgt to use it somewhere.


        public string MemberId { get; set; }


        public string microDespositOne { get; set; }
        public string microDespositTwo { get; set; }


        public string bankId { get; set; }    // this would be bankoid which is sent in response to add node service response.
    }
}
