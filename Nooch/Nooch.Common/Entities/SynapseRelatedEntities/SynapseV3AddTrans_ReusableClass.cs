using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3AddTrans_ReusableClass
    {
        public bool success { get; set; }
        public string ErrorMessage { get; set; }
        public SynapseV3AddTrans_Resp responseFromSynapse { get; set; }
    }

    public class SynapseV3AddTrans_Resp
    {
        public bool success { get; set; }
        public SynapseV3AddTrans_Resp_trans trans { get; set; }
        public synapseV3Response_error error { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans
    {
        public SynapseV3AddTrans_Resp_trans_id _id { get; set; }
        public SynapseV3AddTrans_Resp_trans_amount amount { get; set; }

        public SynapseV3AddTrans_Resp_trans_client client { get; set; }
        public SynapseV3AddTrans_Resp_trans_extra extra { get; set; }
        public SynapseV3AddTrans_Resp_trans_fees[] fees { get; set; }
        public SynapseV3AddTrans_Resp_trans_from from { get; set; }
        public SynapseV3AddTrans_Resp_trans_recentstatus recent_status { get; set; }
        public SynapseV3AddTrans_Resp_trans_timeline[] timeline { get; set; }
        public SynapseV3AddTrans_Resp_trans_to to { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_id
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_amount
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_extra
    {
        public string ip { get; set; }
        public string note { get; set; }
        public SynapseV3AddTrans_Resp_trans_extra_date process_on { get; set; }

        public SynapseV3AddTrans_Resp_trans_extra_date created_on { get; set; }
        public string supp_id { get; set; }
        public string webhook { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_extra_date
    {
        public DateTime date { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_fees
    {
        public string fee { get; set; }
        public string note { get; set; }
        public SynapseV3AddTrans_Resp_trans_fees_to to { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_fees_to
    {
        public SynapseV3AddTrans_Resp_trans_fees_to_id id { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_fees_to_id
    {
        public string oid { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_from
    {
        public SynapseNodeId id { get; set; }
        public string type { get; set; }
        public synapseV3_user_reusable_class user { get; set; }
    }

    public class synapseV3_user_reusable_class
    {
        public synapseV3Result_user_id _id { get; set; }
        public string[] legal_names { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_recentstatus
    {
        public SynapseV3AddTrans_Resp_trans_timeline_date date { get; set; }
        public string note { get; set; }
        public string status { get; set; }
        public string status_id { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_timeline
    {
        public SynapseV3AddTrans_Resp_trans_timeline_date date { get; set; }
        public string note { get; set; }
        public string status { get; set; }
        public string status_id { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_timeline_date
    {
        public DateTime date { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_to
    {
        public SynapseNodeId id { get; set; }
        public string type { get; set; }
        public synapseV3_user_reusable_class user { get; set; }
    }

    public class SynapseV3AddTrans_Resp_trans_client
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
