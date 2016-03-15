using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseBankVerifyV3_Input
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseBankVerifyV3_Input_node node { get; set; }
    }
    public class SynapseBankVerifyV3_Input_node
    {
        public SynapseNodeId _id { get; set; }
        public SynapseBankVerifyV3_Input_node_verify verify { get; set; }
    }

    public class SynapseNodeId
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    public class SynapseBankVerifyV3_Input_node_verify
    {
        public string mfa { get; set; }
    }



    public class SynapseBankVerifyWithMicroDepositsV3_Input
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseBankVerifyV3WithMicroDesposits_Input_node node { get; set; }
    }
    public class SynapseBankVerifyV3WithMicroDesposits_Input_node
    {
        public SynapseNodeId _id { get; set; }
        public SynapseBankVerifyV3WithMicroDesposits_Input_node_verify verify { get; set; }


        
    }

    public class SynapseBankVerifyV3WithMicroDesposits_Input_node_verify
    {
        public string[] micro { get; set; }   // not sure if  I should keep a float type error.... - Malkit
    }


}
