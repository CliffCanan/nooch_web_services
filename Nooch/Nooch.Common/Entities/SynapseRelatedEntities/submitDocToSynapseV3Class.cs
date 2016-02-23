using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class submitDocToSynapseV3Class
    {
        public SynapseV3Input_login login { get; set; }
        public submitDocToSynapse_user user { get; set; }
    }
    public class submitDocToSynapse_user
    {
        public submitDocToSynapse_user_doc doc { get; set; }
        public string fingerprint { get; set; }
    }
    public class submitDocToSynapse_user_doc
    {
        public object attachment { get; set; } // this should be a Base64 encoded image
    }
}
