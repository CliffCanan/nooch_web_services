using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
   public class ResultCancelRequest
    {
        public string reslt1 { get; set; }
        public string reslt { get; set; }
        public string paymentInfo { get; set; }
        public string senderImage { get; set; }
        public string nameLabel { get; set; }
        public string AmountLabel { get; set; }

       // being used with RejectMoney page

        public string TransId { get; set; }
        public string UserType { get; set; }
        public string LinkSource { get; set; }
        public string TransType { get; set; }

        public string TransStatus { get; set; }

       public bool IsTransFound { get; set; }

       public string RecepientName { get; set; }
       public string RecepientPhoto { get; set; }

   }
}
