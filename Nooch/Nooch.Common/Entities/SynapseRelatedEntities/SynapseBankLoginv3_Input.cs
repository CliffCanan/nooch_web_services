using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseBankLoginv3_Input
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseBankLoginV3_Input_node node { get; set; }
    }

    public class SynapseBankLoginV3_Input_node
    {
        public string type { get; set; }
        public SynapseBankLoginV3_Input_bankInfo info { get; set; }
        public SynapseBankLoginV3_Input_extra extra { get; set; }
    }

    public class SynapseBankLoginV3_Input_bankInfo
    {
        public string bank_id { get; set; }
        public string bank_pw { get; set; }
        public string bank_name { get; set; }
    }

    public class SynapseBankLoginV3_Input_extra
    {
        public string supp_id { get; set; }
    }


    // For Adding a Node with Routing / Account number
    public class SynapseBankLoginUsingRoutingAndAccountNumberv3_Input
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node node { get; set; }
    }

    public class SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_node
    {
        public string type { get; set; }
        public SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo info { get; set; }
        public SynapseBankLoginV3_Input_extra extra { get; set; }
    }

    public class SynapseBankLoginUsingRoutingAndAccountNumberV3_Input_bankInfo
    {
        public string nickname { get; set; }
        public string account_num { get; set; }
        public string routing_num { get; set; }
        public string type { get; set; }
        //public string name_on_account { get; set; } // Added by CC (12/6/16), but not necessary: Synapse ignores this and responds with the user's name provided at account creation for this field.
        [JsonProperty(PropertyName = "class")]
        public string _class { get; set; }
    }

}
