using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseDetailsClass_BankDetails
    {
        public string bankid { get; set; } // Don't really need this anymore, V3 uses bank_oid below, but updating this from an int? to a string and keeping it so nothing breaks
        public string bank_oid { get; set; }
        public string Status { get; set; }

        public string allowed { get; set; }
        public string bankType { get; set; }
        public string synapseType { get; set; }
        public string dateVerified { get; set; }

        public DateTime? AddedOn { get; set; }
    }
    public class SynapseDetailsClass_UserDetails
    {
        public string access_token { get; set; }
        public string MemberId { get; set; }
        public string user_id { get; set; }

        public string user_fingerprints { get; set; }
    }
}
