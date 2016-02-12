using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class UpdateMemberIpInput
    {

       
        public string MemberId { get; set; }

       
        public string AccessToken { get; set; }

       
        public string IpAddress { get; set; }

       
        public string DeviceId { get; set; }
    }
}
