using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SaveVerificationIdDocument
    {
        public byte[] Picture { get; set; }
        public string Photo { get;set; }
        public string MemberId { get; set; }
        public string AccessToken { get; set; }
    }
}
