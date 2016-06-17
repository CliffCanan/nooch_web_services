using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class BoolResult
    {
        public bool Result { get; set; }
    }

    public class BankVerification
    {
        public bool openAppText { get; set; }
        public bool Div1 { get; set; }
        public bool Div2 { get; set; }
    }

    public class makePaymentPg
    {
        public string errorId { get; set; }
        public string type { get; set; }
        public string from { get; set; }
        public string classForForm { get; set; }
        public string classForPinButton { get; set; }
    }
}
