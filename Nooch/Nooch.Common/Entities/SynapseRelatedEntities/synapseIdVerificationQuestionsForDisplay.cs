using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapseIdVerificationQuestionsForDisplay
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
