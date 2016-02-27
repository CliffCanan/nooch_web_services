using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseV3checkUsersOauthKey
    {
        public string oauth_consumer_key { get; set; }
        public bool success { get; set; }
        public string msg { get; set; }
    }
}
