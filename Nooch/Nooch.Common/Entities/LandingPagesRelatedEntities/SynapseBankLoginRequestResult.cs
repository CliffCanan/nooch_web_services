using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.LandingPagesRelatedEntities
{
    public class SynapseBankLoginRequestResult
    {
        public bool Is_MFA { get; set; }
        public bool Is_success { get; set; }
        public string ssn_verify_status { get; set; }
        public string mfaMessage { get; set; }
        public string bankoid { get; set; }
        public string ERROR_MSG { get; set; }
        public SynapseBanksListClass SynapseBanksList { get; set; }
        public SynapseQuestionBasedMFAClass SynapseQuestionBasedResponse { get; set; } // Not sure this is still needed with Synapse V3
        public bool IsBankManulAdded { get; set; }
    }

    public class BankLoginResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ssn_verify_status { get; set; }
    }

    public class SynapseMessageClass
    {
        public string message { get; set; }
    }

    public class SynapseCodeBasedMFAResponseIntClass
    {
        public string access_token { get; set; }
        public string type { get; set; }
        public SynapseMessageClass mfa { get; set; }
    }

    public class SynapseQuestionBasedMFAClass
    {
        public bool is_mfa { get; set; }
        public bool success { get; set; }
        public SynapseQuestionBasedMFAResponseIntClass response { get; set; }
    }

    public class SynapseQuestionClass
    {
        public string question { get; set; }
    }

    public class SynapseQuestionBasedMFAResponseIntClass
    {
        public string access_token { get; set; }
        public string type { get; set; }
        public SynapseQuestionClass[] mfa { get; set; }
    }

    public class SynapseBanksListClass
    {
        public List<SynapseBankClass> banks { get; set; }
        public bool success { get; set; }
    }

    public class SynapseBankClass
    {
        public string account_class { get; set; }
        public string account_number_string { get; set; }
        public string account_type { get; set; }
        public string balance { get; set; }
        public string bank_name { get; set; }
        public string date { get; set; }
        public int id { get; set; }
        public string bankoid { get; set; }
        public bool is_active { get; set; }
        public bool is_verified { get; set; }
        public bool mfa_verifed { get; set; }
        public string name_on_account { get; set; }
        public string nickname { get; set; }
        public string resource_uri { get; set; }
        public string routing_number_string { get; set; }
    }
}
