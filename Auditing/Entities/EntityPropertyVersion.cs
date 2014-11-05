using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    public class EntityPropertyVersion<TKey>
        where TKey : IEquatable<TKey>
    {
        public DateTime EditDate { get; internal set; }
        public string UserName { get; internal set; }
        public TKey UserId { get; internal set; }
        public string PropertyName { get; internal set; }
        public string PropertyValue { get; internal set; }

        internal EntityPropertyVersion()
        {
            // prevent public instantiation
        }
    }
}
