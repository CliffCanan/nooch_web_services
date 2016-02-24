using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseBankLoginv3_Input
    {
        //payload = {'login':{'oauth_key':'hTUCH4kO89qGZDpyEdoq55ODYugwwRsd57ti8ohZ'},'user':{'fingerprint':'suasusau21324redakufejfjsf'},'node':{'type':'ACH-US','info':{'bank_id':'synapse_nomfa','bank_pw':'test1234','bank_name':'bofa'},'extra':{'supp_id':'123sa'}}}

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
}
