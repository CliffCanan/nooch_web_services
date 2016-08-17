using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
  public  class synapseSetSubscription_int
    {
      public  client client{get;set;}
      public string url { get; set; }
      public string[] scope { get; set; }
    }

    public class client{

        public string client_id { get; set; }
        public string client_secret { get; set; }

    }
}
