﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class ResultcreateAccount
    {
        public string rs { get; set; }
        public string type { get; set; }
        public string errorId { get; set; }
        public string transId { get; set; }
        public string transType { get; set; }
        public string sentTo { get; set; }
        public string memId { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string dob { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string nameInNav { get; set; }
        public bool nameInNavContainer { get; set; }
        public string ssn { get; set; }
        public string ip { get; set; }
        public string fngprnt { get; set; }
        public string pw { get; set; }
 }

    public class CreateAccountInDB
    {
        public string name { get; set; }
        public string email { get; set; }
        public string pw { get; set; }       
        public string result { get; set; }
        public string TransId { get; set; }
    }

}