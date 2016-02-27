using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseV3ShowUserInput
    {


        public synapseSearchUser_Client client { get; set; }

        public synapseSearchUserResponse_Id1 node { get; set; }
        public SynapseV3Input_login login { get; set; }
        
        
    }


}
