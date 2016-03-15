using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    class synapseClassesMemDataAccess
    {

    }

         public class synapseV2checkUsersOauthKey
        {
            public string oauth_consumer_key { get; set; }
            public bool success { get; set; }
            public string msg { get; set; }
        }

        public class synapseV2refreshOauthKey
        {
            public string refresh_token { get; set; }
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }

        public class RegisterUserSynapseResultClassint
        {
            public string oauth_consumer_key { get; set; }
            public string expires_in { get; set; }
            public string expires_at { get; set; }
            public string reason { get; set; }
            public string refresh_token { get; set; }
            public string success { get; set; }
            public string user_id { get; set; }
            public string username { get; set; }
            public string memberIdGenerated { get; set; }
            public string ssn_verify_status { get; set; }
        }

        // (7/28/15) Added by Cliff
        public class synapseV2ShowUserInput
        {
            public string oauth_consumer_key { get; set; }
        }

      
    }

