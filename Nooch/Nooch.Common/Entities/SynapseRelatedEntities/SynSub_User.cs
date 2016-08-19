using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.SynapseRelatedEntities
{
    public class SynSub_User
    {

        public Document[] documents { get; set; }
        public Extra extra { get; set; }
        public SynSub_User_Login[] logins { get; set; }
        public string[] phone_numbers { get; set; }
        public string permission { get; set; }
        public object[] photos { get; set; }
        public SynSub_User_Client client { get; set; }
        public string[] legal_names { get; set; }
        public bool is_hidden { get; set; }
        public SynSub_User_Id _id { get; set; }
        public SynSub_User_Doc_Status doc_status { get; set; }

    }

    public class Extra
    {
        public bool extra_security { get; set; }
        public bool is_business { get; set; }
        public string supp_id { get; set; }
        public int cip_tag { get; set; }
        public SynSub_User_Date_Joined date_joined { get; set; }
    }

    public class SynSub_User_Date_Joined
    {
        public long date { get; set; }
    }

    public class SynSub_User_Client
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class SynSub_User_Id
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    public class SynSub_User_Doc_Status
    {
        public string physical_doc { get; set; }
        public string virtual_doc { get; set; }
    }

    public class Document
    {
        public string permission_scope { get; set; }
        public string name { get; set; }
        public Virtual_Docs[] virtual_docs { get; set; }
        public Physical_Docs[] physical_docs { get; set; }
        public Social_Docs[] social_docs { get; set; }
        public string id { get; set; }
    }

    public class Virtual_Docs
    {
        public string status { get; set; }
        public string document_type { get; set; }
        public Last_Updated last_updated { get; set; }
        public string id { get; set; }
    }

    public class Last_Updated
    {
        public long date { get; set; }
    }

    public class Physical_Docs
    {
        public string status { get; set; }
        public string document_type { get; set; }
        public Last_Updated1 last_updated { get; set; }
        public string id { get; set; }
    }

    public class Last_Updated1
    {
        public long date { get; set; }
    }

    public class Social_Docs
    {
        public string status { get; set; }
        public string document_type { get; set; }
        public Last_Updated2 last_updated { get; set; }
        public string id { get; set; }
    }

    public class Last_Updated2
    {
        public long date { get; set; }
    }

    public class SynSub_User_Login
    {
        public string scope { get; set; }
        public string email { get; set; }
    }



    // 
    public class SynSub_Node
    {
        public Info info { get; set; }
        public SynSub_Node_Extra extra { get; set; }
        public bool is_active { get; set; }
        public string allowed { get; set; }
        public SynSub_Node_Id _id { get; set; }
        public string type { get; set; }
    }

    public class Info
    {
        public string bank_name { get; set; }
        public string bank_long_name { get; set; }
        public string account_num { get; set; }
        public string name_on_account { get; set; }
        public string type { get; set; }
        public string routing_num { get; set; }
        public Balance balance { get; set; }
        public string nickname { get; set; }
        public string _class { get; set; }
    }

    public class Balance
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class SynSub_Node_Extra
    {
        public object supp_id { get; set; }
    }

    public class SynSub_Node_Id
    {
        [JsonProperty(PropertyName = "$oid")]
        public string oid { get; set; }
    }

    
public class SynSub_Trans
{
public From from { get; set; }
public SynSub_Trans_Extra extra { get; set; }
public Timeline[] timeline { get; set; }
public To to { get; set; }
public Amount amount { get; set; }
public Client client { get; set; }
public Fee[] fees { get; set; }
public _Id2 _id { get; set; }
public Recent_Status recent_status { get; set; }
}

public class From
{
public string type { get; set; }
public string nickname { get; set; }
public Id id { get; set; }
public SynSub_Trans_Users user { get; set; }
}

public class Id
{
    [JsonProperty(PropertyName = "$oid")]
public string oid { get; set; }
}

public class SynSub_Trans_Users
{
public string[] legal_names { get; set; }
public SynSub_Trans_Id _id { get; set; }
}

public class SynSub_Trans_Id
{
public string oid { get; set; }
}

public class SynSub_Trans_Extra
{
public string latlon { get; set; }
public string ip { get; set; }
public string supp_id { get; set; }
public string webhook { get; set; }
public Process_On process_on { get; set; }
public string note { get; set; }
public Created_On created_on { get; set; }
}

public class Process_On
{
public long date { get; set; }
}

public class Created_On
{
public long date { get; set; }
}

public class To
{
public string type { get; set; }
public string nickname { get; set; }
public Id1 id { get; set; }
public SynSub_Trans_User1 user { get; set; }
}

public class Id1
{
public string oid { get; set; }
}

public class SynSub_Trans_User1
{
public string[] legal_names { get; set; }
public _Id1 _id { get; set; }
}

public class _Id1
{
    [JsonProperty(PropertyName = "$oid")]
public string oid { get; set; }
}

public class Amount
{
public string currency { get; set; }
public float amount { get; set; }
}

public class Client
{
public int id { get; set; }
public string name { get; set; }
}

public class _Id2
{
    [JsonProperty(PropertyName = "$oid")]
public string oid { get; set; }
}

public class Recent_Status
{
public Date date { get; set; }
public string note { get; set; }
public string status { get; set; }
public string status_id { get; set; }
}

public class Date
{
public long date { get; set; }
}

public class Timeline
{
public Date1 date { get; set; }
public string note { get; set; }
public string status { get; set; }
public string status_id { get; set; }
}

public class Date1
{
 [JsonProperty(PropertyName = "$date")]
public long date { get; set; }
}

public class Fee
{
public string note { get; set; }
public To1 to { get; set; }
public float fee { get; set; }
}

public class To1
{
public Id2 id { get; set; }
}

public class Id2
{
    [JsonProperty(PropertyName = "$oid")]
public string oid { get; set; }
}


}
