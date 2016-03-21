using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class synapse_user_create_input_class_int
    {
        public string email { get; set; }
        public string fullname { get; set; }
        public string ip_address { get; set; }
        public string phonenumber { get; set; }
        public string dp { get; set; }

        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string force_create { get; set; }
    }
}
