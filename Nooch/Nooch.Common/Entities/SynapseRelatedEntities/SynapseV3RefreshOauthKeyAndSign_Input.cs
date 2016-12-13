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
    // those being null, so have to make new classes here for the 2FA flow when those values are used.
    // Next two classes are for submitting to /user/signin WITHOUT the validation_pin
    public class SynapseV3Signin_InputNoPin
    {
        public createUser_client client { get; set; }
        public createUser_login2 login { get; set; }
        public SynapseV3Signin_Input_UserNoPin user { get; set; }
    }

    public class SynapseV3Signin_Input_UserNoPin
    {
        public synapseSearchUserResponse_Id1 _id { get; set; }
        public string fingerprint { get; set; }
        public string ip { get; set; }
        public string phone_number { get; set; }
    }

    // Next two classes are for submitting to /user/signin WITH the validation_pin
    public class SynapseV3Signin_InputWithPin
    {
        public createUser_client client { get; set; }
        public createUser_login2 login { get; set; }
        public SynapseV3Signin_Input_UserWithPin user { get; set; }
    }

    public class SynapseV3Signin_Input_UserWithPin
    {
        public synapseSearchUserResponse_Id1 _id { get; set; }
        public string fingerprint { get; set; }
        public string ip { get; set; }
        public string phone_number { get; set; }
        public string validation_pin { get; set; }
    }


    public class SynapseSearchInput
    {
        public createUser_client client { get; set; }
        public SynapseSearchInput_Filter filter { get; set; }
    }

    public class SynapseSearchInput_Filter
    {
        public int page = 1;
        public string query { get; set; }
    }
}
