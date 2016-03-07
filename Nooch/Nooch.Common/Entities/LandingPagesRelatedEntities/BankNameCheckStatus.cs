using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.LandingPagesRelatedEntities
{
    public class BankNameCheckStatus
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string MFAType { get; set; }
        public bool IsPinRequired { get; set; }
    }
}
