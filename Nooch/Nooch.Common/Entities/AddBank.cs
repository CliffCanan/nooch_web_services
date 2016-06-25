using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class AddBankResult
    {
        public bool success { get; set; }
        public bool isRs { get; set; }
        public bool isLandlord { get; set; }
        public string memId { get; set; }
        public string redUrl { get; set; }
        public string msg { get; set; }
        public string name { get; set; }
    }
}
