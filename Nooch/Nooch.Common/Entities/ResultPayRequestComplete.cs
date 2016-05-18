using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class ResultPayRequestComplete
    {
        public bool paymentSuccess { get; set; }
        public string memId { get; set; }
        public string rs { get; set; }
        public string senderImage { get; set; }
        public string senderName1 { get; set; }
        public string transAmountd { get; set; }
        public string transMemo { get; set; }
        public string usrTyp { get; set; }
        public string payeeMemId { get; set; }
        public bool IsTransactionStillPending { get; set; }
        public bool payinfobar { get; set; }
    }
}
