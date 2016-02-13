﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Helpers;
using System.Web.Http;

using AutoMapper;
using Nooch.Common;
using Nooch.Common.Entities;
using Nooch.Common.Entities.MobileAppInputEntities;
using Nooch.Common.Entities.MobileAppOutputEnities;
using Nooch.Data;
using Nooch.DataAccess;

namespace Nooch.API.Controllers
{
    public class NoochServicesController : ApiController
    {

        //public IEnumerable<string> Get()
        //{


        //    Logger.Info("came in log");

        //    var config = new MapperConfiguration(cfg => cfg.CreateMap<Member, MemberEnity>());
        //    var mapper = config.CreateMapper();

        //   MembersDataAccess mda = new MembersDataAccess();
        //   Guid memGuid = new Guid("ECA52C86-5674-4008-A09B-0003645D2A1A");
        //    var objtoPam = mda.GetMemberByGuid(memGuid);

        //    MemberEnity me = mapper.Map<Member, MemberEnity>(objtoPam);

        //    return new string[] { "value1", "value2" };
        //}




        [HttpPost]
        [ActionName("UdateMemberIPAddress")]
        public StringResult UdateMemberIPAddress(UpdateMemberIpInput member)
        {
            if (CommonHelper.IsValidRequest(member.AccessToken, member.MemberId))
            {
                try
                {
                    MembersDataAccess mda = new MembersDataAccess();
                    string res = mda.UpdateMemberIpAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId);
                    return new StringResult() { Result = mda.UpdateMemberIpAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId) };
                }
                catch (Exception ex)
                {
                    Logger.Error("Service Layer -> UpdateMemberIPAddressAndDeviceId FAILED - MemberID: [" + member.MemberId +
                                           "], Exception: [" + ex + "]");
                }
                return new StringResult();
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");


            }
        }

        [HttpGet]
        [ActionName("GetMemberByUdId")]
        public MemberDto GetMemberByUdId(string udId, string accessToken, string memberId)
        {
            if (CommonHelper.IsValidRequest(accessToken, memberId))
            {
                try
                {
                    Logger.Info("Service layer - GetPrimaryEmail [udId: " + udId + "]");

                    var memberEntity = CommonHelper.GetMemberByUdId(udId);
                    var member = new MemberDto { UserName = memberEntity.UserName, Status = memberEntity.Status };
                    return member;
                }
                catch (Exception ex)
                {
                    throw new Exception("Server Error");
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
            return new MemberDto();
        }

        [HttpGet]
        [ActionName("GetMemberPendingTransctionsCount")]
        public PendingTransCoutResult GetMemberPendingTransctionsCount(string MemberId, string AccessToken)
        {
            if (CommonHelper.IsValidRequest(AccessToken, MemberId))
            {
                try
                {
                    //Logger.LogDebugMessage("Service layer -> GetMemberPendingTransctionsCount - MemberId: [" + MemberId + "]");
                    var transactionDataAccess = new TransactionsDataAccess();

                    PendingTransCoutResult trans = transactionDataAccess.GetMemberPendingTransCount(MemberId);
                    return trans;
                }
                catch (Exception ex)
                {
                    //throw new Exception("Server Error");
                    return new PendingTransCoutResult { pendingRequestsSent = "0", pendingRequestsReceived = "0", pendingInvitationsSent = "0", pendingDisputesNotSolved = "0" };
                }
            }
            else
            {
                throw new Exception("Invalid OAuth 2 Access");
            }
        }



        [HttpGet]
        [ActionName("GetMemberIdByUserName")]
        public StringResult GetMemberIdByUserName(string userName)
        {
            try
            {

                return new StringResult { Result = CommonHelper.GetMemberIdByUserName(userName) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer - GetMemberByUserName FAILED - [userName: " + userName +
                                       "], Exception: [" + ex + "]");

            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("GetMemberUsernameByMemberId")]
        public StringResult GetMemberUsernameByMemberId(string memberId)
        {
            try
            {

                return new StringResult { Result = CommonHelper.GetMemberUsernameByMemberId(memberId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer - GetMemberUsernameByMemberId FAILED - [memberId: " + memberId +
                                       "], Exception: [" + ex + "]");

            }
            return new StringResult();
        }

        [HttpGet]
        [ActionName("GetMemberNameByUserName")]
        public StringResult GetMemberNameByUserName(string userName)
        {
            try
            {

                return new StringResult { Result = CommonHelper.GetMemberNameByUserName(userName) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer - GetMemberUsernameByMemberId FAILED - [GetMemberNameByUserName : " + userName +
                                       "], Exception: [" + ex + "]");

            }
            return new StringResult();
        }
        [HttpGet]
        [ActionName("MemberActivation")]
        public BoolResult MemberActivation(string tokenId)
        {
            try
            {
                Logger.Info("Service Layer -> MemberActivation Initiated - [tokenId: " + tokenId + "]");
                var mda = new MembersDataAccess();
                return new BoolResult { Result = mda.MemberActivation(tokenId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> MemberActivation Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
                return new BoolResult();
            }

        }
        [HttpGet]
        [ActionName("IsMemberActivated")]
        public BoolResult IsMemberActivated(string tokenId)
        {
            try
            {


                return new BoolResult { Result = CommonHelper.IsMemberActivated(tokenId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> IsMemberActivated Failed - [tokenId: " + tokenId + "]. Exception -> " + ex);
                return new BoolResult();
            }

        }


        [HttpGet]
        [ActionName("IsNonNoochMemberActivated")]
        public BoolResult IsNonNoochMemberActivated(string emailId)
        {
            try
            {
                Logger.Info("Service Layer - IsNonNoochMemberActivated - Email ID: [" + emailId + "]");
               
                return new BoolResult { Result = CommonHelper.IsNonNoochMemberActivated(emailId) };
            }
            catch (Exception ex)
            {
                Logger.Error("Service Layer -> IsNonNoochMemberActivated Failed - [tokenId: " + emailId + "]. Exception -> " + ex);
                return new BoolResult();    
            }
            
        }


    }
}
