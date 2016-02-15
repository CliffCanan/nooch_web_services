using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nooch.Common.Rules
{
    public interface IRuleSpecification<T>
    {
        bool IsSatisfiedBy(T entity);
    }

    public interface IRuleSpecification<T, K>
    {
        bool IsSatisfiedBy(T entity, K entity1);
    }

    public interface IRuleSpecification<T, K, W>
    {
        bool IsSatisfiedBy(T entity, K entity1, W entity2);
    }
}
