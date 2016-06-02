using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseAddKycInfoInputV3Class
    {
        public SynapseV3Input_login login { get; set; }
        public addKycInfoInput_user user { get; set; }
    }
    public class SynapseV3Input_login
    {
        public string oauth_key { get; set; }
    }
    public class SynapseV3Input_user
    {
        public string fingerprint { get; set; }
    }
    public class addKycInfoInput_user
    {
        public string fingerprint { get; set; }
        public addKycInfoInput_user_doc doc { get; set; }
    }

    public class addKycInfoInput_user_doc
    {
        public string birth_day { get; set; }
        public string birth_month { get; set; }
        public string birth_year { get; set; }
        public string name_first { get; set; }
        public string name_last { get; set; }
        public string address_street1 { get; set; }
        public string address_postal_code { get; set; }
        public string address_country_code { get; set; }
        //public string oauth_consumer_key { get; set; }   /// not required  --- 
        public string document_value { get; set; }
        public string document_type { get; set; }
    }


    // Cliff (5/31/16): Adding classes for new Synapse Service for v3/user/docs/add
    public class synapseAddDocsV3InputClass
    {
        public SynapseV3Input_login login { get; set; }
        public synapseAddDocsV3InputClass_user user { get; set; }
    }

    public class synapseAddDocsV3InputClass_user
    {
        public synapseAddDocsV3InputClass_user_docs[] documents { get; set; }
        public string fingerprint { get; set; }
    }

    public class synapseAddDocsV3InputClass_user_docs
    {
        public string email { get; set; }
        public string phone_number { get; set; }
        public string ip { get; set; }
        public string name { get; set; }
        public string alias { get; set; }
        public string entity_type { get; set; }
        public string entity_scope { get; set; }
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public string address_street { get; set; }
        public string address_city { get; set; }
        public string address_subdivision { get; set; } // State
        public string address_postal_code { get; set; }
        public string address_country_code { get; set; }

        public synapseAddDocsV3InputClass_user_docs_doc[] virtual_docs { get; set; }
        public synapseAddDocsV3InputClass_user_docs_doc[] physical_docs { get; set; }
        public synapseAddDocsV3InputClass_user_docs_doc[] social_docs { get; set; }
    }

    public class synapseAddDocsV3InputClass_user_docs_doc
    {
        public string document_value { get; set; }
        public string document_type { get; set; }
    }
}
