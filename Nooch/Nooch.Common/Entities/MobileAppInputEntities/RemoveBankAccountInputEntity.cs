using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class RemoveBankAccountInputEntity
    {
        public string MemberID { get; set; }
        public string AccessToken { get; set; }
        public int  BankAccountId { get; set; }
    }
}
