using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class synapseUsersPage
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public List<synapseUsersPageObj> users { get; set; }
        public string allUsers { get; set; }
    }

    public class synapseUsersPageObj
    {
        public string noochId { get; set; }
        public string signUpDate { get; set; }
        public string oid { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string cip { get; set; }
        public string permission { get; set; }
        public string allowed { get; set; }
        public bool hasBank { get; set; }
    }

}
