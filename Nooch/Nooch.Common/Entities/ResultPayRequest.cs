﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class ResultPayRequest
    {
        public string usrTyp { get; set; }
        public string transType { get; set; }
        public bool payreqInfo { get; set; }
        public bool PayorInitialInfo { get; set; }
        public string pymnt_status { get; set; }
        public string rs { get; set; }
        public string transId { get; set; }
        public string transMemo { get; set; }
        public string senderImage { get; set; }
        public string senderName1 { get; set; }
        public string transAmountd { get; set; }
        public string transAmountc { get; set; }
        public string memidexst { get; set; }
        public string bnkName { get; set; }
        public string bnkNickname { get; set; }
        public bool nonRegUsrContainer { get; set; }
        public string invitationType { get; set; }
        public string invitationSentto { get; set; }
    }
}
