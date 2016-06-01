using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseV3RefreshOauthKeyAndSign_Input
    {
        public createUser_client client { get; set; }
        public createUser_login2 login { get; set; }
        public SynapseV3RefreshOAuthToken_User_Input user { get; set; }
    }

    public class SynapseV3RefreshOAuthToken_User_Input
    {
        public synapseSearchUserResponse_Id1 _id { get; set; }
        public string fingerprint { get; set; }
        public string ip { get; set; }
    }


    // Adding these as separate classes b/c Synapse is retuning an error when I tried
    // using the above after adding 'phone_number' and 'validation_pin'... Synapse doesn't seem to like
    // those being null, so have to make new classes here for the 2FA flow when those values are used
    public class SynapseV3Signin_Input
    {
        public createUser_client client { get; set; }
        public createUser_login2 login { get; set; }
        public SynapseV3Signin_Input_User user { get; set; }
    }

    public class SynapseV3Signin_Input_User
    {
        public synapseSearchUserResponse_Id1 _id { get; set; }
        public string fingerprint { get; set; }
        public string ip { get; set; }
        public string phone_number { get; set; }
        public string validation_pin { get; set; }
    }
}
