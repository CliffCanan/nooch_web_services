using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseSearchUserResponse
    {
        public string error_code { get; set; }
        public string http_code { get; set; }
        public string errorMsg { get; set; }
        public int page { get; set; }
        public int page_count { get; set; }
        public bool success { get; set; }
        public int user_count { get; set; }
        public synapseSearchUserResponse_User[] users { get; set; }
    }

    public class synapseSearchUserResponse_User
    {
        public synapseSearchUserResponse_Id _id { get; set; }
        public synapseSearchUserResponse_Client client { get; set; }
        public object[] emails { get; set; }
        public string[] legal_names { get; set; }
        public synapseSearchUserResponse_Node[] nodes { get; set; }
        public object[] photos { get; set; }
    }

    public class synapseSearchUserResponse_Id
    {
        public string oid { get; set; }
    }

    public class synapseSearchUserResponse_Client
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class synapseSearchUserResponse_Node
    {
        public synapseSearchUserResponse_Id1 _id { get; set; }
        public string allowed { get; set; }
        public synapseSearchUserResponse_Info info { get; set; }
        public bool is_active { get; set; }
        public string type { get; set; }
    }
    public class synapseSearchUserResponse_Id1
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    public class synapseSearchUserResponse_Info
    {
        public string nickname { get; set; }
    }


    // synapse search user related classes

    public class synapseSearchUserInputClass
    {
        public synapseSearchUser_Client client { get; set; }
        public synapseSearchUser_Filter filter { get; set; }
    }
    public class synapseSearchUser_Client
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }

    public class synapseSearchUser_Filter
    {
        public int page { get; set; }
        public bool exact_match { get; set; }
        public string query { get; set; }
    }
}
