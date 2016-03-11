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



    // to be used with add bank node with routing and account number
    public class SynapseBankLoginUsingRoutingAndAccountNumberv3_Input
    {
        //payload = {'login':{'oauth_key':'hTUCH4kO89qGZDpyEdoq55ODYugwwRsd57ti8ohZ'},'user':{fingerprint':'suasusau21324redakufejfjsf'},node':{'type':'WIRE-US','info':{'nickname':'Some Account','name_on_account':'Some Name','account_num':123567443,'routing_num':026009593,'bank_name':'Bank of America',address':'452 Fifth Ave, NY',#optional field, supply only if the account goes via a correspondent bank'correspondent_info':{'routing_num':026009593,'bank_name':'Bank of America','address':'452 Fifth Ave, NY'}},'extra':{'supp_id':'123sa'}}}
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
       
        [JsonProperty(PropertyName = "class")]
        public string _class { get; set; }
    }

}
