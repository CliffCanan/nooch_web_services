using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3AddTransInput
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseV3AddTransInput_trans trans { get; set; }
    }

    public class SynapseV3AddTransInput_trans
    {
        public SynapseV3AddTransInput_trans_from from { get; set; }
        public SynapseV3AddTransInput_trans_to to { get; set; }
        public SynapseV3AddTransInput_trans_amount amount { get; set; }
        public SynapseV3AddTransInput_trans_extra extra { get; set; }
        public SynapseV3AddTransInput_trans_fees[] fees { get; set; }
    }

    public class SynapseV3AddTransInput_trans_from
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class SynapseV3AddTransInput_trans_to
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class SynapseV3AddTransInput_trans_amount
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }

    public class SynapseV3AddTransInput_trans_extra
    {
        public string supp_id { get; set; }
        public string note { get; set; }
        public string webhook { get; set; }
        public int process_on { get; set; }
        public string ip { get; set; }
    }

    public class SynapseV3AddTransInput_trans_fees
    {
        public string fee { get; set; }
        public string note { get; set; }
        public SynapseV3AddTransInput_trans_fees_to to { get; set; }
    }

    public class SynapseV3AddTransInput_trans_fees_to
    {
        public string id { get; set; }
    }

}
