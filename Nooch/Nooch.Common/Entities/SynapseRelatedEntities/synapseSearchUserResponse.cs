﻿using System;
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


    public class synapseV3ShowUserInputClass
    {
        public synapseSearchUser_Client client { get; set; }
        public synapseV3ShowUser_Filter filter { get; set; }
    }

    public class synapseV3ShowUser_Filter
    {
        public int page { get; set; }
        public string query { get; set; }
    }



    // ADDED 12/12/16 AS PART OF SYNAPSE UPDATES FOR NEW NOOCH CIP CONTRACT

    public class synapseSearchResponse
    {
        public string error_code { get; set; }
        public string http_code { get; set; }
        public bool success { get; set; }
        public int page { get; set; }
        public int page_count { get; set; }
        public int users_count { get; set; }
        public addDocsResFromSynapse_user[] users { get; set; }
    }

    /*public class synapseSearchResponse_User
    {
        public synapseSearchUserResponse_Id _id { get; set; }
        public synapseSearchUserResponse_Client client { get; set; }
        public synapseSearchResponse_User_doc_status doc_status { get; set; }

        public object[] emails { get; set; }
        public string[] legal_names { get; set; }
        public synapseSearchUserResponse_Node[] nodes { get; set; }
        public object[] photos { get; set; }
    }

    public class synapseSearchResponse_User_doc_status
    {
        public string physical_doc { get; set; }
        public string virtual_doc { get; set; }
    }

    public class synapseSearchResponse_User_documents
    {
        public string id { get; set; }
        public string name { get; set; }
        public string permission_scope { get; set; }
    }*/
}
