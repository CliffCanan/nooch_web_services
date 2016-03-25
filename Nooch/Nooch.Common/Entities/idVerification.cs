using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class idVerification
    {
        public string error_msg { get; set; }
        public string from { get; set; }
        public string redUrl { get; set; }
        public string memid { get; set; }
        public string was_error { get; set; }
        public string qsetId { get; set; }
        public string question1text { get; set; }
        public short quest1id{ get; set; }
        public string question1_choice1 { get; set; }
        public string question1_choice2 { get; set; }
        public string question1_choice3 { get; set; }
        public string question1_choice4 { get; set; }
        public string question1_choice5 { get; set; }
        public string question2text { get; set; }
        public short quest2id { get; set; }
        public string question2_choice1 { get; set; }
        public string question2_choice2 { get; set; }
        public string question2_choice3 { get; set; }
        public string question2_choice4 { get; set; }
        public string question2_choice5 { get; set; }
        public string question3text { get; set; }
        public short quest3id { get; set; }
        public string question3_choice1 { get; set; }
        public string question3_choice2 { get; set; }
        public string question3_choice3 { get; set; }
        public string question3_choice4 { get; set; }
        public string question3_choice5 { get; set; }

        public string question4text { get; set; }
        public short quest4id { get; set; }
        public string question4_choice1 { get; set; }
        public string question4_choice2 { get; set; }
        public string question4_choice3 { get; set; }
        public string question4_choice4 { get; set; }
        public string question4_choice5 { get; set; }

        public string question5text { get; set; }
        public short quest5id { get; set; }
        public string question5_choice1 { get; set; }
        public string question5_choice2 { get; set; }
        public string question5_choice3 { get; set; }
        public string question5_choice4 { get; set; }
        public string question5_choice5 { get; set; }


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
    public class synapseV2_IdVerQsForDisplay_Int
    {
        public bool success { get; set; }
        public bool submitted { get; set; }
        public string qSetId { get; set; }
        public List<synapseIdVerificationQuestionAnswerSet> questionList { get; set; }

        public string memberId { get; set; }
        public string dateCreated { get; set; }
        public string msg { get; set; }
    }

}
