using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseCreateUserInput_int
    {
        public createUser_client client { get; set; }
        public createUser_login[] logins { get; set; }
        public string[] phone_numbers { get; set; }
        public string[] legal_names { get; set; }
        public createUser_fingerprints[] fingerprints { get; set; }
        public string[] ips { get; set; }
        public createUser_extra extra { get; set; }
    }

    public class createUser_client
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }
    public class createUser_login
    {
        public string email { get; set; }
        //public string password { get; set; }
        public bool read_only { get; set; }
    }

    public class createUser_login2
    {
        public string email { get; set; }
        //public string password { get; set; }
        public string refresh_token { get; set; }
    }

    public class createUser_fingerprints
    {
        public string fingerprint { get; set; }
    }

    public class createUser_extra
    {
        public string note { get; set; }
        public string supp_id { get; set; }
        public bool is_business { get; set; }
    }
}
