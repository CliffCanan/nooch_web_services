using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities.MobileAppInputEntities
{
    public class MemberNotificationsNewStringTypeSettings
    {
        public string MemberId { get; set; }
        public string TransferUnclaimed { get; set; }
        public string NoochToBankCompleted { get; set; } //Email For Withdraw When Completed   
        public string EmailTransferSent { get; set; } //Email For Transfer Sent
        public string EmailTransferReceived { get; set; } //Email For Transfer Received
        public string EmailTransferAttemptFailure { get; set; } //Email For Transfer attempt faliure
        public string BankToNoochRequested { get; set; } //Email For Deposit When Requested
        public string BankToNoochCompleted { get; set; } //Email For Deposit When Completed



        public string NotificationId { get; set; }


        // Push Notification

        public string NoochToBank { get; set; } //Push Notification For Withdraw


        public string BankToNooch { get; set; } //Push Notification For Deposit


        public string TransferReceived { get; set; } //Push Notification For Transfer Received


        public string TransferSent { get; set; } //Push Notification For Transfer Sent


        public string TransferAttemptFailure { get; set; } //Push Notification For Transfer attempt faliure


        public string FriendRequest { get; set; }


        public string InviteRequestAccept { get; set; }



        // Email Notification



        public string EmailFriendRequest { get; set; }


        public string EmailInviteRequestAccept { get; set; }





        public string NoochToBankRequested { get; set; } //Email For Withdraw When Requested



        public string InviteReminder { get; set; }


        public string LowBalance { get; set; } //Email Low Balance Reminder


        public string ValidationRemainder { get; set; }


        public string ProductUpdates { get; set; }


        public string NewAndUpdate { get; set; }


        public string AutomaticWithdrawal { get; set; }
    }
}
