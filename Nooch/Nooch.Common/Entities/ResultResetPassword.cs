﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class ResultResetPassword
    {
        public bool requestExpiredorNotFound { get; set; }
        public bool pin { get; set; }
        public bool isRs { get; set; }
        public string usermail { get; set; }
        public string invalidUser { get; set; }
    }
}
