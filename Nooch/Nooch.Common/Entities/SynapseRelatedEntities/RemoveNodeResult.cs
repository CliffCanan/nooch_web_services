using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    #region for success type result
    public class RemoveNodeResult
    {

        public Message message { get; set; }
        public bool success { get; set; }

    }
    public class Message
    {
        public string en { get; set; }
    }

    #endregion




    #region in case we get error

    public class ErrorRemoveNode
    {
        public Error error { get; set; }
        public bool success { get; set; }
    }

    public class Error
    {
        public string en { get; set; }
    }

    #endregion


    #region remove node input class


    public class RemoveBankNodeRootClass
    {
        public Login login { get; set; }
        public User user { get; set; }
        public Node node { get; set; }
    }

    public class Login
    {
        public string oauth_key { get; set; }
    }

    public class User
    {
        public string fingerprint { get; set; }
    }

    public class Node
    {
        public _Id _id { get; set; }
    }

    public class _Id
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }





    #endregion


    public class RemoveNodeGenricResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
