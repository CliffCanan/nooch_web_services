﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynapseDetailsClass_BankDetails
    {
        public int? bankid { get; set; }
        public string email { get; set; }
        public string Status { get; set; }
        public DateTime? AddedOn { get; set; }
    }
    public class SynapseDetailsClass_UserDetails
    {
        public string access_token { get; set; }
        public string MemberId { get; set; }
        public int? user_id { get; set; }
    }
}
