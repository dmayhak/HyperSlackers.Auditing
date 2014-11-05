using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework
{
    public interface IAuditable<TKey>
    {
        TKey Id { get; set; }
    }

    //public interface IAuditableGuid : IAuditable<Guid>
    //{
    //}

    //public interface IAuditableInt : IAuditable<int>
    //{
    //}

    //public interface IAuditableLong : IAuditable<long>
    //{
    //}
}
