using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Nooch.Data;

namespace Nooch.Repository
{
    public class MembersRepository : Repository<Member>, IMembersRepository
    {
        public MembersRepository(NOOCHEntities context)
            : base(context)
        {
        }

        public IEnumerable<Member> GetActiveMembers(int pageIndex, int pageSize)
        {
            return Context.Members.Where(m => m.IsDeleted == false)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
    }
}
