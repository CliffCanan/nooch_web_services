using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
   public class synapseSetSubscription_out
    {
       public string error_code { get; set; }
       public string http_code { get; set; }
       public subscription subscription {get;set;}
       public string success { get; set; }  
    }

public class subscription{
    public synapseSetSubscription_out_id _id { get; set; }
    public synapseSetSubscription_out_client client { get; set; }
    public bool is_active { get; set; }
    public string[] scope { get; set; }
    public string url { get; set; }
}
public class synapseSetSubscription_out_id
{
    public string oid { set; get; }

}

public class synapseSetSubscription_out_client
{
    public string id { get; set; }
    public string name { get; set; }
}

}
