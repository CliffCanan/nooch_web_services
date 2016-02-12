using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nooch.Data;

namespace Nooch.Repository
{
    public interface IMembersRepository:IRepository<Member>
    {
        IEnumerable<Member> GetActiveMembers(int pageIndex, int pageSize);
    }
}
