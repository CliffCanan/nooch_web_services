using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nooch.Common;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Common.Entities.SynapseRelatedEntities;
using Nooch.Common.Resources;
using Nooch.Common.Rules;
using Nooch.Data;
using Nooch.Common.Entities;


namespace Nooch.DataAccess
{
    public class AccountDataAccess
    {
            private readonly NOOCHEntities _dbContext = null;

        public AccountDataAccess()
        {
            _dbContext = new NOOCHEntities();
        }

        /// <summary>
        /// To get account details.
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns>A Members object.</returns>
        public Member GetMember(string memberId)
        {
            Logger.Info("AccountDataAccess - GetMember - [MemberId: " + memberId + "]");
            using (var noochConnection = new NOOCHEntities())
            {
                return GetMember2(memberId );
            }
        }

        /// <summary>
        /// To get account details.
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="memberRepository"></param>
        public Member GetMember2(string memberId)
        {
            var id = Utility.ConvertToGuid(memberId);

            

          //  var memberDetails = memberRepository.SelectAll(memberSpecification).FirstOrDefault();
            var memberDetails = _dbContext.Members.Where(memberTemp => memberTemp.MemberId == id).FirstOrDefault();

            if (memberDetails != null)
            {
                return memberDetails;
            }
            return null;
        }


        public class SynapseDetailsClass_internal
        {
            public SynapseBanksOfMember BankDetails { get; set; }
            public SynapseCreateUserResult UserDetails { get; set; }

            public bool wereBankDetailsFound { get; set; }
            public bool wereUserDetailsFound { get; set; }

            public string UserDetailsErrMessage { get; set; }
            public string AccountDetailsErrMessage { get; set; }
        }

        public SynapseDetailsClass_internal GetSynapseBankAndUserDetailsforGivenMemberId(string memberId)
        {
            SynapseDetailsClass_internal res = new SynapseDetailsClass_internal();
            res.wereUserDetailsFound = false;
            res.wereBankDetailsFound = false;

            try
            {
                var id = Utility.ConvertToGuid(memberId);

                using (var noochConnection = new NOOCHEntities())
                {
                    // Checking user details for given MemberID
                   // var synapseCreateUsersRepo = new Repository<SynapseCreateUserResults, NoochDataEntities>(noochConnection);

                    //var memberProto = new Specification<SynapseCreateUserResults>
                    //{
                    //    Predicate = memberTemp =>
                    //                    memberTemp.MemberId.Value.Equals(id) &&
                    //                    memberTemp.IsDeleted == false &&
                    //                   (memberTemp.success == true || memberTemp.success == false)
                    //};
                    //var createSynapseUserObj = synapseCreateUsersRepo.SelectAll(memberProto).FirstOrDefault();

                    var createSynapseUserObj = _dbContext.SynapseCreateUserResults.Where(memberTemp =>
                                        memberTemp.MemberId.Value.Equals(id) &&
                                        memberTemp.IsDeleted == false &&
                                       (memberTemp.success == true || memberTemp.success == false)).FirstOrDefault();

                    if (createSynapseUserObj != null &&
                        !String.IsNullOrEmpty(createSynapseUserObj.access_token))
                    {
                        // This MemberId was found in the SynapseCreateUserResults DB
                        res.wereUserDetailsFound = true;

                        Logger.Info("ADA -> GetSynapseBankAndUserDetailsforGivenMemberId - Checkpoint #1 - " +
                                               "SynapseCreateUserResults Record Found! - Now about to check if Synapse OAuth Key is expired or still valid.");

                        // CLIFF (10/3/15): ADDING CALL TO NEW METHOD TO CHECK USER'S STATUS WITH SYNAPSE, AND REFRESHING OAUTH KEY IF NECESSARY
                        MembersDataAccess mda = new MembersDataAccess();

                        #region Check If Testing

                        // CLIFF (10/22/15): Added this block for testing - if you use an email that includes "jones00" in it, 
                        //                   then this method will use the Synapse (v2) SANDBOX.  Leaving this here in case we
                        //                   want to test in the future the same way.
                        bool shouldUseSynapseSandbox = false;
                        string memberUsername = CommonHelper.GetMemberUsernameByMemberId(memberId);

                        if (memberUsername.ToLower().IndexOf("jones00") > -1)
                        {
                            shouldUseSynapseSandbox = true;
                            Logger.Info("**  ADA -> GetSynapseBankAndUserDetailsforGivenMemberId -> TESTING USER DETECTED - [" +
                                                  memberUsername + "], WILL USE SYNAPSE SANDBOX URL FOR CHECKING OAUTH TOKEN STATUS  **");
                        }

                        #endregion Check If Testing

                        #region Check If OAuth Key Still Valid

                        synapseV2checkUsersOauthKey checkTokenResult = mda.checkIfUsersSynapseAuthKeyIsExpired(createSynapseUserObj.access_token,
                                                                                                               shouldUseSynapseSandbox);

                        if (checkTokenResult != null)
                        {
                            if (checkTokenResult.success == true)
                            {
                                res.UserDetails = createSynapseUserObj;
                                res.UserDetails.access_token = checkTokenResult.oauth_consumer_key;
                                res.UserDetailsErrMessage = "OK";
                            }
                            else
                            {
                                Logger.Error("ADA -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                                       "CheckTokenResult.msg: [" + checkTokenResult.msg + "], MemberID: [" + memberId + "]");

                                res.UserDetailsErrMessage = checkTokenResult.msg;
                            }
                        }
                        else
                        {
                            Logger.Error("ADA -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED on Checking User's Synapse OAuth Token - " +
                                                       "CheckTokenResult was NULL, MemberID: [" + memberId + "]");

                            res.UserDetailsErrMessage = "Unable to check user's Oauth Token";
                        }

                        #endregion Check If OAuth Key Still Valid
                    }
                    else
                    {
                        Logger.Error("ADA -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - Unable to find Synapse Create User Details - " +
                                               "MemberID: [" + memberId + "]");

                        res.UserDetails = null;
                        res.UserDetailsErrMessage = "User synapse details not found.";
                    }

                    #region Get The User's Synapse Bank Details

                    // Now get the user's bank account details
                   // var UserBankAccountRepository = new Repository<SynapseBanksOfMembers, NoochDataEntities>(noochConnection);

                    //var bankSpecification = new Specification<SynapseBanksOfMembers>
                    //{
                    //    Predicate = bank =>
                    //                    bank.MemberId.Value.Equals(id) &&
                    //                    bank.IsDefault == true
                    //};                                        
                    //var defaultBank = UserBankAccountRepository.SelectAll(bankSpecification).FirstOrDefault();

                    var defaultBank = _dbContext.SynapseBanksOfMembers.Where(bank =>
                                        bank.MemberId.Value.Equals(id) &&
                                        bank.IsDefault == true).FirstOrDefault();

                    if (defaultBank != null)
                    {
                        // Found a Synapse bank account for this user
                        res.wereBankDetailsFound = true;
                        res.BankDetails = defaultBank;
                        res.AccountDetailsErrMessage = "OK";
                    }
                    else
                    {
                        res.BankDetails = null;
                        res.AccountDetailsErrMessage = "User synapse bank not found.";
                    }

                    #endregion Get The User's Synapse Bank Details
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ADA -> GetSynapseBankAndUserDetailsforGivenMemberId FAILED - MemberID: [" + memberId + "], Outer Exception: [" + ex + "]");
            }

            return res;
        }
        



    }
}
