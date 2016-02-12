using System;
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
                    string res = mda.UpdateMemberIPAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId);
                    return  new StringResult() {Result = mda.UpdateMemberIPAddressAndDeviceId(member.MemberId, member.IpAddress, member.DeviceId)};
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

    }
}
