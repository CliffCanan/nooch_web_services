using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    // RESPONSE CLASS for BOTH **/user/doc/add**  AND  **/user/doc/verify**  AND ** /user/doc/attachment/add **
    public class kycInfoResponseFromSynapse
    {


        public synapseV3Response_message message { get; set; }
        public bool success { get; set; }
        public synapseV3Result_user user { get; set; }

        public synapseV3Response_error error { get; set; }
        public synapseIdVerificationQuestionSet question_set { get; set; } // Only returned for */user/doc/verify* if further verification required

    }
    public class synapseV3Response_message
    {
        public string en { get; set; }
    }
    public class synapseV3Response_error
    {
        public string en { get; set; }
    }

    public class synapseIdVerificationQuestionSet
    {
        public int created_at { get; set; }
        public bool expired { get; set; }
        public string id { get; set; }
        public bool livemode { get; set; }
        public string obj { get; set; }
        public string person_id { get; set; }
        public string score { get; set; }
        public int time_limit { get; set; }
        public int updated_at { get; set; }
        public List<synapseIdVerificationQuestionAnswerSet> questions { get; set; }
    }
    public class synapseIdVerificationQuestionAnswerSet
    {
        public short id { get; set; }
        public string question { get; set; }
        public List<synapseIdVerificationAnswerChoices> answers { get; set; }
    }
    public class synapseIdVerificationAnswerChoices
    {
        public short id { get; set; }
        public string answer { get; set; }
    }
}
