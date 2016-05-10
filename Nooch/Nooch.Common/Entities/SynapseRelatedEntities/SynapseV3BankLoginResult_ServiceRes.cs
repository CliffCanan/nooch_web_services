using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3BankLoginResult_ServiceRes
    {
        public bool Is_MFA { get; set; }
        public bool Is_success { get; set; }
        public string bankOid { get; set; }
        public string errorMsg { get; set; }
        public string mfaQuestion { get; set; }
        public string bankMFA { get; set; }
        public SynapseNodesListClass SynapseNodesList { get; set; }
    }

    public class SynapseNodesListClass
    {
        public List<SynapseIndividualNodeClass> nodes { get; set; }
        public bool success { get; set; }
    }

    public class SynapseIndividualNodeClass
    {
        public string account_class { get; set; }
        public string account_num { get; set; }
        public string routing_num { get; set; }
        public int account_type { get; set; }
        public string bank_name { get; set; }
        public string date { get; set; }
        public string oid { get; set; }
        public bool is_active { get; set; }
        public bool is_verified { get; set; }
        public bool mfa_verifed { get; set; }
        public string name_on_account { get; set; }
        public string nickname { get; set; }
    }
}
