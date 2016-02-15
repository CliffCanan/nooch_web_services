using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Rules
{
    public class InvalidAttemptDurationSpecification : IRuleSpecification<DateTime?, DateTime>
    {
        //To find whether the lastAttempt time is lesser than currentTime - 24 hrs.


        public bool IsSatisfiedBy(DateTime? lastAttemptTime, DateTime dateTime)
        {

            return (lastAttemptTime.HasValue && lastAttemptTime.Value < dateTime);

        }
    }
}
