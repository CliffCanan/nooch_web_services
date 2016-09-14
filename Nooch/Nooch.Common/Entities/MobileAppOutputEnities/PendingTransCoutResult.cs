using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class PendingTransCountResult
    {
        public string pendingRequestsSent { get; set; }
        public string pendingRequestsReceived { get; set; }
        public string pendingInvitationsSent { get; set; }
        public string pendingDisputesNotSolved { get; set; }
    }
}