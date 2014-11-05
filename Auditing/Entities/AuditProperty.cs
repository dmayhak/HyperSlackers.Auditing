using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary> Audit property references an entity and property that is tracked by the auditing system. </summary>
    ///
    /// <seealso cref="T:ClassroomForOne.Entities.IEntity"/>
    public class AuditProperty
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(250)]
        [Index("IX_Entity_Property", 0)]
        public string EntityName { get; set; }
        [MaxLength(250)]
        [Index("IX_Entity_Property", 1)]
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public bool IsRelation { get; set; }
    }
}
