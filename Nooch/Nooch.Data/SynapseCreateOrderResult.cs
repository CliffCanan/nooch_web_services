//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Nooch.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class SynapseCreateOrderResult
    {
        public int Id { get; set; }
        public string TransAmount { get; set; }
        public string TransSellerId { get; set; }
        public string TransOAuthConsumberKey { get; set; }
        public Nullable<int> TransBankId { get; set; }
        public string Result_blanace { get; set; }
        public Nullable<bool> Result_balance_verified { get; set; }
        public Nullable<bool> Result_success { get; set; }
        public Nullable<int> Result_order_account_type { get; set; }
        public string Result_order_amount { get; set; }
        public Nullable<int> Result_order_bank_id { get; set; }
        public string Result_order_bank_nickname { get; set; }
        public string Result_date { get; set; }
        public string Result_date_settled { get; set; }
        public string Result_discount { get; set; }
        public string Result_facilitator_fee { get; set; }
        public string Result_fee { get; set; }
        public string Result_id { get; set; }
        public Nullable<bool> Result_is_buyer { get; set; }
        public string Result_note { get; set; }
        public string Result_resource_uri { get; set; }
        public Nullable<bool> Result_seller_accept_gratuity { get; set; }
        public Nullable<bool> Result_seller_has_avatar { get; set; }
        public string Result_seller_seller_avatar { get; set; }
        public string Result_seller_seller_fullname { get; set; }
        public Nullable<int> Result_seller_seller_id { get; set; }
        public string Result_status { get; set; }
        public string Result_status_url { get; set; }
        public string Result_supp_id { get; set; }
        public string Result_ticket_number { get; set; }
        public string Result_tip { get; set; }
        public string Result_total { get; set; }
        public Nullable<System.DateTime> TransactionDateTime { get; set; }
    }
}