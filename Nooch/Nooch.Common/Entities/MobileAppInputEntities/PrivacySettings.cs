using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class PrivacySettings
    {
        public string MemberId { get; set; }

        
        public bool? ShowInSearch { get; set; }

        public bool? AllowSharing { get; set; }

        
        public bool? RequireImmediately { get; set; }
    }
}
