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
    
    public partial class UnitsOccupiedByTenant
    {
        public int Id { get; set; }
        public Nullable<System.Guid> UnitId { get; set; }
        public Nullable<System.Guid> TenantId { get; set; }
        public Nullable<System.DateTime> OccupiedOn { get; set; }
        public Nullable<bool> IsDeleted { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public Nullable<System.DateTime> LastPaymentDate { get; set; }
        public string LastPaymentAmount { get; set; }
        public Nullable<bool> IsPaymentDueForThisMonth { get; set; }
    }
}
