using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary> An Audit item is a single change (property modification, object insert, or object delete) that occurred in the database. </summary>
    ///
    /// <seealso cref="T:ClassroomForOne.Entities.IEntity"/>
    public class AuditItem<TKey>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index("IX_Audit_Id", 1)]
        public long Id { get; set; }

        [Required]
        [Index("IX_Audit_Id", 0)]
        public long AuditId { get; set; }
        [Required]
        [Index("IX_Entity1")]
        public TKey Entity1Id { get; set; }
        [Required]
        [Index("IX_Entity2")]
        public TKey Entity2Id { get; set; }
        [Required]
        public long AuditPropertyId { get; set; }
        /// <summary> Gets or sets the type of operation. </summary>
        ///
        /// <remarks> <list type="table">
        ///               <listheader>Operation Types</listheader>
        ///               <item>C - Create</item>
        ///               <item>U - Update</item>
        ///               <item>D - Delete</item>
        ///               <item>+ - Add Relation</item>
        ///               <item>- - Remove Relation</item>
        ///           </list> </remarks>
        ///
        /// <value> The type of the operation. </value>
        [Required]
        [MaxLength(1)]
        public string OperationType { get; set; }
        public string OldValue { get; set; }
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

    public class AuditItemGuid : AuditItem<Guid>
    {
    }

    public class AuditItemInt : AuditItem<int>
    {
    }

    public class AuditItemLong : AuditItem<long>
    {
    }
}
