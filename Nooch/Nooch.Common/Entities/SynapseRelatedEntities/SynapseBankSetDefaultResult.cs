using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseBankSetDefaultResult
    {
        public bool Is_success { get; set; }
        public string Message { get; set; }
    }

    public class checkNoochNameAgainstBankName
    {
        public bool firstNameMatched { get; set; }
        public bool nameMatchedExactly { get; set; }
        public bool lastNameMatched { get; set; }

        public bool wasVerificationEmailSent { get; set; }
    }
}
