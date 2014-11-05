using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework.Entities
{
    /// <summary> An audit entry tracks a group of changes that were committed to the database in one
    ///           transaction. Each change has an <see cref="AuditItem"/> in the <c>AuditItems</c>
    ///           List. </summary>
    ///
    /// <seealso cref="T:ClassroomForOne.Entities.IEntity"/>
    public class Audit<TKey>
    {
        [Key]
        [Index("IX_Id_Date", 0)]
        [Index("IX_Date_Id", 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public TKey HostId { get; set; } // only used for multi-host systems
        public TKey UserId { get; set; }
        [MaxLength(256)]
        public string HostName { get; set; } // computer name for a windows app, host name for web app
        [MaxLength(256)]
        public string UserName { get; set; } // from windows identity or web identity
        [Required]
        [Index("IX_Id_Date", 1)]
        [Index("IX_Date_Id", 0)]
        public DateTime AuditDate { get; set; }
    }

    public class AuditGuid : Audit<Guid>
    {
    }

    public class AuditInt : Audit<int>
    {
    }

    public class AuditLong : Audit<long>
    {
    }
}
