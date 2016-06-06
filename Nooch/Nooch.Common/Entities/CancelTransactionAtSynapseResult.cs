using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities
{
    public class CancelTransactionAtSynapseResult
    {
        public bool IsRentScene { get; set; }
        public string errorMsg { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class CancelTransactionClass
    {
        public Login1 login { get; set; }
        public User1 user { get; set; }
        public Trans trans { get; set; }
    }

    public class Login1
    {
        public string oauth_key { get; set; }
    }
    public class User1
    {
        public string fingerprint { get; set; }
    }
    public class Trans
    {
       
        public _ID _id { get; set; }
    }

    public class _ID
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }


    public class ErrorCancelTransation
    {
        public Message message { get; set; }
        public bool success { get; set; }
    }

    public class Message
    {
        public string en { get; set; }
    }
     
}
