using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class RegisterUserSynapseResultClassExt
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string reason { get; set; }
        public string refresh_token { get; set; }
        public string success { get; set; }
        public string user_id { get; set; }
        public string username { get; set; }
        public string memberIdGenerated { get; set; }
        public string ssn_verify_status { get; set; }
    }
}
