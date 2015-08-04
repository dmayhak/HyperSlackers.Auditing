using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary>
    /// Audit property references an entity and property that is tracked by the auditing system.
    /// </summary>
    /// <seealso cref="T:ClassroomForOne.Entities.IEntity" />
    public class AuditProperty
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the entity.
        /// </summary>
        /// <value>
        /// The name of the entity.
        /// </value>
        [Required]
        [MaxLength(250)]
        [Index("IX_Entity_Property", 0)]
        public string EntityName { get; set; }
        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>
        /// The name of the property.
        /// </value>
        [MaxLength(250)]
        [Index("IX_Entity_Property", 1)]
        public string PropertyName { get; set; }
        /// <summary>
        /// Gets or sets the type of the property.
        /// </summary>
        /// <value>
        /// The type of the property.
        /// </value>
        public string PropertyType { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is relation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is relation; otherwise, <c>false</c>.
        /// </value>
        public bool IsRelation { get; set; }
    }
}
