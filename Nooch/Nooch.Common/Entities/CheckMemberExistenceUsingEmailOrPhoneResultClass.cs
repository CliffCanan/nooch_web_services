using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Entities
{
    public class CheckMemberExistenceUsingEmailOrPhoneResultClass
    {
        public string MemberId { get; set; }
        public string Name { get; set; }

        public string UserImage { get; set; }

        public bool IsMemberFound { get; set; }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CheckMemberExistenceUsingEmailOrPhoneInputClass
    {
        public string CheckType { get; set; }   // this would be P for Phone or E for Email type
        public string StringToCheck { get; set; }

        public string AccessToken { get; set; }
        public string MemberId { get; set; }
    }
}
