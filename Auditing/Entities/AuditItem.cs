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
    /// An Audit item is a single change (property modification, object insert, or object delete) that occurred in the database.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <seealso cref="T:ClassroomForOne.Entities.IEntity" />
    public class AuditItem<TKey>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index("IX_Audit_Id", 1)]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the audit identifier.
        /// </summary>
        /// <value>
        /// The audit identifier.
        /// </value>
        [Required]
        [Index("IX_Audit_Id", 0)]
        public long AuditId { get; set; }
        /// <summary>
        /// Gets or sets the entity1 identifier.
        /// </summary>
        /// <value>
        /// The entity1 identifier.
        /// </value>
        [Required]
        [Index("IX_Entity1")]
        public TKey Entity1Id { get; set; }
        /// <summary>
        /// Gets or sets the entity2 identifier.
        /// </summary>
        /// <value>
        /// The entity2 identifier.
        /// </value>
        [Required]
        [Index("IX_Entity2")]
        public TKey Entity2Id { get; set; }
        [Required]
        public long AuditPropertyId { get; set; }
        /// <summary>
        /// Gets or sets the type of operation.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        /// <remarks>
        /// <list type="table">
        ///   <listheader>Operation Types</listheader>
        ///   <item>C - Create</item>
        ///   <item>U - Update</item>
        ///   <item>D - Delete</item>
        ///   <item>+ - Add Relation</item>
        ///   <item>- - Remove Relation</item>
        /// </list>
        /// </remarks>
        [Required]
        [MaxLength(1)]
        public string OperationType { get; set; }
        /// <summary>
        /// Gets or sets the old value.
        /// </summary>
        /// <value>
        /// The old value.
        /// </value>
        public string OldValue { get; set; }
        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        /// <value>
        /// The new value.
        /// </value>
        public string NewValue { get; set; }

        [NotMapped]
        internal Audit<TKey> Audit { get; set; }
        [NotMapped]
        internal AuditProperty AuditProperty { get; set; }
        [NotMapped]
        internal IAuditable<TKey> Entity1 { get; set; }
        [NotMapped]
        internal IAuditable<TKey> Entity2 { get; set; }
    }
}
