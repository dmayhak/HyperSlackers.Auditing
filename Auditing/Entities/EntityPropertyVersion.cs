using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary>
    /// Represents a particular property vale at a point in time.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class EntityPropertyVersion<TKey>
        where TKey : IEquatable<TKey>
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
        /// Gets the name of the property.
        /// </summary>
        /// <value>
        /// The name of the property.
        /// </value>
        public string PropertyName { get; internal set; }
        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <value>
        /// The property value.
        /// </value>
        public string PropertyValue { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityPropertyVersion{TKey}"/> class.
        /// </summary>
        internal EntityPropertyVersion()
        {
            // prevent public instantiation
        }
    }
}
