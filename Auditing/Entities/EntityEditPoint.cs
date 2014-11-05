using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    public class EntityEditPoint<TKey>
        where TKey : IEquatable<TKey>
    {
        public TKey EntityId { get; internal set; }
        public DateTime EditDate { get; internal set; }
        public string UserName { get; internal set; }
        public TKey UserId { get; internal set; }

        internal EntityEditPoint()
        {
            // prevent public instantiation
        }
    }
}
