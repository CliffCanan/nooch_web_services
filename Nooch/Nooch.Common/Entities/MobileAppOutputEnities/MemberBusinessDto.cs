using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
    public class MemberBusinessDto
    {
        public string UserName { get; set; }
        public string Status { get; set; }
    }

    public class AppLogin
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public string note { get; set; }
    }
}
