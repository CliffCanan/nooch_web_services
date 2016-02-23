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
}
