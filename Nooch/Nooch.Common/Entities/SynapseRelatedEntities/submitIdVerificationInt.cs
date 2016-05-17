using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class submitIdVerificationInt
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class isReadyForSyanpse
    {
        public bool success { get; set; }
        public bool hasName { get; set; }
        public bool hasAddress { get; set; }
        public bool hasSSN { get; set; }
        public bool hasDOB { get; set; }
        public bool hasZip { get; set; }
        public bool hasFngrprnt { get; set; }
    }
}
