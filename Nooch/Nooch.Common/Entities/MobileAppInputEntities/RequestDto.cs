using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class RequestDto
    {
        
        public string PinNumber { get; set; }

        
        public string MemberId { get; set; }

        
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }

        
        public string Name { get; set; }

        
        public decimal Amount { get; set; }

        
        public string TransactionId { get; set; }

        
        public string TransactionDate { get; set; }

        
        public string DeviceId { get; set; }

        
        public float Latitude { get; set; }

        
        public float Longitude { get; set; }

        
        public float Altitude { get; set; }

        
        public string AddressLine1 { get; set; }

        
        public string AddressLine2 { get; set; }

        
        public string City { get; set; }

        
        public string State { get; set; }

        
        public string Country { get; set; }

        
        public string ZipCode { get; set; }

        
        public string Memo { get; set; }

        
        public string Status { get; set; }

        
        public string MoneySenderEmailId { get; set; }

        public string MoneyReceiverEmailId { get; set; }

        
        public byte[] Picture { get; set; }

        
        public string isTesting { get; set; }

        
        public string isRentPayment { get; set; }
    }
}
