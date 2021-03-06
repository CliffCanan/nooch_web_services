﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class MemberEnity
    {
        public System.Guid MemberId { get; set; }
        public string Nooch_ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UDID1 { get; set; }
        public string UserName { get; set; }
        public string SecondaryEmail { get; set; }
        public string RecoveryEmail { get; set; }
        public string TertiaryEmail { get; set; }
        public string PinNumber { get; set; }
        public string Password { get; set; }
        public string ContactNumber { get; set; }
        public string Status { get; set; }
        public Nullable<bool> RememberMeEnabled { get; set; }
        public Nullable<System.DateTime> DateCreated { get; set; }
        public Nullable<System.DateTime> DateModified { get; set; }
        public Nullable<System.Guid> ModifiedBy { get; set; }
        public Nullable<System.DateTime> InvalidLoginTime { get; set; }
        public Nullable<int> InvalidLoginAttemptCount { get; set; }
        public Nullable<int> InvalidPinAttemptCount { get; set; }
        public Nullable<System.DateTime> InvalidPinAttemptTime { get; set; }
        public Nullable<System.DateTime> DateOfBirth { get; set; }
        public string FacebookAccountLogin { get; set; }
        public Nullable<bool> AllowPushNotifications { get; set; }
        public string Photo { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string Country { get; set; }
        
        public Nullable<bool> IsDeleted { get; set; }
        
        public Nullable<System.DateTime> InviteReminderDate { get; set; }
        public string UserNameLowerCase { get; set; }
        public string TimeZoneKey { get; set; }
        public string Address2 { get; set; }
        public Nullable<int> TotalNoochTransfersCount { get; set; }
        public Nullable<int> TotalACHTransfersCount { get; set; }
        public Nullable<System.DateTime> ValidatedDate { get; set; }
        public Nullable<int> ValidationFailedCount { get; set; }
        public Nullable<System.Guid> InviteCodeId { get; set; }
        public Nullable<System.Guid> InviteCodeIdUsed { get; set; }
        public string Type { get; set; }
        public string SSN { get; set; }
        public Nullable<System.DateTime> UpgradeDate { get; set; }
        public Nullable<bool> IsVerifiedPhone { get; set; }
        public Nullable<decimal> LastLocationLat { get; set; }
        public Nullable<decimal> LastLocationLng { get; set; }
        public string AccessToken { get; set; }
        public Nullable<bool> Allow2FactorAuthentication { get; set; }
        public Nullable<bool> IsOnline { get; set; }
        public Nullable<System.DateTime> SDNCheckDateTime { get; set; }
        public Nullable<bool> AnyPriliminaryHit { get; set; }
        public Nullable<long> ent_num { get; set; }
        public Nullable<bool> IsRequiredImmediatley { get; set; }
        public string DeviceToken { get; set; }
        public Nullable<bool> IsSDNSafe { get; set; }
        public string AdminNotes { get; set; }
        public Nullable<System.DateTime> PhoneVerifiedOn { get; set; }
        public string VerificationDocumentPath { get; set; }
        public Nullable<bool> IsVerifiedWithSynapse { get; set; }
        public string TransferLimit { get; set; }
        public string FacebookUserId { get; set; }
        public string GoogleUserId { get; set; }
    
    }
}
