using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary>
    /// An entity and its property values at a point in time.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class EntityVersion<TKey, TEntity>
        where TKey : IEquatable<TKey>
        where TEntity : IAuditable<TKey>
    {
        /// <summary>
        /// Gets the edit date.
        /// </summary>
        /// <value>
        /// The edit date.
        /// </value>
        public DateTime EditDate { get; internal set; }
        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName { get; internal set; }
        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        public TKey UserId { get; internal set; }
        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <value>
        /// The entity.
        /// </value>
        public TEntity Entity { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityVersion{TKey, TEntity}"/> class.
        /// </summary>
        internal EntityVersion()
        {
            // prevent public instantiation
        }
    }
}
