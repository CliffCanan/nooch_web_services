﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.LandingPagesRelatedEntities.RejectMoney
{
    public class PageLoadDataRejectMoney
    {
        public string transStatus { get; set; }
        public string errorFromCodeBehind { get; set; }
        public string nameLabel { get; set; }
        public string senderImage { get; set; }
        public string TransId { get; set; }
        public string UserType { get; set; }
        public string transType { get; set; }
        public string transAmout { get; set; }
        public string transMemo { get; set; }
        public bool SenderAndTransInfodiv { get; set; }
        public bool createAccountPrompt { get; set; }
        public bool TransactionResult { get; set; }
    }
}
