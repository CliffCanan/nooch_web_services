using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppOutputEnities
{
   public class StatsForMember
    {
       public string Largest_sent_transfer { get; set; }
      
       
       public string Total_Sent { get; set; }

   
       public string Total_Received { get; set; }

       
       public string Largest_received_transfer { get; set; }
       public string Total_no_of_transfer_Received { get; set; }
       public string Total_no_of_transfer_Sent { get; set; }
       public string Total_P2P_transfers { get; set; }
       public string Total_Friends_Invited { get; set; }
       public string Total_Friends_Joined { get; set; }
       public string Total_Posts_To_TW { get; set; }
       public string Total_Posts_To_FB { get; set; }
    }
}
