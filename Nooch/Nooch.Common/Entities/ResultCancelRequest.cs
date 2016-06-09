using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class ResultCancelRequest
    {
        public bool success { get; set; }
        public bool showPaymentInfo { get; set; }
        public string initStatus { get; set; }
        public string resultMsg { get; set; }
        public string senderImage { get; set; }
        public string nameLabel { get; set; }
        public string AmountLabel { get; set; }
        public string memberId { get; set; }

        // Used with RejectMoney page

        public bool IsTransFound { get; set; }
        public string TransId { get; set; }
        public string UserType { get; set; }
        public string LinkSource { get; set; }
        public string TransType { get; set; }
        public string TransStatus { get; set; }
        public string transMemo { get; set; }
        public string RecepientName { get; set; }
        public string RecepientPhoto { get; set; }
    }
}
