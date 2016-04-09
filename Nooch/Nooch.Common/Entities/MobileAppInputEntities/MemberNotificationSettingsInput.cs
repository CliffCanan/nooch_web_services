using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class MemberNotificationSettingsInput
    {
        
        public string MemberId { get; set; }

        
        public string NotificationId { get; set; }


        // Push Notification
        
        public bool NoochToBank { get; set; } //Push Notification For Withdraw

        
        public bool BankToNooch { get; set; } //Push Notification For Deposit

        
        public bool TransferReceived { get; set; } //Push Notification For Transfer Received

        
        public bool TransferSent { get; set; } //Push Notification For Transfer Sent
        

        public bool TransferAttemptFailure { get; set; } //Push Notification For Transfer attempt faliure

        
        public bool FriendRequest { get; set; }

        
        public bool InviteRequestAccept { get; set; }



        // Email Notification
        
        public bool EmailTransferReceived { get; set; } //Email For Transfer Received

        
        public bool EmailTransferSent { get; set; } //Email For Transfer Sent

        
        public bool EmailTransferAttemptFailure { get; set; } //Email For Transfer attempt faliure

        
        public bool EmailFriendRequest { get; set; }

        
        public bool EmailInviteRequestAccept { get; set; }

        
        public bool TransferUnclaimed { get; set; }

        
        public bool BankToNoochRequested { get; set; } //Email For Deposit When Requested

        
        public bool BankToNoochCompleted { get; set; } //Email For Deposit When Completed

        
        public bool NoochToBankRequested { get; set; } //Email For Withdraw When Requested

        
        public bool NoochToBankCompleted { get; set; } //Email For Withdraw When Completed       

        
        public bool InviteReminder { get; set; }

        
        public bool LowBalance { get; set; } //Email Low Balance Reminder

        
        public bool ValidationRemainder { get; set; }

        
        public bool ProductUpdates { get; set; }

        
        public bool NewAndUpdate { get; set; }

        
        public bool AutomaticWithdrawal { get; set; }
    }
}
