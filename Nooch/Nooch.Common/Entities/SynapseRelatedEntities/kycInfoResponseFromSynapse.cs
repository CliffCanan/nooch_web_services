using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    // RESPONSE CLASS for  **/user/doc/add**  AND  **/user/doc/verify**  AND ** /user/doc/attachment/add **
    public class kycInfoResponseFromSynapse
    {
        public bool success { get; set; }
        public synapseV3Response_message message { get; set; }
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

    public class synapseIdVerificationAnswersInput
    {
        public SynapseV3Input_login login { get; set; }
        public synapseSubmitIdAnswers_answers_input user { get; set; }
    }

    public class synapseSubmitIdAnswers_answers_input
    {
        //public SynapseV3Input_login login { get; set; }
        public synapseSubmitIdAnswers_docSet doc { get; set; }
        public string fingerprint { get; set; }
    }

    public class synapseSubmitIdAnswers_docSet
    {
        public string question_set_id { get; set; }
        public synapseSubmitIdAnswers_Input_quest[] answers { get; set; }
    }

    public class synapseSubmitIdAnswers_Input_quest
    {
        public int question_id { get; set; }
        public int answer_id { get; set; }
    }



    // CC (5/31/16): New RESPONSE CLASS for /user/docs/add
    public class addDocsResFromSynapse
    {
        public bool success { get; set; }
        public string error_code { get; set; }
        public string http_code { get; set; }
        public addDocsResFromSynapse_user user { get; set; }

        public synapseV3Response_error error { get; set; }
    }

    public class addDocsResFromSynapse_user
    {
        public synapseV3Result_user_id _id { get; set; }  // same as before
        public synapseV3Result_user_client client { get; set; } // same as before
        public synapseV3Result_user_docStatus doc_status { get; set; } // same as before
        public synapseV3Result_user_extra extra { get; set; } // same as before (plust 'cip_tag')

        // (CC: 6/1/16) - NEW
        public addDocsResFromSynapse_user_docs[] documents { get; set; }

        public string[] legal_names { get; set; }
        public synapseV3Result_user_logins[] logins { get; set; }  // same as before (plust 'scope')
        public bool is_hidden { get; set; } // same as before

        public string permission { get; set; }  // same as before
        public string[] photos { get; set; } // same as before
        public string[] phone_numbers { get; set; } // same as before
    }

    public class addDocsResFromSynapse_user_docs
    {
        public string id { get; set; }
        public string name { get; set; }
        public string permission_scope { get; set; }
        public addDocsResFromSynapse_user_docs_doc[] physical_docs { get; set; }
        public addDocsResFromSynapse_user_docs_doc[] social_docs { get; set; }
        public addDocsResFromSynapse_user_docs_virtualdoc[] virtual_docs { get; set; }
    }

    public class addDocsResFromSynapse_user_docs_doc // Used for Social & Physical Doc Objects
    {
        public string id { get; set; }
        public string document_type { get; set; }
        public string status { get; set; }
        // LEAVING OUT DATE FOR NOW... NOT SURE ABOUT THE SYNTAX FOR '$date'
    }

    public class addDocsResFromSynapse_user_docs_virtualdoc // Used for Virtual Doc Objects - separate since this is the only one that could have a Question_Set inside a 'meta' object
    {
        public string id { get; set; }
        public string document_type { get; set; }
        public string status { get; set; }
        // LEAVING OUT DATE FOR NOW... NOT SURE ABOUT THE SYNTAX FOR '$date'

        public addDocsResFromSynapse_user_docs_virtualdoc_meta meta { get; set; }
    }

    public class addDocsResFromSynapse_user_docs_virtualdoc_meta
    {
        public addDocsResFromSynapse_user_docs_virtualdoc_meta_qset question_set { get; set; }
    }

    public class addDocsResFromSynapse_user_docs_virtualdoc_meta_qset
    {
        public string id { get; set; }
        public List<synapseIdVerificationQuestionAnswerSet> questions { get; set; } // same as before, but now nested in here inside 'meta'
    }
}
