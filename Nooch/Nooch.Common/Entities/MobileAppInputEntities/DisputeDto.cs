using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
   public class DisputeDto
    {

        public string MemberId { get; set; }

        public string RecepientId { get; set; }
  
        public string TransactionId { get; set; }
       
        public string ListType { get; set; }
      
        public string CcMailIds { get; set; }
    
        public string BccMailIds { get; set; }
    
        public string Subject { get; set; }
    
        public string BodyText { get; set; }
    }
}
