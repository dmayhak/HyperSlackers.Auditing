using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    public class EntityVersion<TKey, TEntity>
        where TKey : IEquatable<TKey>
        where TEntity : IAuditable<TKey>
    {
        public DateTime EditDate { get; internal set; }
        public string UserName { get; internal set; }
        public TKey UserId { get; internal set; }
        public TEntity Entity { get; internal set; }

        internal EntityVersion()
        {
            // prevent public instantiation
        }
    }
}
