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
    
    public partial class AdminUser
    {
        public System.Guid UserId { get; set; }
        public string AdminLevel { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.Guid> CreatedBy { get; set; }
        public Nullable<bool> ChangePasswordDone { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<System.Guid> ModifiedBy { get; set; }
    }
}
