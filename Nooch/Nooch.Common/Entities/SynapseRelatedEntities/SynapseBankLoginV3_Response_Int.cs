using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseBankLoginV3_Response_Int
    {
        public bool Is_MFA { get; set; }
        public bool Is_success { get; set; }

        public string errorMsg { get; set; }

        public RootBankObject SynapseNodesList { get; set; }
        public string mfaMessage { get; set; }
    }

    public class RootBankObject
    {
        public string error_code { get; set; }
        public string http_code { get; set; }
        public nodes[] nodes { get; set; }
        public bool success { get; set; }
    }

    public class nodes
    {
        public _id _id { get; set; }
        public string allowed { get; set; }
        public extra extra { get; set; }
        public info info { get; set; }
        public bool is_active { get; set; }
        public string type { get; set; }
    }

    public class _id
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    public class extra
    {
        public string supp_id { get; set; }
        public extra_mfa mfa { get; set; }
    }

    public class extra_mfa
    {
        public string message { get; set; }
    }

    public class info
    {
        public string account_num { get; set; }
        public balance balance { get; set; } // Only returned for Synapse Node Type (not ACH nodes)
        public string bank_name { get; set; }

        [JsonProperty(PropertyName = "class")]
        public string _class { get; set; }

        public string name_on_account { get; set; }
        public string nickname { get; set; }
        public string routing_num { get; set; }
        public string type { get; set; }
    }

    public class balance
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }
}
