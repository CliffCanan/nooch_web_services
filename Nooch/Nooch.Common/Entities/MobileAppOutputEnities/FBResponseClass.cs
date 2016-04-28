using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{

    public class FBMemberDataClass
    {
        public string id { get; set; }
        public string name { get; set; }
    }
    public class FBPagingClass
    {
        public string next { get; set; }
    }
    public class FBSummaryClass
    {
        public string total_count { get; set; }
    }

    public class FBResponseClass
    {
        public FBMemberDataClass[] data { get; set; }
        public FBPagingClass paging { get; set; }
        public FBSummaryClass summary { get; set; }
    }
}
