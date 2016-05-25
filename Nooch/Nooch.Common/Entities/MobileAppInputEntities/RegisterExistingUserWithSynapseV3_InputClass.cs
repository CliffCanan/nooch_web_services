using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class RegisterExistingUserWithSynapseV3_InputClass
    {
        public string transId { get; set; }

        public string memberId { get; set; }
        public string email { get; set; }
        public string phone { get; set; }

        public string pw { get; set; }

        public string ssn { get; set; }
        public string dob { get; set; }
        public string address { get; set; }
        public string zip { get; set; }
        public string fngprnt { get; set; }
        public string ip { get; set; }
        public string isIdImageAdded { get; set; }
        public string idImageData { get; set; }
        public string fullname { get; set; }


    }


    public class RegisterNonNoochUserWithSynapse_Input_Class
    {
        public string transId { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string fullname { get; set; }
        public string pw { get; set; }
        public string ssn { get; set; }
        public string dob { get; set; }
        public string address { get; set; }
        public string zip { get; set; }

        public string fngprnt { get; set; }
        public string ip { get; set; }
        public string isIdImageAdded { get; set; }

        public string idImageData { get; set; }

    }
}
