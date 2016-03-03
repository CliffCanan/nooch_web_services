using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseRemoveBankV3_Input
    {
        public SynapseV3Input_login login { get; set; }
        public SynapseV3Input_user user { get; set; }
        public SynapseRemoveBankV3_Input_node node { get; set; }
    }

    public class SynapseRemoveBankV3_Input_node
    {
        public SynapseNodeId _id { get; set; }
    }
}
