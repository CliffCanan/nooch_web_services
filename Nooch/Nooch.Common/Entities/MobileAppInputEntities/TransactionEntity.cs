using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class TransactionEntity
    {
        public string TransactionId { get; set; }
        public string PinNumber { get; set; }
        public string MemberId { get; set; }
        public string RecipientId { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public bool IsPrePaidTransaction { get; set; }
        public string DisputeStatus { get; set; }
        public string DeviceId { get; set; }
        public LocationEntity Location { get; set; }
        //added by venturepact
        public byte[] Picture { get; set; }
        public string BankAccountId { get; set; }
        public string TransactionType { get; set; }
        public string BankId { get; set; }
        public string TransactionDateTime { get; set; }
        public bool doNotSendEmails { get; set; }
        public bool isRentAutoPayment { get; set; }
        public bool isForHabitat { get; set; }
    }

    public class LocationEntity
    {
        public string LocationId { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Altitude { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
